using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunLight.Interaction
{
    public class BrainSpawner : MonoBehaviour
    {
        [Header("腦子 Prefab（留空則用佔位球體）")]
        [SerializeField] private GameObject brainPrefab;

        [Header("題目池（隨機抽題）")]
        [SerializeField] private BrainQuestion[] questionPool;

        [Header("壞腦子機率（0~1，0 = 全好腦）")]
        [SerializeField] [Range(0f, 1f)] private float badBrainChance = 0.2f;

        [Header("刷新點（場景中放空物件標記位置）")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("數量與時間")]
        [SerializeField] private int   maxActive    = 3;
        [SerializeField] private float respawnDelay = 20f;

        // 每個刷新點對應的當前腦子（null = 空閒）
        private GameObject[] _brains;

        private void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[BrainSpawner] 未設定刷新點"); return;
            }

            _brains = new GameObject[spawnPoints.Length];

            // 隨機挑 maxActive 個點初始生成
            var freeIndices = GetFreeIndices();
            Shuffle(freeIndices);
            int count = Mathf.Min(maxActive, freeIndices.Count);
            for (int i = 0; i < count; i++)
                SpawnAt(freeIndices[i]);
        }

        private void Update()
        {
            if (_brains == null) return;

            for (int i = 0; i < _brains.Length; i++)
            {
                // 腦子被拿走（Destroy）後 Unity 會把 reference 變成 fake-null
                if (_brains[i] != null && !_brains[i]) _brains[i] = null;
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            // 數量未達上限才生成
            if (CountActive() >= maxActive) yield break;

            var free = GetFreeIndices();
            if (free.Count == 0) yield break;

            SpawnAt(free[Random.Range(0, free.Count)]);
        }

        private void SpawnAt(int index)
        {
            if (questionPool == null || questionPool.Length == 0)
            {
                Debug.LogWarning("[BrainSpawner] 題目池是空的"); return;
            }

            var q   = questionPool[Random.Range(0, questionPool.Length)];
            bool bad = Random.value < badBrainChance;

            GameObject go;
            if (brainPrefab != null)
            {
                go = Instantiate(brainPrefab, spawnPoints[index].position, spawnPoints[index].rotation);
            }
            else
            {
                go = new GameObject("Brain_Placeholder");
                go.transform.position = spawnPoints[index].position;

                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh.transform.SetParent(go.transform, false);
                mesh.transform.localScale = Vector3.one * 1.5f;
                var mr = mesh.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(mr.sharedMaterial);
                    Color c = bad ? new Color(1f, 0.3f, 0.3f) : new Color(0.6f, 0.8f, 1f);
                    mat.SetColor("_BaseColor", c);
                    mat.SetColor("_Color",     c);   // 相容非 URP
                    mr.material = mat;
                }

                go.AddComponent<BrainInteractable>();
            }

            var bi = go.GetComponent<BrainInteractable>();
            if (bi != null) bi.Init(q, bad);

            _brains[index] = go;

            // 腦子被拿走後啟動重生
            StartCoroutine(WatchAndRespawn(index));
        }

        private IEnumerator WatchAndRespawn(int index)
        {
            // 等到該格腦子消失
            while (_brains[index] != null && _brains[index])
                yield return null;

            _brains[index] = null;
            yield return RespawnRoutine();
        }

        private int CountActive()
        {
            int n = 0;
            foreach (var b in _brains)
                if (b != null && b) n++;
            return n;
        }

        private List<int> GetFreeIndices()
        {
            var list = new List<int>();
            for (int i = 0; i < _brains.Length; i++)
                if (_brains[i] == null || !_brains[i]) list.Add(i);
            return list;
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
