using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>Shared GameSession builders for EditMode tests (board-only vs controlled).</summary>
    public static class SessionFactory
    {
        public static GameSession Board(World world, params Creature[] creatures)
        {
            return new GameSession(world, Cells(creatures));
        }

        public static GameSession WithControlled(
            World world,
            Creature controlled,
            params Creature[] others
        )
        {
            var cells = new List<Vector2Int> { controlled.Cell };
            if (others != null)
            {
                foreach (var other in others)
                {
                    if (other != null)
                    {
                        cells.Add(other.Cell);
                    }
                }
            }

            var session = new GameSession(world, cells);
            session.SetControlled(controlled.Cell);
            return session;
        }

        public static GameSession WithControlled(
            World world,
            Vector2Int controlled,
            params Vector2Int[] others
        )
        {
            var cells = new List<Vector2Int> { controlled };
            if (others != null)
            {
                cells.AddRange(others);
            }

            var session = new GameSession(world, cells);
            session.SetControlled(controlled);
            return session;
        }

        private static IEnumerable<Vector2Int> Cells(Creature[] creatures)
        {
            if (creatures == null)
            {
                yield break;
            }

            foreach (var creature in creatures)
            {
                if (creature != null)
                {
                    yield return creature.Cell;
                }
            }
        }
    }
}
