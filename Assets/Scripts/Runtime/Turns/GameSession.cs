using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Board state: world + unified occupancy. Player input is optional via
    /// <see cref="ControlledCell"/> — the board does not require a controlled creature.
    /// </summary>
    public sealed class GameSession
    {
        private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        public GameSession(World world, IEnumerable<Vector2Int> occupants = null)
        {
            World = world;
            WaitingForInput = true;
            if (occupants == null)
            {
                return;
            }

            foreach (var cell in occupants)
            {
                occupied.Add(cell);
            }
        }

        public World World { get; }

        /// <summary>
        /// Cell of the creature driven by player input. Null when the scene has no
        /// controlled participant. When set, the cell is always a member of occupied.
        /// </summary>
        public Vector2Int? ControlledCell { get; private set; }

        public bool WaitingForInput { get; private set; }

        public int Turn { get; private set; }

        public Vector2Int? HighlightCell { get; set; }

        public Action OnControlledMoved { get; set; }

        public Action OnEnemyTurn { get; set; }

        public Action OnEnvironment { get; set; }

        public bool IsOccupied(Vector2Int cell)
        {
            return occupied.Contains(cell);
        }

        public void Occupy(Vector2Int cell)
        {
            occupied.Add(cell);
        }

        public void Vacate(Vector2Int cell)
        {
            occupied.Remove(cell);
            if (ControlledCell == cell)
            {
                ControlledCell = null;
            }
        }

        /// <summary>
        /// Bind which occupied cell receives WASD / attack. Null clears control.
        /// Non-null <paramref name="cell"/> must already be occupied (or call <see cref="Occupy"/> first).
        /// </summary>
        public void SetControlled(Vector2Int? cell)
        {
            if (cell != null && !occupied.Contains(cell.Value))
            {
                return;
            }

            ControlledCell = cell;
        }

        public bool CanAttack(Vector2Int cell)
        {
            return WaitingForInput
                && ControlledCell != null
                && World.Contains(cell)
                && GridStep.IsAdjacent(ControlledCell.Value, cell)
                && occupied.Contains(cell);
        }

        public bool TryMoveTo(Vector2Int destination)
        {
            if (!WaitingForInput || ControlledCell == null || !CanEnter(destination))
            {
                return false;
            }

            Resolve(destination);
            return true;
        }

        public bool TryStep(Vector2Int offset)
        {
            if (ControlledCell == null)
            {
                return false;
            }

            return TryMoveTo(ControlledCell.Value + offset);
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

        public bool CanOccupantStep(Vector2Int from, Vector2Int to)
        {
            var distance = GridStep.Chebyshev(from, to);
            return occupied.Contains(from)
                && World.IsTerrainWalkable(to)
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
            if (ControlledCell == from)
            {
                ControlledCell = to;
            }

            return true;
        }

        private bool CompleteInPlace()
        {
            if (!WaitingForInput || ControlledCell == null)
            {
                return false;
            }

            Resolve(ControlledCell.Value);
            return true;
        }

        private bool CanEnter(Vector2Int destination)
        {
            return ControlledCell != null
                && World.IsTerrainWalkable(destination)
                && GridStep.IsAdjacent(ControlledCell.Value, destination)
                && !occupied.Contains(destination);
        }

        private void Resolve(Vector2Int nextControlledCell)
        {
            WaitingForInput = false;
            if (ControlledCell is Vector2Int from && from != nextControlledCell)
            {
                occupied.Remove(from);
                occupied.Add(nextControlledCell);
            }

            ControlledCell = nextControlledCell;
            OnControlledMoved?.Invoke();
            // Round order: each enemy takes their own turn (statuses → act), then the
            // controlled creature's next turn begins. No global status pass for all
            // creatures at once after the controlled act.
            TickEnemies();
            BeginControlledTurn();
            Turn++;
            HighlightCell = null;
            WaitingForInput = true;
        }

        private void TickEnemies()
        {
            OnEnemyTurn?.Invoke();
        }

        private void BeginControlledTurn()
        {
            OnEnvironment?.Invoke();
        }
    }
}
