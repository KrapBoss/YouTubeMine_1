using UnityEngine;
using MemeLab.Data;
using MemeLab.Core;
using MemeLab.Utility;

namespace MemeLab.Objects
{
    /// <summary>
    /// 인게임 1~10단계 동물 및 트롤링 방해물을 담당하는 물리 머지 객체
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MergeObject : MonoBehaviour
    {
        [Header("오브젝트 상태 데이터")]
        [Tooltip("진화 단계: 1 ~ 10, 트롤링 방해물은 -1")]
        [SerializeField] private int level = 1;
        [SerializeField] private bool isObstacle = false;
        [SerializeField] private string ownerName = string.Empty;
        [SerializeField] private int donatedAmount = 0;

        [Header("컴포넌트 참조")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private NameTagUI nameTag;

        public int Level => level;
        public bool IsObstacle => isObstacle;
        public string OwnerName => ownerName;
        public int DonatedAmount => donatedAmount;
        public bool IsMerging { get; set; } = false;
        public float CreationTime { get; private set; }

        public Rigidbody2D Rb => rb;
        public Vector2 Velocity
        {
            get
            {
                if (rb == null) return Vector2.zero;
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity;
#else
                return rb.velocity;
#endif
            }
            set
            {
                if (rb == null) return;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = value;
#else
                rb.velocity = value;
#endif
            }
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (nameTag == null) nameTag = GetComponentInChildren<NameTagUI>();

            CreationTime = Time.time;
            ConfigurePhysics();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterObject(this);
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterObject(this);
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// 오브젝트 풀로 안전하게 반환 (풀이 없으면 Destroy)
        /// </summary>
        public void Despawn()
        {
            if (MergeObjectPool.Instance != null)
            {
                MergeObjectPool.Instance.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 물리 연산 및 Rigidbody / Collider 세팅 (Continuous, Mass, Material 등)
        /// </summary>
        public void ConfigurePhysics()
        {
            if (rb == null) return;

            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (isObstacle)
            {
                rb.mass = 5.0f;
            }
            else
            {
                // 일반 동물: 1.0 ~ 3.0 (단계 비례)
                rb.mass = Mathf.Lerp(1.0f, 3.0f, Mathf.Clamp01((level - 1) / 9f));
            }

            // PhysicsMaterial2D: Friction = 0.4, Bounciness = 0.15
            PhysicsMaterial2D mat = new PhysicsMaterial2D("MergePhysicsMaterial")
            {
                friction = 0.4f,
                bounciness = 0.15f
            };

            rb.sharedMaterial = mat;
            if (col != null)
            {
                col.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// 데이터 기반 초기화
        /// </summary>
        public void Initialize(int newLevel, string newOwner, int newDonatedAmount, AnimalStageDataSO.AnimalStageInfo stageInfo, bool obstacle = false)
        {
            level = newLevel;
            ownerName = newOwner ?? string.Empty;
            donatedAmount = newDonatedAmount;
            isObstacle = obstacle || (newLevel == -1);
            IsMerging = false;
            CreationTime = Time.time;

            if (stageInfo != null)
            {
                transform.localScale = stageInfo.scale;

                if (spriteRenderer != null)
                {
                    if (stageInfo.sprite != null)
                    {
                        spriteRenderer.sprite = stageInfo.sprite;
                    }
                    spriteRenderer.color = stageInfo.debugColor;
                }

                if (rb != null)
                {
                    rb.mass = stageInfo.mass;
                }
            }

            ConfigurePhysics();
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (nameTag != null)
            {
                nameTag.SetOwner(ownerName, donatedAmount);
            }
        }

        public void SetOwnerInfo(string newOwner, int amount)
        {
            ownerName = newOwner ?? string.Empty;
            donatedAmount = amount;
            UpdateVisuals();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 방해물이거나 이미 합체 진행 중이면 무시
            if (isObstacle || IsMerging) return;

            MergeObject other = collision.gameObject.GetComponent<MergeObject>();
            if (other == null || other.IsObstacle || other.IsMerging) return;

            // 동일 레벨 합체 (1 ~ 9단계 충돌 시 상위 단계로)
            if (this.level == other.Level && this.level < 10)
            {
                // 양방향 충돌 이벤트 중복 방지 (InstanceID가 작은 쪽만 단일 실행)
                if (this.GetInstanceID() > other.GetInstanceID()) return;

                this.IsMerging = true;
                other.IsMerging = true;

                Vector2 spawnPos = (transform.position + other.transform.position) * 0.5f;

                // 닉네임 계승(Inheritance) 알고리즘
                string nextOwner = string.Empty;
                int nextAmount = 0;

                bool hasThisOwner = !string.IsNullOrEmpty(this.ownerName);
                bool hasOtherOwner = !string.IsNullOrEmpty(other.OwnerName);

                if (hasThisOwner && !hasOtherOwner)
                {
                    nextOwner = this.ownerName;
                    nextAmount = this.donatedAmount;
                }
                else if (!hasThisOwner && hasOtherOwner)
                {
                    nextOwner = other.OwnerName;
                    nextAmount = other.DonatedAmount;
                }
                else if (hasThisOwner && hasOtherOwner)
                {
                    if (this.donatedAmount > other.DonatedAmount)
                    {
                        nextOwner = this.ownerName;
                        nextAmount = this.donatedAmount;
                    }
                    else if (other.DonatedAmount > this.donatedAmount)
                    {
                        nextOwner = other.OwnerName;
                        nextAmount = other.DonatedAmount;
                    }
                    else
                    {
                        // 금액까지 동일할 경우 먼저 생성된 객체 우선
                        if (this.CreationTime <= other.CreationTime)
                        {
                            nextOwner = this.ownerName;
                            nextAmount = this.donatedAmount;
                        }
                        else
                        {
                            nextOwner = other.OwnerName;
                            nextAmount = other.DonatedAmount;
                        }
                    }
                }

                int nextLevel = this.level + 1;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnMergeSuccess(nextLevel, spawnPos, nextOwner, nextAmount);
                }

                this.Despawn();
                other.Despawn();
            }
        }
    }
}
