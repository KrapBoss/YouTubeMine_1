using TMPro;
using UnityEngine;
using MemeLab.Core;
using MemeLab.Data;

namespace MemeLab.UI
{
    /// <summary>
    /// 한계선 위험 상태 진입 시 화면 중앙에 10.00초 카운트다운을 출력하는 경고 UI
    /// </summary>
    public class CountdownUI : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private GameObject countdownRoot;
        [SerializeField] private TMP_Text countdownText;
        //[SerializeField] private TMP_Text warningLabel;

        private DeadlineSensor deadlineSensor;

        private void Awake()
        {
            if (countdownRoot == null) countdownRoot = gameObject;
            HideCountdown();
        }

        private void Start()
        {
            deadlineSensor = FindFirstObjectByType<DeadlineSensor>();
            if (deadlineSensor != null)
            {
                deadlineSensor.OnDangerTick += UpdateCountdown;
                deadlineSensor.OnDangerCleared += HideCountdown;
                deadlineSensor.OnDeadlineExceeded += HideCountdown;
            }
        }

        private void OnDestroy()
        {
            if (deadlineSensor != null)
            {
                deadlineSensor.OnDangerTick -= UpdateCountdown;
                deadlineSensor.OnDangerCleared -= HideCountdown;
                deadlineSensor.OnDeadlineExceeded -= HideCountdown;
            }
        }

        public void UpdateCountdown(float remainingSeconds)
        {
            if (countdownRoot != null && !countdownRoot.activeSelf)
            {
                countdownRoot.SetActive(true);

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.Play(ResourceName.SOUND_WARNING, false);
                }
            }

            if (countdownText != null)
            {
                countdownText.text = $"{remainingSeconds:00.00}s";
            }

            //if (warningLabel != null)
            //{
            //    warningLabel.text = "⚠️ 한계선 초과 위험! 구원 후원이 필요합니다!";
            //}
        }

        public void HideCountdown()
        {
            if (countdownRoot != null)
            {
                countdownRoot.SetActive(false);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Stop(ResourceName.SOUND_WARNING);
            }
        }
    }
}
