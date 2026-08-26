using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class VolumeTests
    {
        [Test]
        public void Capacity_IsTenUnits()
        {
            Assert.AreEqual(10, Volume.Capacity);
        }

        [Test]
        public void Add_StopsAtCapacity()
        {
            var volume = new Volume();
            for (var i = 0; i < Volume.Capacity + 3; i++)
            {
                volume.Add(new Water());
            }

            Assert.AreEqual(Volume.Capacity, volume.UnitCount);
        }

        [Test]
        public void Substances_HaveDistinctOpaqueColors()
        {
            Substance[] substances =
            {
                new Water(),
                new Oil(),
                new Poison(),
                new Acid(),
                new Blood(),
                new Lava()
            };
            var colors = new HashSet<Color>();
            foreach (var substance in substances)
            {
                Assert.AreEqual(1f, substance.Color.a, 0.001f);
                Assert.IsTrue(colors.Add(substance.Color), substance.GetType().Name);
            }
        }

        [Test]
        public void Damage_RemovesNewestUnitsFromTheEnd()
        {
            // Stack: Add appends; Damage pops the newest unit first.
            var volume = new Volume();
            volume.Fill(new Water(), new Oil());

            Assert.AreEqual(1, volume.Damage(1));
            Assert.AreEqual(1, volume.UnitCount);
            Assert.IsInstanceOf<Water>(volume.Units[0].Substance);
        }

        [Test]
        public void TryTakeEnd_PeelsNewestForDevour()
        {
            var volume = new Volume();
            volume.Fill(new Water(), new Oil(), new Blood());

            Assert.IsTrue(volume.TryTakeEnd(out var first));
            Assert.IsInstanceOf<Blood>(first);
            Assert.AreEqual(2, volume.UnitCount);
            Assert.IsInstanceOf<Oil>(volume.Units[1].Substance);
        }

        [Test]
        public void TryReplace_SwapsTheFirstMatchingUnit()
        {
            var volume = new Volume();
            volume.Fill(new Blood(), new Blood(), new Water());

            Assert.IsTrue(volume.TryReplace<Blood>(new Poison()));
            Assert.IsInstanceOf<Poison>(volume.Units[0].Substance);
            Assert.IsInstanceOf<Blood>(volume.Units[1].Substance);
            Assert.IsInstanceOf<Water>(volume.Units[2].Substance);
            Assert.IsFalse(volume.TryReplace<Oil>(new Poison()));
        }

        [Test]
        public void TryRemoveLast_TakesNewestMatchEvenWhenNotOnTop()
        {
            var volume = new Volume();
            volume.Fill(new Blood(), new Water(), new Blood(), new Water());

            Assert.IsTrue(volume.TryRemoveLast<Blood>());
            Assert.AreEqual(3, volume.UnitCount);
            Assert.IsInstanceOf<Blood>(volume.Units[0].Substance);
            Assert.IsInstanceOf<Water>(volume.Units[1].Substance);
            Assert.IsInstanceOf<Water>(volume.Units[2].Substance);
            Assert.IsFalse(volume.TryRemoveLast<Oil>());
        }
    }
}
