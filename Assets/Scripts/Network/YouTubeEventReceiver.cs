using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using MemeLab.Core;
using MemeLab.Effects;
using NativeWebSocket;

namespace MemeLab.Network
{
    public enum YouTubeEventType
    {
        ChatCommand,    // 챗
        Like,           // 좋아요
        Subscribe,      // 구독
        Donation        // 후원
    }

    [Serializable]
    public struct YouTubeStreamEvent
    {
        public YouTubeEventType eventType;
        public string userName;
        public int amount; // $1, $2, $3, $5 등
        public string message;

        public YouTubeStreamEvent(YouTubeEventType type, string user, int amount, string msg)
        {
            this.eventType = type;
            this.userName = user;
            this.amount = amount;
            this.message = msg;
        }
    }

    /// <summary>
    /// 유튜브 라이브 채팅/좋아요/구독/슈퍼챗 이벤트를 수신하여 인게임 큐에 전달하고 각 시스템에 디스패치
    /// </summary>
    public class YouTubeEventReceiver : MonoBehaviour
    {
        public static YouTubeEventReceiver Instance { get; private set; }

        [Header("연동 시스템 참조")]
        [SerializeField] private Spawner spawner;
        [SerializeField] private FeverController feverController;

        private readonly ConcurrentQueue<YouTubeStreamEvent> eventQueue = new ConcurrentQueue<YouTubeStreamEvent>();

        // UI 통보용 이벤트
        public event Action<YouTubeStreamEvent> OnEventLogged;
        public event Action<string, int> OnDonationLogged; // 유저명, 금액
        public event Action<string, string> OnFullScreenEvent; // 제목, 상세 문구

        // Enviroment
        readonly static string ADDRESS = "ws://localhost";
        readonly static string PORT = "444";

        // 채팅 메세지 인코딩용
        struct ChatMessage
        {
            string type;
            string author;
            string message;
            string amount;
            string currency;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (spawner == null) spawner = FindFirstObjectByType<Spawner>();
            if (feverController == null) feverController = FindFirstObjectByType<FeverController>();

            ConnectToWeb();
        }

        private void Update()
        {
            // 외부 네트워크 스레드로부터 안전하게 메인 스레드에서 이벤트 큐 처리
            while (eventQueue.TryDequeue(out YouTubeStreamEvent evt))
            {
                ProcessEvent(evt);
            }
        }

        private void ConnectToWeb()
        {
            var web = new WebSocket($"{ADDRESS}:{PORT}");
            web.OnOpen += () =>
                {
                Console.WriteLine("서버 연결 완료");
                };
            web.OnError += (x) =>
            {
                Console.WriteLine($"서버 에러 발생!!\n{x}");
            };

            web.OnClose += (x) =>
            {
                Console.WriteLine($"서버 닫혔어\n{x}");
            };

            web.OnMessage += (x) =>
            {
                string bytr = System.Text.Encoding.UTF8.GetString(x);
                var msg = JsonUtility.FromJson<ChatMessage>(bytr);

                HandleChatEvent(msg);
            };
        }

        /// <summary>
        /// 이벤트 필터링
        /// </summary>
        /// <param name="msg"></param>
        private void HandleChatEvent(ChatMessage msg)
        {

        }

        /// <summary>
        /// 재화를 USD로 변환하여 내림한 값의 수를 반환한다.
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="amound"></param>
        private int ConvertToUSD(string currency,string amound)
        {
            return 0;
        }

        /// <summary>
        /// 외부 소켓/웹훅/API 연동 모듈에서 호출하는 이벤트 진입점
        /// </summary>
        public void EnqueueEvent(YouTubeStreamEvent evt)
        {
            eventQueue.Enqueue(evt);
        }

        private void ProcessEvent(YouTubeStreamEvent evt)
        {
            OnEventLogged?.Invoke(evt);

            switch (evt.eventType)
            {
                case YouTubeEventType.ChatCommand:
                    HandleChatCommand(evt.userName, evt.message);
                    break;

                case YouTubeEventType.Like:
                    HandleLike();
                    break;

                case YouTubeEventType.Subscribe:
                    HandleSubscribe(evt.userName);
                    break;

                case YouTubeEventType.Donation:
                    HandleDonation(evt.userName, evt.amount);
                    break;
            }
        }

