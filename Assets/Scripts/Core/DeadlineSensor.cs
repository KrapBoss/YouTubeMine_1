using System;
using System.Collections.Generic;
using UnityEngine;
using MemeLab.Objects;

namespace MemeLab.Core
{
    /// <summary>
    /// 상자 상단 한계선(Dead Line) 체류 감지, 10초 카운트다운 타이머 및 실패 트리거
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeadlineSensor : MonoBehaviour
    {
        [Header("센서 감지 설정")]
        [Tooltip("낙하 중인 객체를 제외하기 위한 정체 속도 임계값")]
        [SerializeField] private float velocityThreshold = 0.1f;
        [Tooltip("실패 판정까지의 제한 시간 (초)")]
        [SerializeField] private float timeLimit = 10.0f;

        private BoxCollider2D triggerCollider;
        private readonly HashSet<MergeObject> objectsInTrigger = new HashSet<MergeObject>();

        private float dangerTimer = 0f;
        private bool isDangerous = false;

        public float RemainingTime => Mathf.Max(0f, timeLimit - dangerTimer);
        public bool IsDangerous => isDangerous;

        public event Action<float> OnDangerTick; // 잔여 시간 (10.00초 ~ 0초)
        public event Action OnDangerCleared;     // 위험 해제 이벤트
        public event Action OnDeadlineExceeded;  // 10초 초과 실패 이벤트

        private void Awake()
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                return;
            }

            // 파괴된 객체 정리
            objectsInTrigger.RemoveWhere(obj => obj == null);

            // 속도가 0.1 이하로 한계선에 안착(정체) 중인 위험 객체 검사
            int dangerCount = 0;
            foreach (var obj in objectsInTrigger)
            {
                if (obj.Velocity.magnitude <= velocityThreshold)
                {
                    dangerCount++;
                }
            }

            if (dangerCount > 0)
            {
                isDangerous = true;
                dangerTimer += Time.deltaTime;
                OnDangerTick?.Invoke(RemainingTime);

                if (dangerTimer >= timeLimit)
                {
                    Debug.LogWarning("[DeadlineSensor] 한계선 10초 초과! 게임 오버 리셋 시퀀스 진입.");
                    OnDeadlineExceeded?.Invoke();
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnDeadlineExceeded();
                    }
                    ResetSensor();
                }
            }
            else
            {
                if (isDangerous)
                {
                    isDangerous = false;
                    dangerTimer = 0f;
                    OnDangerCleared?.Invoke();
                    Debug.Log("[DeadlineSensor] 위험 상태 해제. 타이머 초기화.");
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            MergeObject obj = collision.GetComponent<MergeObject>();
            if (obj != null)
            {
                objectsInTrigger.Add(obj);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            MergeObject obj = collision.GetComponent<MergeObject>();
            if (obj != null)
            {
                objectsInTrigger.Remove(obj);
            }
        }

        public void ResetSensor()
        {
            objectsInTrigger.Clear();
            dangerTimer = 0f;
            if (isDangerous)
            {
                isDangerous = false;
                OnDangerCleared?.Invoke();
            }
        }
    }
}
