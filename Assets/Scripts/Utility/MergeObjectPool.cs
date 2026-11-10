using System.Collections.Generic;
using UnityEngine;
using MemeLab.Objects;
using MemeLab.Data;

namespace MemeLab.Utility
{
    /// <summary>
    /// 머지 오브젝트(1~10단계 동물 및 트롤링 방해물) 전용 오브젝트 풀러
    /// 잦은 합체와 소탕, 리셋 시 발생하는 GC Alloc 및 인스턴스화 오버헤드를 방지합니다.
    /// </summary>
    public class MergeObjectPool : MonoBehaviour
    {
        public static MergeObjectPool Instance { get; private set; }

        [Header("풀 관리 설정")]
        [SerializeField] private Transform poolRoot;
        [SerializeField] private AnimalStageDataSO stageData;
        [SerializeField] private GameObject defaultAnimalPrefab;
        [SerializeField] private GameObject trollBlockPrefab;

        private readonly Dictionary<int, Stack<MergeObject>> poolDict = new Dictionary<int, Stack<MergeObject>>();

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

            if (poolRoot == null)
            {
                GameObject rootObj = new GameObject("[MergeObjectPool_Root]");
                poolRoot = rootObj.transform;
                poolRoot.SetParent(this.transform);
            }

            InitializePools();
        }

        private void InitializePools()
        {
            for (int i = 1; i <= 10; i++)
            {
                poolDict[i] = new Stack<MergeObject>();
            }
            poolDict[-1] = new Stack<MergeObject>();
        }

        /// <summary>
        /// 풀에서 오브젝트를 대여하거나 필요 시 새로 생성하여 초기화 후 반환
        /// </summary>
        public MergeObject Get(int level, string owner, int amount, Vector3 position, bool isObstacle = false)
        {
            int poolKey = isObstacle ? -1 : level;
            if (!poolDict.TryGetValue(poolKey, out Stack<MergeObject> stack))
            {
                stack = new Stack<MergeObject>();
                poolDict[poolKey] = stack;
            }

            MergeObject obj = null;
            while (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj != null) break;
            }

            if (obj == null)
            {
                obj = CreateNewInstance(level, isObstacle);
            }

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;

            var stageInfo = stageData != null ? stageData.GetStageInfo(level) : null;
            obj.Initialize(level, owner, amount, stageInfo, isObstacle);

            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 사용이 끝난 머지 오브젝트를 풀로 반환
        /// </summary>
        public void Return(MergeObject obj)
        {
            if (obj == null) return;

            obj.IsMerging = false;
            obj.Velocity = Vector2.zero;
            if (obj.Rb != null)
            {
                obj.Rb.angularVelocity = 0f;
            }

            obj.gameObject.SetActive(false);

            if (poolRoot != null)
            {
                obj.transform.SetParent(poolRoot);
            }

            int poolKey = obj.IsObstacle ? -1 : obj.Level;
            if (!poolDict.TryGetValue(poolKey, out Stack<MergeObject> stack))
            {
                stack = new Stack<MergeObject>();
                poolDict[poolKey] = stack;
            }

            stack.Push(obj);
        }

        private MergeObject CreateNewInstance(int level, bool isObstacle)
        {
            var info = stageData != null ? stageData.GetStageInfo(level) : null;
            GameObject prefabToUse = (info != null && info.customPrefab != null) ? info.customPrefab : null;

            if (prefabToUse == null)
            {
                if (isObstacle && trollBlockPrefab != null)
                {
                    prefabToUse = trollBlockPrefab;
                }
                else
                {
                    prefabToUse = defaultAnimalPrefab;
                }
            }

            GameObject go;
            if (prefabToUse != null)
            {
                go = Instantiate(prefabToUse);
            }
            else
            {
                go = new GameObject(isObstacle ? "Pooled_Obstacle" : $"Pooled_Animal_Lv{level}");
                go.AddComponent<SpriteRenderer>();
                go.AddComponent<Rigidbody2D>();
                go.AddComponent<PolygonCollider2D>();
            }

            MergeObject mergeObj = go.GetComponent<MergeObject>();
            if (mergeObj == null)
            {
                mergeObj = go.AddComponent<MergeObject>();
            }

            if (poolRoot != null)
            {
                go.transform.SetParent(poolRoot);
            }

            go.SetActive(false);
            return mergeObj;
        }

        public void ClearAllPools()
        {
            foreach (var pair in poolDict)
            {
                while (pair.Value.Count > 0)
                {
                    MergeObject item = pair.Value.Pop();
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                    }
                }
            }
        }
    }
}
