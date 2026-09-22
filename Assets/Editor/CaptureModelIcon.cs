using UnityEditor;
using UnityEngine;
using System.IO;

public class CaptureModelIcon : EditorWindow
{
    [MenuItem("Tools/擷取模型圖示 (Model Icon)")]
    static void Open() => GetWindow<CaptureModelIcon>("擷取模型圖示");

    private int    resolution = 256;
    private Color  bgColor    = new Color(0f, 0f, 0f, 0f);   // 透明背景

    void OnGUI()
    {
        GUILayout.Label("先在 Hierarchy 或 Project 選好模型 Prefab，再按擷取", EditorStyles.wordWrappedLabel);
        GUILayout.Space(6);
        resolution = EditorGUILayout.IntField("解析度", resolution);
        bgColor    = EditorGUILayout.ColorField("背景色（透明 = 0 alpha）", bgColor);
        GUILayout.Space(8);

        if (GUILayout.Button("擷取並儲存 PNG", GUILayout.Height(36)))
            Capture();
    }

    void Capture()
    {
        string saveFolder = "Assets/_Art/Icons";
        if (!AssetDatabase.IsValidFolder("Assets/_Art"))
            AssetDatabase.CreateFolder("Assets", "_Art");
        if (!AssetDatabase.IsValidFolder(saveFolder))
            AssetDatabase.CreateFolder("Assets/_Art", "Icons");

        // 優先用 Hierarchy 選取，否則用 Project 選取
        GameObject source = Selection.activeGameObject;
        bool       wasInstantiated = false;

        if (source == null)
        {
            var prefab = Selection.activeObject as GameObject;
            if (prefab == null) { EditorUtility.DisplayDialog("提示", "請先選一個 GameObject 或 Prefab", "OK"); return; }
            source = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            wasInstantiated = true;
        }

        // 把物件移到遠處避免干擾
        source.transform.position = new Vector3(9999f, 9999f, 9999f);

        // 建臨時相機
        var camGo = new GameObject("__IconCam__");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cam.nearClipPlane   = 0.01f;
        cam.farClipPlane    = 1000f;
        cam.orthographic    = false;

        // 讓相機自動框住 Mesh
        var bounds = GetBounds(source);
        float size = bounds.extents.magnitude;
        cam.transform.position = bounds.center + new Vector3(0f, size * 0.4f, -size * 2.2f);
        cam.transform.LookAt(bounds.center);

        // 渲染到 RenderTexture
        var rt  = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        // 讀回 Texture2D
        RenderTexture.active = rt;
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // 清理臨時物件
        DestroyImmediate(camGo);
        rt.Release();
        if (wasInstantiated) DestroyImmediate(source);
        else source.transform.position = Vector3.zero;   // 移回原點

        // 存 PNG
        string name = Selection.activeObject != null ? Selection.activeObject.name : "icon";
        string path = $"{saveFolder}/{name}_icon.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        // 設定為 Sprite
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType  = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("完成", $"已儲存：{path}", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
