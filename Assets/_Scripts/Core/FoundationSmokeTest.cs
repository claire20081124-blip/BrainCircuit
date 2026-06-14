using UnityEngine;
using RunLight.Identity;
using RunLight.Save;

namespace RunLight.Core
{
    /// <summary>
    /// 地基系統煙霧測試。把此元件掛在場景中任一 GameObject 上,進入 Play 模式會自動
    /// 跑一輪「旗標 / 認同值 / 碎片 / 結局判定 / 存讀檔」的驗證,並把結果輸出到 Console。
    /// 也可在 Inspector 元件右上角選單手動觸發。驗證完成後正式版可移除此檔。
    /// </summary>
    public class FoundationSmokeTest : MonoBehaviour
    {
        [Tooltip("進入 Play 模式時自動執行測試")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("測試用的存檔槽")]
        [SerializeField] private int testSlot = 0;

        private void Start()
        {
            if (runOnStart) RunAll();
        }

        [ContextMenu("執行全部測試")]
        public void RunAll()
        {
            var gm = GameManager.Instance;
            Debug.Log("===== RunLight 地基系統測試開始 =====");

            // 1. 旗標系統
            gm.NewGame(testSlot);
            gm.Flags.SetBool("met_grief_shadow", true);
            gm.Flags.SetInt("dialogue_count", 3);
            gm.Flags.AddInt("dialogue_count", 2);
            gm.Flags.SetString("player_choice_ch1", "embrace");
            Debug.Log($"[旗標] met_grief_shadow={gm.Flags.GetBool("met_grief_shadow")}(應 True), " +
                      $"dialogue_count={gm.Flags.GetInt("dialogue_count")}(應 5), " +
                      $"choice={gm.Flags.GetString("player_choice_ch1")}(應 embrace)");

            // 2. 自我認同值
            gm.Identity.Add(EmotionDimension.Grief, 60f);
            gm.Identity.Add(EmotionDimension.Anger, 55f);
            gm.Identity.Add(EmotionDimension.Joy, 50f);
            gm.Identity.Add(EmotionDimension.Bliss, 45f);
            gm.Identity.Add(EmotionDimension.Self, 40f);
            Debug.Log($"[認同值] 總和={gm.Identity.Total}(應 250), 正規化={gm.Identity.Normalized:0.00}(應 0.50)");

            // 3. 記憶碎片(重複收集不應重複計算)
            gm.CollectShard("ch1_shard_01");
            gm.CollectShard("ch1_shard_02");
            gm.CollectShard("ch1_shard_01");
            Debug.Log($"[碎片] 數量={gm.CollectedShardCount}(應 2)");

            // 4. 結局判定
            Debug.Log($"[結局] 未全收集={gm.DetermineEnding(false)}(應 Daybreak), " +
                      $"全收集={gm.DetermineEnding(true)}(應 Daybreak,因比例 0.50 未達 Verso 門檻 0.80)");

            // 5. 存檔 → 竄改 → 讀檔還原
            gm.SaveGame(testSlot);
            gm.Flags.SetInt("dialogue_count", 999);
            gm.Identity.Set(EmotionDimension.Self, 100f);
            bool loaded = gm.LoadGame(testSlot);
            Debug.Log($"[存讀檔] 讀檔成功={loaded}, " +
                      $"還原 dialogue_count={gm.Flags.GetInt("dialogue_count")}(應 5), " +
                      $"還原 Self={gm.Identity.Get(EmotionDimension.Self)}(應 40)");

            Debug.Log($"[存檔位置] {Application.persistentDataPath}/Saves/");
            Debug.Log("===== 測試結束:若以上括號內「應」的值全部符合即為通過 =====");
        }

        [ContextMenu("刪除測試存檔")]
        public void DeleteTestSave() => SaveSystem.Delete(testSlot);
    }
}
