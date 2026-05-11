using System;
using UnityEngine;

namespace WolfMini.Core
{
    [Serializable]
    public readonly struct AtlasTile : IEquatable<AtlasTile>
    {
        public const int Columns = 16;
        public const int Rows = 16;
        public const int TileCount = Columns * Rows;

        public AtlasTile(int index, int column, int row, Vector2 offset, Vector2 repeat)
        {
            Index = index;
            Column = column;
            Row = row;
            Offset = offset;
            Repeat = repeat;
        }

        public int Index { get; }
        public int Column { get; }
        public int Col => Column;
        public int Row { get; }
        public Vector2 Offset { get; }
        public Vector2 UVOffset => Offset;
        public Vector2 Repeat { get; }
        public Vector2 UVRepeat => Repeat;

        public static AtlasTile FromIndex(int tileIndex)
        {
            int index = ClampIndex(tileIndex);
            int column = index % Columns;
            int row = index / Columns;
            Vector2 repeat = GetRepeat();
            Vector2 offset = new Vector2(column * repeat.x, 1f - repeat.y - row * repeat.y);

            return new AtlasTile(index, column, row, offset, repeat);
        }

        public static int ClampIndex(int tileIndex)
        {
            return Mathf.Clamp(tileIndex, 0, TileCount - 1);
        }

        public static int GetColumn(int tileIndex)
        {
            return ClampIndex(tileIndex) % Columns;
        }

        public static int GetRow(int tileIndex)
        {
            return ClampIndex(tileIndex) / Columns;
        }

        public static Vector2 GetRepeat()
        {
            return new Vector2(1f / Columns, 1f / Rows);
        }

        public static Vector2 GetOffset(int tileIndex)
        {
            return FromIndex(tileIndex).Offset;
        }

        public bool Equals(AtlasTile other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is AtlasTile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return $"AtlasTile {Index} ({Column}, {Row})";
        }
    }
}
