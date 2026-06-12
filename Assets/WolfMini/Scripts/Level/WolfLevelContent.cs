using System.Collections.Generic;
using UnityEngine;
using WolfMini.Core;

namespace WolfMini.Level
{
    /// <summary>
    /// Shared static/enemy catalog for the level builders: sprite tables, pickup
    /// scales and lamp colors that are identical for the grid and sector
    /// pipelines. Actor and pickup dimensions stay human-scale while the
    /// architecture can use <see cref="WolfMiniConstants.WorldScale"/>.
    /// </summary>
    public static class WolfLevelContent
    {
        /// <summary>Offset of the first static sprite inside the sprites atlas.</summary>
        public const int SpriteOffset = 2;

        public const int ChandelierTypeIndex = 4;
        public const int CeilLightTypeIndex = 14;
        public const int PansTypeIndex = 15;

        // Lamp light colors ported from WolfDynamicLightingSetup (chandeliers
        // warm and stronger, ceiling lights cooler).
        public static readonly Color WarmLampColor = new Color(1.0f, 0.72f, 0.36f);
        public static readonly Color CoolLampColor = new Color(1.0f, 0.84f, 0.62f);

        public static readonly StatInfo[] StatInfos =
        {
            new StatInfo("puddle", false, null),
            new StatInfo("greenBarrel", true, null),
            new StatInfo("tableChairs", true, null),
            new StatInfo("floorLamp", true, null),
            new StatInfo("chandelier", false, null),
            new StatInfo("hangedMan", true, null),
            new StatInfo("dogFood", false, "food"),
            new StatInfo("pillar", true, null),
            new StatInfo("tree", true, null),
            new StatInfo("skeleton", false, null),
            new StatInfo("sink", true, null),
            new StatInfo("plant", true, null),
            new StatInfo("urn", true, null),
            new StatInfo("bareTable", true, null),
            new StatInfo("ceilLight", false, null),
            new StatInfo("pans", false, null),
            new StatInfo("armor", true, null),
            new StatInfo("cage", true, null),
            new StatInfo("cageSkel", true, null),
            new StatInfo("bonesRelax", false, null),
            new StatInfo("key1", false, "key1"),
            new StatInfo("key2", false, "key2"),
            new StatInfo("stuff", true, null),
            new StatInfo("junk", false, null),
            new StatInfo("food", false, "food"),
            new StatInfo("firstaid", false, "health"),
            new StatInfo("clip", false, "ammo"),
            new StatInfo("machinegun", false, "machinegun"),
            new StatInfo("chaingun", false, "chaingun"),
            new StatInfo("cross", false, "cross"),
            new StatInfo("chalice", false, "chalice"),
            new StatInfo("bible", false, "bible"),
            new StatInfo("crown", false, "crown"),
            new StatInfo("oneUp", false, "oneup"),
            new StatInfo("gibs", false, null),
            new StatInfo("barrel", true, null),
            new StatInfo("well", true, null),
            new StatInfo("emptyWell", true, null),
            new StatInfo("gibs2", false, null),
            new StatInfo("flag", true, null),
            new StatInfo("callApogee", true, null),
            new StatInfo("junk2", false, null),
            new StatInfo("junk3", false, null),
            new StatInfo("junk4", false, null),
            new StatInfo("pots", false, null),
            new StatInfo("stove", true, null),
            new StatInfo("spears", true, null)
        };

        public static readonly Dictionary<string, EnemyInfo> EnemyInfos = new Dictionary<string, EnemyInfo>
        {
            ["guard"] = new EnemyInfo(1.55f, 2.08f, Color.white),
            ["officer"] = new EnemyInfo(1.55f, 2.08f, new Color(0.9f, 0.9f, 1.2f)),
            ["ss"] = new EnemyInfo(1.68f, 2.24f, new Color(0.5f, 0.5f, 0.5f)),
            ["dog"] = new EnemyInfo(1.30f, 1.05f, Color.white),
            ["mutant"] = new EnemyInfo(1.68f, 2.24f, new Color(0.4f, 0.8f, 0.3f)),
            ["boss"] = new EnemyInfo(2.30f, 2.78f, new Color(1.2f, 0.8f, 0.8f))
        };

        /// <summary>World-space billboard scale for a static prop (pickups are smaller).</summary>
        public static float GetStaticScale(string pickupType)
        {
            float baseScale = pickupType switch
            {
                null => 1.0f,
                "ammo" => 0.66f,
                "food" => 0.72f,
                "health" => 0.78f,
                "key1" => 0.72f,
                "key2" => 0.72f,
                _ => 0.82f
            };

            return baseScale;
        }

        public readonly struct StatInfo
        {
            public readonly string name;
            public readonly bool blocking;
            public readonly string pickupType;

            public StatInfo(string name, bool blocking, string pickupType)
            {
                this.name = name;
                this.blocking = blocking;
                this.pickupType = pickupType;
            }
        }

        public readonly struct EnemyInfo
        {
            public readonly float width;
            public readonly float height;
            public readonly Color tint;

            public EnemyInfo(float width, float height, Color tint)
            {
                this.width = width;
                this.height = height;
                this.tint = tint;
            }
        }
    }
}
