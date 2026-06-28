using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RunLight.UI;

namespace RunLight.Editor
{
    /// <summary>
    /// 一鍵建立 Prologue 開場場景並加入 Build Settings。
    /// 選單位置：腦迴路 ▸ 建立 Prologue 開場場景
    /// </summary>
    public static class PrologueSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/Prologue.unity";

        [MenuItem("腦迴路/建立 Prologue 開場場景")]
        public static void CreateScene()
        {
            // 確保目錄存在
            var dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 建立空場景（Additive 模式，不關閉目前場景）
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(newScene);

            // 建立 PrologueController 物件
            var go = new GameObject("PrologueController");
            go.AddComponent<Prologue>();

            // 儲存場景
            EditorSceneManager.SaveScene(newScene, ScenePath);
            AssetDatabase.Refresh();

            // 加入 Build Settings（若尚未存在則插入最前面）
            var buildScenes = EditorBuildSettings.scenes.ToList();
            bool alreadyAdded = buildScenes.Any(s => s.path == ScenePath);
            if (!alreadyAdded)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log("[腦迴路] 已將 Prologue 場景插入 Build Settings（索引 0）。");
            }

            Debug.Log($"[腦迴路] Prologue 場景已建立：{ScenePath}");
            EditorUtility.DisplayDialog(
                "建立完成",
                $"Prologue 開場場景已建立並加入 Build Settings。\n\n路徑：{ScenePath}\n\n" +
                "請確認 MainMenu 場景的「Game Scene Name」欄位已設為 Prologue。",
                "OK");
        }

        [MenuItem("腦迴路/建立 Prologue 開場場景", validate = true)]
        public static bool ValidateCreateScene()
        {
            // 若場景已存在，選單仍可用（允許重建）
            return true;
        }
    }
}
