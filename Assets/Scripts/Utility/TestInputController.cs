using UnityEngine;
using MemeLab.Core;
using MemeLab.Effects;
using MemeLab.Network;
using MemeLab.Objects;
using MemeLab.Utility;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemeLab.Utility
{
    /// <summary>
    /// 개발 및 디버깅을 위한 키보드 입력 테스터.
    /// New Input System과 Legacy Input 모두 지원하며,
    /// 크레인 이동, 채팅 투하, 좋아요, 구독, $1~$5 후원, 치트 스폰 등을 키보드로 시뮬레이션합니다.
    /// </summary>
    public class TestInputController : MonoBehaviour
    {
        [Header("연동 시스템 (미할당 시 자동 탐색)")]
        [SerializeField] private Spawner spawner;
        [SerializeField] private FeverController feverController;
        [SerializeField] private YouTubeEventReceiver eventReceiver;
        [SerializeField] private GameManager gameManager;

        [Header("테스트 UI 가이드 표시")]
        [SerializeField] private bool showOnScreenGuide = true;

        private int mockUserIndex = 1;

        private void Awake()
        {
            if (spawner == null) spawner = FindFirstObjectByType<Spawner>();
            if (feverController == null) feverController = FindFirstObjectByType<FeverController>();
            if (eventReceiver == null) eventReceiver = FindFirstObjectByType<YouTubeEventReceiver>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        }

        private void Update()
        {
            // [H] 키: 화면 조작 가이드 토글
            if (IsKeyDown(Key.H, KeyCode.H))
            {
                showOnScreenGuide = !showOnScreenGuide;
            }

            // 1. 크레인 이동 (!좌 / !우)
            if (IsKeyDown(Key.A, KeyCode.A) || IsKeyDown(Key.LeftArrow, KeyCode.LeftArrow))
            {
                SimulateChatCommand("!좌");
            }
            if (IsKeyDown(Key.D, KeyCode.D) || IsKeyDown(Key.RightArrow, KeyCode.RightArrow))
            {
                SimulateChatCommand("!우");
            }

            // 2. 크레인 열 지정 이동 (!1 ~ !5)
            if (IsKeyDown(Key.Digit1, KeyCode.Alpha1)) SimulateChatCommand("!1");
            if (IsKeyDown(Key.Digit2, KeyCode.Alpha2)) SimulateChatCommand("!2");
            if (IsKeyDown(Key.Digit3, KeyCode.Alpha3)) SimulateChatCommand("!3");
            if (IsKeyDown(Key.Digit4, KeyCode.Alpha4)) SimulateChatCommand("!4");
            if (IsKeyDown(Key.Digit5, KeyCode.Alpha5)) SimulateChatCommand("!5");

            // 3. 채팅 드롭 (!드롭 - 20초 쿨타임 포함)
            if (IsKeyDown(Key.Space, KeyCode.Space))
            {
                string testUser = $"시청자_{mockUserIndex}";
                SimulateChatCommand("!드롭", testUser);
                mockUserIndex = (mockUserIndex % 5) + 1;
            }

            // 4. [무료 협동 1 - L]: 좋아요 바운스
            if (IsKeyDown(Key.L, KeyCode.L))
            {
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Like, $"좋아요유저_{Random.Range(1, 99)}", 0, "좋아요"));
                }
                else if (feverController != null)
                {
                    feverController.TriggerBounce();
                }
            }

            // 5. [무료 협동 2 - S]: 신규 구독 (4단계 확정 소환)
            if (IsKeyDown(Key.S, KeyCode.S))
            {
                string subUser = $"구독자_{Random.Range(100, 999)}";
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Subscribe, subUser, 0, "신규 구독"));
                }
                else if (spawner != null)
                {
                    spawner.SpawnSubAnimal(subUser);
                }
            }

            // 6. [협동 후원 1 - Q]: $1 소형 동물(1~2단계) 일괄 폭파 청소
            if (IsKeyDown(Key.Q, KeyCode.Q))
            {
                string donor = $"구원천사_{Random.Range(10, 99)}";
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, donor, 1, "$1 후원"));
                }
                else if (feverController != null)
                {
                    feverController.ClearSmallAnimals(donor);
                }
            }

            // 7. [협동 후원 2 - W]: $2 무지개 별 투하 (강제 1단계 진화)
            if (IsKeyDown(Key.W, KeyCode.W))
            {
                string donor = $"스타술사_{Random.Range(10, 99)}";
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, donor, 2, "$2 후원"));
                }
                else if (spawner != null)
                {
                    spawner.SpawnRainbowStar(donor);
                }
            }

            // 8. [트롤링 후원 - E]: $3 무적 방해 블록 투하
            if (IsKeyDown(Key.E, KeyCode.E))
            {
                string donor = $"트롤마왕_{Random.Range(10, 99)}";
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, donor, 3, "$3 후원"));
                }
                else if (spawner != null)
                {
                    spawner.SpawnTrollBlock(donor);
                }
            }

            // 9. [피버 후원 - R]: $5 10초 자석 폭풍 연쇄 머지
            if (IsKeyDown(Key.R, KeyCode.R))
            {
                string donor = $"피버군주_{Random.Range(10, 99)}";
                if (eventReceiver != null)
                {
                    eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.Donation, donor, 5, "$5 후원"));
                }
                else if (feverController != null)
                {
                    feverController.StartMagnetFever(donor, 10.0f);
                }
            }

            // 10. [치트/디버그 - 9]: 9단계 동물 2마리 즉시 투하 (10단계 클리어 테스트)
            if (IsKeyDown(Key.Digit9, KeyCode.Alpha9))
            {
                if (spawner != null)
                {
                    Vector3 pos = spawner.transform.position;
                    spawner.SpawnAnimal(9, "합체후원자A", 2, pos + Vector3.left * 0.5f);
                    spawner.SpawnAnimal(9, "합체후원자B", 3, pos + Vector3.right * 0.5f);
                    Debug.Log("[TestInputController] 9단계 동물 2마리 스폰 (충돌 시 10단계 클리어)");
                }
            }

            // 11. [치트/디버그 - 0]: 10단계 동물 즉시 스폰 (클리어 세리머니 테스트)
            if (IsKeyDown(Key.Digit0, KeyCode.Alpha0))
            {
                if (gameManager != null && spawner != null)
                {
                    gameManager.OnMergeSuccess(10, Vector2.zero, "VIP_MVP_테스터", 5);
                }
            }

            // 12. [치트/디버그 - X]: 실패 리셋 강제 발동
            if (IsKeyDown(Key.X, KeyCode.X))
            {
                if (gameManager != null)
                {
                    gameManager.OnDeadlineExceeded();
                }
            }
        }

        private void SimulateChatCommand(string command, string user = null)
        {
            if (string.IsNullOrEmpty(user))
            {
                user = $"유저_{mockUserIndex}";
            }

            if (eventReceiver != null)
            {
                eventReceiver.EnqueueEvent(new YouTubeStreamEvent(YouTubeEventType.ChatCommand, user, 0, command));
            }
            else if (spawner != null)
            {
                if (command == "!좌") spawner.MoveSpawnerDirection(-1);
                else if (command == "!우") spawner.MoveSpawnerDirection(1);
                else if (command == "!드롭") spawner.CommandDrop(user);
            }
        }

        private bool IsKeyDown(Key newKey, KeyCode legacyKey)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return Keyboard.current[newKey].wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown(legacyKey);
        }

        private void OnGUI()
        {
            if (!showOnScreenGuide) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 230, 480, 220), "🎮 인게임 시뮬레이션 테스터 (H 키: 가이드 토글)", GUI.skin.window);
            GUILayout.BeginVertical();

            GUILayout.Label("• [A / D / ← / →]: 크레인 이동 (!좌 / !우)");
            GUILayout.Label("• [1 ~ 5]: 1~5번 열로 크레인 순간 이동");
            GUILayout.Label("• [Space]: 시청자 채팅 동물 투하 (!드롭 - 쿨타임 20초)");
            GUILayout.Label("• [L]: 좋아요 바운스 | [S]: 신규 구독 (4단계 확정 소환)");
            GUILayout.Label("• [Q]: $1 소탕 (소형 동물 폭파) | [W]: $2 무지개 별 (강제 진화)");
            GUILayout.Label("• [E]: $3 무적 방해 블록 | [R]: $5 자석 피버 폭풍");
            GUILayout.Label("• [9]: 9단계 2마리 투하 (머지 테스트) | [0]: 10단계 클리어 테스트 | [X]: 리셋");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
