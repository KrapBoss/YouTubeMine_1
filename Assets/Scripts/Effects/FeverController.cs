using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MemeLab.Core;
using MemeLab.Objects;

namespace MemeLab.Effects
{
    /// <summary>
    /// 좋아요 바운스, $1 잔챙이 소탕, $5 10초 자석 폭풍(인력/척력) 물리 효과 컨트롤러
    /// </summary>
    public class FeverController : MonoBehaviour
    {
        public static FeverController Instance { get; private set; }

        [Header("물리 효과 설정")]
        [SerializeField] private float bounceForce = 6.0f;
        [SerializeField] private float magnetPullForce = 8.0f;
        [SerializeField] private float obstacleRepelForce = 12.0f;
        [SerializeField] private ParticleSystem popEffectPrefab;

        public bool IsFeverActive { get; private set; } = false;

        public event Action<bool> OnFeverStateChanged;
        public event Action<string> OnSmallAnimalsCleared;

        private Coroutine feverCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// [무료 협동 1 - 좋아요]: 상자 내 전체 객체에 순간 상향 물리력(Impulse) 부여
        /// </summary>
        public void TriggerBounce()
        {
            if (GameManager.Instance == null) return;

            var objects = GameManager.Instance.ActiveObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null && objects[i].Rb != null)
                {
                    // 모서리에 끼인 객체 탈출을 위해 약간의 무작위 X값 추가
                    Vector2 forceDir = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), 1f).normalized;
                    objects[i].Rb.AddForce(forceDir * bounceForce, ForceMode2D.Impulse);
                }
            }
        }

        /// <summary>
        /// [협동 후원 1 - $1]: Level <= 2 소형 일반 동물 일괄 폭파 삭제 (방해물은 절대 불가)
        /// </summary>
        public void ClearSmallAnimals(string user)
        {
            if (GameManager.Instance == null) return;

            var objects = new List<MergeObject>(GameManager.Instance.ActiveObjects);
            int clearedCount = 0;

            for (int i = 0; i < objects.Count; i++)
            {
                MergeObject obj = objects[i];
                if (obj != null && !obj.IsObstacle && obj.Level <= 2 && !obj.IsMerging)
                {
                    if (popEffectPrefab != null)
                    {
                        Instantiate(popEffectPrefab, obj.transform.position, Quaternion.identity);
                    }
                    obj.Despawn();
                    clearedCount++;
                }
            }

            Debug.Log($"[FeverController] 후원자 [{user}] 님의 $1 효과로 소형 동물 {clearedCount}마리 소탕 완료!");
            OnSmallAnimalsCleared?.Invoke(user);
        }

        /// <summary>
        /// [피버 후원 - $5]: 10초간 자석 폭풍 실행 (방해물 외곽 척력 + 동일 단계 동물 인력 연쇄 합체)
        /// </summary>
        public void StartMagnetFever(string user, float duration = 10f)
        {
            if (feverCoroutine != null)
            {
                StopCoroutine(feverCoroutine);
            }

            feverCoroutine = StartCoroutine(MagnetFeverRoutine(user, duration));
        }

        private IEnumerator MagnetFeverRoutine(string user, float duration)
        {
            IsFeverActive = true;
            OnFeverStateChanged?.Invoke(true);
            Debug.Log($"[FeverController] 후원자 [{user}] 님의 $5 자석 피버 폭풍 시작 ({duration}초)");

            float elapsed = 0f;
            WaitForFixedUpdate waitFixed = new WaitForFixedUpdate();

            while (elapsed < duration)
            {
                yield return waitFixed;
                elapsed += Time.fixedDeltaTime;

                if (GameManager.Instance == null) continue;
                var objects = GameManager.Instance.ActiveObjects;

                // 1. 트롤링 블록: 파괴하지 않고 중앙에서 바깥 외곽으로 밀어내는 물리 척력
                for (int i = 0; i < objects.Count; i++)
                {
                    MergeObject obj = objects[i];
                    if (obj == null || obj.Rb == null) continue;

                    if (obj.IsObstacle)
                    {
                        float dirX = obj.transform.position.x >= 0 ? 1f : -1f;
                        obj.Rb.AddForce(new Vector2(dirX * obstacleRepelForce, 0f), ForceMode2D.Force);
                    }
                }

                // 2. 일반 동물: 동일 레벨끼리 서로 끌어당기는 자석 인력 (연쇄 합체 유도)
                for (int level = 1; level <= 9; level++)
                {
                    List<MergeObject> sameLevelAnimals = new List<MergeObject>();
                    Vector2 centroid = Vector2.zero;

                    for (int i = 0; i < objects.Count; i++)
                    {
                        MergeObject candidate = objects[i];
                        if (candidate != null && !candidate.IsObstacle && !candidate.IsMerging && candidate.Level == level)
                        {
                            sameLevelAnimals.Add(candidate);
                            centroid += (Vector2)candidate.transform.position;
                        }
                    }

                    if (sameLevelAnimals.Count >= 2)
                    {
                        centroid /= sameLevelAnimals.Count;

                        for (int i = 0; i < sameLevelAnimals.Count; i++)
                        {
                            MergeObject animal = sameLevelAnimals[i];
                            if (animal != null && animal.Rb != null)
                            {
                                Vector2 pullDir = (centroid - (Vector2)animal.transform.position).normalized;
                                animal.Rb.AddForce(pullDir * magnetPullForce, ForceMode2D.Force);
                            }
                        }
                    }
                }
            }

            IsFeverActive = false;
            OnFeverStateChanged?.Invoke(false);
            feverCoroutine = null;
            Debug.Log("[FeverController] 자석 피버 폭풍 종료");
        }
    }
}
