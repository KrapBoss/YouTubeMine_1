using System.Collections.Generic;
using UnityEngine;
using MemeLab.Data;

namespace MemeLab.Core
{
    /// <summary>
    /// 24시간 무인 스트리밍 환경을 위한 중앙 집중식 사운드 관리자
    /// - 효과음: 사용 후 10초 동안 캐싱 후 미사용 시 자동 해제
    /// - 배경음: 현재 재생 중인 배경음 1개만 캐싱, 사용하지 않을 시 즉시 리소스 해제
    /// - AudioSource 풀링: 종류별(BGM/SFX) 기본 10개 보유, 모두 재생 중일 시 동적 확장
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const int DEFAULT_POOL_SIZE = 10;
        private const float SFX_CACHE_DURATION = 10.0f; // 효과음 10초 캐싱 주기

        private class CachedSFX
        {
            public AudioClip clip;
            public float lastUsedTime;

            public CachedSFX(AudioClip clip, float time)
            {
                this.clip = clip;
                this.lastUsedTime = time;
            }
        }

        [Header("볼륨 설정")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;

        // AudioSource 풀 (기본 각 10개씩 보유)
        private readonly List<AudioSource> sfxPool = new List<AudioSource>(DEFAULT_POOL_SIZE);
        private readonly List<AudioSource> bgmPool = new List<AudioSource>(DEFAULT_POOL_SIZE);

        // 효과음 캐시 (사용 후 10초간 보관)
        private readonly Dictionary<string, CachedSFX> sfxCache = new Dictionary<string, CachedSFX>();
        private readonly List<string> expiredKeys = new List<string>();

        // 배경음 캐시 (현재 사용 중인 1개만 보관)
        private AudioClip currentBGMClip = null;
        private string currentBGMName = string.Empty;
        private AudioSource currentBGMSource = null;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // 최상위 루트로 설정 후 DontDestroyOnLoad
                DontDestroyOnLoad(gameObject);
                InitializeAudioPools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // 효과음 10초 캐시 유효기간 검사 및 해제
            UpdateSFXCacheLifetime();
        }

        private void InitializeAudioPools()
        {
            // 1. 효과음(SFX) AudioSource 기본 10개 생성
            for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
            {
                sfxPool.Add(CreateNewAudioSource("SFX_Source_" + i, false));
            }

            // 2. 배경음(BGM) AudioSource 기본 10개 생성
            for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
            {
                bgmPool.Add(CreateNewAudioSource("BGM_Source_" + i, true));
            }
        }

