using UnityEngine;

namespace RunLight.Interaction
{
    /// <summary>
    /// 集中管理每幀的互動提示，解決多個 OnGUI 同時繪製造成疊字。
    /// 在 LateUpdate() 呼叫 Submit()；在 GameHUD.OnGUI() 呼叫 Draw()。
    /// </summary>
    public static class InteractionHint
    {
        static string _hint;
        static string _warning;
        static int    _hintPriority = -1;
        static int    _hintFrame    = -1;
        static int    _warnFrame    = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _hint         = null;
            _warning      = null;
            _hintPriority = -1;
            _hintFrame    = -1;
            _warnFrame    = -1;
        }

        /// <summary>在 LateUpdate 呼叫。每幀只有優先度最高的提示會顯示。</summary>
        public static void Submit(string text, int priority = 0)
        {
            if (_hintFrame != Time.frameCount)
            {
                _hintFrame    = Time.frameCount;
                _hintPriority = -1;
                _hint         = null;
            }
            if (priority > _hintPriority)
            {
                _hintPriority = priority;
                _hint         = text;
            }
        }

        /// <summary>在 LateUpdate 呼叫。紅色警告提示（智力不足等），每幀只顯示最後一個。</summary>
        public static void SubmitWarning(string text)
        {
            _warning   = text;
            _warnFrame = Time.frameCount;
        }

        /// <summary>在 GameHUD.OnGUI() 呼叫，每幀繪製優先度最高的提示。</summary>
        public static void Draw()
        {
            if (UI.CaughtUI.Instance != null && UI.CaughtUI.Instance.IsActive) return;

            if (_hint != null && _hintFrame == Time.frameCount)
            {
                var s = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Color.white }
                };
                GUI.Label(new Rect((Screen.width - 400f) * 0.5f, Screen.height * 0.68f, 400f, 32f), _hint, s);
            }

            if (_warning != null && _warnFrame == Time.frameCount)
            {
                var w = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
                GUI.Label(new Rect((Screen.width - 400f) * 0.5f, Screen.height * 0.63f, 400f, 32f), _warning, w);
            }
        }
    }
}
