using System;
using System.Collections.Generic;

namespace RunLight.Flags
{
    /// <summary>
    /// 事件旗標系統(開發指南 P0)。
    /// 全域的具名變數儲存,用來追蹤玩家選擇與遊戲進度(例如是否見過某情緒化身、
    /// 某關卡的對話次數、某章節做了什麼選擇)。支援 bool / int / float / string 四種型別。
    /// 純 C# 類別,不依賴 MonoBehaviour,方便單元測試,並可被存檔系統序列化。
    /// </summary>
    public class FlagSystem
    {
        private readonly Dictionary<string, bool> _bools = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly Dictionary<string, float> _floats = new();
        private readonly Dictionary<string, string> _strings = new();

        /// <summary>任一旗標變動時觸發,參數為旗標名稱。</summary>
        public event Action<string> OnFlagChanged;

        // ---------- bool ----------
        public void SetBool(string key, bool value)
        {
            _bools[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        public bool GetBool(string key, bool fallback = false)
            => _bools.TryGetValue(key, out var v) ? v : fallback;

        // ---------- int ----------
        public void SetInt(string key, int value)
        {
            _ints[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        public int GetInt(string key, int fallback = 0)
            => _ints.TryGetValue(key, out var v) ? v : fallback;

        /// <summary>對整數旗標加值並回傳新值(找不到時以 0 為基底)。</summary>
        public int AddInt(string key, int delta)
        {
            int v = GetInt(key) + delta;
            SetInt(key, v);
            return v;
        }

        // ---------- float ----------
        public void SetFloat(string key, float value)
        {
            _floats[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        public float GetFloat(string key, float fallback = 0f)
            => _floats.TryGetValue(key, out var v) ? v : fallback;

        // ---------- string ----------
        public void SetString(string key, string value)
        {
            _strings[key] = value;
            OnFlagChanged?.Invoke(key);
        }

        public string GetString(string key, string fallback = "")
            => _strings.TryGetValue(key, out var v) ? v : fallback;

        // ---------- 共用 ----------
        /// <summary>是否存在任一型別、此名稱的旗標。</summary>
        public bool HasFlag(string key)
            => _bools.ContainsKey(key) || _ints.ContainsKey(key)
            || _floats.ContainsKey(key) || _strings.ContainsKey(key);

        public void Clear()
        {
            _bools.Clear();
            _ints.Clear();
            _floats.Clear();
            _strings.Clear();
        }

        // ---------- 存檔轉換 ----------
        public FlagData ToData()
        {
            var data = new FlagData();
            foreach (var kv in _bools) data.bools.Add(new FlagData.BoolEntry { key = kv.Key, value = kv.Value });
            foreach (var kv in _ints) data.ints.Add(new FlagData.IntEntry { key = kv.Key, value = kv.Value });
            foreach (var kv in _floats) data.floats.Add(new FlagData.FloatEntry { key = kv.Key, value = kv.Value });
            foreach (var kv in _strings) data.strings.Add(new FlagData.StringEntry { key = kv.Key, value = kv.Value });
            return data;
        }

        public void LoadFromData(FlagData data)
        {
            Clear();
            if (data == null) return;
            foreach (var e in data.bools) _bools[e.key] = e.value;
            foreach (var e in data.ints) _ints[e.key] = e.value;
            foreach (var e in data.floats) _floats[e.key] = e.value;
            foreach (var e in data.strings) _strings[e.key] = e.value;
        }
    }
}
