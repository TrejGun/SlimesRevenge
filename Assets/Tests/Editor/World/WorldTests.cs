using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class WorldTests
    {
        [Test]
        public void GrassWorld_IsTenByTenAndFilledWithGrass()
        {
            var world = World.CreateGrass();

            Assert.AreEqual(10, world.Width);
            Assert.AreEqual(10, world.Height);
            Assert.AreEqual(new Vector2Int(4, 4), world.Center);
            Assert.IsTrue(world.Contains(world.Center));
            Assert.IsFalse(world.Contains(new Vector2Int(-1, 0)));

            for (var y = 0; y < world.Height; y++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    var cell = world.GetCell(x, y);
                    Assert.AreEqual(new Vector2Int(x, y), cell.Position);
                    Assert.AreEqual(TerrainType.Grass, cell.Terrain);
                }
            }
        }
    }
}
