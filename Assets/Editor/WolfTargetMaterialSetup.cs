
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
    private const string WhiteStoneWallTargetName = "WhiteStoneWall_Target";
    private const string WhiteStoneWallDarkTargetName = "WhiteStoneWall_Dark_Target";
    private const int SurfaceTextureAnisoLevel = 8;

    [MenuItem("Tools/Wolf Target Look/Setup Generated Materials")]
    public static void SetupGeneratedMaterials()
    {
        Directory.CreateDirectory(MaterialRoot);

        PolishAngleGlossMaps();
        SetupBlueWallTargetTextureImports();

        SetupWhiteStoneWallTextureImports();
        SetupDetailedBannerTextureImports();

        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{TextureRoot}/DoorTeal/DoorTeal_Target_Height.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);

        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/FloorTile/FloorTile_Target_Height.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Height.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);

        SetupTextureImport($"{TextureRoot}/PrisonCellDoor/PrisonCellDoor_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Clamp);

        AssetDatabase.Refresh();

        CreateBlueWallTargetMaterial();
        CreateWhiteStoneWallTargetMaterials();
        ConfigureDetailedBannerMaterial();

        CreateMaterial(
            "DoorTeal_Target",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Albedo.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Normal.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_AO.png",
            0.34f,
            0.54f
        );
        ConfigureDoorTargetMaterial();

        CreateMaterial(
            "FloorTile_Target",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png",
            $"{TextureRoot}/FloorTile/FloorTile_Target_AO.png",
            0.0f,
            0.14f
        );
        ConfigureFloorTileMaterial();

        CreateMaterial(
            "CeilingPanel_Target",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Albedo.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Normal.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_MetallicSmoothness.png",
            $"{TextureRoot}/CeilingPanel/CeilingPanel_Target_AO.png",
            0.0f,
            0.16f
        );
        ConfigureCeilingPanelVisibility();

        CreateMaterial(
            "DarkMetalTrim_Target",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Albedo.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_Normal.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_MetallicSmoothness.png",
            $"{TextureRoot}/DarkMetalTrim/DarkMetalTrim_Target_AO.png",
            0.34f,
            0.38f
        );

        CreateMaterial(
            "PrisonCellDoor_Target",
            $"{TextureRoot}/PrisonCellDoor/PrisonCellDoor_Target_Albedo.png",
            null,
            null,
            null,
            0.18f,
            0.30f
        );

        CreateWhiteStoneWallTileMaterials();

        Debug.Log("Wolf Target Look materials created/updated.");
    }

    [MenuItem("Tools/Wolf Target Look/Polish Angle Gloss Maps")]
    public static void PolishAngleGlossMaps()
    {
        WriteMetallicSmoothnessFromRoughness(
            $"{TextureRoot}/BlueWall/BlueWall_Target_Roughness.png",
            $"{TextureRoot}/BlueWall/BlueWall_Target_Metallic.png",
            $"{TextureRoot}/BlueWall/BlueWall_Target_MetallicSmoothness.png",
            0.08f,
            1.00f,
            0.54f,
            0.82f);

        WriteMetallicSmoothnessFromRoughness(
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Roughness.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_Metallic.png",
            $"{TextureRoot}/DoorTeal/DoorTeal_Target_MetallicSmoothness.png",
            0.46f,
            0.58f,
            0.50f,
            0.74f);

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Wolf Target Look/Setup Blue Wall Target Material")]
    public static void SetupBlueWallTargetMaterial()
    {
        Directory.CreateDirectory(MaterialRoot);
        SetupBlueWallTargetTextureImports();
        AssetDatabase.Refresh();
        CreateBlueWallTargetMaterial();

        Debug.Log("BlueWall_Target material created/updated.");
    }

    [MenuItem("Tools/Wolf Target Look/Setup White Stone Wall Target Material")]
    public static void SetupWhiteStoneWallTargetMaterial()
    {
        Directory.CreateDirectory(MaterialRoot);
        SetupWhiteStoneWallTextureImports();
        AssetDatabase.Refresh();
        CreateWhiteStoneWallTargetMaterials();
        CreateWhiteStoneWallTileMaterials();

        Debug.Log("White stone wall target materials created/updated.");
    }

    [MenuItem("Tools/Wolf Target Look/Setup Detailed Atrium Banner Material")]
    public static void SetupDetailedAtriumBannerMaterial()
    {
        SetupDetailedBannerTextureImports();
        AssetDatabase.Refresh();
        ConfigureDetailedBannerMaterial();

        Debug.Log("Detailed atrium banner material created/updated.");
    }

    private static void SetupBlueWallTargetTextureImports()
    {
        string root = $"{TextureRoot}/BlueWall";

        SetupTextureImport($"{root}/BlueWall_Target_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_Height.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_Metallic.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{root}/BlueWall_Target_Roughness.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
    }

    private static void SetupWhiteStoneWallTextureImports()
    {
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Albedo.png", TextureImporterType.Default, true, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_AO.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
        SetupTextureImport($"{StoneWallTextureRoot}/StoneWall_WhiteLarge_Height.png", TextureImporterType.Default, false, TextureWrapMode.Repeat);
    }

    private static void SetupDetailedBannerTextureImports()
    {
        string root = $"{TextureRoot}/AtriumDecor";
        SetupTextureImport($"{root}/AtriumBanner_Detailed_Cutout.png", TextureImporterType.Default, true, TextureWrapMode.Clamp);
        SetupTextureImport($"{root}/AtriumBanner_Detailed_Normal.png", TextureImporterType.NormalMap, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{root}/AtriumBanner_Detailed_MetallicSmoothness.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);
        SetupTextureImport($"{root}/AtriumBanner_Detailed_AO.png", TextureImporterType.Default, false, TextureWrapMode.Clamp);

        var cutoutImporter = AssetImporter.GetAtPath($"{root}/AtriumBanner_Detailed_Cutout.png") as TextureImporter;
        if (cutoutImporter != null && !cutoutImporter.alphaIsTransparency)
        {
            cutoutImporter.alphaIsTransparency = true;
            cutoutImporter.SaveAndReimport();
        }
    }

    private static void SetupTextureImport(
        string path,
        TextureImporterType type,
        bool srgb,
        TextureWrapMode wrapMode,
        TextureImporterCompression compression = TextureImporterCompression.CompressedHQ)
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
        importer.anisoLevel = SurfaceTextureAnisoLevel;
        importer.alphaIsTransparency = false;
        importer.textureCompression = compression;
        importer.SaveAndReimport();
    }

    private static void CreateBlueWallTargetMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No Standard shader found.");
            return;
        }

        string materialPath = $"{MaterialRoot}/BlueWall_Target.mat";
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

        string root = $"{TextureRoot}/BlueWall";
        Texture2D albedo = LoadTexture($"{root}/BlueWall_Target_Albedo.png");
        Texture2D normal = LoadTexture($"{root}/BlueWall_Target_Normal.png");
        Texture2D occlusion = LoadTexture($"{root}/BlueWall_Target_AO.png");
        Texture2D metallicSmoothness = LoadTexture($"{root}/BlueWall_Target_MetallicSmoothness.png");
        Texture2D height = LoadTexture($"{root}/BlueWall_Target_Height.png");

        SetTextureIfHas(mat, "_MainTex", albedo);
        SetTextureIfHas(mat, "_BumpMap", normal);
        SetTextureIfHas(mat, "_OcclusionMap", occlusion);
        SetTextureIfHas(mat, "_MetallicGlossMap", metallicSmoothness);
        SetTextureIfHas(mat, "_ParallaxMap", height);
        SetTextureIfHas(mat, "_EmissionMap", null);

        SetColorIfHas(mat, "_Color", new Color(0.70f, 0.75f, 1.00f, 1f));
        SetColorIfHas(mat, "_EmissionColor", Color.black);
        SetFloatIfHas(mat, "_Mode", 0.0f);
        SetFloatIfHas(mat, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfHas(mat, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        SetFloatIfHas(mat, "_ZWrite", 1.0f);
        SetFloatIfHas(mat, "_Metallic", 0.0f);
        SetFloatIfHas(mat, "_Smoothness", 0.62f);
        SetFloatIfHas(mat, "_Glossiness", 0.62f);
        SetFloatIfHas(mat, "_GlossMapScale", 0.86f);
        SetFloatIfHas(mat, "_SmoothnessTextureChannel", 0.0f);
        SetFloatIfHas(mat, "_BumpScale", 0.38f);
        SetFloatIfHas(mat, "_OcclusionStrength", 0.20f);
        SetFloatIfHas(mat, "_Parallax", 0.0f);
        SetFloatIfHas(mat, "_GlossyReflections", 1.0f);
        SetFloatIfHas(mat, "_SpecularHighlights", 0.92f);

        SetKeyword(mat, "_NORMALMAP", normal != null);
        SetKeyword(mat, "_METALLICGLOSSMAP", metallicSmoothness != null);
        SetKeyword(mat, "_PARALLAXMAP", false);
        SetKeyword(mat, "_EMISSION", false);
        ConfigureStandardReflectionKeywords(mat, 1.0f, 0.92f);
        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.DisableKeyword("_SPECGLOSSMAP");

        mat.renderQueue = -1;
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static void CreateWhiteStoneWallTileMaterials()
    {
        Color lightTint = new Color(0.92f, 0.90f, 0.86f, 1f);
        Color lightEmission = Color.black;

        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_000", lightTint, lightEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_001", lightTint, lightEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_002", lightTint, lightEmission);
        CreateWhiteStoneWallTileMaterial("Mat_WolfRepo_WallTile_003", lightTint, lightEmission);
    }

    private static void CreateWhiteStoneWallTargetMaterials()
    {
        CreateWhiteStoneMaterial(
            $"{MaterialRoot}/{WhiteStoneWallTargetName}.mat",
            new Color(0.92f, 0.90f, 0.86f, 1f),
            Color.black);

        CreateWhiteStoneMaterial(
            $"{MaterialRoot}/{WhiteStoneWallDarkTargetName}.mat",
            new Color(0.82f, 0.80f, 0.76f, 1f),
            Color.black);
    }

    private static void ConfigureDetailedBannerMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/AtriumBannerWall_Target.mat");
        if (mat == null)
        {
            Debug.LogWarning("Missing AtriumBannerWall_Target material.");
            return;
        }

        string root = $"{TextureRoot}/AtriumDecor";
        Texture2D albedo = LoadTexture($"{root}/AtriumBanner_Detailed_Cutout.png");
        Texture2D normal = LoadTexture($"{root}/AtriumBanner_Detailed_Normal.png");
        Texture2D metallicSmoothness = LoadTexture($"{root}/AtriumBanner_Detailed_MetallicSmoothness.png");
        Texture2D occlusion = LoadTexture($"{root}/AtriumBanner_Detailed_AO.png");

        SetTextureIfHas(mat, "_BaseMap", albedo);
        SetTextureIfHas(mat, "_MainTex", albedo);
        SetTextureIfHas(mat, "_BumpMap", normal);
        SetTextureIfHas(mat, "_MetallicGlossMap", metallicSmoothness);
        SetTextureIfHas(mat, "_OcclusionMap", occlusion);
        SetTextureIfHas(mat, "_EmissionMap", null);
        SetKeyword(mat, "_NORMALMAP", normal != null);
        SetKeyword(mat, "_METALLICGLOSSMAP", metallicSmoothness != null);
        SetKeyword(mat, "_EMISSION", false);
        SetKeyword(mat, "_ALPHATEST_ON", true);
        SetKeyword(mat, "_ALPHABLEND_ON", false);
        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
        SetColorIfHas(mat, "_Color", Color.white);
        SetColorIfHas(mat, "_BaseColor", Color.white);
        SetColorIfHas(mat, "_EmissionColor", Color.black);
        SetFloatIfHas(mat, "_Metallic", 0.0f);
        SetFloatIfHas(mat, "_Mode", 1.0f);
        SetFloatIfHas(mat, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfHas(mat, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        SetFloatIfHas(mat, "_ZWrite", 1.0f);
        SetFloatIfHas(mat, "_Cutoff", 0.18f);
        SetFloatIfHas(mat, "_Smoothness", 0.45f);
        SetFloatIfHas(mat, "_Glossiness", 0.45f);
        SetFloatIfHas(mat, "_GlossMapScale", 1.0f);
        SetFloatIfHas(mat, "_BumpScale", 0.72f);
        SetFloatIfHas(mat, "_OcclusionStrength", 0.58f);
        SetFloatIfHas(mat, "_GlossyReflections", 1.0f);
        SetFloatIfHas(mat, "_SpecularHighlights", 1.0f);
        ConfigureStandardReflectionKeywords(mat, 1.0f, 1.0f);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static void CreateWhiteStoneWallTileMaterial(string materialName, Color tint, Color emissionTint)
    {
        string materialPath = $"{WolfRepoMaterialRoot}/{materialName}.mat";
        CreateWhiteStoneMaterial(materialPath, tint, emissionTint);
    }

    private static void CreateWhiteStoneMaterial(string materialPath, Color tint, Color emissionTint)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No Standard shader found.");
            return;
        }

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
        SetTextureIfHas(mat, "_EmissionMap", null);

        SetKeyword(mat, "_NORMALMAP", normal != null);
        SetKeyword(mat, "_PARALLAXMAP", false);
        SetKeyword(mat, "_EMISSION", false);
        SetKeyword(mat, "_METALLICGLOSSMAP", false);
        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.DisableKeyword("_SPECGLOSSMAP");

        SetFloatIfHas(mat, "_Metallic", 0.0f);
        SetFloatIfHas(mat, "_Smoothness", 0.56f);
        SetFloatIfHas(mat, "_Glossiness", 0.56f);
        SetFloatIfHas(mat, "_GlossMapScale", 0.0f);
        SetFloatIfHas(mat, "_SmoothnessTextureChannel", 0.0f);
        SetFloatIfHas(mat, "_BumpScale", 0.42f);
        SetFloatIfHas(mat, "_OcclusionStrength", 0.30f);
        SetFloatIfHas(mat, "_Parallax", 0.0f);
        SetFloatIfHas(mat, "_GlossyReflections", 0.90f);
        SetFloatIfHas(mat, "_SpecularHighlights", 1.0f);
        ConfigureStandardReflectionKeywords(mat, 0.90f, 1.0f);
        SetColorIfHas(mat, "_Color", tint);
        SetColorIfHas(mat, "_BaseColor", tint);
        SetColorIfHas(mat, "_EmissionColor", emissionTint);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mat.renderQueue = -1;

        SetTextureScaleAndOffsetIfHas(mat, "_BaseMap", Vector2.one, Vector2.zero);
        SetTextureScaleAndOffsetIfHas(mat, "_MainTex", Vector2.one, Vector2.zero);
        SetTextureScaleAndOffsetIfHas(mat, "_BumpMap", Vector2.one, Vector2.zero);
        SetTextureScaleAndOffsetIfHas(mat, "_OcclusionMap", Vector2.one, Vector2.zero);
        SetTextureScaleAndOffsetIfHas(mat, "_ParallaxMap", Vector2.one, Vector2.zero);
        SetTextureScaleAndOffsetIfHas(mat, "_EmissionMap", Vector2.one, Vector2.zero);

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
        ConfigureMetallicGlossMapKeyword(mat, metallicSmoothness != null);

        SetTextureIfHas(mat, "_OcclusionMap", occlusion);

        SetFloatIfHas(mat, "_Metallic", fallbackMetallic);
        SetFloatIfHas(mat, "_Smoothness", fallbackSmoothness);
        SetFloatIfHas(mat, "_Glossiness", fallbackSmoothness);
        SetFloatIfHas(mat, "_BumpScale", 1.0f);
        SetFloatIfHas(mat, "_OcclusionStrength", 1.0f);
        SetFloatIfHas(mat, "_GlossyReflections", 1.0f);
        SetFloatIfHas(mat, "_SpecularHighlights", 1.0f);
        ConfigureStandardReflectionKeywords(mat, 1.0f, 1.0f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureCeilingPanelVisibility()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/CeilingPanel_Target.mat");
        if (material == null)
        {
            return;
        }

        ApplyCeilingPanelVisibility(material);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureDoorTargetMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/DoorTeal_Target.mat");
        if (material == null)
        {
            return;
        }

        Texture height = LoadTexture($"{TextureRoot}/DoorTeal/DoorTeal_Target_Height.png");

        SetColorIfHas(material, "_Color", new Color(0.78f, 0.90f, 0.94f, 1f));
        SetColorIfHas(material, "_BaseColor", new Color(0.78f, 0.90f, 0.94f, 1f));
        SetColorIfHas(material, "_EmissionColor", Color.black);
        SetTextureIfHas(material, "_EmissionMap", null);
        SetTextureIfHas(material, "_ParallaxMap", height);
        SetFloatIfHas(material, "_Metallic", 0.34f);
        SetFloatIfHas(material, "_Smoothness", 0.54f);
        SetFloatIfHas(material, "_Glossiness", 0.54f);
        SetFloatIfHas(material, "_GlossMapScale", 0.68f);
        SetFloatIfHas(material, "_OcclusionStrength", 0.24f);
        SetFloatIfHas(material, "_BumpScale", 0.72f);
        SetFloatIfHas(material, "_Parallax", 0.0f);
        SetFloatIfHas(material, "_GlossyReflections", 0.62f);
        SetFloatIfHas(material, "_SpecularHighlights", 0.45f);
        SetKeyword(material, "_EMISSION", false);
        SetKeyword(material, "_PARALLAXMAP", false);
        ConfigureStandardReflectionKeywords(material, 0.62f, 0.45f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureFloorTileMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/FloorTile_Target.mat");
        if (material == null)
        {
            return;
        }

        Texture albedo = LoadTexture($"{TextureRoot}/FloorTile/FloorTile_Target_Albedo.png");
        Texture normal = LoadTexture($"{TextureRoot}/FloorTile/FloorTile_Target_Normal.png");
        Texture metallicSmoothness = LoadTexture($"{TextureRoot}/FloorTile/FloorTile_Target_MetallicSmoothness.png");
        Texture height = LoadTexture($"{TextureRoot}/FloorTile/FloorTile_Target_Height.png");

        // The reference floor is charcoal stone whose high smoothness carries
        // warm lamp streaks. A near-white tint clips the direct light and hides
        // both the tile albedo and the reflections.
        SetColorIfHas(material, "_Color", new Color(0.68f, 0.63f, 0.56f, 1f));
        SetColorIfHas(material, "_BaseColor", new Color(0.68f, 0.63f, 0.56f, 1f));
        SetColorIfHas(material, "_EmissionColor", Color.black);
        SetTextureIfHas(material, "_BaseMap", albedo);
        SetTextureIfHas(material, "_MainTex", albedo);
        SetTextureIfHas(material, "_BumpMap", normal);
        SetTextureIfHas(material, "_MetallicGlossMap", metallicSmoothness);
        Texture occlusion = LoadTexture($"{TextureRoot}/FloorTile/FloorTile_Target_AO.png");
        SetTextureIfHas(material, "_OcclusionMap", occlusion);
        SetTextureIfHas(material, "_EmissionMap", null);
        SetTextureIfHas(material, "_ParallaxMap", height);
        SetFloatIfHas(material, "_Metallic", 0.0f);
        SetFloatIfHas(material, "_Smoothness", 0.78f);
        SetFloatIfHas(material, "_Glossiness", 0.78f);
        SetFloatIfHas(material, "_GlossMapScale", 1.0f);
        SetFloatIfHas(material, "_OcclusionStrength", 0.28f);
        SetFloatIfHas(material, "_BumpScale", 0.16f);
        SetFloatIfHas(material, "_Parallax", 0.0f);
        SetFloatIfHas(material, "_GlossyReflections", 1.0f);
        SetFloatIfHas(material, "_SpecularHighlights", 1.0f);
        SetKeyword(material, "_NORMALMAP", normal != null);
        ConfigureMetallicGlossMapKeyword(material, metallicSmoothness != null);
        SetKeyword(material, "_EMISSION", false);
        SetKeyword(material, "_PARALLAXMAP", false);
        ConfigureStandardReflectionKeywords(material, 1.0f, 1.0f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyCeilingPanelVisibility(Material material)
    {
        Texture height = LoadTexture($"{TextureRoot}/CeilingPanel/CeilingPanel_Target_Height.png");

        Color ceilingTint = new Color(0.26f, 0.27f, 0.28f, 1f);

        SetColorIfHas(material, "_Color", ceilingTint);
        SetColorIfHas(material, "_BaseColor", ceilingTint);
        SetColorIfHas(material, "_EmissionColor", Color.black);
        SetTextureIfHas(material, "_EmissionMap", null);
        SetTextureIfHas(material, "_ParallaxMap", height);
        SetFloatIfHas(material, "_Metallic", 0.0f);
        SetFloatIfHas(material, "_Smoothness", 0.46f);
        SetFloatIfHas(material, "_Glossiness", 0.46f);
        SetFloatIfHas(material, "_GlossMapScale", 0.50f);
        SetFloatIfHas(material, "_OcclusionStrength", 0.35f);
        SetFloatIfHas(material, "_BumpScale", 0.30f);
        SetFloatIfHas(material, "_Parallax", 0.0f);
        SetFloatIfHas(material, "_GlossyReflections", 0.78f);
        SetFloatIfHas(material, "_SpecularHighlights", 0.75f);
        SetKeyword(material, "_EMISSION", false);
        SetKeyword(material, "_PARALLAXMAP", false);
        ConfigureStandardReflectionKeywords(material, 0.78f, 0.75f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    private static Texture GetMainTexture(Material material)
    {
        if (material.HasProperty("_MainTex"))
        {
            return material.GetTexture("_MainTex");
        }

        return material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
    }

    private static Texture2D LoadTexture(string path)
    {
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void WriteMetallicSmoothnessFromRoughness(
        string roughnessPath,
        string metallicPath,
        string outputPath,
        float fallbackMetallic,
        float metallicScale,
        float minSmoothness,
        float maxSmoothness)
    {
        Texture2D roughness = LoadPngFromDisk(roughnessPath);
        Texture2D metallic = LoadPngFromDisk(metallicPath);
        int width = roughness != null ? roughness.width : metallic != null ? metallic.width : 1024;
        int height = roughness != null ? roughness.height : metallic != null ? metallic.height : 1024;
        Color32[] roughnessPixels = roughness != null ? roughness.GetPixels32() : null;
        Color32[] metallicPixels = metallic != null ? metallic.GetPixels32() : null;
        Color32[] output = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 roughnessPixel = roughnessPixels != null
                    ? SamplePixels(roughnessPixels, roughness.width, roughness.height, x, y, width, height)
                    : new Color32(64, 64, 64, 255);
                Color32 metallicPixel = metallicPixels != null
                    ? SamplePixels(metallicPixels, metallic.width, metallic.height, x, y, width, height)
                    : new Color32((byte)Mathf.RoundToInt(fallbackMetallic * 255f), 0, 0, 255);

                float roughnessValue = (roughnessPixel.r + roughnessPixel.g + roughnessPixel.b) / (3f * 255f);
                float sourceSmoothness = Mathf.Clamp01(1f - roughnessValue);
                float polishedSmoothness = Mathf.Lerp(minSmoothness, maxSmoothness, Mathf.Pow(sourceSmoothness, 0.72f));
                byte smoothnessByte = (byte)Mathf.RoundToInt(polishedSmoothness * 255f);
                byte metallicByte = (byte)Mathf.Clamp(Mathf.RoundToInt((metallicPixel.r + metallicPixel.g + metallicPixel.b) / 3f * metallicScale), 0, 255);

                output[y * width + x] = new Color32(metallicByte, 0, 0, smoothnessByte);
            }
        }

        Texture2D combined = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
        combined.SetPixels32(output);
        combined.Apply(true, false);
        File.WriteAllBytes(outputPath, combined.EncodeToPNG());

        Object.DestroyImmediate(combined);
        if (roughness != null)
        {
            Object.DestroyImmediate(roughness);
        }
        if (metallic != null)
        {
            Object.DestroyImmediate(metallic);
        }

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
    }

    private static Texture2D LoadPngFromDisk(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Missing source texture: {path}");
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
        {
            Object.DestroyImmediate(texture);
            Debug.LogWarning($"Could not decode source texture: {path}");
            return null;
        }

        return texture;
    }

    private static Color32 SamplePixels(Color32[] pixels, int sourceWidth, int sourceHeight, int x, int y, int targetWidth, int targetHeight)
    {
        int sx = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) * sourceWidth / targetWidth), 0, sourceWidth - 1);
        int sy = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) * sourceHeight / targetHeight), 0, sourceHeight - 1);
        return pixels[sy * sourceWidth + sx];
    }

    private static void SetTextureIfHas(Material mat, string name, Texture tex)
    {
        if (mat.HasProperty(name))
        {
            mat.SetTexture(name, tex);
        }
    }

    private static void SetTextureScaleAndOffsetIfHas(Material mat, string name, Vector2 scale, Vector2 offset)
    {
        if (mat.HasProperty(name))
        {
            mat.SetTextureScale(name, scale);
            mat.SetTextureOffset(name, offset);
        }
    }

    private static void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled)
        {
            mat.EnableKeyword(keyword);
        }
        else
        {
            mat.DisableKeyword(keyword);
        }
    }

    private static void ConfigureMetallicGlossMapKeyword(Material mat, bool enabled)
    {
        string shaderName = mat.shader != null ? mat.shader.name : string.Empty;
        bool isUrpLit = shaderName.Contains("Universal Render Pipeline");
        string activeKeyword = isUrpLit ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP";
        string inactiveKeyword = isUrpLit ? "_METALLICGLOSSMAP" : "_METALLICSPECGLOSSMAP";

        SetKeyword(mat, activeKeyword, enabled);
        mat.DisableKeyword(inactiveKeyword);
    }

    private static void ConfigureStandardReflectionKeywords(Material mat, float glossyReflections, float specularHighlights)
    {
        SetKeyword(mat, "_GLOSSYREFLECTIONS_OFF", glossyReflections <= 0f);
        SetKeyword(mat, "_SPECULARHIGHLIGHTS_OFF", specularHighlights <= 0f);
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
