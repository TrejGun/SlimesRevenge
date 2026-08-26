using System;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class World
    {
        public const int DefaultSize = 10;

        private readonly Cell[,] cells;

        public World(int width, int height, TerrainType fill)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "World must be at least 1x1.");
            }

            cells = new Cell[width, height];
            Floor = new Floor();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    cells[x, y] = new Cell(new Vector2Int(x, y), fill);
                }
            }
        }

        public Floor Floor { get; }

        public int Width => cells.GetLength(0);

        public int Height => cells.GetLength(1);

        public Vector2Int Center => new Vector2Int((Width - 1) / 2, (Height - 1) / 2);

        public Cell GetCell(int x, int y)
        {
            return cells[x, y];
        }

        public bool Contains(Vector2Int position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < Width && position.y < Height;
        }

        public Cell GetCell(Vector2Int position)
        {
            return cells[position.x, position.y];
        }

        public static World CreateGrass(int size = DefaultSize)
        {
            return new World(size, size, TerrainType.Grass);
        }
    }
}
