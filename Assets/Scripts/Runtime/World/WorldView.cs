using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SlimesRevenge
{
    [DefaultExecutionOrder(-100)]
    public sealed class WorldView : MonoBehaviour
    {
        private const int DecorSeed = 42;
        private const float DecorChance = 0.22f;

        [SerializeField]
        private Tilemap tilemap;

        [SerializeField]
        private TileBase grass;

        /// <summary>Example Ground002 set A: SE/ES corner (FDR_Ground_Tiles_0).</summary>
        [SerializeField]
        private TileBase pathCornerES;

        /// <summary>Horizontal mid (FDR_Ground_Tiles_1).</summary>
        [SerializeField]
        private TileBase pathHorizontal;

        /// <summary>Vertical mid / caps (FDR_Ground_Tiles_24).</summary>
        [SerializeField]
        private TileBase pathVertical;

        /// <summary>NW corner / east-to-north turn (FDR_Ground_Tiles_49).</summary>
        [SerializeField]
        private TileBase pathCornerNW;

        [SerializeField]
        private TileBase[] grassDecorations;

        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private Sprite slimeWater;

        [SerializeField]
        private Sprite slimeOil;

        [SerializeField]
        private Sprite slimePoison;

        [SerializeField]
        private Sprite slimeAcid;

        [SerializeField]
        private Sprite slimeBlood;

        [SerializeField]
        private Sprite slimeLava;

        [SerializeField]
        private Sprite slimeMercury;

        [SerializeField]
        private TurnManager turnManager;

        public World Map { get; private set; }

        private Slime slime;
        private Tilemap decorTilemap;
        private Tilemap pathTilemap;
        private SpriteRenderer cursor;
        private Texture2D cursorTexture;
        private RunConfig activeRun;
        private Substance[] pendingLoadout;

        private void Awake()
        {
            if (!RunConfig.TryPeek(out var run))
            {
                AppNavigation.GoToMainMenu();
                return;
            }

            activeRun = run;
            pendingLoadout = run.Loadout;

            if (activeRun.Kind == RunKind.Duel)
            {
                BootstrapDuel(activeRun);
            }
            else
            {
                BootstrapCampaign();
            }

            cursor = CreateCursor();
            FrameCamera();

            UnityEngine.Debug.Log(
                $"[PlayReady] {activeRun?.Kind.ToString() ?? "Campaign"} {Map.Width}x{Map.Height}"
            );

            if (gameObject.GetComponent<ActionLogView>() == null)
            {
                gameObject.AddComponent<ActionLogView>();
            }

            if (gameObject.GetComponent<PopupHost>() == null)
            {
                gameObject.AddComponent<PopupHost>();
            }
        }

        private void Start()
        {
            RunConfig.TryConsume(out _);

            if (slime != null && pendingLoadout != null)
            {
                slime.Volume.Clear();
                for (var i = 0; i < pendingLoadout.Length; i++)
                {
                    slime.Volume.Add(Volume.CloneSubstance(pendingLoadout[i]));
                }

                slime.RefreshVolumeStatuses();
                slime.ApplyAppearance();
                pendingLoadout = null;
            }

            ActionLog.Clear();
            ActionLog.AnnounceRunStarted(activeRun?.Kind ?? RunKind.Campaign);

            if (slime != null)
            {
                var hud = gameObject.GetComponent<VolumeHud>();
                if (hud == null)
                {
                    hud = gameObject.AddComponent<VolumeHud>();
                }

                hud.Bind(slime);
            }

            // Duel + campaign share this scene; always offer a way back to Main.
            if (gameObject.GetComponent<ExitHud>() == null)
            {
                gameObject.AddComponent<ExitHud>();
            }
        }

        private void BootstrapCampaign()
        {
            Map = World.CreateGrass();
            EnsureDecorTilemap();
            EnsurePathTilemap();
            var center = Map.Center;
            Paint(paintPath: false, center);
            slime = SpawnSlime(center);

            var offsets = new[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.right + Vector2Int.up,
                Vector2Int.right + Vector2Int.down,
                Vector2Int.left + Vector2Int.up,
                Vector2Int.left + Vector2Int.down,
            };
            var opponents = CreatureCatalog.Opponents;
            var spawned = new List<Creature>(opponents.Count);
            for (var i = 0; i < opponents.Count; i++)
            {
                var cell = center + offsets[i % offsets.Length];
                var foe = SpawnFromCatalog(opponents[i].Kind, cell);
                if (foe == null)
                {
                    continue;
                }

                CreatureSpriteAnimator.PlayWalk(foe, center);
                spawned.Add(foe);
            }

            if (slime != null)
            {
                slime.Face(center + Vector2Int.right);
                slime.ApplyAppearance();
            }

            if (turnManager != null && slime != null)
            {
                turnManager.TrySoftcoreContinue = () =>
                    SoftcoreRevive.TryContinue(turnManager, slime, Map);
                turnManager.Bind(Map, slime, spawned.ToArray());
            }
        }

        private void BootstrapDuel(RunConfig run)
        {
            Map = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            EnsureDecorTilemap();
            EnsurePathTilemap();

            var slimeCell = new Vector2Int(
                (Map.Width - RunConfig.DuelSeparation) / 2,
                Map.Height / 2
            );
            var foeCell = new Vector2Int(slimeCell.x + RunConfig.DuelSeparation, slimeCell.y);
            Paint(paintPath: true, slimeCell, foeCell);
            slime = SpawnSlime(slimeCell);

            var foe = SpawnFromCatalog(run.Opponent, foeCell);
            if (slime != null)
            {
                slime.Face(foeCell);
                slime.ApplyAppearance();
            }
            if (turnManager != null && slime != null && foe != null)
            {
                turnManager.TrySoftcoreContinue = () =>
                    SoftcoreRevive.TryContinue(turnManager, slime, Map);
                turnManager.Bind(Map, slime, foe);
            }
        }

        private Slime SpawnSlime(Vector2Int cell)
        {
            var go = new GameObject("Slime");
            var spawned = go.AddComponent<Slime>();
            spawned.BindSheets(
                slimeWater,
                slimeOil,
                slimePoison,
                slimeAcid,
                slimeBlood,
                slimeLava,
                slimeMercury
            );
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            Place(spawned, cell);
            return spawned;
        }

        private Creature SpawnFromCatalog(CreatureKind kind, Vector2Int cell)
        {
            var type = CreatureCatalog.TypeOf(kind);
            if (type == null)
            {
                return null;
            }

            var go = new GameObject(kind.ToString());
            var creature = go.AddComponent(type) as Creature;
            if (creature == null)
            {
                Destroy(go);
                return null;
            }

            var renderer = go.AddComponent<SpriteRenderer>();
            var idle = CreatureSheet.Clip(kind, WalkFacing.South, FdrClip.Idle);
            renderer.sprite = idle != null && idle.Length > 0 ? idle[0] : null;
            renderer.sortingOrder = 10;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            Place(creature, cell);
            CreatureSpriteAnimator.PlayWalk(
                creature,
                slime != null ? slime.Cell : cell + Vector2Int.left
            );
            return creature;
        }

        private void LateUpdate()
        {
            if (cursor == null)
            {
                return;
            }

            var cell =
                turnManager != null && turnManager.Session != null
                    ? turnManager.Session.HighlightCell
                    : null;
            if (cell == null)
            {
                cursor.enabled = false;
                return;
            }

            cursor.enabled = true;
            cursor.transform.position = new Vector3(
                cell.Value.x + 0.5f,
                cell.Value.y + 0.5f,
                -0.05f
            );
        }

        private void Paint(bool paintPath, params Vector2Int[] spawnCells)
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

            var pathCells = paintPath ? PaintDiagonalPathLayer() : new HashSet<Vector2Int>();
            PaintDecorations(pathCells, spawnCells);
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
            // Above base grass (0), below path cliff layer (2).
            renderer.sortingOrder = 1;
        }

        private void EnsurePathTilemap()
        {
            if (pathTilemap != null || tilemap == null)
            {
                return;
            }

            var go = new GameObject("GroundPath");
            go.transform.SetParent(tilemap.transform.parent, false);
            pathTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            // Example_Grasslands Ground002 — cliff/path over fill.
            renderer.sortingOrder = 2;
        }

        /// <summary>
        /// Continuous BL→TR staircase using Example Ground002 set A (0/1/24/49).
        /// </summary>
        private HashSet<Vector2Int> PaintDiagonalPathLayer()
        {
            var pathCells = new HashSet<Vector2Int>();
            if (pathTilemap == null || !HasPathTiles() || Map == null)
            {
                return pathCells;
            }

            pathTilemap.ClearAllTiles();
            BuildStaircasePath(pathCells);
            foreach (var cell in pathCells)
            {
                var tile = AutotilePath(cell, pathCells);
                if (tile != null)
                {
                    pathTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
                }
            }

            pathTilemap.RefreshAllTiles();
            return pathCells;
        }

        private bool HasPathTiles()
        {
            return pathCornerES != null
                && pathHorizontal != null
                && pathVertical != null
                && pathCornerNW != null;
        }

        private void BuildStaircasePath(HashSet<Vector2Int> pathCells)
        {
            var x = 0;
            var y = 0;
            pathCells.Add(new Vector2Int(x, y));
            var endX = Map.Width - 1;
            var endY = Map.Height - 1;
            while (x < endX || y < endY)
            {
                var remainX = endX - x;
                var remainY = endY - y;
                if (remainX == 0)
                {
                    y++;
                    pathCells.Add(new Vector2Int(x, y));
                    continue;
                }

                if (remainY == 0)
                {
                    x++;
                    pathCells.Add(new Vector2Int(x, y));
                    continue;
                }

                // East run, then one step north — same rhythm as Example Ground002.
                var run = Mathf.Max(1, remainX / remainY);
                for (var i = 0; i < run && x < endX; i++)
                {
                    x++;
                    pathCells.Add(new Vector2Int(x, y));
                }

                if (y < endY)
                {
                    y++;
                    pathCells.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// Cardinal autotile matching Example set A: ES→0, EW→1, NS→24, NW→49.
        /// </summary>
        private TileBase AutotilePath(Vector2Int cell, HashSet<Vector2Int> pathCells)
        {
            var n = pathCells.Contains(cell + Vector2Int.up);
            var e = pathCells.Contains(cell + Vector2Int.right);
            var s = pathCells.Contains(cell + Vector2Int.down);
            var w = pathCells.Contains(cell + Vector2Int.left);

            if (e && s && !n && !w)
            {
                return pathCornerES;
            }

            if (n && w && !e && !s)
            {
                return pathCornerNW;
            }

            if (e && w)
            {
                return pathHorizontal;
            }

            if (n && s)
            {
                return pathVertical;
            }

            if (e && !w)
            {
                return pathCornerES;
            }

            if (w && !e)
            {
                return pathCornerNW;
            }

            return pathVertical;
        }

        private void PaintDecorations(HashSet<Vector2Int> pathCells, Vector2Int[] spawnCells)
        {
            if (decorTilemap == null || grassDecorations == null || grassDecorations.Length == 0)
            {
                return;
            }

            var reserved = new HashSet<Vector2Int>(pathCells);
            if (spawnCells != null)
            {
                for (var i = 0; i < spawnCells.Length; i++)
                {
                    reserved.Add(spawnCells[i]);
                }
            }

            decorTilemap.ClearAllTiles();
            var rng = new System.Random(DecorSeed);
            for (var y = 0; y < Map.Height; y++)
            {
                for (var x = 0; x < Map.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (reserved.Contains(cell) || rng.NextDouble() > DecorChance)
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
                wrapMode = TextureWrapMode.Clamp,
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
                size
            );
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
