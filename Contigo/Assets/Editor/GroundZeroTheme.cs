using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gameplay;

/// <summary>
/// W-1 founding art pass (client repo issue #1): the site's law made physical.
/// Removes the terrain/trees/checker, builds the void floor with brand-token grid lines,
/// the spawn Beacon (flickers until GF-1 is fixed), and the fog void.
/// Idempotent: safe to run again after changes.
/// Menu: GridSchool > Apply Ground Zero Theme. Also callable in batchmode for CI.
/// </summary>
public static class GroundZeroTheme
{
    // site/landing.css tokens
    private static readonly Color Void = Hex("05070C");      // --bg
    private static readonly Color GridBase = Hex("070A11");  // between --bg and --bg2
    private static readonly Color GridLine = Hex("16233D");  // --line
    private static readonly Color Cyan = Hex("28E1FF");      // --cyan

    private const string ScenePath = "Assets/Scenes/MainClientScene.unity";
    private const string MatDir = "Assets/Materials/GroundZero";
    private const float FloorSize = 2000f;   // meters
    private const float CellMeters = 10f;

    // One-shot auto-apply: runs once after this script compiles, then never again
    // (marker file guards it). The menu item remains for deliberate re-runs.
    [InitializeOnLoadMethod]
    private static void AutoApplyOnce()
    {
        string marker = Path.GetFullPath("Library/GroundZeroTheme.applied");
        if (File.Exists(marker)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.WriteAllText(marker, System.DateTime.UtcNow.ToString("o"));
            Apply();
        };
    }

    [MenuItem("GridSchool/Apply Ground Zero Theme")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RemoveTerrain(scene);
        BuildFloor();
        BuildBeacon();
        SetAtmosphere();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GroundZeroTheme] Applied. Press Play: you should spawn on the void grid near the Beacon.");
    }

    private static void RemoveTerrain(Scene scene)
    {
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects().ToArray())
        {
            bool isTerrain = root.name.StartsWith("Terrain") ||
                             root.GetComponentInChildren<Terrain>(true) != null;
            if (isTerrain)
            {
                Object.DestroyImmediate(root);
                removed++;
            }
        }
        RenderSettings.skybox = null;
        Debug.Log($"[GroundZeroTheme] Removed {removed} terrain objects (trees and checker went with them).");
    }

    private static void BuildFloor()
    {
        GameObject old = GameObject.Find("GroundZeroFloor");
        if (old != null) Object.DestroyImmediate(old);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "GroundZeroFloor";
        floor.transform.position = Vector3.zero;                    // top surface at y = 0
        floor.transform.localScale = Vector3.one * (FloorSize / 10f); // Unity plane is 10x10 m

        Material mat = LoadOrCreateMaterial("GridFloor");
        Texture2D grid = LoadOrCreateGridTexture();
        mat.SetTexture("_BaseMap", grid);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.25f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetTexture("_EmissionMap", grid);
        mat.SetColor("_EmissionColor", GridLine * 1.6f);
        Vector2 tiling = Vector2.one * (FloorSize / CellMeters);
        mat.SetTextureScale("_BaseMap", tiling);
        mat.SetTextureScale("_EmissionMap", tiling);

        floor.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void BuildBeacon()
    {
        GameObject old = GameObject.Find("Beacon");
        if (old != null) Object.DestroyImmediate(old);

        var beacon = new GameObject("Beacon");
        beacon.transform.position = new Vector3(0f, 0f, 15f); // clear of the spawn area around origin

        GameObject monolith = GameObject.CreatePrimitive(PrimitiveType.Cube);
        monolith.name = "Monolith";
        monolith.transform.SetParent(beacon.transform, false);
        monolith.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        monolith.transform.localScale = new Vector3(1.6f, 9f, 1.6f);
        Material dark = LoadOrCreateMaterial("BeaconMonolith");
        dark.SetColor("_BaseColor", GridBase);
        dark.SetFloat("_Smoothness", 0.6f);
        monolith.GetComponent<Renderer>().sharedMaterial = dark;

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
        core.name = "Core";
        core.transform.SetParent(beacon.transform, false);
        core.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        core.transform.localScale = new Vector3(0.35f, 8.6f, 1.7f); // blade of light through the monolith
        Object.DestroyImmediate(core.GetComponent<Collider>());
        Material coreMat = LoadOrCreateMaterial("BeaconCore");
        coreMat.SetColor("_BaseColor", Color.black);
        coreMat.EnableKeyword("_EMISSION");
        coreMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        coreMat.SetColor("_EmissionColor", Cyan * 4f);
        core.GetComponent<Renderer>().sharedMaterial = coreMat;

        var lightGo = new GameObject("BeaconLight");
        lightGo.transform.SetParent(beacon.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 5f, 0f);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Cyan;
        light.range = 40f;
        light.intensity = 3f;

        BeaconFlicker flicker = core.AddComponent<BeaconFlicker>();
        SerializedObject so = new SerializedObject(flicker);
        so.FindProperty("beaconLight").objectReferenceValue = light;
        so.FindProperty("coreRenderer").objectReferenceValue = core.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = Void;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("0B1220");

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Void;
        }

        Light sun = Object.FindFirstObjectByType<Light>();
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.intensity = 0.3f;
            sun.color = Hex("AFC8FF");
        }
    }

    private static Material LoadOrCreateMaterial(string name)
    {
        Directory.CreateDirectory(MatDir);
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(lit);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Texture2D LoadOrCreateGridTexture()
    {
        Directory.CreateDirectory(MatDir);
        string path = $"{MatDir}/GridCell.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int size = 256;
        const int line = 3; // px of line at each cell edge
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool isLine = x < line || x >= size - line || y < line || y >= size - line;
            pixels[y * size + x] = isLine ? GridLine : GridBase;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 8;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
