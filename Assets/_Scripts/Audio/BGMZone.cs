using UnityEngine;

namespace RunLight.Audio
{
    /// <summary>
    /// 掛在有 Collider (isTrigger) 的物件上。
    /// 玩家進入時換歌，離開時可選擇換回另一首。
    /// 勾選 playOnStart 可讓該場景一開始就播 enterMusic。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BGMZone : MonoBehaviour
    {
        [SerializeField] private AudioClip enterMusic;
        [SerializeField] private AudioClip exitMusic;       // 離開時播；留空 = 不換
        [SerializeField] private float     fadeDuration = 1f;
        [SerializeField] private bool      playOnStart  = false;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;

            if (playOnStart && enterMusic != null)
                BGMManager.Instance.PlayMusic(enterMusic, fadeDuration);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[BGMZone] OnTriggerEnter: {other.name} tag={other.tag}");
            if (!other.CompareTag("Player")) return;
            Debug.Log($"[BGMZone] 換歌 → {(enterMusic != null ? enterMusic.name : "null")}");
            if (enterMusic != null)
                BGMManager.Instance.PlayMusic(enterMusic, fadeDuration);
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[BGMZone] OnTriggerExit: {other.name} tag={other.tag}");
            if (!other.CompareTag("Player")) return;
            Debug.Log($"[BGMZone] 離開換歌 → {(exitMusic != null ? exitMusic.name : "null")}");
            if (exitMusic != null)
                BGMManager.Instance.PlayMusic(exitMusic, fadeDuration);
        }
    }
}
