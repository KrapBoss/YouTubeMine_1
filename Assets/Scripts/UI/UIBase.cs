using UnityEngine;

namespace MemeLab.UI
{
    public class UIBase : MonoBehaviour
    {
        public string UIName { get; private set; }

        public virtual void Init(string name)
        {
            UIName = name;
        }

        public virtual void OnShow() { }
        public virtual void OnClose() { }
    }
}
