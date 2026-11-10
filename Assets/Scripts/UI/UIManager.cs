using System.Collections.Generic;
using UnityEngine;
using MemeLab.Utility;

namespace MemeLab.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Stack<UIBase> uiStack = new Stack<UIBase>();
        
        [SerializeField] private Transform uiRoot; // UI 생성 시 부모가 될 캔버스 트랜스폼

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public UIBase Show(string name)
        {
            // Resources/UI/name 경로에서 로드한다고 가정
            GameObject prefab = ResourceManager.Instance.Load<GameObject>($"UI/{name}");
            if (prefab == null) return null;

            GameObject uiObj = Instantiate(prefab, uiRoot);
            UIBase uiBase = uiObj.GetComponent<UIBase>();
            
            if (uiBase == null)
            {
                Debug.LogError($"[UIManager] {name} 프리팹에 UIBase 컴포넌트가 없습니다.");
                Destroy(uiObj);
                return null;
            }

            uiBase.Init(name);
            uiBase.OnShow();
            uiStack.Push(uiBase);

            return uiBase;
        }

        public void Close()
        {
            if (uiStack.Count > 0)
            {
                UIBase topUI = uiStack.Pop();
                topUI.OnClose();
                Destroy(topUI.gameObject);
            }
        }

        public void Close(string name)
        {
            if (uiStack.Count == 0) return;

            // 스택의 최상단이면 바로 Close
            if (uiStack.Peek().UIName == name)
            {
                Close();
                return;
            }

            // 스택 내부에서 특정 이름의 UI를 찾아 제거
            Stack<UIBase> tempStack = new Stack<UIBase>();
            bool found = false;

            while (uiStack.Count > 0)
            {
                UIBase ui = uiStack.Pop();
                if (!found && ui.UIName == name)
                {
                    ui.OnClose();
                    Destroy(ui.gameObject);
                    found = true;
                }
                else
                {
                    tempStack.Push(ui);
                }
            }

            // 임시 스택의 항목들을 다시 원본 스택으로 복구
            while (tempStack.Count > 0)
            {
                uiStack.Push(tempStack.Pop());
            }

            if (!found)
            {
                Debug.LogWarning($"[UIManager] {name} 이름의 UI를 찾을 수 없습니다.");
            }
        }
    }
}
