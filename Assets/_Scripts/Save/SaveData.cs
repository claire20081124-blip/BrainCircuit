using System;
using System.Collections.Generic;
using RunLight.Flags;
using RunLight.Identity;

namespace RunLight.Save
{
    /// <summary>
    /// 單一存檔槽的完整資料。所有欄位皆為 JsonUtility 可序列化型別。
    /// 新增欄位時請一併提高 saveVersion,方便日後做存檔升級轉換。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>目前存檔格式版本。</summary>
        public const int CurrentVersion = 1;

        public int slot;
        public int saveVersion = CurrentVersion;
        public string savedAtIso = "";   // 存檔時間 (ISO 8601)
        public float playSeconds;        // 累計遊玩秒數

        public string currentScene = ""; // 目前場景名稱
        public string currentChapter = ""; // 目前章節 / 關卡 id

        public FlagData flags = new();
        public IdentityData identity = new();
        public List<string> collectedShards = new(); // 已收集的記憶碎片 id

        /// <summary>給讀檔 UI 用的單行摘要。</summary>
        public string DisplaySummary()
        {
            int mins = (int)Math.Round(playSeconds / 60f);
            string chapter = string.IsNullOrEmpty(currentChapter) ? currentScene : currentChapter;
            if (string.IsNullOrEmpty(chapter)) chapter = "序章";
            string when = string.IsNullOrEmpty(savedAtIso) ? "—" : savedAtIso;
            return $"槽 {slot} · {chapter} · 遊玩 {mins} 分 · {when}";
        }
    }
}
