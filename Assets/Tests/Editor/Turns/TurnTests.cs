using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class TurnTests
    {
        [Test]
        public void PlayerStep_MovesOneCellAndAdvancesTurn()
        {
            var session = SessionFactory.WithControlled(World.CreateGrass(), new Vector2Int(4, 4));

            Assert.IsTrue(session.TryStep(Vector2Int.up));
            Assert.AreEqual(new Vector2Int(4, 5), session.ControlledCell);
            Assert.AreEqual(1, session.Turn);
            Assert.IsTrue(session.WaitingForInput);
        }

        [Test]
        public void PlayerStep_OffTheMap_IsIgnored()
        {
            var session = SessionFactory.WithControlled(World.CreateGrass(), new Vector2Int(0, 0));

            Assert.IsFalse(session.TryStep(Vector2Int.left));
            Assert.AreEqual(new Vector2Int(0, 0), session.ControlledCell);
            Assert.AreEqual(0, session.Turn);
        }

        [Test]
        public void Wait_AdvancesTurnWithoutMoving()
        {
            var session = SessionFactory.WithControlled(World.CreateGrass(), new Vector2Int(4, 4));

            Assert.IsTrue(session.TryWait());
            Assert.AreEqual(new Vector2Int(4, 4), session.ControlledCell);
            Assert.AreEqual(1, session.Turn);
        }

        [Test]
        public void NumpadKeys_MapToEightDirectionsAndWait()
        {
            Assert.IsTrue(GridStep.TryFromKey(KeyCode.Keypad8, out var north, out var waitN));
            Assert.AreEqual(Vector2Int.up, north);
            Assert.IsFalse(waitN);

            Assert.IsTrue(GridStep.TryFromKey(KeyCode.Keypad5, out _, out var wait));
            Assert.IsTrue(wait);

            Assert.IsTrue(GridStep.TryFromKey(KeyCode.Keypad7, out var nw, out _));
            Assert.AreEqual(new Vector2Int(-1, 1), nw);
        }

        [Test]
        public void Swipe_MapsToEightDirections()
        {
            Assert.AreEqual(Vector2Int.up, GridStep.FromSwipe(Vector2.up));
            Assert.AreEqual(Vector2Int.right, GridStep.FromSwipe(Vector2.right));
            Assert.AreEqual(new Vector2Int(1, 1), GridStep.FromSwipe(new Vector2(1f, 1f)));
        }

        [Test]
        public void Move_ClearsHighlight()
        {
            var session = SessionFactory.WithControlled(World.CreateGrass(), new Vector2Int(4, 4));
            session.HighlightCell = new Vector2Int(4, 5);

            Assert.IsTrue(session.TryStep(Vector2Int.up));
            Assert.IsNull(session.HighlightCell);
        }
    }
}
