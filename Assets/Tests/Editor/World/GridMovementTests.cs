using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class GridMovementTests
    {
        [Test]
        public void CenterSlime_PressesD_MovesOneCellRight()
        {
            var session = OpenArena(new Vector2Int(1, 1));

            Assert.IsTrue(Press(session, KeyCode.D));
            Assert.AreEqual(new Vector2Int(2, 1), session.ControlledCell);
        }

        [Test]
        public void SlimeAtWall_PressesTowardWall_DoesNotMove()
        {
            var session = OpenArena(new Vector2Int(0, 1));

            Assert.IsFalse(Press(session, KeyCode.A));
            Assert.AreEqual(new Vector2Int(0, 1), session.ControlledCell);
            Assert.AreEqual(0, session.Turn);
        }

        [Test]
        public void CenterSlime_StepsIntoRatOnTheRight_DoesNotMove()
        {
            var rat = new Vector2Int(2, 1);
            var session = OpenArena(new Vector2Int(1, 1), rat);

            Assert.IsFalse(Press(session, KeyCode.D));
            Assert.AreEqual(new Vector2Int(1, 1), session.ControlledCell);
            Assert.AreEqual(0, session.Turn);
        }

        [Test]
        public void CenterSlime_CanStepDiagonallyIntoEmptyCell()
        {
            var session = OpenArena(new Vector2Int(1, 1));

            Assert.IsTrue(Press(session, KeyCode.Keypad9));
            Assert.AreEqual(new Vector2Int(2, 2), session.ControlledCell);
        }

        [Test]
        public void CenterSlime_CannotStepTwoCells()
        {
            var session = OpenArena(new Vector2Int(1, 1));

            Assert.IsFalse(session.TryMoveTo(new Vector2Int(1, 1) + Vector2Int.right * 2));
            Assert.AreEqual(new Vector2Int(1, 1), session.ControlledCell);
        }

        [Test]
        public void BlockedMove_StillWaitsForInput()
        {
            var session = OpenArena(new Vector2Int(1, 1), new Vector2Int(2, 1));

            Assert.IsFalse(session.TryStep(Vector2Int.right));
            Assert.IsTrue(session.WaitingForInput);
        }

        private static GameSession OpenArena(Vector2Int slime, params Vector2Int[] others)
        {
            var world = World.CreateGrass(3);
            Assert.AreEqual(3, world.Width);
            Assert.AreEqual(3, world.Height);
            Assert.AreEqual(new Vector2Int(1, 1), world.Center);
            return SessionFactory.WithControlled(world, slime, others);
        }

        private static bool Press(GameSession session, KeyCode key)
        {
            Assert.IsTrue(GridStep.TryFromKey(key, out var offset, out var wait));
            Assert.IsFalse(wait);
            return session.TryStep(offset);
        }
    }
}