        private AudioSource CreateNewAudioSource(string sourceName, bool isLoop)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = isLoop;
            source.volume = masterVolume * (isLoop ? bgmVolume : sfxVolume);
            return source;
        }

        // ==================== [효과음 (SFX) 재생] ====================

        /// <summary>
        /// 1회성 효과음 사운드를 재생합니다.
        /// 사용된 효과음은 10초 동안 캐싱되며, 풀의 모든 소스가 재생 중일 경우 새로운 AudioSource를 동적으로 생성합니다.
        /// </summary>
        /// <param name="soundName">Resources 내부의 사운드 클립 이름 또는 ResourceName 상수</param>
        /// <param name="force">true일 경우 가용 소스가 없어도 즉시 재생 우선순위를 부여</param>
        public void Play(string soundName, bool force = false)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            // 1. 캐시 확인 또는 Resources 로드
            AudioClip clip = LoadSFXClip(soundName);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] 사운드 리소스를 찾을 수 없습니다: {soundName}");
                return;
            }

            // 2. 재생 가능한 SFX AudioSource 획득 (모두 사용 중이면 새 AudioSource 생성)
            AudioSource source = GetAvailableSFXSource(force);
            if (source == null)
            {
                Debug.LogWarning("[SoundManager] 사용 가능한 SFX AudioSource를 확보하지 못했습니다.");
                return;
            }

            // 3. 효과음 재생
            source.clip = clip;
            source.volume = masterVolume * sfxVolume;
            source.loop = false;
            source.Play();
        }

        private AudioClip LoadSFXClip(string soundName)
        {
            // 캐시에 존재하면 마지막 사용 시점 갱신 후 반환
            if (sfxCache.TryGetValue(soundName, out CachedSFX cached))
            {
                cached.lastUsedTime = Time.time;
                return cached.clip;
            }

            // Resources에서 로드 시도
            AudioClip clip = Resources.Load<AudioClip>(soundName);
            if (clip == null && !soundName.StartsWith("Sounds/"))
            {
                clip = Resources.Load<AudioClip>("Sounds/" + soundName);
            }

            if (clip != null)
            {
                sfxCache[soundName] = new CachedSFX(clip, Time.time);
            }

            return clip;
        }

        private AudioSource GetAvailableSFXSource(bool force)
        {
            // 1. 현재 재생 중이지 않은 AudioSource 탐색
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (sfxPool[i] != null && !sfxPool[i].isPlaying)
                {
                    return sfxPool[i];
                }
            }

            // 2. 모두 재생 중인 경우:
            // "audioSource 컴포넌트는 기본적으로 각 사운드 종류별 10개씩 가지고 있되, 모두 사운드 클립을 플레이 중인 경우 새롭게 생성할 것."
            AudioSource newSource = CreateNewAudioSource($"SFX_Source_Dynamic_{sfxPool.Count}", false);
            sfxPool.Add(newSource);
            return newSource;
        }

        private void UpdateSFXCacheLifetime()
        {
            if (sfxCache.Count == 0) return;

            float currentTime = Time.time;
            expiredKeys.Clear();

            foreach (var kvp in sfxCache)
            {
                // 마지막 사용 후 10초 초과 여부 검사
                if (currentTime - kvp.Value.lastUsedTime >= SFX_CACHE_DURATION)
                {
                    // 현재 재생 중인 AudioSource에서 사용 중인지 확인
                    bool isStillPlaying = false;
                    for (int i = 0; i < sfxPool.Count; i++)
                    {
                        if (sfxPool[i] != null && sfxPool[i].isPlaying && sfxPool[i].clip == kvp.Value.clip)
                        {
                            isStillPlaying = true;
                            break;
                        }
                    }

                    // 재생 중이지 않다면 만료 처리
                    if (!isStillPlaying)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
            }

            // 만료된 효과음 캐시 제거 및 리소스 해제
            for (int i = 0; i < expiredKeys.Count; i++)
            {
                string key = expiredKeys[i];
                if (sfxCache.TryGetValue(key, out CachedSFX cached))
                {
                    sfxCache.Remove(key);
                    // Resources.UnloadAsset은 메모리 절약을 위해 수행 (인스턴스화되지 않은 에셋인 경우)
                    if (cached.clip != null)
                    {
                        Resources.UnloadAsset(cached.clip);
                    }
                }
            }
        }

        // ==================== [배경음 (BGM) 재생 및 해제] ====================

        /// <summary>
        /// 배경음(BGM)을 재생합니다.
        /// 현재 사용 중인 배경음 1개만 캐싱되며, 이전 배경음은 즉시 리소스에서 해제됩니다.
        /// </summary>
        /// <param name="soundName">Resources 내부의 사운드 클립 이름 또는 ResourceName 상수</param>
        /// <param name="loop">반복 재생 여부 (기본 true)</param>
        public void PlayBGM(string soundName, bool loop = true)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                StopBGM();
                return;
            }

            // 이미 동일한 배경음이 재생 중이면 건너뜀
            if (currentBGMName == soundName && currentBGMSource != null && currentBGMSource.isPlaying)
            {
                return;
            }

            // 1. 기존 사용 중이던 배경음 리소스 즉시 해제
            ReleaseCurrentBGM();

            // 2. 새 배경음 로드
            AudioClip clip = Resources.Load<AudioClip>(soundName);
            if (clip == null && !soundName.StartsWith("Sounds/"))
            {
                clip = Resources.Load<AudioClip>("Sounds/" + soundName);
            }

            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] 사운드 리소스를 찾을 수 없습니다: {soundName}");
                return;
            }

            // 3. 현재 사용 중인 배경음 1개만 캐싱
            currentBGMClip = clip;
            currentBGMName = soundName;

            // 4. BGM AudioSource 획득 (풀 미사용 소스 검색 또는 동적 확장)
            currentBGMSource = GetAvailableBGMSource();
            currentBGMSource.clip = currentBGMClip;
            currentBGMSource.loop = loop;
            currentBGMSource.volume = masterVolume * bgmVolume;
            currentBGMSource.Play();
        }

        /// <summary>
        /// Request.md 요구 규격: BGM 재생 상태를 제어합니다.
        /// true일 경우 현재 배경음 재생/재개, false일 경우 배경음 정지 및 리소스 즉시 해제
        /// </summary>
        /// <param name="soundName">재생(true) 또는 정지/해제(false) 플래그</param>
        public void PlayBGM(bool soundName)
        {
            if (soundName)
            {
                // true: 현재 배경음이 정지되어 있으면 재개하거나 기본 BGM 재생
                if (currentBGMSource != null && !currentBGMSource.isPlaying && currentBGMClip != null)
                {
                    currentBGMSource.Play();
                }
                else if (currentBGMClip == null)
                {
                    PlayBGM(ResourceName.SOUND_BGM_MAIN);
                }
            }
            else
            {
                // false: 배경음 정지 및 리소스 해제
                StopBGM();
            }
        }

        private AudioSource GetAvailableBGMSource()
        {
            // 미사용 BGM 소스 검색
            for (int i = 0; i < bgmPool.Count; i++)
            {
                if (bgmPool[i] != null && !bgmPool[i].isPlaying)
                {
                    return bgmPool[i];
                }
            }

            // 10개 모두 사용 중인 경우 새로 생성
            AudioSource newSource = CreateNewAudioSource($"BGM_Source_Dynamic_{bgmPool.Count}", true);
            bgmPool.Add(newSource);
            return newSource;
        }

        /// <summary>
        /// 현재 배경음을 정지하고 캐싱된 배경음 리소스를 즉시 메모리에서 해제합니다.
        /// </summary>
        public void StopBGM()
        {
            if (currentBGMSource != null)
            {
                currentBGMSource.Stop();
                currentBGMSource.clip = null;
                currentBGMSource = null;
            }

            ReleaseCurrentBGM();
        }

        private void ReleaseCurrentBGM()
        {
            if (currentBGMClip != null)
            {
                Resources.UnloadAsset(currentBGMClip);
                currentBGMClip = null;
                currentBGMName = string.Empty;
            }
        }

        /// <summary>
        /// 특정 사운드를 재생 중인 모든 효과음 AudioSource를 정지합니다.
        /// </summary>
        public void Stop(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (sfxPool[i] != null && sfxPool[i].isPlaying && sfxPool[i].clip != null)
                {
                    if (sfxPool[i].clip.name == soundName || soundName.EndsWith(sfxPool[i].clip.name))
                    {
                        sfxPool[i].Stop();
                        sfxPool[i].clip = null;
                    }
                }
            }
        }

        private void UpdateAllVolumes()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (sfxPool[i] != null)
                {
                    sfxPool[i].volume = masterVolume * sfxVolume;
                }
            }

            for (int i = 0; i < bgmPool.Count; i++)
            {
                if (bgmPool[i] != null)
                {
                    bgmPool[i].volume = masterVolume * bgmVolume;
                }
            }
        }
    }
}
