using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MemeLab.Data;
using MemeLab.Objects;
using MemeLab.Utility;
using UnityEditor;

namespace MemeLab.Core
{
    /// <summary>
    /// 상단 크레인 이동, m초 주기 v초 팅클 생성 및 자동 낙하, 시청자 채팅 명령어 제어 및 구독/후원 특수 오브젝트 스폰
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Header("크레인 이동 설정 (구역 a~b, 속도 n)")]
        [Tooltip("크레인 좌측 이동 한계 위치 a")]
        [SerializeField] private float minX = -3.0f;
        [Tooltip("크레인 우측 이동 한계 위치 b")]
        [SerializeField] private float maxX = 3.0f;
        [Tooltip("크레인 좌우 왕복 이동 속도 n")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private bool autoMove = true;

        [Header("타이머 및 팅클 생성 설정 (주기 m, 생성시간 v)")]
        [Tooltip("오브젝트 생성 주기 m (초)")]
        [SerializeField] private float autoDropInterval = 5.0f;
        [Tooltip("오브젝트 생성 연출 시간 v (초)")]
        [SerializeField] private float spawnDuration = 1.0f;
        [Tooltip("생성 시 스케일 팅클(Tinkle) 효과 커브 (최종 스케일 1 도달)")]
        [SerializeField] private AnimationCurve tinkleCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.00f, 0f, 2.5f),
            new Keyframe(0.60f, 1.18f, 0.5f, -0.5f),
            new Keyframe(0.85f, 0.94f, -0.3f, 0.3f),
            new Keyframe(1.00f, 1.00f, 0f, 0f)
        );
        [SerializeField] private float userDropCooldown = 20.0f;

        [Header("데이터 및 프리팹 참조")]
        [SerializeField] private AnimalStageDataSO stageData;
        [SerializeField] private GameObject defaultAnimalPrefab;
        [SerializeField] private GameObject rainbowStarPrefab;
        [SerializeField] private GameObject trollBlockPrefab;

        private float autoDropTimer = 0f;
        private int moveDirection = 1;
        private readonly Dictionary<string, float> userCooldowns = new Dictionary<string, float>();

        private Coroutine spawnCoroutine;
        private bool isSpawning = false;

        public bool IsActive { get; set; } = true;
        public bool IsSpawning => isSpawning;
        public float SpawnInterval { get => autoDropInterval; set => autoDropInterval = value; }
        public float SpawnDuration { get => spawnDuration; set => spawnDuration = value; }

        private void OnDisable()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            isSpawning = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var currentPostion = transform.position;
            currentPostion.z = 0;
            currentPostion.x = transform.position.x + minX;
            Gizmos.DrawSphere(currentPostion, 0.1f);

            currentPostion.x = transform.position.x + maxX;
            Gizmos.DrawSphere(currentPostion, 0.1f);
        }

        private void Update()
        {
            if (!IsActive) return;

            // 1. 크레인 좌우 왕복 이동 (구역 a~b 사이 속도 n)
            if (autoMove)
            {
                UpdateCraneMovement();
            }

            // 2. m초 주기 기본 동물 자동 생성 (생성 중이 아닐 때)
            autoDropTimer += Time.deltaTime;
            if (autoDropTimer >= autoDropInterval && !isSpawning)
            {
                autoDropTimer = 0f;
                AutoDrop();
            }
        }

        private void UpdateCraneMovement()
        {
            Vector3 pos = transform.position;
            pos.x += moveDirection * moveSpeed * Time.deltaTime;

            if (pos.x >= maxX)
            {
                pos.x = maxX;
                moveDirection = -1;
            }
            else if (pos.x <= minX)
            {
                pos.x = minX;
                moveDirection = 1;
            }

            transform.position = pos;
        }

        /// <summary>
        /// [자동 스폰]: m초 주기 크레인 위치에서 v초 동안 팅클 효과를 거치며 최종 스케일 1로 생성 후 낙하
        /// </summary>
        public void AutoDrop()
        {
            if (isSpawning) return;

            int level = UnityEngine.Random.Range(1, 3); // 1 or 2
            spawnCoroutine = StartCoroutine(SpawnTinkleRoutine(level, string.Empty, 0, isObstacle: false));
        }

        /// <summary>
        /// v초 동안 크레인 위치를 따라가며 스케일 팅클 효과를 연출하고 최종 스케일 1에 도달하면 물리 낙하를 시작하는 코루틴
        /// </summary>
        private IEnumerator SpawnTinkleRoutine(int level, string owner, int amount, bool isObstacle)
        {
            isSpawning = true;

            // 1. 크레인 위치에 인스턴스 생성
            MergeObject animal = SpawnAnimal(level, owner, amount, transform.position, isObstacle);
            if (animal == null)
            {
                isSpawning = false;
                yield break;
            }

            // 2. 생성 중 물리 및 충돌 일시 정지 (크레인을 따라가며 생성)
            Rigidbody2D rb = animal.Rb;
            Collider2D col = animal.GetComponent<Collider2D>();
            RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;

            if (rb != null)
            {
                originalBodyType = rb.bodyType;
                rb.bodyType = RigidbodyType2D.Kinematic;
                animal.Velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            // 스케일 0부터 시작
            animal.transform.localScale = Vector3.zero;

            // 3. v초 동안 크레인 위치를 따라가며 tinkle 스케일 애니메이션 실행
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, spawnDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 팅클 커브 기반 스케일 산출 (0 -> 오버슈트 팽창 -> 최종 1.0)
                float scaleFactor = tinkleCurve != null ? tinkleCurve.Evaluate(t) : EvaluateDefaultTinkle(t);
                animal.transform.localScale = Vector3.one * scaleFactor;

                // 크레인이 움직이는 동안 크레인 위치를 따라감
                animal.transform.position = transform.position;

                yield return null;
            }

            // 4. 최종 스케일 1 확정 (요구사항: 최종 스케일이 1이 되게 함)
            animal.transform.localScale = Vector3.one;

            // 5. 물리 및 충돌 복구 -> 아래로 자연 낙하 시작
            if (rb != null)
            {
                rb.bodyType = originalBodyType;
            }

            if (col != null)
            {
                col.enabled = true;
            }

            isSpawning = false;
            spawnCoroutine = null;
        }

        private float EvaluateDefaultTinkle(float t)
        {
            if (t < 0.6f)
            {
                return Mathf.Lerp(0f, 1.18f, t / 0.6f);
            }
            else if (t < 0.85f)
            {
                return Mathf.Lerp(1.18f, 0.94f, (t - 0.6f) / 0.25f);
            }
            else
            {
                return Mathf.Lerp(0.94f, 1.0f, (t - 0.85f) / 0.15f);
            }
        }

        /// <summary>
        /// [채팅 제어 - 이동]: !좌, !우 제어
        /// </summary>
        public void MoveSpawnerDirection(int direction)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + direction * 1.0f, minX, maxX);
            transform.position = pos;
        }

        /// <summary>
        /// [채팅 제어 - 열 지정]: !1 ~ !5 지정 위치로 스포너 이동
        /// </summary>
        public void MoveSpawnerColumn(int column)
        {
            column = Mathf.Clamp(column, 1, 5);
            float ratio = (column - 1) / 4.0f;
            float targetX = Mathf.Lerp(minX, maxX, ratio);
            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;
        }

        /// <summary>
        /// [채팅 제어 - !드롭]: 20초 개인 쿨타임 검증 후 1~3단계 기본 동물 즉시 투하
        /// </summary>
        public bool CommandDrop(string user)
        {
            if (!IsActive) return false;

            if (userCooldowns.TryGetValue(user, out float lastDropTime))
            {
                if (Time.time - lastDropTime < userDropCooldown)
                {
                    Debug.Log($"[Spawner] 유저 [{user}] 쿨타임 대기 중 (잔여: {userDropCooldown - (Time.time - lastDropTime):F1}초)");
                    return false;
                }
            }

            userCooldowns[user] = Time.time;
            int level = UnityEngine.Random.Range(1, 4); // 1 ~ 3단계
            SpawnAnimal(level, user, 0, transform.position);
            return true;
        }

        /// <summary>
        /// [무료 협동 2 - 구독]: [구독자 닉네임]이 각인된 4단계 동물 확정 스폰
        /// </summary>
        public void SpawnSubAnimal(string user)
        {
            SpawnAnimal(4, user, 0, transform.position);
        }

        /// <summary>
        /// [협동 후원 2 - $2]: [후원자 닉네임]이 각인된 만능 진화 별 투하
        /// </summary>
        public void SpawnRainbowStar(string user)
        {
            Vector3 spawnPos = transform.position;
            if (rainbowStarPrefab != null)
            {
                GameObject starObj = Instantiate(rainbowStarPrefab, spawnPos, Quaternion.identity);
                RainbowStarObject star = starObj.GetComponent<RainbowStarObject>();
                if (star != null)
                {
                    star.Initialize(user);
                }
            }
            else
            {
                // Fallback 동적 생성
                GameObject starObj = new GameObject($"RainbowStar_{user}");
                starObj.transform.position = spawnPos;
                var rb = starObj.AddComponent<Rigidbody2D>();
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.mass = 1.5f;
                var col = starObj.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                var sr = starObj.AddComponent<SpriteRenderer>();
                sr.color = Color.yellow;
                var star = starObj.AddComponent<RainbowStarObject>();
                star.Initialize(user);
            }
        }

        /// <summary>
        /// [트롤링 후원 - $3]: [후원자 닉네임]이 각인된 무적 고중량 방해 블록 투하
        /// </summary>
        public void SpawnTrollBlock(string user)
        {
            Vector3 spawnPos = transform.position;
            var obstacleInfo = stageData != null ? stageData.GetObstacleInfo() : null;

            if (trollBlockPrefab != null)
            {
                GameObject blockObj = Instantiate(trollBlockPrefab, spawnPos, Quaternion.identity);
                MergeObject mergeObj = blockObj.GetComponent<MergeObject>();
                if (mergeObj != null)
                {
                    mergeObj.Initialize(-1, user, 3, obstacleInfo, obstacle: true);
                }
            }
            else
            {
                SpawnAnimal(-1, user, 3, spawnPos, isObstacle: true);
            }
        }

        /// <summary>
        /// 단계별 동물 인스턴스 생성 헬퍼
        /// </summary>
        public MergeObject SpawnAnimal(int level, string owner, int amount, Vector3 position, bool isObstacle = false)
        {
            if (MergeObjectPool.Instance != null)
            {
                return MergeObjectPool.Instance.Get(level, owner, amount, position, isObstacle);
            }

            var info = stageData != null ? stageData.GetStageInfo(level) : null;
            GameObject prefabToUse = (info != null && info.customPrefab != null) ? info.customPrefab : defaultAnimalPrefab;

            GameObject obj;
            if (prefabToUse != null)
            {
                obj = Instantiate(prefabToUse, position, Quaternion.identity);
            }
            else
            {
                // Fallback 동적 런타임 생성 (프리팹 미설정 시 안전 보장)
                obj = new GameObject(isObstacle ? $"Obstacle_{owner}" : $"Animal_Lv{level}_{owner}");
                obj.transform.position = position;
                obj.AddComponent<SpriteRenderer>();
                obj.AddComponent<Rigidbody2D>();
                obj.AddComponent<PolygonCollider2D>();
            }

            MergeObject mergeObj = obj.GetComponent<MergeObject>();
            if (mergeObj == null)
            {
                mergeObj = obj.AddComponent<MergeObject>();
            }

            mergeObj.Initialize(level, owner, amount, info, isObstacle);
            return mergeObj;
        }

        public void ResetCooldowns()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            isSpawning = false;
            userCooldowns.Clear();
            autoDropTimer = 0f;
        }
    }
}
