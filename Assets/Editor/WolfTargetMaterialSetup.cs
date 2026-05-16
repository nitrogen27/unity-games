
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class WolfTargetMaterialSetup
{
    private const string TextureRoot = "Assets/Textures/WolfTargetLook";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";

    [MenuItem("Tools/Wolf Target Look/Setup Generated Materials")]
    public static void SetupGeneratedMaterials()
    {
        Directory.CreateDirectory(MaterialRoot);

        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/BlueWall/BlueWall_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

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
            0.50f
        );

        CreateMaterial(
            "DoorTeal_Target",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Albedo.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Normal.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_AO.png",
            0.70f,
            0.55f
        );

        CreateMaterial(
            "FloorTile_Target",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_AO.png",
            0.0f,
            0.35f
        );

        CreateMaterial(
            "CeilingPanel_Target",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Albedo.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Normal.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_MetallicSmoothness.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_AO.png",
            0.20f,
            0.25f
        );

        CreateMaterial(
            "DarkMetalTrim_Target",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Albedo.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Normal.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_AO.png",
            0.85f,
            0.45f
        );

        CreateMaterial(
            "PrisonCellDoor_Target",
            $"{TextureRoot}/PrisonCellDoor/PrisonCellDoor_Target_Albedo.png",
            null,
            null,
            null,
            0.60f,
            0.45f
        );

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
}
#endif
