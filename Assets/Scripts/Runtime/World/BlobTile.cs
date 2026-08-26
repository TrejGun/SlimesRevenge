using UnityEngine;
using UnityEngine.Tilemaps;

namespace SlimesRevenge
{
    [CreateAssetMenu(menuName = "SlimesRevenge/Blob Tile", fileName = "BlobTile")]
    public sealed class BlobTile : TileBase
    {
        private const int VariantCount = 47;

        [SerializeField] private Sprite[] sprites = new Sprite[VariantCount];

        private static readonly int[] MaskToIndex = BuildLookup();

        public Sprite[] Sprites => sprites;

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    tilemap.RefreshTile(new Vector3Int(position.x + x, position.y + y, position.z));
                }
            }
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.color = Color.white;
            tileData.transform = Matrix4x4.identity;
            tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
            tileData.colliderType = Tile.ColliderType.None;
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            var index = MaskToIndex[BuildMask(position, tilemap)];
            if (index >= 0 && index < sprites.Length)
            {
                tileData.sprite = sprites[index];
            }
        }

        private int BuildMask(Vector3Int position, ITilemap tilemap)
        {
            var n = Matches(tilemap, position + Vector3Int.up);
            var e = Matches(tilemap, position + Vector3Int.right);
            var s = Matches(tilemap, position + Vector3Int.down);
            var w = Matches(tilemap, position + Vector3Int.left);
            var ne = n && e && Matches(tilemap, position + new Vector3Int(1, 1, 0));
            var nw = n && w && Matches(tilemap, position + new Vector3Int(-1, 1, 0));
            var se = s && e && Matches(tilemap, position + new Vector3Int(1, -1, 0));
            var sw = s && w && Matches(tilemap, position + new Vector3Int(-1, -1, 0));

            var mask = 0;
            if (nw)
            {
                mask |= 1;
            }

            if (n)
            {
                mask |= 2;
            }

            if (ne)
            {
                mask |= 4;
            }

            if (w)
            {
                mask |= 8;
            }

            if (e)
            {
                mask |= 16;
            }

            if (sw)
            {
                mask |= 32;
            }

            if (s)
            {
                mask |= 64;
            }

            if (se)
            {
                mask |= 128;
            }

            return mask;
        }

        private bool Matches(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) == this;
        }

        private static int[] BuildLookup()
        {
            var valid = new bool[256];
            for (var n = 0; n <= 1; n++)
            {
                for (var e = 0; e <= 1; e++)
                {
                    for (var s = 0; s <= 1; s++)
                    {
                        for (var w = 0; w <= 1; w++)
                        {
                            var neMax = n == 1 && e == 1 ? 1 : 0;
                            var nwMax = n == 1 && w == 1 ? 1 : 0;
                            var seMax = s == 1 && e == 1 ? 1 : 0;
                            var swMax = s == 1 && w == 1 ? 1 : 0;
                            for (var ne = 0; ne <= neMax; ne++)
                            {
                                for (var nw = 0; nw <= nwMax; nw++)
                                {
                                    for (var se = 0; se <= seMax; se++)
                                    {
                                        for (var sw = 0; sw <= swMax; sw++)
                                        {
                                            var mask = 0;
                                            if (nw == 1)
                                            {
                                                mask |= 1;
                                            }

                                            if (n == 1)
                                            {
                                                mask |= 2;
                                            }

                                            if (ne == 1)
                                            {
                                                mask |= 4;
                                            }

                                            if (w == 1)
                                            {
                                                mask |= 8;
                                            }

                                            if (e == 1)
                                            {
                                                mask |= 16;
                                            }

                                            if (sw == 1)
                                            {
                                                mask |= 32;
                                            }

                                            if (s == 1)
                                            {
                                                mask |= 64;
                                            }

                                            if (se == 1)
                                            {
                                                mask |= 128;
                                            }

                                            valid[mask] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var lookup = new int[256];
            var index = 0;
            for (var mask = 0; mask < valid.Length; mask++)
            {
                if (valid[mask])
                {
                    lookup[mask] = index;
                    index++;
                }
            }

            return lookup;
        }
    }
}