        // 챗 입력으로 실행할 이벤트
        private void HandleChatCommand(string user, string message)
        {
            if (spawner == null || string.IsNullOrEmpty(message)) return;

            string cmd = message.Trim();
            //if (cmd.Equals("!좌", StringComparison.OrdinalIgnoreCase))
            //{
            //    spawner.MoveSpawnerDirection(-1);
            //}
            //else if (cmd.Equals("!우", StringComparison.OrdinalIgnoreCase))
            //{
            //    spawner.MoveSpawnerDirection(1);
            //}
            //else 
            //    if (cmd.StartsWith("!") && int.TryParse(cmd.Substring(1), out int column) && column >= 1 && column <= 5)
            //{
            //    spawner.MoveSpawnerColumn(column);
            //}
            //else 

            // 스폰
            if (cmd.Equals("!Drop", StringComparison.OrdinalIgnoreCase))
            {
                spawner.CommandDrop(user);
            }
        }

        private void HandleLike()
        {
            if (feverController != null)
            {
                feverController.TriggerBounce();
            }
        }

        private void HandleSubscribe(string user)
        {
            if (spawner != null)
            {
                spawner.SpawnSubAnimal(user);
            }
            OnFullScreenEvent?.Invoke("✨ 신규 구독 발생!", $"[{user}] 님의 4단계 중형 동물 각인 소환!");
        }

        private void HandleDonation(string user, int amount)
        {
            OnDonationLogged?.Invoke(user, amount);

            if (amount <= 1)
            {
                // 협동 후원 1 - $1: 소형 동물(1~2단계) 일괄 폭파 삭제
                if (feverController != null)
                {
                    feverController.ClearSmallAnimals(user);
                }
                OnFullScreenEvent?.Invoke("💥 $1 힌트 폭발 (소형 동물 청소)!", $"[{user}] 님이 잔챙이 동물을 모두 폭파 정리했습니다!");
            }
            else if (amount == 2)
            {
                // 협동 후원 2 - $2: 만능 진화 무지개 별 투하
                if (spawner != null)
                {
                    spawner.SpawnRainbowStar(user);
                }
                OnFullScreenEvent?.Invoke("⭐ $2 마법 무지개 별 투하!", $"[{user}] 님의 만능 진화 별이 투하되었습니다!");
            }
            else if (amount == 3 || amount == 4)
            {
                // 트롤링 후원 - $3: 무적 영구 고중량 방해 블록 투하
                if (spawner != null)
                {
                    spawner.SpawnTrollBlock(user);
                }
                OnFullScreenEvent?.Invoke("😈 $3 트롤링 방해 블록 투하!", $"[{user}] 님이 파괴 불가 무적 블록을 소환했습니다!");
            }
            else if (amount >= 5)
            {
                // 피버 후원 - $5: 10초 자석 폭풍 연쇄 머지
                if (feverController != null)
                {
                    feverController.StartMagnetFever(user, 10.0f);
                }
                OnFullScreenEvent?.Invoke("⚡ $5 자석 피버 폭풍 발동!", $"[{user}] 님이 10초간 강력한 연쇄 합체 자석을 작동시켰습니다!");
            }
        }

        // ==================== 에디터 인스펙터 테스트용 ContextMenu ====================

        [ContextMenu("Test: Like (좋아요 바운스)")]
        public void TestLike() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Like, "시청자A", 0, "좋아요 누름"));

        [ContextMenu("Test: Subscribe (구독 소환)")]
        public void TestSubscribe() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Subscribe, "신규구독자", 0, "채널 구독"));

        [ContextMenu("Test: $1 소형 동물 소탕")]
        public void TestDonation1() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, "구원천사", 1, "$1 후원"));

        [ContextMenu("Test: $2 무지개 별 투하")]
        public void TestDonation2() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, "별마법사", 2, "$2 후원"));

        [ContextMenu("Test: $3 트롤링 블록 투하")]
        public void TestDonation3() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, "빌런마왕", 3, "$3 후원"));

        [ContextMenu("Test: $5 자석 피버 폭풍")]
        public void TestDonation5() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, "피버대왕", 5, "$5 후원"));

        [ContextMenu("Test: Chat !좌")]
        public void TestChatLeft() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.ChatCommand, "유저1", 0, "!좌"));

        [ContextMenu("Test: Chat !우")]
        public void TestChatRight() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.ChatCommand, "유저2", 0, "!우"));

        [ContextMenu("Test: Chat !드롭")]
        public void TestChatDrop() => EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.ChatCommand, "유저3", 0, "!드롭"));
    }
}
