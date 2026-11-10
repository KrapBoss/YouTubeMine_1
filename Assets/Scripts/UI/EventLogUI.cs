using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MemeLab.Network;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 좌상단: 사용자 이벤트를 1열 리스트 형식으로 표시 ("아이콘 : 텍스트")
    /// 최대 3개의 EventLogSlotUI 객체를 재사용하며, 새 이벤트 발생 시 이전 메시지를 비활성화하고 새 메시지를 활성화합니다.
    /// </summary>
    public class EventLogUI : MonoBehaviour
    {
        [Header("로그 슬롯 설정 (최대 3개 고정 재사용)")]
        [Tooltip("고정 슬롯 3개 컴포넌트 목록 (미할당 시 자식에서 자동 탐색 또는 템플릿으로 생성)")]
        [SerializeField] private List<EventLogSlotUI> logSlots = new List<EventLogSlotUI>();
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemTemplate;

        [Header("이벤트 로그 동작 모드")]
        [Tooltip("새 이벤트 발생 시 이전 메시지를 비활성화하고 새 메시지만 단일 활성화할지 여부")]
        [SerializeField] private bool singleActiveMode = true;

        [Header("이벤트별 기본 아이콘 (인스펙터 대입용)")]
        [SerializeField] private Sprite likeIcon;
        [SerializeField] private Sprite subscribeIcon;
        [SerializeField] private Sprite donationIcon;
        [SerializeField] private Sprite chatIcon;

        private const int MAX_SLOT_COUNT = 3;
        private int currentSlotIndex = 0;
        private EventLogSlotUI lastActiveSlot = null;
        private readonly Queue<EventLogSlotUI> activeSlotQueue = new Queue<EventLogSlotUI>();

        private void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            if (itemContainer == null) itemContainer = transform;

            // 1. 인스펙터에 슬롯이 없으면 자식 오브젝트에서 탐색
            if (logSlots == null || logSlots.Count == 0)
            {
                var found = GetComponentsInChildren<EventLogSlotUI>(true);
                logSlots = new List<EventLogSlotUI>();
                for (int i = 0; i < found.Length && i < MAX_SLOT_COUNT; i++)
                {
                    logSlots.Add(found[i]);
                }
            }

            // 2. 슬롯이 3개 미만이면 itemTemplate 또는 동적 생성으로 최대 3개 보장
            while (logSlots.Count < MAX_SLOT_COUNT)
            {
                EventLogSlotUI newSlot = CreateSlotInstance();
                if (newSlot != null)
                {
                    logSlots.Add(newSlot);
                }
                else
                {
                    break;
                }
            }

            // 3. 초기에는 모든 슬롯 비활성화
            for (int i = 0; i < logSlots.Count; i++)
            {
                if (logSlots[i] != null)
                {
                    logSlots[i].SetVisible(false);
                }
            }
        }

        private EventLogSlotUI CreateSlotInstance()
        {
            GameObject newObj;
            if (itemTemplate != null)
            {
                newObj = Instantiate(itemTemplate, itemContainer);
            }
            else
            {
                // Fallback 동적 런타임 UI 생성
                newObj = new GameObject($"LogSlot_{logSlots.Count + 1}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                newObj.transform.SetParent(itemContainer, false);

                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(newObj.transform, false);

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(newObj.transform, false);
            }

            newObj.SetActive(false);

            EventLogSlotUI slot = newObj.GetComponent<EventLogSlotUI>();
            if (slot == null)
            {
                slot = newObj.AddComponent<EventLogSlotUI>();
            }

            return slot;
        }

        private void Start()
        {
            if (itemTemplate != null)
            {
                itemTemplate.SetActive(false);
            }

            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnEventLogged += AddEventLog;
            }
        }

        private void OnDestroy()
        {
            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnEventLogged -= AddEventLog;
            }
        }

        public void AddEventLog(YouTubeStreamEvent evt)
        {
            string content;
            Sprite icon = chatIcon;

            switch (evt.eventType)
            {
                case YouTubeEventType.Like:
                    content = $"[{evt.userName}] 님이 좋아요를 눌렀습니다!";
                    icon = likeIcon;
                    break;
                case YouTubeEventType.Subscribe:
                    content = $"[{evt.userName}] 님이 채널을 구독했습니다!";
                    icon = subscribeIcon;
                    break;
                case YouTubeEventType.Donation:
                    content = $"[{evt.userName}] 님이 ${evt.amount} 후원했습니다!";
                    icon = donationIcon;
                    break;
                case YouTubeEventType.ChatCommand:
                default:
                    content = $"[{evt.userName}]: {evt.message}";
                    icon = chatIcon;
                    break;
            }

            DisplayLog(content, icon);
        }

        private void DisplayLog(string text, Sprite icon)
        {
            if (logSlots == null || logSlots.Count == 0) return;

            if (singleActiveMode)
            {
                // 요구사항: 새 이벤트 발생 시 이전 메시지 비활성화, 새 메시지 활성화
                if (lastActiveSlot != null)
                {
                    lastActiveSlot.SetVisible(false);
                }

                EventLogSlotUI currentSlot = logSlots[currentSlotIndex];
                if (currentSlot != null)
                {
                    currentSlot.SetLog(text, icon);
                    currentSlot.SetVisible(true);
                    lastActiveSlot = currentSlot;
                }

                currentSlotIndex = (currentSlotIndex + 1) % logSlots.Count;
            }
            else
            {
                // 최대 3개 누적 표시 모드 (3개 초과 시 가장 오래된 이전 메시지 비활성화)
                if (activeSlotQueue.Count >= MAX_SLOT_COUNT)
                {
                    EventLogSlotUI oldestSlot = activeSlotQueue.Dequeue();
                    if (oldestSlot != null)
                    {
                        oldestSlot.SetVisible(false);
                    }
                }

                EventLogSlotUI currentSlot = logSlots[currentSlotIndex];
                if (currentSlot != null)
                {
                    currentSlot.SetLog(text, icon);
                    currentSlot.SetVisible(true);
                    activeSlotQueue.Enqueue(currentSlot);
                }

                currentSlotIndex = (currentSlotIndex + 1) % logSlots.Count;
            }
        }
    }
}
