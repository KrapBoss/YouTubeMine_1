using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MemeLab.Network;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 우상단: 후원자 목록 5명, 후원 금액 순으로 정렬 ("아이콘 : 텍스트")
    /// 순위 아이콘은 인스펙터 슬롯 오브젝트에 직접 대입하여 사용합니다.
    /// </summary>
    public class DonorRankUI : MonoBehaviour
    {
        [Header("랭킹 5인 슬롯 컴포넌트 목록")]
        [SerializeField] private List<DonorRankSlotUI> rankSlots = new List<DonorRankSlotUI>();

        // 유저별 누적 후원 금액 딕셔너리
        private readonly Dictionary<string, int> donorTotals = new Dictionary<string, int>();

        private void Awake()
        {
            // 인스펙터 미할당 시 자식 오브젝트에서 DonorRankSlotUI 자동 탐색
            if (rankSlots == null || rankSlots.Count == 0)
            {
                rankSlots = new List<DonorRankSlotUI>(GetComponentsInChildren<DonorRankSlotUI>(true));
            }
        }

        private void Start()
        {
            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnDonationLogged += RecordDonation;
            }
            RefreshRankUI();
        }

        private void OnDestroy()
        {
            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnDonationLogged -= RecordDonation;
            }
        }

        public void RecordDonation(string user, int amount)
        {
            if (string.IsNullOrEmpty(user)) return;

            if (donorTotals.ContainsKey(user))
            {
                donorTotals[user] += amount;
            }
            else
            {
                donorTotals[user] = amount;
            }

            RefreshRankUI();
        }

        public void RefreshRankUI()
        {
            // 후원 금액 기준 내림차순 정렬 상위 5명
            var topDonors = donorTotals
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .ToList();

            for (int i = 0; i < rankSlots.Count; i++)
            {
                if (rankSlots[i] == null) continue;

                if (i < topDonors.Count)
                {
                    var donor = topDonors[i];
                    rankSlots[i].SetData(i + 1, donor.Key, donor.Value);
                }
                else
                {
                    // 빈 슬롯 숨김 처리
                    rankSlots[i].SetVisible(false);
                }
            }
        }
    }
}
