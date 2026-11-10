using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MemeLab.Objects;
using MemeLab.Data;

namespace MemeLab.Core
{
    /// <summary>
    /// 게임 상태(Playing, Clear, GameOver), 클리어 빵빠레 및 24시간 무인 자동 순환 전체 리셋 제어기
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Playing,
            GameOverReset,
            ClearCeremony
        }

        [Header("게임 상태")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("핵심 컴포넌트 참조")]
        [SerializeField] private Spawner spawner;
        [SerializeField] private DeadlineSensor deadlineSensor;

        [Header("클리어 연출 세팅")]
        [SerializeField] private ParticleSystem fanfareParticle;
        [SerializeField] private AudioSource fanfareAudioSource;
        [SerializeField] private ParticleSystem popEffectPrefab;

        private readonly List<MergeObject> activeObjects = new List<MergeObject>();
        private Coroutine activeSequenceCoroutine;

        public GameState CurrentState => currentState;
        public IReadOnlyList<MergeObject> ActiveObjects => activeObjects;

        public event Action<GameState> OnGameStateChanged;
        public event Action<string, MergeObject> OnGameClear; // 배너 텍스트, 10단계 동물
        public event Action OnGameOver;
        public event Action OnGameRestart;
        public event Action<int, Vector2, string> OnMergeEvent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (spawner == null) spawner = FindFirstObjectByType<Spawner>();
            if (deadlineSensor == null) deadlineSensor = FindFirstObjectByType<DeadlineSensor>();
        }

        public void RegisterObject(MergeObject obj)
        {
            if (!activeObjects.Contains(obj))
            {
                activeObjects.Add(obj);
            }
        }

        public void UnregisterObject(MergeObject obj)
        {
            activeObjects.Remove(obj);
        }

        /// <summary>
        /// 동일 단계 동물 충돌 시 또는 무지개 별 효과로 상위 동물 진화 시 호출
        /// </summary>
        public void OnMergeSuccess(int newLevel, Vector2 position, string ownerName, int donatedAmount)
        {
            if (currentState != GameState.Playing) return;

            OnMergeEvent?.Invoke(newLevel, position, ownerName);

            // 사운드 매니저를 통한 머지 효과음 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play(ResourceName.SOUND_MERGE, false);
            }

            if (spawner != null)
            {
                MergeObject evolvedAnimal = spawner.SpawnAnimal(newLevel, ownerName, donatedAmount, position);

                // 최종 10단계 완성 시 즉시 클리어 빵빠레 시퀀스 실행
                if (newLevel >= 10)
                {
                    StartClearSequence(evolvedAnimal, ownerName);
                }
            }
        }

        /// <summary>
        /// 한계선 10초 초과 실패 리셋 시퀀스
        /// </summary>
        public void OnDeadlineExceeded()
        {
            if (currentState != GameState.Playing) return;

            if (activeSequenceCoroutine != null)
            {
                StopCoroutine(activeSequenceCoroutine);
            }
            activeSequenceCoroutine = StartCoroutine(FailResetSequenceRoutine());
        }

        private IEnumerator FailResetSequenceRoutine()
        {
            SetGameState(GameState.GameOverReset);
            Debug.Log("[GameManager] 게임 오버: 3초 후 상자 전체 리셋 및 새 게임 시작");

            if (spawner != null) spawner.IsActive = false;
            OnGameOver?.Invoke();

            // 상자 내부의 모든 일반 동물과 무적 트롤링 방해물을 팝(Pop) 이펙트와 함께 전부 소멸
            ClearAllObjectsWithPop();

            // 3초 대기
            yield return new WaitForSeconds(3.0f);

            ResetGame();
        }

        private void StartClearSequence(MergeObject finalAnimal, string ownerName)
        {
            if (activeSequenceCoroutine != null)
            {
                StopCoroutine(activeSequenceCoroutine);
            }
            activeSequenceCoroutine = StartCoroutine(ClearSequenceRoutine(finalAnimal, ownerName));
        }

        private IEnumerator ClearSequenceRoutine(MergeObject finalAnimal, string ownerName)
        {
            SetGameState(GameState.ClearCeremony);

            // 즉시 스폰 및 데드라인 판정 중지
            if (spawner != null) spawner.IsActive = false;
            if (deadlineSensor != null) deadlineSensor.ResetSensor();

            // 빵빠레 사운드 및 폭죽 파티클 재생
            if (fanfareParticle != null) fanfareParticle.Play();
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play(ResourceName.SOUND_FANFARE, true);
            }
            else if (fanfareAudioSource != null)
            {
                fanfareAudioSource.Play();
            }

            // 10단계 동물 화면 중앙으로 연출 강조
            if (finalAnimal != null)
            {
                if (finalAnimal.Rb != null)
                {
                    finalAnimal.Rb.bodyType = RigidbodyType2D.Kinematic;
                    finalAnimal.Velocity = Vector2.zero;
                    finalAnimal.Rb.angularVelocity = 0f;
                }
            }

            // 기여자 닉네임 배너 문구 설정
            string bannerText = string.IsNullOrEmpty(ownerName)
                ? "🎉 시청자 협동 클리어! 🎉"
                : $"🎉 최종 완성! 기여자: [{ownerName}] 님! 🎉";

            Debug.Log($"[GameManager] 클리어! {bannerText}");
            OnGameClear?.Invoke(bannerText, finalAnimal);

            // 약 8초간 세리머니 진행 (중앙 이동 보간)
            float elapsed = 0f;
            Vector3 startPos = finalAnimal != null ? finalAnimal.transform.position : Vector3.zero;
            Vector3 centerPos = Vector3.zero;

            while (elapsed < 8.0f)
            {
                elapsed += Time.deltaTime;
                if (finalAnimal != null && elapsed < 2.0f)
                {
                    finalAnimal.transform.position = Vector3.Lerp(startPos, centerPos, elapsed / 2.0f);
                }
                yield return null;
            }

            // 8초 후 모든 동물과 트롤링 방해물 일괄 소멸 및 자동 재시작
            ClearAllObjectsWithPop();
            ResetGame();
        }

        private void ClearAllObjectsWithPop()
        {
            var objectsToDestroy = new List<MergeObject>(activeObjects);
            for (int i = 0; i < objectsToDestroy.Count; i++)
            {
                MergeObject obj = objectsToDestroy[i];
                if (obj != null)
                {
                    if (popEffectPrefab != null)
                    {
                        Instantiate(popEffectPrefab, obj.transform.position, Quaternion.identity);
                    }
                    obj.Despawn();
                }
            }
            activeObjects.Clear();
        }

        private void ResetGame()
        {
            if (deadlineSensor != null) deadlineSensor.ResetSensor();
            if (spawner != null)
            {
                spawner.ResetCooldowns();
                spawner.IsActive = true;
            }

            SetGameState(GameState.Playing);
            OnGameRestart?.Invoke();
            activeSequenceCoroutine = null;
            Debug.Log("[GameManager] 새 판이 자동으로 시작되었습니다.");
        }

        private void SetGameState(GameState state)
        {
            currentState = state;
            OnGameStateChanged?.Invoke(currentState);
        }
    }
}
