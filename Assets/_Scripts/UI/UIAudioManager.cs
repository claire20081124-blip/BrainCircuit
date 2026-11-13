using UnityEngine;
using UnityEngine.UI;

namespace RunLight.UI
{
    /// <summary>
    /// 掛在任一持久物件上。自動監聽場景內所有 Button 的點擊，播放音效。
    /// </summary>
    public class UIAudioManager : MonoBehaviour
    {
        public static UIAudioManager Instance { get; private set; }

        [Header("音效")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        private AudioSource _audio;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake  = false;
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            RegisterAllButtons();
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene s,
                                   UnityEngine.SceneManagement.LoadSceneMode m)
            => RegisterAllButtons();

        private void RegisterAllButtons()
        {
            foreach (var btn in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                btn.onClick.RemoveListener(PlayClick);
                btn.onClick.AddListener(PlayClick);
            }
        }

        private void PlayClick()
        {
            if (clickSound != null) _audio.PlayOneShot(clickSound, volume);
        }

        // 外部呼叫（非 Button 元件也想播音效時）
        public static void Play() => Instance?.PlayClick();
    }
}
