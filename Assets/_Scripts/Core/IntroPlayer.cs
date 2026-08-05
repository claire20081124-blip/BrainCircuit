using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace RunLight.Core
{
    public class IntroPlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private string nextScene = "場景";

        private void Start()
        {
            videoPlayer.loopPointReached += OnVideoEnd;
        }

        private void OnVideoEnd(VideoPlayer vp)
        {
            SceneManager.LoadScene(nextScene);
        }

        private void Update()
        {
            // 按任意鍵跳過
            if (Input.anyKeyDown)
                SceneManager.LoadScene(nextScene);
        }
    }
}
