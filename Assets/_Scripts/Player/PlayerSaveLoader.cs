using UnityEngine;
using RunLight.Core;

namespace RunLight.Player
{
    public class PlayerSaveLoader : MonoBehaviour
    {
        private void Start()
        {
            if (!GameManager.HasInstance) return;
            if (!GameManager.Instance.HasPendingSpawn) return;

            transform.position    = GameManager.Instance.PendingSpawnPos;
            transform.eulerAngles = new Vector3(0f, GameManager.Instance.PendingSpawnRotY, 0f);
            GameManager.Instance.ClearPendingSpawn();
        }
    }
}
