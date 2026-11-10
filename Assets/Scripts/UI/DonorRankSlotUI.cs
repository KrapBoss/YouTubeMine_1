using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 우상단 후원자 랭킹 보드의 개별 순위 슬롯 UI 컴포넌트 ("아이콘 : 텍스트")
    /// </summary>
    public class DonorRankSlotUI : MonoBehaviour
    {
        [Header("슬롯 UI 요소")]
        [Tooltip("순위별 아이콘 (1~3위 트로피/메달 등)")]
        [SerializeField] private Image rankIcon;

        [Tooltip("순위 및 후원자 정보 텍스트 (예: '1위: [닉네임] ($금액)')")]
        [SerializeField] private TMP_Text rankText;

        [Tooltip("슬롯 루트 오브젝트 (미지정 시 본 컴포넌트의 gameObject 사용)")]
        [SerializeField] private GameObject rootObject;

        public Image RankIcon => rankIcon;
        public TMP_Text RankText => rankText;
        public GameObject RootObject => rootObject != null ? rootObject : gameObject;

        private void Awake()
        {
            if (rootObject == null)
            {
                rootObject = gameObject;
            }

            if (rankIcon == null)
            {
                rankIcon = GetComponentInChildren<Image>();
            }

            if (rankText == null)
            {
                rankText = GetComponentInChildren<TMP_Text>();
            }
        }

        /// <summary>
        /// 순위, 유저명, 누적 후원 금액을 받아 슬롯 UI를 갱신하고 활성화합니다.
        /// (순위 아이콘은 인스펙터에 직접 대입된 이미지를 유지합니다)
        /// </summary>
        public void SetData(int rank, string userName, int amount)
        {
            SetVisible(true);

            if (rankText != null)
            {
                rankText.text = $"{rank}위: [{userName}] (${amount})";
            }
        }

        /// <summary>
        /// 텍스트 내용을 직접 설정하고 활성화합니다.
        /// </summary>
        public void SetContent(string text)
        {
            SetVisible(true);

            if (rankText != null)
            {
                rankText.text = text;
            }
        }

        /// <summary>
        /// 슬롯의 가시성(활성/비활성)을 설정합니다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            GameObject target = rootObject != null ? rootObject : gameObject;
            if (target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// 슬롯 텍스트 내용을 초기화하고 비활성화합니다.
        /// (인스펙터에 대입된 아이콘 스프라이트는 훼손하지 않습니다)
        /// </summary>
        public void Clear()
        {
            SetVisible(false);

            if (rankText != null)
            {
                rankText.text = string.Empty;
            }
        }
    }
}
