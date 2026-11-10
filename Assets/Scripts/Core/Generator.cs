using UnityEngine;
using MemeLab.Data;
using MemeLab.Effects;
using MemeLab.Network;
using MemeLab.Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MemeLab.Core
{
    /// <summary>
    /// UIManager를 제외한 모든 인게임 핵심 매니저 및 물리 환경, 크레인 스포너 오브젝트를 
    /// 코드로 자동 생성하고 컴포넌트 간 참조를 자동으로 연결해 주는 제너레이터 클래스입니다.
    /// 에디터 메뉴 및 런타임(Awake) 자동 생성을 모두 지원합니다.
    /// </summary>
    [ExecuteAlways]
    public class Generator : MonoBehaviour
    {
        [Header("자동 생성 옵션")]
        [Tooltip("게임 시작(Awake) 시 씬에 매니저가 누락되어 있으면 코드로 자동 생성합니다.")]
        [SerializeField] private bool autoGenerateOnAwake = true;

        [Header("물리 상자 규격 설정")]
        [SerializeField] private float boxWidth = 7.0f;
        [SerializeField] private float boxHeight = 8.5f;
        [SerializeField] private float wallThickness = 0.5f;
        [SerializeField] private float boxCenterY = -0.5f;
        [SerializeField] private float deadlineOffsetY = 3.2f;
        [SerializeField] private float spawnerOffsetY = 4.2f;

        [Header("물리 재질 (기본 마찰 0.4, 반발 0.15)")]
        [SerializeField] private float physicsFriction = 0.4f;
        [SerializeField] private float physicsBounciness = 0.15f;

        [Header("데이터 에셋 참조 (미지정 시 자동 탐색/생성)")]
        [SerializeField] private AnimalStageDataSO stageData;

        private const string MANAGERS_ROOT_NAME = "[GameManagers]";
        private const string PHYSICS_BOX_ROOT_NAME = "[Physics Box]";
        private const string SPANWER_ROOT_NAME = "[Crane Spawner]";

        private void Awake()
        {
            if (Application.isPlaying && autoGenerateOnAwake)
            {
                GenerateAllManagers();
            }
        }

        /// <summary>
        /// UIManager를 제외한 인게임 관리 오브젝트 및 물리 환경을 코드로 자동 생성합니다.
        /// </summary>
        [ContextMenu("Generate Managers (Except UI)")]
        public void GenerateAllManagers()
        {
            Debug.Log("[Generator] UIManager 제외 인게임 관리 오브젝트 자동 생성 시작...");

            // 1. ScriptableObject 데이터 확인 또는 동적 로드
            EnsureStageData();

            // 2. [GameManagers] 루트 및 핵심 매니저 생성
            GameObject managersRoot = FindOrCreateRoot(MANAGERS_ROOT_NAME);
            GameManager gm = EnsureComponent<GameManager>(managersRoot);
            SoundManager sound = EnsureComponent<SoundManager>(managersRoot);
            MergeObjectPool pool = EnsureComponent<MergeObjectPool>(managersRoot);
            FeverController fever = EnsureComponent<FeverController>(managersRoot);
            YouTubeEventReceiver receiver = EnsureComponent<YouTubeEventReceiver>(managersRoot);
            TestInputController tester = EnsureComponent<TestInputController>(managersRoot);

            // 3. [Physics Box] 루트 및 벽, 바닥, 데드라인 센서 생성
            GameObject boxRoot = FindOrCreateRoot(PHYSICS_BOX_ROOT_NAME);
            PhysicsMaterial2D boxMaterial = CreateOrGetPhysicsMaterial();
            SetupPhysicsBoundaries(boxRoot, boxMaterial);
            DeadlineSensor deadline = SetupDeadlineSensor(boxRoot);

            // 4. [Crane Spawner] 루트 및 스포너 생성
            GameObject spawnerRoot = FindOrCreateRoot(SPANWER_ROOT_NAME);
            Spawner spawner = SetupCraneSpawner(spawnerRoot);

            // 5. 상호 참조 자동 연결
            WireReferences(gm, spawner, deadline, fever, receiver, tester, pool);

            Debug.Log("[Generator] UIManager를 제외한 관리 오브젝트 및 물리 구조 생성이 완료되었습니다!");
        }

        /// <summary>
        /// 생성된 관리 오브젝트 정리 (디버그 및 재생성용)
        /// </summary>
        [ContextMenu("Clear Generated Managers")]
        public void ClearGeneratedManagers()
        {
            DestroyRootIfExists(MANAGERS_ROOT_NAME);
            DestroyRootIfExists(PHYSICS_BOX_ROOT_NAME);
            DestroyRootIfExists(SPANWER_ROOT_NAME);
            Debug.Log("[Generator] 생성된 관리 오브젝트들이 정리되었습니다.");
        }

        private GameObject FindOrCreateRoot(string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                root = new GameObject(rootName);
                root.transform.position = Vector3.zero;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(root, $"Create {rootName}");
                }
#endif
            }
            return root;
        }

        private T EnsureComponent<T>(GameObject target) where T : Component
        {
            T comp = target.GetComponent<T>();
            if (comp == null)
            {
                comp = target.AddComponent<T>();
            }
            return comp;
        }

        private PhysicsMaterial2D CreateOrGetPhysicsMaterial()
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("BoxPhysicsMaterial")
            {
                friction = physicsFriction,
                bounciness = physicsBounciness
            };
            return mat;
        }

        private void SetupPhysicsBoundaries(GameObject boxRoot, PhysicsMaterial2D mat)
        {
            float halfWidth = boxWidth * 0.5f;
            float bottomY = boxCenterY - (boxHeight * 0.5f);

            // 1. 좌측 벽 (Left Wall)
            SetupWall(boxRoot, "LeftWall", new Vector3(-halfWidth - (wallThickness * 0.5f), boxCenterY, 0f), new Vector2(wallThickness, boxHeight), mat);

            // 2. 우측 벽 (Right Wall)
            SetupWall(boxRoot, "RightWall", new Vector3(halfWidth + (wallThickness * 0.5f), boxCenterY, 0f), new Vector2(wallThickness, boxHeight), mat);

            // 3. 바닥 (Bottom Floor)
            SetupWall(boxRoot, "BottomFloor", new Vector3(0f, bottomY - (wallThickness * 0.5f), 0f), new Vector2(boxWidth + (wallThickness * 2f), wallThickness), mat);
        }

        private void SetupWall(GameObject parent, string wallName, Vector3 localPos, Vector2 size, PhysicsMaterial2D mat)
        {
            Transform existing = parent.transform.Find(wallName);
            GameObject wallObj = existing != null ? existing.gameObject : new GameObject(wallName);
            wallObj.transform.SetParent(parent.transform);
            wallObj.transform.localPosition = localPos;

            BoxCollider2D col = wallObj.GetComponent<BoxCollider2D>();
            if (col == null) col = wallObj.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = mat;
        }

        private DeadlineSensor SetupDeadlineSensor(GameObject parent)
        {
            Transform existing = parent.transform.Find("DeadlineSensor");
            GameObject sensorObj = existing != null ? existing.gameObject : new GameObject("DeadlineSensor");
            sensorObj.transform.SetParent(parent.transform);
            sensorObj.transform.localPosition = new Vector3(0f, deadlineOffsetY, 0f);

            BoxCollider2D trigger = sensorObj.GetComponent<BoxCollider2D>();
            if (trigger == null) trigger = sensorObj.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(boxWidth - 0.2f, 0.4f);
            trigger.isTrigger = true;

            DeadlineSensor sensor = sensorObj.GetComponent<DeadlineSensor>();
            if (sensor == null) sensor = sensorObj.AddComponent<DeadlineSensor>();

            return sensor;
        }

        private Spawner SetupCraneSpawner(GameObject spawnerRoot)
        {
            spawnerRoot.transform.position = new Vector3(0f, spawnerOffsetY, 0f);
            Spawner spawner = spawnerRoot.GetComponent<Spawner>();
            if (spawner == null) spawner = spawnerRoot.AddComponent<Spawner>();
            return spawner;
        }

        private void EnsureStageData()
        {
            if (stageData == null)
            {
#if UNITY_EDITOR
                string[] guids = AssetDatabase.FindAssets("t:AnimalStageDataSO");
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    stageData = AssetDatabase.LoadAssetAtPath<AnimalStageDataSO>(path);
                }
#endif
                if (stageData == null)
                {
                    stageData = ScriptableObject.CreateInstance<AnimalStageDataSO>();
                }
            }
        }

        private void WireReferences(GameManager gm, Spawner spawner, DeadlineSensor deadline, FeverController fever, YouTubeEventReceiver receiver, TestInputController tester, MergeObjectPool pool)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // GameManager 참조 설정
                SerializedObject soGM = new SerializedObject(gm);
                soGM.FindProperty("spawner")?.SetObjectReference(spawner);
                soGM.FindProperty("deadlineSensor")?.SetObjectReference(deadline);
                soGM.ApplyModifiedProperties();

                // YouTubeEventReceiver 참조 설정
                SerializedObject soReceiver = new SerializedObject(receiver);
                soReceiver.FindProperty("spawner")?.SetObjectReference(spawner);
                soReceiver.FindProperty("feverController")?.SetObjectReference(fever);
                soReceiver.ApplyModifiedProperties();

                // Spawner 참조 설정
                if (stageData != null)
                {
                    SerializedObject soSpawner = new SerializedObject(spawner);
                    soSpawner.FindProperty("stageData")?.SetObjectReference(stageData);
                    soSpawner.ApplyModifiedProperties();

                    SerializedObject soPool = new SerializedObject(pool);
                    soPool.FindProperty("stageData")?.SetObjectReference(stageData);
                    soPool.ApplyModifiedProperties();
                }

                // TestInputController 참조 설정
                SerializedObject soTester = new SerializedObject(tester);
                soTester.FindProperty("spawner")?.SetObjectReference(spawner);
                soTester.FindProperty("feverController")?.SetObjectReference(fever);
                soTester.FindProperty("eventReceiver")?.SetObjectReference(receiver);
                soTester.FindProperty("gameManager")?.SetObjectReference(gm);
                soTester.ApplyModifiedProperties();

                EditorUtility.SetDirty(gm);
                EditorUtility.SetDirty(spawner);
                EditorUtility.SetDirty(receiver);
                EditorUtility.SetDirty(tester);
            }
