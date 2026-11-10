using UnityEngine;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 컨트롤러: 좌상단 이벤트 로그, 우상단 후원 랭킹, 중앙 전면 연출 및 데드라인 카운트다운 통합 관리
    /// HTML 모달 구조 기반 (UIBase 상속)
    /// </summary>
    public class MainUI : UIBase
    {
        [Header("메인 UI 섹션 참조")]
        [Tooltip("좌상단: 사용자 이벤트 1열 리스트")]
        [SerializeField] private EventLogUI eventLogUI;

        [Tooltip("우상단: 후원자 상위 5명 랭킹")]
        [SerializeField] private DonorRankUI donorRankUI;

        [Tooltip("중앙: 전면 이벤트 연출 공간 (힌트 폭발, 마법 조각, 빵빠레)")]
        [SerializeField] private FullScreenEventUI fullScreenEventUI;

        [Tooltip("중앙: 한계선 10초 카운트다운 경고 UI")]
        [SerializeField] private CountdownUI countdownUI;

        public EventLogUI EventLog => eventLogUI;
        public DonorRankUI DonorRank => donorRankUI;
        public FullScreenEventUI FullScreenEvent => fullScreenEventUI;
        public CountdownUI Countdown => countdownUI;

        public override void Init(string name)
        {
            base.Init(name);

            if (eventLogUI == null) eventLogUI = GetComponentInChildren<EventLogUI>(true);
            if (donorRankUI == null) donorRankUI = GetComponentInChildren<DonorRankUI>(true);
            if (fullScreenEventUI == null) fullScreenEventUI = GetComponentInChildren<FullScreenEventUI>(true);
            if (countdownUI == null) countdownUI = GetComponentInChildren<CountdownUI>(true);
        }

        public override void OnShow()
        {
            base.OnShow();
            Debug.Log("[MainUI] 메인 인게임 UI 표시 완료");
        }

        public override void OnClose()
        {
            base.OnClose();
            Debug.Log("[MainUI] 메인 인게임 UI 닫힘");
        }
    }
}
