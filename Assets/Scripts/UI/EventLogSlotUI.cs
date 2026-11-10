using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 좌상단 시청자 참여 이벤트 로그의 개별 슬롯 UI 컴포넌트 ("아이콘 : 텍스트")
    /// </summary>
    public class EventLogSlotUI : MonoBehaviour
    {
        [Header("슬롯 UI 요소")]
        [Tooltip("이벤트 아이콘 (좋아요, 구독, 후원, 채팅 등)")]
        [SerializeField] private Image iconImage;

        [Tooltip("이벤트 내용 텍스트")]
        [SerializeField] private TMP_Text logText;

        [Tooltip("슬롯 루트 오브젝트 (미지정 시 본 컴포넌트의 gameObject 사용)")]
        [SerializeField] private GameObject rootObject;

        public Image IconImage => iconImage;
        public TMP_Text LogText => logText;
        public GameObject RootObject => rootObject != null ? rootObject : gameObject;

        private void Awake()
        {
            if (rootObject == null)
            {
                rootObject = gameObject;
            }

            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }

            if (logText == null)
            {
                logText = GetComponentInChildren<TMP_Text>();
            }
        }

        /// <summary>
        /// 로그 메시지와 아이콘(선택)을 설정하고 슬롯을 활성화합니다.
        /// </summary>
        public void SetLog(string message, Sprite icon = null)
        {
            SetVisible(true);

            if (logText != null)
            {
                logText.text = message;
            }

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                }
                else
                {
                    // 인스펙터에 이미 대입된 스프라이트가 있으면 유지
                    iconImage.enabled = (iconImage.sprite != null);
                }
            }
        }

        /// <summary>
        /// 슬롯의 가시성(활성/비활성)을 설정합니다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            GameObject target = rootObject != null ? rootObject : gameObject;
            if (target != null && target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// 슬롯 내용을 비우고 비활성화합니다.
        /// </summary>
        public void Clear()
        {
            SetVisible(false);

            if (logText != null)
            {
                logText.text = string.Empty;
            }
        }
    }
}
