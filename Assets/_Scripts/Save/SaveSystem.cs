using System;
using System.IO;
using UnityEngine;

namespace RunLight.Save
{
    /// <summary>
    /// 存檔系統(開發指南 P0)。JSON 序列化,支援多存檔槽。
    /// 檔案位置:Application.persistentDataPath/Saves/save_{slot}.json
    /// 採「先寫暫存檔再覆蓋」的原子寫入,避免寫到一半當機而毀損存檔。
    /// 純靜態工具類別,只負責檔案 IO 與序列化;遊戲狀態由 GameManager 持有。
    /// </summary>
    public static class SaveSystem
    {
        public const int MaxSlots = 3;

        private const string FolderName = "Saves";
        private const string FilePrefix = "save_";
        private const string FileExt = ".json";

        private static string SaveFolder => Path.Combine(Application.persistentDataPath, FolderName);
        private static string PathForSlot(int slot) => Path.Combine(SaveFolder, $"{FilePrefix}{slot}{FileExt}");

        public static bool SlotExists(int slot) => File.Exists(PathForSlot(slot));

        public static void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveSystem] 嘗試存入 null 資料,已略過。");
                return;
            }

            try
            {
                Directory.CreateDirectory(SaveFolder);
                data.savedAtIso = DateTime.Now.ToString("o");

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                string path = PathForSlot(data.slot);
                string tmp = path + ".tmp";

                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);

                Debug.Log($"[SaveSystem] 已存檔到槽 {data.slot}:{path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 存檔失敗(槽 {data.slot}):{e}");
            }
        }

        public static SaveData Load(int slot)
        {
            string path = PathForSlot(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] 槽 {slot} 無存檔。");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] 已從槽 {slot} 讀檔。");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 讀檔失敗(槽 {slot}):{e}");
                return null;
            }
        }

        /// <summary>只讀取摘要(供存讀檔選單列出,不套用到遊戲狀態)。</summary>
        public static SaveData PeekSummary(int slot) => Load(slot);

        public static void Delete(int slot)
        {
            string path = PathForSlot(slot);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log($"[SaveSystem] 已刪除槽 {slot} 的存檔。");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 刪除存檔失敗(槽 {slot}):{e}");
            }
        }
    }
}
