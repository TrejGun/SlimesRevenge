using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class TurnManager : MonoBehaviour
    {
        public GameSession Session { get; private set; }

        public bool IsWaitingForInput =>
            !IsGameOver && (Session == null || Session.WaitingForInput);

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
        private readonly List<GameObject> puddleViews = new List<GameObject>();

        // Death mid-resolve: skip floor aging so a fresh corpse is still there next input,
        // and an older 1 unit corpse can stack with a kill on the immediate next action.
        private bool pauseCorpseAging;
        private GameOverScreen gameOverScreen;

        public IRng Rng { get; set; } = new SystemRng();

        /// <summary>Optional creature driven by player input. Null for board-only sims.</summary>
        public Creature Controlled => player;

        /// <summary>
        /// Bind a board. Pass <paramref name="controlled"/> null for sims without a player;
        /// all <paramref name="occupants"/> (and controlled, if any) occupy cells.
        /// </summary>
        public void Bind(World world, Creature controlled, params Creature[] occupants)
        {
            player = controlled;
            IsGameOver = false;
            others.Clear();
            var cells = new List<Vector2Int>();
            if (controlled != null)
            {
                cells.Add(controlled.Cell);
            }

            foreach (var creature in occupants)
            {
                if (creature == null)
                {
                    continue;
                }

                others.Add(creature);
                if (!cells.Contains(creature.Cell))
                {
                    cells.Add(creature.Cell);
                }
            }

            Session = new GameSession(world, cells)
            {
                OnControlledMoved = AfterControlledMoved,
                OnEnemyTurn = TickMobs,
                OnEnvironment = BeginPlayerTurn,
            };
            Session.SetControlled(controlled?.Cell);
            RefreshFloorViews();
            // Opening turn only if a controlled creature is already alive.
            if (controlled != null && controlled.IsAlive)
            {
                BeginPlayerTurn();
            }

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

            if (player.Volume == null || !player.Volume.CanSpend)
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

            if (player.Volume == null || !player.Volume.CanSpend)
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

            ActionLog.BeginKey(
                TextKey.LogMesses,
                ActionLogPart.Creature(player.Kind),
                ActionLogPart.Substance(substance)
            );
            try
            {
                player.RefreshVolumeStatuses();
            }
            finally
            {
                ActionLog.End();
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

            ActionLog.BeginKey(
                TextKey.LogCollects,
                ActionLogPart.Creature(player.Kind),
                ActionLogPart.Substance(substance)
            );
            try
            {
                player.Volume.Add(Volume.CloneSubstance(substance));
                player.RefreshVolumeStatuses();
            }
            finally
            {
                ActionLog.End();
            }

            RefreshFloorViews();
            return Act(session => session.TryWait());
        }

        public bool TryDevourCorpse(int index)
        {
            if (Session == null || player == null || player.IsDigesting)
            {
                return false;
            }

            if (!Session.World.Floor.TryTakeCorpse(player.Cell, index, out var corpse))
            {
                return false;
            }

            if (!Digesting.CanBegin(player.Volume, corpse))
            {
                Session.World.Floor.AddCorpse(corpse);
                return false;
            }

            ActionLog.BeginKey(
                TextKey.LogDevours,
                ActionLogPart.Creature(player.Kind),
                ActionLogPart.Creature(corpse.Kind)
            );
            try
            {
                if (!player.AddStatus(new Digesting(corpse)))
                {
                    Session.World.Floor.AddCorpse(corpse);
                    return false;
                }
            }
            finally
            {
                ActionLog.End();
            }

            RefreshFloorViews();
            return Act(session => session.TryWait());
        }

        public Creature LivingAt(Vector2Int cell)
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

        private Creature CreatureAt(Vector2Int cell) => LivingAt(cell);

        private void AfterControlledMoved()
        {
            if (player != null && Session.ControlledCell != null)
            {
                player.PlaceOn(Session.ControlledCell.Value);
                Session.World.Floor.ApplyContact(player);
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

                creature.TakeTurn(Session, player, Rng, others);
                // Puddle trap applies inside CreatureMoves.TryStep → Floor.ApplyContact.

                DropFallenPrey();

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

            // Player turn start: statuses (including Digesting) share one log group; corpse aging sits between.
            if (player != null && player.IsAlive)
            {
                ActionLog.BeginKey(TextKey.LogTurn, ActionLogPart.Creature(player.Kind));
                try
                {
                    if (!pauseCorpseAging)
                    {
                        Session?.World.Floor.Tick();
                    }

                    pauseCorpseAging = false;
                    player.TickStatuses();
                }
                finally
                {
                    ActionLog.End(discardIfEmpty: true);
                }

                RefreshFloorViews();
                if (!player.IsAlive)
                {
                    HandlePlayerDefeated();
                }

                return;
            }

            if (!pauseCorpseAging)
            {
                Session?.World.Floor.Tick();
            }

            pauseCorpseAging = false;
            RefreshFloorViews();
        }

        private void TickStatus(Creature creature, bool dropIfDead)
        {
            if (creature == null || !creature.IsAlive)
            {
                return;
            }

            ActionLog.BeginKey(TextKey.LogTurn, ActionLogPart.Creature(creature.Kind));
            try
            {
                creature.TickStatuses();
            }
            finally
            {
                ActionLog.End(discardIfEmpty: true);
            }

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

            if (
                GameSettings.Mode == GameMode.Softcore
                && TrySoftcoreContinue != null
                && TrySoftcoreContinue()
            )
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

        private void DropFallenPrey()
        {
            for (var i = 0; i < others.Count; i++)
            {
                var victim = others[i];
                if (victim != null && !victim.IsAlive && !victim.IsCorpse)
                {
                    DropCorpse(victim);
                }
            }
        }

        private void DropCorpse(Creature creature)
        {
            if (creature == null || Session == null || creature.IsCorpse)
            {
                return;
            }

            Session.Vacate(creature.Cell);
            creature.BecomeCorpse();
            Session.World.Floor.AddCorpse(creature);
            pauseCorpseAging = true;
            RefreshFloorViews();
        }

        private void RefreshFloorViews()
        {
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
            if (IsGameOver || Session == null)
            {
                return false;
            }

            if (player != null && !player.IsAlive)
            {
                HandlePlayerDefeated();
                return false;
            }

            if (!attempt(Session))
            {
                return false;
            }

            if (player != null && Session.ControlledCell != null)
            {
                player.PlaceOn(Session.ControlledCell.Value);
            }

            if (player != null && !player.IsAlive)
            {
                HandlePlayerDefeated();
            }

            return true;
        }
    }
}
