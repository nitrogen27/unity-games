
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class WolfTargetMaterialSetup
{
    private const string TextureRoot = "Assets/Textures/WolfTargetLook";
    private const string StoneWallTextureRoot = "Assets/Textures/WolfStoneWallWhiteLarge";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";
    private const string WolfRepoMaterialRoot = "Assets/Materials/WolfRepo";

    [MenuItem("Tools/Wolf Target Look/Setup Generated Materials")]
    public static void SetupGeneratedMaterials()
    {
        Directory.CreateDirectory(MaterialRoot);

        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Height.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);

        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/PrisonCellDoor/PrisonCellDoor_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Clamp);

        AssetDatabase.Refresh();

        CreateMaterial(
            "BlueWall_Target",
            $"{TextureRoot}/BlueWall/BlueWall_Target_Albedo.png",
            $"{TextureRoot}/BlueWall/BlueWall_Target_Normal.png",
            $"{TextureRoot}/BlueWall/BlueWall_Target_MetallicSmoothness.png",
            $"{TextureRoot}/BlueWall/BlueWall_Target_AO.png",
            0.0f,
            0.58f
        );
        ApplyBlueWallTargetBrightness();

        CreateMaterial(
            "DoorTeal_Target",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Albedo.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Normal.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_AO.png",
            0.70f,
            0.72f
        );

        CreateMaterial(
            "FloorTile_Target",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_AO.png",
            0.04f,
            0.62f
        );

        CreateMaterial(
            "CeilingPanel_Target",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Albedo.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Normal.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_MetallicSmoothness.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_AO.png",
            0.20f,
            0.38f
        );

        CreateMaterial(
            "DarkMetalTrim_Target",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Albedo.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Normal.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_AO.png",
            0.85f,
            0.68f
        );

        CreateMaterial(
            "PrisonCellDoor_Target",
            $"{TextureRoot}/PrisonCellDoor/PrisonCellDoor_Target_Albedo.png",
            null,
            null,
            null,
            0.60f,
            0.58f
        );

        CreateWhiteStoneWallTileMaterials();

        Debug.Log("Wolf Target Look materials created/updated.");
    }

    private static void SetupTextureImport(string path, TextureImporterType type, bool srgb, TextureWrapMode wrapMode)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Missing texture: {path}");
            return;
        }

        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.wrapMode = wrapMode;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void ApplyBlueWallTargetBrightness()
    {
        string materialPath = $"{MaterialRoot}/BlueWall_Target.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            Debug.LogWarning($"Missing material: {materialPath}");
            return;
        }

        Texture2D albedo = LoadTexture($"{TextureRoot}/BlueWall/BlueWall_Target_Albedo.png");

        SetTextureIfHas(mat, "_EmissionMap", albedo);
        mat.EnableKeyword("_EMISSION");

        SetColorIfHas(mat, "_Color", new Color(1.36f, 1.44f, 1.92f, 1f));
        SetColorIfHas(mat, "_BaseColor", new Color(1.36f, 1.44f, 1.92f, 1f));
        SetColorIfHas(mat, "_EmissionColor", new Color(0.04f, 0.06f, 0.20f, 1f));
        SetFloatIfHas(mat, "_Smoothness", 0.40f);
        SetFloatIfHas(mat, "_Glossiness", 0.40f);
        SetFloatIfHas(mat, "_BumpScale", 0.65f);
        SetFloatIfHas(mat, "_OcclusionStrength", 0.45f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static void CreateWhiteStoneWallTileMaterials()
    {
        Color lightTint = new Color(1.68f, 1.64f, 1.55f, 1f);
        Color darkTint = new Color(1.28f, 1.24f, 1.16f, 1f);
        Color lightEmission = new Color(0.24f, 0.23f, 0.21f, 1f);
        Color darkEmission = new Color(0.16f, 0.15f, 0.14f, 1f);

        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_000", lightTint, lightEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_001", darkTint, darkEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_002", lightTint, lightEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_003", darkTint, darkEmission);
    }

    private static void CreateWhiteStoneWallTileMaterial(string materialName, Color tint, Color emissionTint)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No Standard shader found.");
            return;
        }

        string materialPath = $"{WolfRepoMaterialRoot}/{materialName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }
        else
        {
            mat.shader = shader;
        }

        Texture2D albedo = LoadTexture($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Albedo.png");
        Texture2D normal = LoadTexture($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Normal.png");
        Texture2D occlusion = LoadTexture($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_AO.png");
        Texture2D height = LoadTexture($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Height.png");

        SetTextureIfHas(mat, "_BaseMap", albedo);
        SetTextureIfHas(mat, "_MainTex", albedo);
        SetTextureIfHas(mat, "_BumpMap", normal);
        SetTextureIfHas(mat, "_MetallicGlossMap", null);
        SetTextureIfHas(mat, "_OcclusionMap", occlusion);
        SetTextureIfHas(mat, "_ParallaxMap", height);
        SetTextureIfHas(mat, "_EmissionMap", albedo);

        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_EMISSION");
        mat.DisableKeyword("_METALLICGLOSSMAP");
        mat.DisableKeyword("_METALLICSPECGLOSSMAP");

        SetFloatIfHas(mat, "_Metallic", 0.0f);
        SetFloatIfHas(mat, "_Smoothness", 0.28f);
        SetFloatIfHas(mat, "_Glossiness", 0.28f);
        SetFloatIfHas(mat, "_BumpScale", 0.7f);
        SetFloatIfHas(mat, "_OcclusionStrength", 0.45f);
        SetFloatIfHas(mat, "_Parallax", 0.01f);
        SetFloatIfHas(mat, "_GlossyReflections", 1.0f);
        SetFloatIfHas(mat, "_SpecularHighlights", 1.0f);
        SetColorIfHas(mat, "_Color", tint);
        SetColorIfHas(mat, "_BaseColor", tint);
        SetColorIfHas(mat, "_EmissionColor", emissionTint);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static void CreateMaterial(
        string materialName,
        string albedoPath,
        string normalPath,
        string metallicSmoothnessPath,
        string occlusionPath,
        float fallbackMetallic,
        float fallbackSmoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError("No URP/Lit or Standard shader found.");
            return;
        }

        string materialPath = $"{MaterialRoot}/{materialName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }
        else
        {
            mat.shader = shader;
        }

        Texture2D albedo = LoadTexture(albedoPath);
        Texture2D normal = LoadTexture(normalPath);
        Texture2D metallicSmoothness = LoadTexture(metallicSmoothnessPath);
        Texture2D occlusion = LoadTexture(occlusionPath);

        SetTextureIfHas(mat, "_BaseMap", albedo);
        SetTextureIfHas(mat, "_MainTex", albedo);

        SetTextureIfHas(mat, "_BumpMap", normal);
        if (normal != null)
        {
            mat.EnableKeyword("_NORMALMAP");
        }

        SetTextureIfHas(mat, "_MetallicGlossMap", metallicSmoothness);
        if (metallicSmoothness != null)
        {
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        SetTextureIfHas(mat, "_OcclusionMap", occlusion);

        SetFloatIfHas(mat, "_Metallic", fallbackMetallic);
        SetFloatIfHas(mat, "_Smoothness", fallbackSmoothness);
        SetFloatIfHas(mat, "_Glossiness", fallbackSmoothness);
        SetFloatIfHas(mat, "_BumpScale", 1.0f);
        SetFloatIfHas(mat, "_OcclusionStrength", 1.0f);
        SetFloatIfHas(mat, "_GlossyReflections", 1.0f);
        SetFloatIfHas(mat, "_SpecularHighlights", 1.0f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static Texture2D LoadTexture(string path)
    {
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void SetTextureIfHas(Material mat, string name, Texture tex)
    {
        if (mat.HasProperty(name))
        {
            mat.SetTexture(name, tex);
        }
    }

    private static void SetFloatIfHas(Material mat, string name, float value)
    {
        if (mat.HasProperty(name))
        {
            mat.SetFloat(name, value);
        }
    }

    private static void SetColorIfHas(Material mat, string name, Color value)
    {
        if (mat.HasProperty(name))
        {
            mat.SetColor(name, value);
        }
    }
}
#endif
