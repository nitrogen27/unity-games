using System.Collections.Generic;
using UnityEngine;

namespace WolfE1M1
{
    public sealed class E1M1TwoRoomsBuilder : MonoBehaviour
    {
        private const int Width = 17;
        private const int Height = 9;
        private const int ReferenceMinX = 26;
        private const int ReferenceMinY = 0;
        private const int ReferenceSpawnX = 29;
        private const int ReferenceSpawnY = 6;

        private static readonly int[,] Map =
        {
            { 8, 8, 5, 9, 8, 5, 8, 9, 7, 8, 9, 5, 9, 8, 5, 8, 8 },
            { 8, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8 },
            { 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8 },
            { 8, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8 },
            { 8, 8, 9, 9, 8, 8, 9, 0, 0, 0, 9, 8, 8, 9, 8, 8, 8 },
            { 8, 8, 0, 0, 0, 0, 8, 0, 0, 0, 8, 0, 0, 0, 0, 8, 8 },
            { 8, 9, 0, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, 0, 9, 8 },
            { 8, 8, 0, 0, 0, 0, 8, 0, 0, 0, 8, 0, 0, 0, 0, 8, 8 },
            { 8, 8, 8, 9, 8, 8, 9, 0, 0, 0, 8, 8, 8, 8, 8, 8, 8 },
        };

        private readonly Dictionary<int, Material> wallMaterials = new Dictionary<int, Material>();
        private Material floorMaterial;
        private Material ceilingMaterial;
        private Material doorFaceMaterial;
        private Material doorSideMaterial;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Rebuild();
            }
        }

        public Vector3 GetSpawnPosition()
        {
            int localX = ReferenceSpawnX - ReferenceMinX;
            int localZ = ReferenceSpawnY - ReferenceMinY;
            return CellCenter(localX, localZ, WolfE1M1Constants.UpperFloorY + 0.04f);
        }

        public void Rebuild()
        {
            Transform oldRoot = transform.Find("GeneratedGeometry");
            if (oldRoot != null)
            {
                DestroyObject(oldRoot.gameObject);
            }

            GameObject generated = new GameObject("GeneratedGeometry");
            generated.transform.SetParent(transform, false);

            BuildMaterials();
            BuildFloorAndCeiling(generated.transform);
            BuildWallsAndDoors(generated.transform);
        }

        private void BuildMaterials()
        {
            wallMaterials.Clear();
            foreach (int id in new[] { 5, 7, 8, 9 })
            {
                wallMaterials[id] = TextureMaterial($"wall_{id:00}_light");
            }

            floorMaterial = ColorMaterial("Floor Gray", new Color32(112, 112, 112, 255));
            ceilingMaterial = ColorMaterial("Ceiling Gray", new Color32(56, 56, 56, 255));
            doorFaceMaterial = TextureMaterial("door_normal_face");
            doorSideMaterial = TextureMaterial("door_normal_side");
        }

        private void BuildFloorAndCeiling(Transform parent)
        {
            Vector3 center = new Vector3(0f, 0f, 0f);
            center.x = Width * WolfE1M1Constants.CellSize * 0.5f;
            center.z = Height * WolfE1M1Constants.CellSize * 0.5f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Upper Floor Slab";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(center.x, WolfE1M1Constants.UpperFloorY - 0.03f, center.z);
            floor.transform.localScale = new Vector3(Width * WolfE1M1Constants.CellSize, 0.06f, Height * WolfE1M1Constants.CellSize);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling Slab";
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.localPosition = new Vector3(center.x, WolfE1M1Constants.UpperFloorY + WolfE1M1Constants.WallHeight + 0.03f, center.z);
            ceiling.transform.localScale = new Vector3(Width * WolfE1M1Constants.CellSize, 0.06f, Height * WolfE1M1Constants.CellSize);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMaterial;
        }

        private void BuildWallsAndDoors(Transform parent)
        {
            for (int z = 0; z < Height; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int value = Map[z, x];
                    if (value > 0)
                    {
                        BuildWall(parent, x, z, value);
                    }
                    else if (value == -1)
                    {
                        BuildDoor(parent, x, z);
                    }
                }
            }
        }

        private void BuildWall(Transform parent, int x, int z, int wallId)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall {wallId:00} ({ReferenceMinX + x},{ReferenceMinY + z})";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = CellCenter(x, z, WolfE1M1Constants.UpperFloorY + WolfE1M1Constants.WallHeight * 0.5f);
            wall.transform.localScale = Vector3.one * WolfE1M1Constants.CellSize;

            if (wallMaterials.TryGetValue(wallId, out Material material))
            {
                wall.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private void BuildDoor(Transform parent, int x, int z)
        {
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = $"Sliding Door ({ReferenceMinX + x},{ReferenceMinY + z})";
            door.transform.SetParent(parent, false);
            door.transform.localPosition = CellCenter(x, z, WolfE1M1Constants.UpperFloorY + WolfE1M1Constants.WallHeight * 0.5f);
            door.transform.localScale = new Vector3(
                WolfE1M1Constants.DoorThickness,
                WolfE1M1Constants.WallHeight,
                WolfE1M1Constants.CellSize);

            Renderer renderer = door.GetComponent<Renderer>();
            renderer.sharedMaterials = new[] { doorSideMaterial, doorSideMaterial, doorSideMaterial, doorSideMaterial, doorFaceMaterial, doorFaceMaterial };

            DemoDoor demoDoor = door.AddComponent<DemoDoor>();
            demoDoor.Initialize(Vector3.forward);
        }

        private Vector3 CellCenter(int x, int z, float y)
        {
            return new Vector3(
                x * WolfE1M1Constants.CellSize + WolfE1M1Constants.CellSize * 0.5f,
                y,
                z * WolfE1M1Constants.CellSize + WolfE1M1Constants.CellSize * 0.5f);
        }

        private static Material TextureMaterial(string textureName)
        {
            Texture2D texture = Resources.Load<Texture2D>($"WolfE1M1/Textures/{textureName}");
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            Shader shader = Shader.Find("Unlit/Texture");
            Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.name = textureName;
            material.mainTexture = texture;
            return material;
        }

        private static Material ColorMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
