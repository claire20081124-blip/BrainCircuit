using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RunLight.Interaction
{
    public class BrainSpawner : MonoBehaviour
    {
        [Header("腦子 Prefab（從 Hierarchy 拖到 Project 建立）")]
        [SerializeField] private GameObject brainPrefab;

        [Header("題目池（隨機抽題）")]
        [SerializeField] private BrainQuestion[] questionPool;

        [Header("壞腦子機率（0~1，0 = 全好腦）")]
        [SerializeField] [Range(0f, 1f)] private float badBrainChance = 0.2f;

        [Header("刷新點（場景中放空物件標記位置）")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("數量與時間")]
        [SerializeField] private int   maxActive     = 3;   // 同時存在的最大數量
        [SerializeField] private float respawnDelay  = 20f; // 被拿走後幾秒重生

        private class Slot
        {
            public Transform  point;
            public GameObject brain;
            public bool       respawning;
        }

        private readonly List<Slot> _slots = new();

        private void Start()
        {
            if (brainPrefab == null)
            {
                Debug.LogWarning("[BrainSpawner] 未設定 Brain Prefab，停止運作");
                return;
            }
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[BrainSpawner] 未設定刷新點");
                return;
            }

            foreach (var p in spawnPoints)
                _slots.Add(new Slot { point = p });

            // 初始生成（最多 maxActive 顆）
            int count = 0;
            foreach (var slot in _slots)
            {
                if (count >= maxActive) break;
                SpawnAt(slot);
                count++;
            }
        }

        private void Update()
        {
            foreach (var slot in _slots)
            {
                if (slot.brain == null && !slot.respawning)
                    StartCoroutine(RespawnRoutine(slot));
            }
        }

        private IEnumerator RespawnRoutine(Slot slot)
        {
            slot.respawning = true;
            yield return new WaitForSeconds(respawnDelay);

            // 超過上限就等下一輪
            int active = 0;
            foreach (var s in _slots)
                if (s.brain != null) active++;

            if (active < maxActive)
                SpawnAt(slot);
            else
                slot.respawning = false;   // 下一幀 Update 會再觸發
        }

        private void SpawnAt(Slot slot)
        {
            if (questionPool == null || questionPool.Length == 0)
            {
                Debug.LogWarning("[BrainSpawner] 題目池是空的");
                slot.respawning = false;
                return;
            }

            var q   = questionPool[Random.Range(0, questionPool.Length)];
            bool bad = Random.value < badBrainChance;

            GameObject go;
            if (brainPrefab != null)
            {
                go = Instantiate(brainPrefab, slot.point.position, slot.point.rotation);
            }
            else
            {
                // 沒有建模時用球體佔位，之後換 Prefab 即可
                go = new GameObject("Brain_Placeholder");
                go.transform.position = slot.point.position;
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh.transform.SetParent(go.transform, false);
                mesh.transform.localScale = Vector3.one * 0.3f;
                // 淡粉紅讓佔位球好辨識
                var mr = mesh.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mr.material.color = new Color(1f, 0.6f, 0.7f);
                }
                go.AddComponent<BrainInteractable>();
            }

            var bi = go.GetComponent<BrainInteractable>();
            if (bi != null) bi.Init(q, bad);

            slot.brain      = go;
            slot.respawning = false;
        }
    }
}
