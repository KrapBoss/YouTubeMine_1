using UnityEngine;
using MemeLab.Core;

namespace MemeLab.Objects
{
    /// <summary>
    /// $2 협동 후원 만능 별 아이템: 처음 충돌한 일반 동물을 즉시 Level + 1로 강제 진화시키고 후원자 이름을 각인
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RainbowStarObject : MonoBehaviour
    {
        [SerializeField] private string ownerName = string.Empty;
        [SerializeField] private int donatedAmount = 2;
        [SerializeField] private NameTagUI nameTag;

        private bool isTriggered = false;

        public void Initialize(string sponsorName)
        {
            ownerName = sponsorName ?? string.Empty;
            donatedAmount = 2;

            if (nameTag == null)
            {
                nameTag = GetComponentInChildren<NameTagUI>();
            }

            if (nameTag != null)
            {
                nameTag.SetOwner(ownerName, donatedAmount);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isTriggered) return;

            MergeObject target = collision.gameObject.GetComponent<MergeObject>();
            if (target != null && !target.IsObstacle && target.Level < 10 && !target.IsMerging)
            {
                isTriggered = true;
                target.IsMerging = true;

                Vector2 spawnPos = target.transform.position;
                int nextLevel = target.Level + 1;

                if (GameManager.Instance != null)
                {
                    // 즉시 Level + 1로 진화시키고 후원자 이름 각인 (DonatedAmount = 2)
                    GameManager.Instance.OnMergeSuccess(nextLevel, spawnPos, ownerName, donatedAmount);
                }

                target.Despawn();
                Destroy(this.gameObject);
            }
        }
    }
}
