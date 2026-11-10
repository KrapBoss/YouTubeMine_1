using TMPro;
using UnityEngine;

namespace MemeLab.Objects
{
    /// <summary>
    /// 부모 다각형 물리 객체가 회전하더라도 닉네임 텍스트가 항상 정방향(Quaternion.identity)을 유지하도록 제어하는 UI 컴포넌트
    /// </summary>
    public class NameTagUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.6f, 0f);

        private Transform parentTransform;

        private void Awake()
        {
            if (nameText == null)
            {
                nameText = GetComponentInChildren<TMP_Text>();
            }

            if (transform.parent != null)
            {
                parentTransform = transform.parent;
            }
        }

        private void LateUpdate()
        {
            // 부모 다각형 객체가 굴러가서 뒤집히더라도 항상 정방향으로 읽히도록 회전값 고정
            transform.rotation = Quaternion.identity;

            if (parentTransform != null)
            {
                transform.position = parentTransform.position + localOffset;
            }
        }

        public void SetOwner(string ownerName, int donatedAmount = 0)
        {
            if (string.IsNullOrEmpty(ownerName))
            {
                if (nameText != null)
                {
                    nameText.text = string.Empty;
                }
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (nameText != null)
            {
                if (donatedAmount > 0)
                {
                    nameText.text = $"[{ownerName}] (${donatedAmount})";
                }
                else
                {
                    nameText.text = $"[{ownerName}]";
                }
            }
        }

        public void SetOffset(Vector3 offset)
        {
            localOffset = offset;
        }
    }
}
