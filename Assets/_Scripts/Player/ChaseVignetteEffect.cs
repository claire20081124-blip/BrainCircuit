using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RunLight.Enemy;

namespace RunLight.Player
{
    // 掛到任何場景物件即可，自動建立 Post Processing Volume
    public class ChaseVignetteEffect : MonoBehaviour
    {
        [SerializeField] private float maxIntensity  = 0.35f;  // 追擊時最大暗角強度
        [SerializeField] private float fadeInSpeed   = 2.5f;   // 變暗速度
        [SerializeField] private float fadeOutSpeed  = 1.2f;   // 恢復速度

        private Vignette _vignette;
        private AISweeperController[] _sweepers;

        private void Start()
        {
            SetupVolume();
            _sweepers = FindObjectsByType<AISweeperController>(FindObjectsSortMode.None);
        }

        private void SetupVolume()
        {
            // 建立全域 Volume 並加入 Vignette override
            var volGo = new GameObject("ChaseVignetteVolume");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value         = 0f;
            _vignette.color.overrideState     = true;
            _vignette.color.value             = Color.black;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value         = 0.4f;

            volume.profile = profile;
        }

        private void Update()
        {
            // 如果清單過期（場景加了新清道夫）就重新找
            if (_sweepers == null || _sweepers.Length == 0)
                _sweepers = FindObjectsByType<AISweeperController>(FindObjectsSortMode.None);

            bool anyChasing = false;
            foreach (var s in _sweepers)
                if (s != null && s.IsChasing) { anyChasing = true; break; }

            float target = anyChasing ? maxIntensity : 0f;
            float speed  = anyChasing ? fadeInSpeed : fadeOutSpeed;
            _vignette.intensity.value = Mathf.MoveTowards(
                _vignette.intensity.value, target, speed * Time.deltaTime);
        }
    }
}
