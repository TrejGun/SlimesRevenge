using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Cell
    {
        public Cell(Vector2Int position, TerrainType terrain)
        {
            Position = position;
            Terrain = terrain;
        }

        public Vector2Int Position { get; }

        public TerrainType Terrain { get; set; }
    }
}
