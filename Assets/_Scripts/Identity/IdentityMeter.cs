using System;
using System.Collections.Generic;

namespace RunLight.Identity
{
    /// <summary>
    /// 自我認同值系統(企劃書 5.2、開發指南 P1)。
    /// 五個面向(哀/怒/樂/喜/自我)各自累積一個隱性數值,全程累積。
    /// 依設計「不顯示具體數字」,改以程熠外觀的微妙變化呈現;數值僅供內部邏輯與結局判定。
    /// 純 C# 類別,方便單元測試,並可被存檔系統序列化。
    /// </summary>
    public class IdentityMeter
    {
        /// <summary>每個面向數值的建議上限(用於正規化與結局門檻計算)。</summary>
        public const float MaxPerDimension = 100f;

        // 結局門檻(以正規化比例 0~1 判定,可依測試調整)。
        public const float DaybreakThreshold = 0.5f;
        public const float VersoThreshold = 0.8f;

        private readonly Dictionary<EmotionDimension, float> _values = new();

        /// <summary>某面向數值變動時觸發 (面向, 新值)。可用於即時更新角色外觀表現。</summary>
        public event Action<EmotionDimension, float> OnDimensionChanged;

        public IdentityMeter()
        {
            foreach (EmotionDimension d in Enum.GetValues(typeof(EmotionDimension)))
                _values[d] = 0f;
        }

        public float Get(EmotionDimension dimension)
            => _values.TryGetValue(dimension, out var v) ? v : 0f;

        /// <summary>設定面向數值(下限 0,上限 MaxPerDimension)。</summary>
        public void Set(EmotionDimension dimension, float value)
        {
            value = Math.Clamp(value, 0f, MaxPerDimension);
            _values[dimension] = value;
            OnDimensionChanged?.Invoke(dimension, value);
        }

        /// <summary>對面向數值加值(可為負)。</summary>
        public void Add(EmotionDimension dimension, float delta)
            => Set(dimension, Get(dimension) + delta);

        /// <summary>所有面向的總和。</summary>
        public float Total
        {
            get
            {
                float sum = 0f;
                foreach (var v in _values.Values) sum += v;
                return sum;
            }
        }

        /// <summary>整體認同比例 0~1(總和 ÷ 面向數 ÷ 每面向上限)。</summary>
        public float Normalized
        {
            get
            {
                int count = _values.Count;
                if (count == 0) return 0f;
                return Math.Clamp(Total / (count * MaxPerDimension), 0f, 1f);
            }
        }

        /// <summary>依目前認同比例與是否全收集記憶碎片,判定結局走向。</summary>
        public Ending DetermineEnding(bool allShardsCollected)
        {
            float ratio = Normalized;
            if (ratio >= VersoThreshold && allShardsCollected) return Ending.Verso;
            if (ratio >= DaybreakThreshold) return Ending.Daybreak;
            return Ending.Glimmer;
        }

        public void Reset()
        {
            foreach (EmotionDimension d in Enum.GetValues(typeof(EmotionDimension)))
                Set(d, 0f);
        }

        // ---------- 存檔轉換 ----------
        public IdentityData ToData()
        {
            var data = new IdentityData();
            foreach (var kv in _values)
                data.entries.Add(new IdentityData.Entry { dimension = kv.Key, value = kv.Value });
            return data;
        }

        public void LoadFromData(IdentityData data)
        {
            Reset();
            if (data == null) return;
            foreach (var e in data.entries) Set(e.dimension, e.value);
        }
    }
}
