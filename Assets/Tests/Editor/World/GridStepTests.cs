using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class GridStepTests
    {
        [Test]
        public void IsAdjacent_IncludesDiagonals_NotFarther()
        {
            var origin = Vector2Int.zero;
            Assert.IsTrue(GridStep.IsAdjacent(origin, Vector2Int.right));
            Assert.IsTrue(GridStep.IsAdjacent(origin, Vector2Int.up));
            Assert.IsTrue(GridStep.IsAdjacent(origin, new Vector2Int(1, 1)));
            Assert.IsTrue(GridStep.IsAdjacent(origin, new Vector2Int(-1, 1)));
            Assert.IsFalse(GridStep.IsAdjacent(origin, origin));
            Assert.IsFalse(GridStep.IsAdjacent(origin, new Vector2Int(2, 0)));
            Assert.IsFalse(GridStep.IsAdjacent(origin, new Vector2Int(2, 1)));
        }

        [Test]
        public void AdjacentOffsets_AreExactlyChebyshevOne()
        {
            Assert.AreEqual(8, GridStep.AdjacentOffsets.Length);
            var seen = new HashSet<Vector2Int>();
            foreach (var offset in GridStep.AdjacentOffsets)
            {
                Assert.AreEqual(1, GridStep.Chebyshev(Vector2Int.zero, offset));
                Assert.IsTrue(seen.Add(offset));
            }
        }

        [Test]
        public void Ring_DistanceOne_MatchesAdjacentOffsets()
        {
            var origin = new Vector2Int(3, 4);
            var ring = new HashSet<Vector2Int>(GridStep.Ring(origin, 1));
            Assert.AreEqual(8, ring.Count);
            foreach (var offset in GridStep.AdjacentOffsets)
            {
                Assert.IsTrue(ring.Contains(origin + offset));
            }
        }

        [Test]
        public void IsAdjacent_Creatures_UsesCells()
        {
            var a = new GameObject("A").AddComponent<Rat>();
            var b = new GameObject("B").AddComponent<Dog>();
            try
            {
                a.PlaceOn(Vector2Int.zero);
                b.PlaceOn(new Vector2Int(1, 1));
                Assert.IsTrue(GridStep.IsAdjacent(a, b));
                Assert.IsTrue(CreatureMoves.IsAdjacent(a, b));
                b.PlaceOn(new Vector2Int(2, 0));
                Assert.IsFalse(GridStep.IsAdjacent(a, b));
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
            }
        }
    }
}