#endif
        }

        private void DestroyRootIfExists(string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root);
                }
                else
                {
                    DestroyImmediate(root);
                }
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MemeLab/Generate Management Structure (Except UI)")]
        public static void GenerateFromMenu()
        {
            Generator generator = FindFirstObjectByType<Generator>();
            if (generator == null)
            {
                GameObject genObj = new GameObject("[Generator]");
                generator = genObj.AddComponent<Generator>();
                Undo.RegisterCreatedObjectUndo(genObj, "Create Generator");
            }

            generator.GenerateAllManagers();
            Selection.activeGameObject = generator.gameObject;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Generator))]
    public class GeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Generator generator = (Generator)target;
            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.3f, 1f, 0.4f);
            if (GUILayout.Button("⚡ 매니저 및 물리 구조 자동 생성 (UIManager 제외)", GUILayout.Height(38)))
            {
                generator.GenerateAllManagers();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ 생성된 구조 일괄 정리 (Clear)", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("구조 삭제 확인", "생성된 관리 오브젝트 및 물리 상자를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    generator.ClearGeneratedManagers();
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    internal static class SerializedPropertyExtensions
    {
        public static void SetObjectReference(this SerializedProperty prop, UnityEngine.Object obj)
        {
            if (prop != null)
            {
                prop.objectReferenceValue = obj;
            }
        }
    }
#endif
}
