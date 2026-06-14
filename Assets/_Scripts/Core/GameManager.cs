using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RunLight.Flags;
using RunLight.Identity;
using RunLight.Save;

namespace RunLight.Core
{
    /// <summary>
    /// 遊戲總管:跨場景常駐,持有旗標系統與自我認同值,並負責存讀檔。
    /// 其他系統一律透過 GameManager.Instance 取用這些核心狀態,避免到處散落單例。
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public FlagSystem Flags { get; private set; }
        public IdentityMeter Identity { get; private set; }

        /// <summary>目前使用中的存檔槽。</summary>
        public int CurrentSlot { get; private set; }

        /// <summary>目前章節 / 關卡 id(供存檔與流程判斷)。</summary>
        public string CurrentChapter { get; set; } = "";

        private readonly HashSet<string> _collectedShards = new();
        private float _sessionStartTime;
        private float _accumulatedPlaySeconds;

        /// <summary>讀檔完成後觸發(可用於刷新場景狀態)。</summary>
        public event Action OnGameLoaded;

        /// <summary>存檔完成後觸發。</summary>
        public event Action OnGameSaved;

        protected override void OnAwake()
        {
            Flags = new FlagSystem();
            Identity = new IdentityMeter();
            _sessionStartTime = Time.unscaledTime;
        }

        /// <summary>本次存檔應記錄的累計遊玩秒數。</summary>
        public float PlaySeconds => _accumulatedPlaySeconds + (Time.unscaledTime - _sessionStartTime);

        // ---------- 記憶碎片 ----------
        /// <summary>收集一個記憶碎片;若是新碎片回傳 true。</summary>
        public bool CollectShard(string shardId) => _collectedShards.Add(shardId);

        public bool HasShard(string shardId) => _collectedShards.Contains(shardId);

        public int CollectedShardCount => _collectedShards.Count;

        // ---------- 遊戲流程 ----------
        /// <summary>開始新遊戲,清空所有狀態並綁定到指定存檔槽。</summary>
        public void NewGame(int slot)
        {
            CurrentSlot = slot;
            Flags.Clear();
            Identity.Reset();
            _collectedShards.Clear();
            CurrentChapter = "";
            _accumulatedPlaySeconds = 0f;
            _sessionStartTime = Time.unscaledTime;
        }

        // ---------- 存檔 ----------
        /// <summary>存檔到指定槽(省略則存到目前槽)。</summary>
        public void SaveGame(int? slot = null)
        {
            int targetSlot = slot ?? CurrentSlot;
            var data = new SaveData
            {
                slot = targetSlot,
                currentScene = SceneManager.GetActiveScene().name,
                currentChapter = CurrentChapter,
                playSeconds = PlaySeconds,
                flags = Flags.ToData(),
                identity = Identity.ToData(),
                collectedShards = new List<string>(_collectedShards),
            };

            SaveSystem.Save(data);
            CurrentSlot = targetSlot;
            OnGameSaved?.Invoke();
        }

        // ---------- 讀檔 ----------
        /// <summary>從指定槽讀檔並套用到目前狀態;成功回傳 true。</summary>
        public bool LoadGame(int slot)
        {
            var data = SaveSystem.Load(slot);
            if (data == null) return false;

            ApplySaveData(data);
            CurrentSlot = slot;
            OnGameLoaded?.Invoke();
            return true;
        }

        private void ApplySaveData(SaveData data)
        {
            Flags.LoadFromData(data.flags);
            Identity.LoadFromData(data.identity);

            _collectedShards.Clear();
            foreach (var s in data.collectedShards) _collectedShards.Add(s);

            CurrentChapter = data.currentChapter;
            _accumulatedPlaySeconds = data.playSeconds;
            _sessionStartTime = Time.unscaledTime;
        }

        /// <summary>依目前認同值與碎片收集度判定結局。</summary>
        public Ending DetermineEnding(bool allShardsCollected)
            => Identity.DetermineEnding(allShardsCollected);
    }
}
