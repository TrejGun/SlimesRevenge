using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class TurnManager : MonoBehaviour
    {
        public GameSession Session { get; private set; }

        public bool IsWaitingForInput => !IsGameOver && (Session == null || Session.WaitingForInput);

        public bool IsGameOver { get; private set; }

        /// <summary>Always raised when the slime is defeated (before mode-specific handling).</summary>
        public event Action PlayerDefeated;

        /// <summary>
        /// Softcore connector: return true if defeat was handled (e.g. revive).
        /// Ignored in Hardcore.
        /// </summary>
        public Func<bool> TrySoftcoreContinue { get; set; }

        private Creature player;
        private readonly List<Creature> others = new List<Creature>();
        private readonly List<GameObject> corpseViews = new List<GameObject>();
        private readonly List<GameObject> puddleViews = new List<GameObject>();
        // Death mid-resolve: skip floor aging so a fresh corpse is still there next input,
        // and an older 1 unit corpse can stack with a kill on the immediate next action.
        private bool pauseCorpseAging;
        private GameOverScreen gameOverScreen;

        public IRng Rng { get; set; } = new SystemRng();

        public void Bind(World world, Creature player, params Creature[] occupants)
        {
            this.player = player;
            IsGameOver = false;
            others.Clear();
            var cells = new List<Vector2Int>();
            foreach (var creature in occupants)
            {
                if (creature == null)
                {
                    continue;
                }

                others.Add(creature);
                cells.Add(creature.Cell);
            }

            Session = new GameSession(world, player.Cell, cells)
            {
                OnPlayerMoved = AfterPlayerMoved,
                OnEnemyTurn = TickMobs,
                OnEnvironment = BeginPlayerTurn
            };
            RefreshFloorViews();
            // Opening the scene starts the player's first turn (statuses / floor).
            BeginPlayerTurn();
            if (IsGameOver)
            {
                return;
            }
        }

        public bool TryMoveTo(Vector2Int destination)
        {
            return Act(session => session.TryMoveTo(destination));
        }

        public bool TryStep(Vector2Int offset)
        {
            return Act(session => session.TryStep(offset));
        }

        public bool TryWait()
        {
            return Act(session => session.TryWait());
        }

        public bool TryAttack(Vector2Int cell, Substance substance)
        {
            if (Session == null || player == null || !Session.CanAttack(cell))
            {
                return false;
            }

            var target = CreatureAt(cell);
            if (target == null || !Combat.Attack(player, target, substance))
            {
                return false;
            }

            target.MarkAggro();
            if (!target.IsAlive)
            {
                DropCorpse(target);
            }

            if (!player.IsAlive)
            {
                HandlePlayerDefeated();
                return true;
            }

            return Act(session => session.TryWait());
        }

        public bool TryMakeMess(Substance substance)
        {
            if (Session == null || player == null || substance == null)
            {
                return false;
            }

            if (Session.World.Floor.GetPuddle(player.Cell) != null)
            {
                return false;
            }

            if (!player.Volume.TryRemove(substance))
            {
                return false;
            }

            if (!Session.World.Floor.TryPlacePuddle(player.Cell, Volume.CloneSubstance(substance)))
            {
                player.Volume.Add(Volume.CloneSubstance(substance));
                return false;
            }

            if (player is Slime)
            {
                player.RefreshBodyTraits();
            }

            RefreshFloorViews();
            return Act(session => session.TryWait());
        }

        public bool TryCollectPuddle()
        {
            if (Session == null || player == null)
            {
                return false;
            }

            if (player.Volume.UnitCount >= Volume.Capacity)
            {
                return false;
            }

            if (!Session.World.Floor.TryCollectPuddle(player.Cell, out var substance))
            {
                return false;
            }

            player.Volume.Add(Volume.CloneSubstance(substance));
            if (player is Slime)
            {
                player.RefreshBodyTraits();
            }

            RefreshFloorViews();
            return Act(session => session.TryWait());
        }

        public bool TryDevourCorpse(int index)
        {
            if (Session == null || player == null || player.Digestion.IsBusy)
            {
                return false;
            }

            if (!Session.World.Floor.TryTakeCorpse(player.Cell, index, out var corpse))
            {
                return false;
            }

            if (!player.Digestion.TryBegin(corpse))
            {
                Session.World.Floor.AddCorpse(corpse);
                return false;
            }

            RefreshFloorViews();
            return Act(session => session.TryWait());
        }

        private Creature CreatureAt(Vector2Int cell)
        {
            foreach (var creature in others)
            {
                if (creature != null && creature.IsAlive && creature.Cell == cell)
                {
                    return creature;
                }
            }

            return null;
        }

        private void AfterPlayerMoved()
        {
            if (player != null)
            {
                player.PlaceOn(Session.PlayerCell);
            }
        }

        private void TickMobs()
        {
            // Each creature: tick ITS statuses at the start of ITS turn, then act.
            // Death during the status phase skips the act and vacates the cell.
            foreach (var creature in others)
            {
                if (IsGameOver)
                {
                    return;
                }

                if (creature == null || !creature.IsAlive)
                {
                    continue;
                }

                TickStatus(creature, dropIfDead: true);
                if (!creature.IsAlive)
                {
                    continue;
                }

                var from = creature.Cell;
                creature.TakeTurn(Session, player, Rng);
                if (creature.IsAlive && creature.Cell != from)
                {
                    ApplyPuddle(creature);
                }

                if (player != null && !player.IsAlive)
                {
                    HandlePlayerDefeated();
                    return;
                }
            }
        }

        private void BeginPlayerTurn()
        {
            if (IsGameOver)
            {
                return;
            }

            // Player turn start: digestion, corpse aging, then the slime's own statuses.
            if (player != null && player.Digestion.Tick(player.Volume) && player is Slime)
            {
                player.RefreshBodyTraits();
            }

            if (!pauseCorpseAging)
            {
                Session?.World.Floor.Tick();
            }

            pauseCorpseAging = false;
            TickStatus(player, dropIfDead: false);
            RefreshFloorViews();
            if (player != null && !player.IsAlive)
            {
                HandlePlayerDefeated();
            }
        }

        private void TickStatus(Creature creature, bool dropIfDead)
        {
            if (creature == null || !creature.IsAlive)
            {
                return;
            }

            creature.TickStatuses();
            if (creature.IsAlive)
            {
                return;
            }

            if (dropIfDead)
            {
                DropCorpse(creature);
            }
        }

        private void HandlePlayerDefeated()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            player?.Die();
            PlayerDefeated?.Invoke();

            if (GameSettings.Mode == GameMode.Softcore
                && TrySoftcoreContinue != null
                && TrySoftcoreContinue())
            {
                IsGameOver = false;
                return;
            }

            // Hardcore (default): game over → Main menu.
            if (gameOverScreen == null)
            {
                gameOverScreen = gameObject.GetComponent<GameOverScreen>();
                if (gameOverScreen == null)
                {
                    gameOverScreen = gameObject.AddComponent<GameOverScreen>();
                }
            }

            gameOverScreen.Show();
        }

        private void DropCorpse(Creature creature)
        {
            if (creature == null || Session == null)
            {
                return;
            }

            Session.Vacate(creature.Cell);
            var corpse = new Corpse(creature, KindOf(creature));
            Session.World.Floor.AddCorpse(corpse);
            creature.Die();
            pauseCorpseAging = true;
            RefreshFloorViews();
        }

        private void ApplyPuddle(Creature creature)
        {
            if (creature == null || Session == null)
            {
                return;
            }

            var puddle = Session.World.Floor.GetPuddle(creature.Cell);
            puddle?.Substance.Apply(creature);
            if (!creature.IsAlive)
            {
                DropCorpse(creature);
            }
        }

        private static CreatureKind KindOf(Creature creature)
        {
            if (creature is Rat)
            {
                return CreatureKind.Rat;
            }

            if (creature is Cat)
            {
                return CreatureKind.Cat;
            }

            if (creature is Dog)
            {
                return CreatureKind.Dog;
            }

            return CreatureKind.Slime;
        }

        private void RefreshFloorViews()
        {
            ClearViews(corpseViews);
            ClearViews(puddleViews);
            if (Session?.World?.Floor == null)
            {
                return;
            }

            var floor = Session.World.Floor;
            for (var y = 0; y < Session.World.Height; y++)
            {
                for (var x = 0; x < Session.World.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var puddle = floor.GetPuddle(cell);
                    if (puddle != null)
                    {
                        puddleViews.Add(CreateMarker(cell, puddle.Substance.Color, 0.35f, -0.2f));
                    }

                    var corpses = floor.GetCorpses(cell);
                    for (var i = 0; i < corpses.Count; i++)
                    {
                        var tint = new Color(0.55f, 0.45f, 0.45f, 0.55f);
                        corpseViews.Add(CreateMarker(cell + new Vector2(0.08f * i, 0.08f * i), tint, 0.7f, -0.15f));
                    }
                }
            }
        }

        private static GameObject CreateMarker(Vector2 cell, Color color, float scale, float z)
        {
            var go = new GameObject("FloorMarker");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            renderer.color = color;
            renderer.sprite = WhiteSprite();
            go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, z);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static Sprite white;

        private static Sprite WhiteSprite()
        {
            if (white != null)
            {
                return white;
            }

            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return white;
        }

        private static void ClearViews(List<GameObject> views)
        {
            foreach (var view in views)
            {
                if (view != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(view);
                    }
                    else
                    {
                        DestroyImmediate(view);
                    }
                }
            }

            views.Clear();
        }

        private bool Act(Func<GameSession, bool> attempt)
        {
            if (IsGameOver || Session == null || !attempt(Session))
            {
                return false;
            }

            if (player != null)
            {
                player.PlaceOn(Session.PlayerCell);
            }

            if (player != null && !player.IsAlive)
            {
                HandlePlayerDefeated();
            }

            return true;
        }
    }
}
