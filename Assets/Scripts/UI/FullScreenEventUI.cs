using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MemeLab.Network;
using MemeLab.Core;

namespace MemeLab.UI
{
    /// <summary>
    /// 메인 UI 중앙: 후원, 구독, 클리어 빵빠레 등 전면 이벤트 연출 다목적 공간
    /// </summary>
    public class FullScreenEventUI : MonoBehaviour
    {
        [Header("전면 연출 UI 컴포넌트")]
        [SerializeField] private GameObject displayRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("연출 시간 설정")]
        [SerializeField] private float defaultDisplayDuration = 3.0f;

        private Coroutine displayCoroutine;

        private void Awake()
        {
            if (displayRoot == null) displayRoot = gameObject;
            if (canvasGroup == null) canvasGroup = displayRoot.GetComponent<CanvasGroup>();
            HideImmediate();
        }

        private void Start()
        {
            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnFullScreenEvent += ShowEvent;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameClear += (banner, animal) =>
                {
                    ShowEvent("🎉 10단계 최종 완성!", banner, 7.5f);
                };
            }
        }

        private void OnDestroy()
        {
            if (YouTubeEventReceiver.Instance != null)
            {
                YouTubeEventReceiver.Instance.OnFullScreenEvent -= ShowEvent;
            }
        }

        public void ShowEvent(string title, string description)
        {
            ShowEvent(title, description, defaultDisplayDuration);
        }

        public void ShowEvent(string title, string description, float duration)
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            displayCoroutine = StartCoroutine(DisplayRoutine(title, description, duration));
        }

        private IEnumerator DisplayRoutine(string title, string desc, float duration)
        {
            if (displayRoot != null) displayRoot.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = desc;

            // 페이드 인
            if (canvasGroup != null)
            {
                float fadeInTime = 0.3f;
                float elapsed = 0f;
                while (elapsed < fadeInTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
                    yield return null;
                }
                canvasGroup.alpha = 1.0f;
            }

            yield return new WaitForSeconds(duration);

            // 페이드 아웃
            if (canvasGroup != null)
            {
                float fadeOutTime = 0.5f;
                float elapsed = 0f;
                while (elapsed < fadeOutTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(1.0f - (elapsed / fadeOutTime));
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            HideImmediate();
            displayCoroutine = null;
        }

        public void HideImmediate()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (displayRoot != null) displayRoot.SetActive(false);
        }
    }
}
