using UnityEditor;
using UnityEngine;

public class SetRenderFaceBoth : EditorWindow
{
    private string _folderPath = "Assets/_Art/建模";

    [MenuItem("Tools/Set Render Face = Both")]
    static void Open() => GetWindow<SetRenderFaceBoth>("Render Face Both");

    void OnGUI()
    {
        GUILayout.Label("把指定資料夾內所有材質 Render Face 改成 Both", EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);
        _folderPath = EditorGUILayout.TextField("資料夾路徑", _folderPath);
        GUILayout.Space(8);

        if (GUILayout.Button("執行", GUILayout.Height(36)))
            Apply();
    }

    void Apply()
    {
        var guids = AssetDatabase.FindAssets("t:Material", new[] { _folderPath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("完成", "沒找到材質", "OK");
            return;
        }

        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // URP Lit shader：_Cull 0 = Both, 1 = Back, 2 = Front
            if (mat.HasProperty("_Cull"))
            {
                mat.SetFloat("_Cull", 0f);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已修改 {count} 個材質", "OK");
    }
}
