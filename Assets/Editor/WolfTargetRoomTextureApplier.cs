#if UNITY_EDITOR
using System.Collections.Generic;
using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WolfTargetRoomTextureApplier
{
    private const string ScenePath = "Assets/Scenes/WolfRepoLevel1.unity";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";
    private const string OriginalWallMaterialRoot = "Assets/Materials/WolfRepo";
    private static readonly Dictionary<int, Material> OriginalWallMaterialCache = new();

    [MenuItem("Tools/Wolf Target Look/Apply To Wolf Repo Room")]
    public static void ApplyToWolfRepoRoom()
    {
        AssetDatabase.Refresh();
        WolfTargetMaterialSetup.SetupGeneratedMaterials();
        AssetDatabase.Refresh();

        var blueWall = LoadMaterial("BlueWall_Target");
        var whiteStoneWall = LoadMaterial("WhiteStoneWall_Target");
        var whiteStoneWallDark = LoadMaterial("WhiteStoneWall_Dark_Target");
        var doorTeal = LoadMaterial("DoorTeal_Target");
        var floorTile = LoadMaterial("FloorTile_Target");
        var ceilingPanel = LoadMaterial("CeilingPanel_Target");
        var darkMetal = LoadMaterial("DarkMetalTrim_Target");
        var prisonCellDoor = LoadMaterial("PrisonCellDoor_Target");

        ConfigureMaterial(blueWall, new Vector2(1f, 1f), 0.0f, 0.10f, 0.08f, 0.20f, 0.0f, 0.0f, 0.0f);
        ConfigureMaterial(whiteStoneWall, new Vector2(1f, 1f), 0.0f, 0.10f, 0.08f, 0.18f, 0.0f, 0.0f, 0.0f);
        ConfigureMaterial(whiteStoneWallDark, new Vector2(1f, 1f), 0.0f, 0.10f, 0.08f, 0.18f, 0.0f, 0.0f, 0.0f);
        ConfigureMaterial(doorTeal, new Vector2(1f, 1f), 0.46f, 0.48f, 0.56f, 0.72f, 0.24f, 0.48f, 0.52f);
        ConfigureMaterial(floorTile, new Vector2(32f, 32f), 0.0f, 0.16f, 0.0f, 0.08f, 0.0f, 0.08f, 0.14f);
        ConfigureMaterial(ceilingPanel, new Vector2(16f, 16f), 0.0f, 0.08f, 0.08f, 0.18f, 0.0f, 0.0f, 0.0f);
        WolfTargetMaterialSetup.ConfigureDoorTargetMaterial();
        WolfTargetMaterialSetup.ConfigureFloorTileMaterial();
        WolfTargetMaterialSetup.ConfigureCeilingPanelVisibility();
        ConfigureMaterial(darkMetal, new Vector2(4f, 4f), 0.34f, 0.38f, 0.48f, 0.64f, 0.22f, 0.40f, 0.46f);
        ConfigureMaterial(prisonCellDoor, new Vector2(1f, 1f), 0.18f, 0.30f, 0.38f, 0.52f, 0.22f, 0.34f, 0.40f);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ApplyResult result = ApplyStructuralTargetMaterials(
            blueWall,
            whiteStoneWall,
            whiteStoneWallDark,
            doorTeal,
            floorTile,
            ceilingPanel,
            darkMetal,
            prisonCellDoor);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[WolfTargetRoomTextureApplier] Applied target look to {ScenePath}. " +
            $"Wall slots: {result.WallSlots}, white stone slots: {result.WhiteStoneWallSlots}, door slots: {result.DoorSlots}, " +
            $"floor/stair slots: {result.FloorSlots}, ceiling slots: {result.CeilingSlots}, " +
            $"dark metal/trim slots: {result.DarkMetalSlots}, prison cell slots: {result.PrisonCellSlots}, " +
            $"restored wall slots: {result.RestoredWallSlots}, " +
            $"changed slots: {result.ChangedSlots}.");
    }

    private static Material LoadMaterial(string materialName)
    {
        string path = $"{MaterialRoot}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            throw new MissingReferenceException($"Missing target material at {path}.");
        }

        return material;
    }

    private static ApplyResult ApplyStructuralTargetMaterials(
        Material blueWall,
        Material whiteStoneWall,
        Material whiteStoneWallDark,
        Material doorTeal,
        Material floorTile,
        Material ceilingPanel,
        Material darkMetal,
        Material prisonCellDoor)
    {
        ApplyResult result = new ApplyResult();
        foreach (WolfDoor door in Object.FindObjectsByType<WolfDoor>(FindObjectsInactive.Exclude))
        {
            Renderer renderer = door.GetComponent<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                AssignMaterial(ref materials[i], doorTeal, ref result, MaterialTarget.Door);
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (renderer.GetComponent<WolfDoor>() != null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name;
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material current = materials[i];
                if (current == null)
                {
                    continue;
                }

                MaterialTarget materialTarget;
                Material target = ResolveTargetMaterial(
                    objectName,
                    current.name,
                    i,
                    blueWall,
                    whiteStoneWall,
                    whiteStoneWallDark,
                    doorTeal,
                    floorTile,
                    ceilingPanel,
                    darkMetal,
                    prisonCellDoor,
                    out materialTarget);
                if (target == null)
                {
                    continue;
                }

                if (!ReferenceEquals(current, target))
                {
                    changed = true;
                }

                AssignMaterial(ref materials[i], target, ref result, materialTarget);
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        return result;
    }

    private static Material ResolveTargetMaterial(
        string objectName,
        string materialName,
        int materialIndex,
        Material blueWall,
        Material whiteStoneWall,
        Material whiteStoneWallDark,
        Material doorTeal,
        Material floorTile,
        Material ceilingPanel,
        Material darkMetal,
        Material prisonCellDoor,
        out MaterialTarget materialTarget)
    {
        if (TryGetWallValue(objectName, out int wallValue))
        {
            if (ShouldUsePrisonCellDoor(wallValue))
            {
                materialTarget = MaterialTarget.PrisonCell;
                return prisonCellDoor;
            }

            if (ShouldUseTargetWhiteStoneWall(wallValue))
            {
                materialTarget = MaterialTarget.WhiteStoneWall;
                return whiteStoneWall;
            }

            if (ShouldUseTargetBlueWall(wallValue))
            {
                materialTarget = MaterialTarget.Wall;
                return blueWall;
            }

            materialTarget = MaterialTarget.RestoredWall;
            return LoadOriginalWallSideMaterial(wallValue, materialIndex);
        }

        if (IsDoorObject(objectName) || IsDoorMaterial(materialName))
        {
            materialTarget = MaterialTarget.Door;
            return doorTeal;
        }

        if (IsDarkMetalObject(objectName) || IsDarkMetalMaterial(materialName))
        {
            materialTarget = MaterialTarget.DarkMetal;
            return darkMetal;
        }

        if (IsFloorObject(objectName) || IsFloorMaterial(materialName))
        {
            materialTarget = MaterialTarget.Floor;
            return floorTile;
        }

        if (IsCeilingObject(objectName) || IsCeilingMaterial(materialName))
        {
            materialTarget = MaterialTarget.Ceiling;
            return ceilingPanel;
        }

        if (IsWallObject(objectName) || IsWallMaterial(materialName))
        {
            materialTarget = MaterialTarget.Wall;
            return blueWall;
        }

        materialTarget = MaterialTarget.None;
        return null;
    }

    private static void AssignMaterial(ref Material current, Material target, ref ApplyResult result, MaterialTarget materialTarget)
    {
        result.Count(materialTarget);
        if (!ReferenceEquals(current, target))
        {
            current = target;
            result.ChangedSlots++;
        }
    }

    private static bool TryGetWallValue(string objectName, out int wallValue)
    {
        const string typeMarker = " Type ";
        int markerIndex = objectName.LastIndexOf(typeMarker, System.StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            wallValue = 0;
            return false;
        }

        string valueText = objectName.Substring(markerIndex + typeMarker.Length);
        return int.TryParse(valueText, out wallValue);
    }

    private static bool ShouldUseTargetBlueWall(int wallValue)
    {
        return wallValue == 8 || wallValue == 9;
    }

    private static bool ShouldUseTargetWhiteStoneWall(int wallValue)
    {
        return wallValue == 1 || wallValue == 2;
    }

    private static bool ShouldUsePrisonCellDoor(int wallValue)
    {
        return wallValue == 5 || wallValue == 7;
    }

    private static Material LoadOriginalWallSideMaterial(int wallValue, int materialIndex)
    {
        int lightTileIndex = (wallValue - 1) * 2;
        return LoadOriginalWallMaterial(lightTileIndex);
    }

    private static Material LoadOriginalWallMaterial(int tileIndex)
    {
        if (OriginalWallMaterialCache.TryGetValue(tileIndex, out Material cached))
        {
            return cached;
        }

        string path = $"{OriginalWallMaterialRoot}/Mat_WolfRepo_WallTile_{tileIndex:000}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            throw new MissingReferenceException($"Missing original wall material at {path}.");
        }

        OriginalWallMaterialCache[tileIndex] = material;
        return material;
    }

    private static bool IsDoorObject(string objectName)
    {
        return StartsWith(objectName, "Door ")
            || Has(objectName, " Door ")
            || Has(objectName, "Cyan Panel");
    }

    private static bool IsFloorObject(string objectName)
    {
        return objectName == "Floor"
            || StartsWith(objectName, "Floor ")
            || Has(objectName, " Floor")
            || Has(objectName, "Stone Threshold")
            || Has(objectName, "Stair Down")
            || Has(objectName, "Stair Up")
            || Has(objectName, "Landing Stone")
            || Has(objectName, "Bottom Landing");
    }

    private static bool IsCeilingObject(string objectName)
    {
        if (Has(objectName, "Lamp") || Has(objectName, "Bulb"))
        {
            return false;
        }

        return objectName == "Ceiling"
            || StartsWith(objectName, "Ceiling ")
            || Has(objectName, " Ceiling");
    }

    private static bool IsWallObject(string objectName)
    {
        return StartsWith(objectName, "Wall ")
            || StartsWith(objectName, "WolfRepoWallCell_")
            || Has(objectName, " Wall")
            || Has(objectName, " Header")
            || Has(objectName, "Void Blocker")
            || Has(objectName, "Facade")
            || Has(objectName, "Pier");
    }

    private static bool IsDarkMetalObject(string objectName)
    {
        return Has(objectName, "Trim")
            || Has(objectName, "Rail")
            || Has(objectName, "Handrail")
            || Has(objectName, "Switch")
            || Has(objectName, "Hole Lip")
            || Has(objectName, "Corner Cap")
            || Has(objectName, "Corner Post")
            || Has(objectName, "Baluster")
            || Has(objectName, "Chandelier Chain")
            || Has(objectName, "Chandelier Hub")
            || Has(objectName, "Chandelier Arm")
            || Has(objectName, "Floor Plate")
            || Has(objectName, "Stair Nose")
            || Has(objectName, "Riser Face")
            || Has(objectName, "Lamp Cap");
    }

    private static bool IsDoorMaterial(string materialName)
    {
        return materialName == "Mat_WolfRepo_WallTile_098"
            || materialName == "Mat_WolfRepo_WallTile_100"
            || materialName == "Mat_WolfRepo_WallTile_102"
            || Has(materialName, "Door");
    }

    private static bool IsFloorMaterial(string materialName)
    {
        return materialName == "Mat_WolfRepo_FloorColor"
            || materialName == "Mat_WolfRepo_BasementFloor"
            || materialName == "Mat_WolfRepo_StairStone"
            || materialName == "Mat_WolfRepo_StairRiser"
            || materialName == "Mat_BlockoutWolf3D_Stair"
            || materialName == "Mat_Blockout_Stair"
            || materialName == "Mat_BlockoutRTX_GreyMarbleStair"
            || materialName == "Mat_Q2_Stairs"
            || Has(materialName, "Floor");
    }

    private static bool IsCeilingMaterial(string materialName)
    {
        return materialName == "Mat_WolfRepo_CeilingColor"
            || materialName == "Mat_WolfRepo_BasementCeiling"
            || Has(materialName, "Ceiling");
    }

    private static bool IsWallMaterial(string materialName)
    {
        return StartsWith(materialName, "Mat_WolfRepo_WallTile_")
            || materialName == "Mat_WolfRepo_BasementWall"
            || Has(materialName, "Wall")
            || Has(materialName, "StoneBlock")
            || Has(materialName, "BlueStone");
    }

    private static bool IsDarkMetalMaterial(string materialName)
    {
        return materialName == "Mat_WolfRepo_StairRail"
            || materialName == "Mat_WolfRepo_StairTrim"
            || materialName == "Mat_WolfRepo_Wood"
            || materialName == "Mat_WolfRepo_WoodDark"
            || materialName == "Mat_WolfRepo_Brass"
            || Has(materialName, "DarkMetal")
            || Has(materialName, "Trim")
            || Has(materialName, "Rail")
            || Has(materialName, "Brass")
            || Has(materialName, "Gold")
            || Has(materialName, "Wood");
    }

    private static bool Has(string value, string token)
    {
        return value.IndexOf(token, System.StringComparison.Ordinal) >= 0;
    }

    private static bool StartsWith(string value, string token)
    {
        return value.StartsWith(token, System.StringComparison.Ordinal);
    }

    private static void ConfigureMaterial(
        Material material,
        Vector2 scale,
        float metallic,
        float smoothness,
        float glossMapScale,
        float bumpScale,
        float occlusionStrength,
        float glossyReflections,
        float specularHighlights)
    {
        SetTextureScale(material, "_BaseMap", scale);
        SetTextureScale(material, "_MainTex", scale);
        SetTextureScale(material, "_BumpMap", scale);
        SetTextureScale(material, "_MetallicGlossMap", scale);
        SetTextureScale(material, "_OcclusionMap", scale);
        SetTextureScale(material, "_ParallaxMap", scale);
        SetTextureScale(material, "_EmissionMap", scale);

        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_Glossiness", smoothness);
        SetFloat(material, "_GlossMapScale", glossMapScale);
        SetFloat(material, "_BumpScale", bumpScale);
        SetFloat(material, "_OcclusionStrength", occlusionStrength);
        SetFloat(material, "_Parallax", 0.0f);
        SetFloat(material, "_GlossyReflections", glossyReflections);
        SetFloat(material, "_SpecularHighlights", specularHighlights);

        EditorUtility.SetDirty(material);
    }

    private static void SetTextureScale(Material material, string propertyName, Vector2 scale)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTextureScale(propertyName, scale);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private enum MaterialTarget
    {
        None,
        Wall,
        WhiteStoneWall,
        Door,
        Floor,
        Ceiling,
        DarkMetal,
        PrisonCell,
        RestoredWall
    }

    private struct ApplyResult
    {
        public int WallSlots;
        public int WhiteStoneWallSlots;
        public int DoorSlots;
        public int FloorSlots;
        public int CeilingSlots;
        public int DarkMetalSlots;
        public int PrisonCellSlots;
        public int RestoredWallSlots;
        public int ChangedSlots;

        public void Count(MaterialTarget target)
        {
            switch (target)
            {
                case MaterialTarget.Wall:
                    WallSlots++;
                    break;
                case MaterialTarget.WhiteStoneWall:
                    WhiteStoneWallSlots++;
                    break;
                case MaterialTarget.Door:
                    DoorSlots++;
                    break;
                case MaterialTarget.Floor:
                    FloorSlots++;
                    break;
                case MaterialTarget.Ceiling:
                    CeilingSlots++;
                    break;
                case MaterialTarget.DarkMetal:
                    DarkMetalSlots++;
                    break;
                case MaterialTarget.PrisonCell:
                    PrisonCellSlots++;
                    break;
                case MaterialTarget.RestoredWall:
                    RestoredWallSlots++;
                    break;
            }
        }
    }
}
#endif
