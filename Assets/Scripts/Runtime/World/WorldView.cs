using UnityEngine;
using UnityEngine.Tilemaps;

namespace SlimesRevenge
{
    [DefaultExecutionOrder(-100)]
    public sealed class WorldView : MonoBehaviour
    {
        private const int DecorSeed = 42;
        private const float DecorChance = 0.35f;

        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase grass;
        [SerializeField] private TileBase[] grassDecorations;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Slime slime;
        [SerializeField] private Rat rat;
        [SerializeField] private Cat cat;
        [SerializeField] private Dog dog;
        [SerializeField] private Bat bat;
        [SerializeField] private Scorpion scorpion;
        [SerializeField] private Sprite batSprite;
        [SerializeField] private Sprite scorpionSprite;
        [SerializeField] private TurnManager turnManager;

        public World Map { get; private set; }

        private Tilemap decorTilemap;
        private SpriteRenderer cursor;
        private Texture2D cursorTexture;

        private void Awake()
        {
            Map = World.CreateGrass();
            EnsureDecorTilemap();
            Paint();
            Place(slime, Map.Center);
            Place(rat, Map.Center + Vector2Int.right);
            Place(cat, Map.Center + Vector2Int.right + Vector2Int.up);
            Place(dog, Map.Center + Vector2Int.right + Vector2Int.down);
            bat = EnsureBeast(bat, "Bat", batSprite, Map.Center + Vector2Int.left);
            scorpion = EnsureBeast(scorpion, "Scorpion", scorpionSprite, Map.Center + Vector2Int.left + Vector2Int.up);
            cursor = CreateCursor();
            FrameCamera();
            if (turnManager != null && slime != null)
            {
                turnManager.Bind(Map, slime, rat, cat, dog, bat, scorpion);
            }

            if (slime != null)
            {
                var hud = gameObject.GetComponent<VolumeHud>();
                if (hud == null)
                {
                    hud = gameObject.AddComponent<VolumeHud>();
                }

                hud.Bind(slime);
            }
        }

        private void LateUpdate()
        {
            if (cursor == null)
            {
                return;
            }

            var cell = turnManager != null && turnManager.Session != null
                ? turnManager.Session.HighlightCell
                : null;
            if (cell == null)
            {
                cursor.enabled = false;
                return;
            }

            cursor.enabled = true;
            cursor.transform.position = new Vector3(cell.Value.x + 0.5f, cell.Value.y + 0.5f, -0.05f);
        }

        private void Paint()
        {
            tilemap.ClearAllTiles();
            for (var y = 0; y < Map.Height; y++)
            {
                for (var x = 0; x < Map.Width; x++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), grass);
                }
            }

            tilemap.RefreshAllTiles();
            PaintDecorations();
        }

        private void EnsureDecorTilemap()
        {
            if (decorTilemap != null || tilemap == null)
            {
                return;
            }

            var go = new GameObject("GroundDecor");
            go.transform.SetParent(tilemap.transform.parent, false);
            decorTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;
        }

        private void PaintDecorations()
        {
            if (decorTilemap == null || grassDecorations == null || grassDecorations.Length == 0)
            {
                return;
            }

            decorTilemap.ClearAllTiles();
            var rng = new System.Random(DecorSeed);
            for (var y = 0; y < Map.Height; y++)
            {
                for (var x = 0; x < Map.Width; x++)
                {
                    if (rng.NextDouble() > DecorChance)
                    {
                        continue;
                    }

                    var tile = grassDecorations[rng.Next(grassDecorations.Length)];
                    if (tile != null)
                    {
                        decorTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }

            decorTilemap.RefreshAllTiles();
        }

        private static void Place(Creature creature, Vector2Int cell)
        {
            if (creature != null)
            {
                creature.PlaceOn(cell);
            }
        }

        private static T EnsureBeast<T>(T existing, string name, Sprite sprite, Vector2Int cell)
            where T : Creature
        {
            if (existing != null)
            {
                Place(existing, cell);
                return existing;
            }

            var go = new GameObject(name);
            var creature = go.AddComponent<T>();
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            Place(creature, cell);
            return creature;
        }

        private SpriteRenderer CreateCursor()
        {
            var go = new GameObject("CellCursor");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5;
            spriteRenderer.sprite = CreateCursorSprite();
            spriteRenderer.enabled = false;
            return spriteRenderer;
        }

        private Sprite CreateCursorSprite()
        {
            const int size = 16;
            cursorTexture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var fill = new Color(1f, 0.92f, 0.2f, 0.35f);
            var edge = new Color(1f, 0.85f, 0.1f, 0.95f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x <= 1 || y <= 1 || x >= size - 2 || y >= size - 2;
                    cursorTexture.SetPixel(x, y, border ? edge : fill);
                }
            }

            cursorTexture.Apply();
            return Sprite.Create(
                cursorTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        private void FrameCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            worldCamera.transform.position = new Vector3(Map.Width * 0.5f, Map.Height * 0.5f, -10f);
            var halfWidth = Map.Width * 0.5f;
            var halfHeight = Map.Height * 0.5f;
            var aspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
            worldCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
        }
    }
}
