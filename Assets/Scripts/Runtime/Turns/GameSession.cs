using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class GameSession
    {
        private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        public GameSession(World world, Vector2Int playerCell, IEnumerable<Vector2Int> occupants = null)
        {
            World = world;
            PlayerCell = playerCell;
            WaitingForInput = true;
            if (occupants == null)
            {
                return;
            }

            foreach (var cell in occupants)
            {
                if (cell != playerCell)
                {
                    occupied.Add(cell);
                }
            }
        }

        public World World { get; }

        public Vector2Int PlayerCell { get; private set; }

        public bool WaitingForInput { get; private set; }

        public int Turn { get; private set; }

        public Vector2Int? HighlightCell { get; set; }

        public Action OnPlayerMoved { get; set; }

        public Action OnEnemyTurn { get; set; }

        public Action OnEnvironment { get; set; }

        public bool IsOccupied(Vector2Int cell)
        {
            return occupied.Contains(cell);
        }

        public bool CanAttack(Vector2Int cell)
        {
            return WaitingForInput
                && World.Contains(cell)
                && GridStep.IsAdjacent(PlayerCell, cell)
                && occupied.Contains(cell);
        }

        public bool TryMoveTo(Vector2Int destination)
        {
            if (!WaitingForInput || !CanEnter(destination))
            {
                return false;
            }

            Resolve(destination);
            return true;
        }

        public bool TryStep(Vector2Int offset)
        {
            return TryMoveTo(PlayerCell + offset);
        }

        public bool TryWait()
        {
            return CompleteInPlace();
        }

        public bool TryAttack(Vector2Int cell)
        {
            if (!CanAttack(cell))
            {
                return false;
            }

            return CompleteInPlace();
        }

        public void Vacate(Vector2Int cell)
        {
            occupied.Remove(cell);
        }

        public bool CanOccupantStep(Vector2Int from, Vector2Int to)
        {
            var distance = GridStep.Chebyshev(from, to);
            return occupied.Contains(from)
                && World.IsTerrainWalkable(to)
                && to != PlayerCell
                && !occupied.Contains(to)
                && distance >= 1
                && distance <= 2;
        }

        public bool TryMoveOccupant(Vector2Int from, Vector2Int to)
        {
            if (!CanOccupantStep(from, to))
            {
                return false;
            }

            occupied.Remove(from);
            occupied.Add(to);
            return true;
        }

        private bool CompleteInPlace()
        {
            if (!WaitingForInput)
            {
                return false;
            }

            Resolve(PlayerCell);
            return true;
        }

        private bool CanEnter(Vector2Int destination)
        {
            return World.IsTerrainWalkable(destination)
                && GridStep.IsAdjacent(PlayerCell, destination)
                && !occupied.Contains(destination);
        }

        private void Resolve(Vector2Int nextPlayerCell)
        {
            WaitingForInput = false;
            PlayerCell = nextPlayerCell;
            OnPlayerMoved?.Invoke();
            // Round order: each enemy takes their own turn (statuses → act), then the
            // player's next turn begins (statuses / floor / digestion). No global
            // status pass for all creatures at once after the player acts.
            TickEnemies();
            BeginPlayerTurn();
            Turn++;
            HighlightCell = null;
            WaitingForInput = true;
        }

        private void TickEnemies()
        {
            OnEnemyTurn?.Invoke();
        }

        private void BeginPlayerTurn()
        {
            OnEnvironment?.Invoke();
        }
    }
}
