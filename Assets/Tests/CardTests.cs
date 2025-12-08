using NUnit.Framework;
using Daifugo;

namespace Daifugo.Tests
{
    public class CardTests
    {
        [Test]
        public void TestCardComparison()
        {
            // 3 vs 4
            Card c3 = new Card(Suit.Spade, 3);
            Card c4 = new Card(Suit.Spade, 4);
            Assert.Less(c3.CompareTo(c4), 0, "Rank 3 should be less than Rank 4 in sort order (not strength yet)");

            // 2 (Rank 15) vs A (Rank 14)
            Card c2 = new Card(Suit.Spade, 15);
            Card cA = new Card(Suit.Spade, 14);
            Assert.Greater(c2.CompareTo(cA), 0, "Rank 2 (15) should be greater than Rank A (14)");
        }

        [Test]
        public void TestCardStrength_Normal()
        {
            // Normal: 3 is weak (3), 2 is strong (15)
            Card c3 = new Card(Suit.Spade, 3);
            Card c2 = new Card(Suit.Spade, 15);

            Assert.Less(c3.GetStrength(false), c2.GetStrength(false), "In Normal, 3 should be weaker than 2");
        }

        [Test]
        public void TestCardStrength_Revolution()
        {
            // Revolution: 3 is strong, 2 is weak
            Card c3 = new Card(Suit.Spade, 3);
            Card c2 = new Card(Suit.Spade, 15);

            // 3 strength becomes 18-3 = 15
            // 2 strength becomes 18-15 = 3
            Assert.Greater(c3.GetStrength(true), c2.GetStrength(true), "In Revolution, 3 should be stronger than 2");
        }

        [Test]
        public void TestToString_Emoji()
        {
            Card c = new Card(Suit.Heart, 11); // Jack
            Assert.AreEqual("♥️J", c.ToString());

            Card c2 = new Card(Suit.Spade, 15); // 2
            Assert.AreEqual("♠️2", c2.ToString());
        }
    }
}
