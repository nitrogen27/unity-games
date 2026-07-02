using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;
using WolfMini.Rendering;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Converts the legacy single-floor repo format (level0.json) into the
    /// <see cref="WolfLevelDefinition"/> and bootstraps the shared material
    /// library from existing project textures/materials.
    /// </summary>
    public static class WolfFull3DImporter
    {
        public const string DefinitionAssetPath = "Assets/WolfMini/Data/WolfRepoLevel1Full3D.asset";
        public const string LibraryAssetPath = "Assets/WolfMini/Data/WolfFull3DMaterialLibrary.asset";
        public const string MainFloorId = "F1";

        private const string Level0Path = "Assets/Data/WolfRepo/level0.json";
        private const string GeneratedMaterialFolder = "Assets/Materials/WolfFull3D";
        private const string RepoTextureFolder = "Assets/Textures/WolfRepo";
        private const string TargetLookFolder = "Assets/Materials/WolfTargetLook";
        private const string RepoMaterialFolder = "Assets/Materials/WolfRepo";
        private const int MapSize = 64;

        [MenuItem("Tools/Wolf Full3D/Import Repo Level Into Definition")]
        public static void ImportRepoLevel()
        {
            if (!File.Exists(Level0Path))
            {
                Debug.LogError($"[WolfFull3D] Missing {Level0Path}.");
                return;
            }

            WolfLevelDefinition definition = LoadOrCreateDefinition();
            PopulateFromRepoJson(definition, File.ReadAllText(Level0Path));
            EnsureMaterialLibrary();

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            ReportValidation(definition);
            Debug.Log($"[WolfFull3D] Imported '{definition.levelName}' into {DefinitionAssetPath}: " +
                      $"{definition.floors.Count} floor(s), {definition.floors[0].doors.Count} doors, " +
                      $"{definition.floors[0].statics.Count} statics, {definition.floors[0].enemies.Count} enemies.");
        }

        /// <summary>Fills the definition with floor F1 converted from repo json. Testable without asset IO.</summary>
        public static void PopulateFromRepoJson(WolfLevelDefinition definition, string json)
        {
            RepoLevelDto dto = JsonUtility.FromJson<RepoLevelDto>(json);
            if (dto == null || dto.walls == null || dto.walls.Length != MapSize * MapSize)
            {
                throw new InvalidOperationException("Invalid repo level json: walls grid must be 64x64.");
            }

            definition.levelName = string.IsNullOrEmpty(dto.name) ? "Wolf Repo Level 1" : dto.name;
            definition.floors.Clear();
            definition.openings.Clear();

            var floor = new GridFloorSpec
            {
                id = MainFloorId,
                y = 0f,
                ceilingHeight = WolfMiniConstants.WallHeight,
                width = MapSize,
                height = MapSize,
                walls = (int[])dto.walls.Clone(),
                floorColor = ColorFromRgb(dto.floor, new Color32(112, 112, 112, 255)),
                ceilingColor = ColorFromRgb(dto.ceiling, new Color32(56, 56, 56, 255))
            };

            if (dto.doors != null)
            {
                foreach (RepoDoorDto door in dto.doors)
                {
                    floor.doors.Add(new GridDoorSpec { x = door.x, y = door.y, type = door.type, vertical = door.vertical });
                }
            }

            if (dto.statics != null)
            {
                foreach (RepoStaticDto item in dto.statics)
                {
                    floor.statics.Add(new GridStaticSpec { x = item.x, y = item.y, typeIndex = item.typeIndex, typeName = item.typeName });
                }
            }

            if (dto.enemies != null)
            {
                foreach (RepoEnemyDto enemy in dto.enemies)
                {
                    floor.enemies.Add(new GridEnemySpec
                    {
                        x = enemy.x,
                        y = enemy.y,
                        type = enemy.type,
                        dir = enemy.dir,
                        patrol = enemy.patrol,
                        difficulty = enemy.difficulty
                    });
                }
            }

            definition.floors.Add(floor);
            definition.playerSpawn = new GridSpawnSpec
            {
                floorId = MainFloorId,
                cell = new Vector2Int(dto.spawnX, dto.spawnY),
                angle = dto.spawnAngle
            };
        }

        public static WolfFull3DMaterialLibrary EnsureMaterialLibrary()
        {
            WolfFull3DMaterialLibrary library = AssetDatabase.LoadAssetAtPath<WolfFull3DMaterialLibrary>(LibraryAssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<WolfFull3DMaterialLibrary>();
                AssetDatabase.CreateAsset(library, LibraryAssetPath);
            }

            library.WallsAtlas = LoadTexture("walls.png");
            library.SpritesAtlas = LoadTexture("sprites.png");
            library.GuardSheet = LoadTexture("guard.png");
            library.DogSheet = LoadTexture("dog.png");

            EnsureGeneratedMaterialFolder();
            library.WallAtlasMaterial = EnsureAtlasRepeatMaterial("Full3D_WallAtlas", library.WallsAtlas);

            // Generated mesh UVs are measured in the unscaled 2 m texture
            // module. The target-look floor still repeats every 4 m and the
            // ceiling every 8 m, independent of the enlarged geometry cell.
            library.FloorMaterial = EnsureSurfaceMaterial("Full3D_Floor", $"{TargetLookFolder}/FloorTile_Target.mat", new Color32(112, 112, 112, 255), new Vector2(0.5f, 0.5f));
            library.CeilingMaterial = EnsureSurfaceMaterial("Full3D_Ceiling", $"{TargetLookFolder}/CeilingPanel_Target.mat", new Color32(56, 56, 56, 255), new Vector2(0.25f, 0.25f));

            // Wall value mapping mirrors WolfTargetRoomTextureApplier: 8/9 blue
            // stone, 1/2 white stone, 5/7 prison cell fronts.
            Material blueWall = LoadTargetMaterial("BlueWall_Target");
            Material whiteStoneWall = LoadTargetMaterial("WhiteStoneWall_Target");
            Material prisonCellDoor = LoadTargetMaterial("PrisonCellDoor_Target");
            library.SetWallOverride(8, blueWall);
            library.SetWallOverride(9, blueWall);
            library.SetWallOverride(1, whiteStoneWall);
            library.SetWallOverride(2, whiteStoneWall);
            // Prison fronts are clamped feature textures. The mesh builder emits
            // them as fixed 2 m slices, so they repeat without full-height
            // stretching and without requiring Repeat wrap mode.
            library.SetWallOverride(5, prisonCellDoor, tileVertically: false);
            library.SetWallOverride(7, prisonCellDoor, tileVertically: false);

            Material darkMetal = LoadTargetMaterial("DarkMetalTrim_Target");
            library.DoorFaceMaterial = LoadTargetMaterial("DoorTeal_Target");
            library.DoorJambMaterial = darkMetal;
            library.LampCapMaterial = darkMetal;
            library.LampGlowMaterial = LoadTargetMaterial("DynamicLampGlow");
            library.CeilingSpillWarmMaterial = LoadTargetMaterial("CeilingLampSpillWarm");
            library.CeilingSpillCoolMaterial = LoadTargetMaterial("CeilingLampSpillCool");
            library.LampFloorReflectionWarmMaterial = LoadTargetMaterial("LampFloorReflectionWarm");
            library.LampFloorReflectionCoolMaterial = LoadTargetMaterial("LampFloorReflectionCool");
            library.LampWallReflectionWarmMaterial = LoadTargetMaterial("LampWallReflectionWarm");
            library.LampWallReflectionCoolMaterial = LoadTargetMaterial("LampWallReflectionCool");
            library.RailMaterial = LoadTargetMaterial("RailMetal_Target");

            library.LampWarmCap = AssetDatabase.LoadAssetAtPath<Material>($"{RepoMaterialFolder}/Mat_WolfRepo_LampWarmCap.mat");
            library.LampGreenCap = AssetDatabase.LoadAssetAtPath<Material>($"{RepoMaterialFolder}/Mat_WolfRepo_LampGreenCap.mat");
            library.LampWarmBulb = AssetDatabase.LoadAssetAtPath<Material>($"{RepoMaterialFolder}/Mat_WolfRepo_LampWarmBulb.mat");
            library.LampGreenBulb = AssetDatabase.LoadAssetAtPath<Material>($"{RepoMaterialFolder}/Mat_WolfRepo_LampGreenBulb.mat");

            EditorUtility.SetDirty(library);
            return library;
        }

        private static WolfLevelDefinition LoadOrCreateDefinition()
        {
            WolfLevelDefinition definition = AssetDatabase.LoadAssetAtPath<WolfLevelDefinition>(DefinitionAssetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WolfLevelDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionAssetPath);
            }

            return definition;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{RepoTextureFolder}/{fileName}");
        }

        private static void EnsureGeneratedMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedMaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "WolfFull3D");
            }
        }

        /// <summary>
        /// Generated meshes carry UVs in fixed texture modules. Target-look
        /// materials are cloned with the requested tiling instead of being
        /// referenced directly.
        /// </summary>
        private static Material EnsureSurfaceMaterial(string name, string targetLookPath, Color fallbackColor, Vector2 textureScale)
        {
            string path = $"{GeneratedMaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material targetLook = AssetDatabase.LoadAssetAtPath<Material>(targetLookPath);

            if (material == null)
            {
                material = targetLook != null
                    ? new Material(targetLook)
                    : new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard")) { color = fallbackColor };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (targetLook != null)
            {
                material.shader = targetLook.shader;
                material.CopyPropertiesFromMaterial(targetLook);
            }

            material.mainTextureScale = textureScale;
            material.mainTextureOffset = Vector2.zero;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadTargetMaterial(string materialName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{TargetLookFolder}/{materialName}.mat");
        }

        /// <summary>
        /// Material for atlas walls: WolfMini/AtlasRepeat tiles one atlas window
        /// using continuous module UVs (TEXCOORD0) and the window origin baked
        /// into TEXCOORD1 by the mesh builder.
        /// </summary>
        private static Material EnsureAtlasRepeatMaterial(string name, Texture2D texture)
        {
            string path = $"{GeneratedMaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("WolfMini/AtlasRepeat") ?? Shader.Find("Unlit/Texture");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            if (material.HasProperty("_WindowScale"))
            {
                material.SetFloat("_WindowScale", 1f / WolfFull3DMaterialLibrary.WallAtlasSize);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureUnlitTextureMaterial(string name, Texture2D texture)
        {
            string path = $"{GeneratedMaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color ColorFromRgb(int[] rgb, Color fallback)
        {
            if (rgb == null || rgb.Length < 3)
            {
                return fallback;
            }

            return new Color32((byte)rgb[0], (byte)rgb[1], (byte)rgb[2], 255);
        }

        private static void ReportValidation(WolfLevelDefinition definition)
        {
            if (!definition.TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[WolfFull3D] Validation: {error}", definition);
                }
            }
        }

        [Serializable]
        public sealed class RepoLevelDto
        {
            public string name;
            public string music;
            public string parTime;
            public int[] ceiling;
            public int[] floor;
            public int[] walls;
            public RepoDoorDto[] doors;
            public RepoEnemyDto[] enemies;
            public RepoStaticDto[] statics;
            public int spawnX;
            public int spawnY;
            public int spawnAngle;
        }

        [Serializable]
        public sealed class RepoDoorDto
        {
            public int x;
            public int y;
            public string type;
            public bool vertical;
        }

        [Serializable]
        public sealed class RepoEnemyDto
        {
            public int x;
            public int y;
            public string type;
            public int dir;
            public bool patrol;
            public int difficulty;
        }

        [Serializable]
        public sealed class RepoStaticDto
        {
            public int x;
            public int y;
            public int typeIndex;
            public string typeName;
        }
    }
}
