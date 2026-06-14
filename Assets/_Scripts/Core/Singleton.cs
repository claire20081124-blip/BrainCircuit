using UnityEngine;

namespace RunLight.Core
{
    /// <summary>
    /// 泛型 MonoBehaviour 單例基底,跨場景常駐(DontDestroyOnLoad)。
    /// 用法:public class GameManager : Singleton&lt;GameManager&gt; { }
    /// 子類別請覆寫 OnAwake() 做初始化,而非 Awake()。
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        /// <summary>取得單例。若場景中不存在會自動建立一個。</summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>是否已有有效實例(避免在結束遊戲時意外重建)。</summary>
        public static bool HasInstance => _instance != null && !_isQuitting;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // 已存在實例(例如重新載入場景),銷毀重複者。
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            transform.SetParent(null); // DontDestroyOnLoad 只對根物件有效
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        /// <summary>子類別覆寫此方法做初始化(取代 Awake)。</summary>
        protected virtual void OnAwake() { }

        protected virtual void OnApplicationQuit() => _isQuitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
