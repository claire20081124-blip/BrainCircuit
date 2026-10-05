using System.Collections;
using UnityEngine;
using RunLight.Core;

namespace RunLight.Audio
{
    public class BGMManager : Singleton<BGMManager>
    {
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _current;
        private Coroutine   _fadeRoutine;

        protected override void OnAwake()
        {
            _sourceA = MakeSource("BGM_A");
            _sourceB = MakeSource("BGM_B");
            _current = _sourceA;
        }

        private AudioSource MakeSource(string goName)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.playOnAwake  = false;
            src.loop         = true;
            src.volume       = 0f;
            return src;
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
        {
            if (clip == null)            { StopMusic(fadeDuration); return; }
            if (_current.clip == clip && _current.isPlaying) return;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossFade(clip, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOut(_current, fadeDuration));
        }

        private IEnumerator CrossFade(AudioClip clip, float duration)
        {
            var next    = _current == _sourceA ? _sourceB : _sourceA;
            next.clip   = clip;
            next.volume = 0f;
            next.Play();

            float elapsed     = 0f;
            float startVolume = _current.volume;

            while (elapsed < duration)
            {
                elapsed      += Time.unscaledDeltaTime;
                float t       = Mathf.Clamp01(elapsed / duration);
                _current.volume = Mathf.Lerp(startVolume, 0f, t);
                next.volume     = Mathf.Lerp(0f, masterVolume, t);
                yield return null;
            }

            _current.volume = 0f;
            _current.Stop();
            next.volume = masterVolume;
            _current    = next;
        }

        private IEnumerator FadeOut(AudioSource src, float duration)
        {
            float start = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed    += Time.unscaledDeltaTime;
                src.volume  = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }
            src.volume = 0f;
            src.Stop();
        }
    }
}
