using UnityEngine;
using UnityEditor;

public class AddEmissionToMaterials : EditorWindow
{
    private float emissionIntensity = 0.25f;

    [MenuItem("Tools/Add Emission To All Materials")]
    static void Open() => GetWindow<AddEmissionToMaterials>("Add Emission");

    void OnGUI()
    {
        GUILayout.Label("對 Assets 裡所有材質加上 Emission（底色發光）\n讓室內不靠燈光也有基本亮度", EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);
        emissionIntensity = EditorGUILayout.Slider("Emission 強度", emissionIntensity, 0f, 1f);
        GUILayout.Space(8);
        if (GUILayout.Button("執行", GUILayout.Height(36)))
            Apply(emissionIntensity);
    }

    static void Apply(float intensity)
    {
        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int count = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (!mat.shader.name.Contains("Universal Render Pipeline")) continue;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.white * intensity);
            // Render Face = Both (_Cull 0 = Off = Both sides)
            mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);
            count++;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已對 {count} 個 URP 材質加上 Emission", "OK");
    }
}
