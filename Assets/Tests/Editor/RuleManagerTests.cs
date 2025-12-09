using System.Collections.Generic;
using NUnit.Framework;
using Daifugo.Core;

namespace Daifugo.Tests
{
    public class RuleManagerTests
    {
        private RuleManager _ruleManager;

        [SetUp]
        public void Setup()
        {
            _ruleManager = new RuleManager();
        }

        [Test]
        public void CardStrength_Normal()
        {
            var weak = new Card(Suit.Spades, 3);
            var strong = new Card(Suit.Spades, 2);
            
            Assert.Less(weak.GetStrength(), strong.GetStrength());
        }

        [Test]
        public void CanPlayCards_Normal_StrongerWins()
        {
            var fieldCards = new List<Card> { new Card(Suit.Clubs, 3) };
            var handCards = new List<Card> { new Card(Suit.Clubs, 4) };

            Assert.IsTrue(_ruleManager.CanPlayCards(handCards, fieldCards));
        }

        [Test]
        public void CanPlayCards_Normal_WeakerFails()
        {
            var fieldCards = new List<Card> { new Card(Suit.Clubs, 4) };
            var handCards = new List<Card> { new Card(Suit.Clubs, 3) };

            Assert.IsFalse(_ruleManager.CanPlayCards(handCards, fieldCards));
        }

        [Test]
        public void CanPlayCards_Revolution_WeakerWins()
        {
            // Trigger revolution manually or simulate it? 
            // Since IsRevolution is private set, we can trigger it via CommitPlay with 4 cards or J
            // Or use reflection? For now, let's use CommitPlay to trigger revolution with 4 cards first.
            
            var fourCards = new List<Card> { 
                new Card(Suit.Spades, 3), new Card(Suit.Hearts, 3), 
                new Card(Suit.Diamonds, 3), new Card(Suit.Clubs, 3) 
            };
            _ruleManager.CommitPlay(fourCards, null);

            Assert.IsTrue(_ruleManager.IsRevolution);

            var fieldCards = new List<Card> { new Card(Suit.Clubs, 4) };
            var handCards = new List<Card> { new Card(Suit.Clubs, 3) };

            // In revolution, 3 is stronger than 4
            Assert.IsTrue(_ruleManager.CanPlayCards(handCards, fieldCards));
        }

        [Test]
        public void CommitPlay_EightCut()
        {
            var played = new List<Card> { new Card(Suit.Spades, 8) };
            var result = _ruleManager.CommitPlay(played, null);

            Assert.IsTrue(result.HasFlag(GameEvent.EightCut));
        }

        [Test]
        public void CommitPlay_FiveSkip()
        {
            var played = new List<Card> { new Card(Suit.Spades, 5) };
            var result = _ruleManager.CommitPlay(played, null);

            Assert.IsTrue(result.HasFlag(GameEvent.FiveSkip));
        }

        [Test]
        public void CommitPlay_JBack_ActivatesTemporaryRevolution()
        {
            Assert.IsFalse(_ruleManager.IsRevolution);
            Assert.IsFalse(_ruleManager.IsJBackActive);

            var played = new List<Card> { new Card(Suit.Spades, 11) }; // Jack
            var result = _ruleManager.CommitPlay(played, null);

            Assert.IsTrue(result.HasFlag(GameEvent.JBack));
            Assert.IsTrue(_ruleManager.IsJBackActive);
            Assert.IsTrue(_ruleManager.IsEffectiveRevolution);
            
            // Field clear should reset it
            _ruleManager.ResetField();
            Assert.IsFalse(_ruleManager.IsJBackActive);
            Assert.IsFalse(_ruleManager.IsEffectiveRevolution);
        }

        [Test]
        public void CanPlay_JBack_NoBind_3StrongerThan10()
        {
            // 1. Play Heart J (Activates J-Back/Revolution)
            var jCards = new List<Card> { new Card(Suit.Hearts, 11) };
            _ruleManager.CommitPlay(jCards, null);
            Assert.IsTrue(_ruleManager.IsJBackActive);

            // 2. Play Club 10 (on top of Heart J) -> No bind
            var tenCards = new List<Card> { new Card(Suit.Clubs, 10) };
            _ruleManager.CommitPlay(tenCards, jCards);
            Assert.IsFalse(_ruleManager.IsSuitBound);

            // 3. Try to play Spade 3
            // In Revolution, 3 (Strength=0) > 10 (Strength=7)? 
            // Wait, standard strength: 3=0, 4=1 ... 10=7 ... A=11, 2=12
            // Revolution: Weaker is Stronger.
            // 3 has strength 0. 10 has strength 7.
            // 0 < 7. In revolution, 0 wins.
            
            var threeCards = new List<Card> { new Card(Suit.Spades, 3) };
            Assert.IsTrue(_ruleManager.CanPlayCards(threeCards, tenCards), "3 should beat 10 in J-Back");
        }

        [Test]
        public void CanPlay_JBack_WithBind_SuitRestriction()
        {
            // 1. Play Club J (Activates J-Back)
            var jCards = new List<Card> { new Card(Suit.Clubs, 11) };
            _ruleManager.CommitPlay(jCards, null);

            // 2. Play Club 10 (on top of Club J) -> Club Bind!
            var tenCards = new List<Card> { new Card(Suit.Clubs, 10) };
            var events = _ruleManager.CommitPlay(tenCards, jCards);
            
            Assert.IsTrue(events.HasFlag(GameEvent.SuitBind));
            Assert.IsTrue(_ruleManager.IsSuitBound);

            // 3. Try to play Spade 3 (Strong enough, but wrong suit)
            var spadeThree = new List<Card> { new Card(Suit.Spades, 3) };
            Assert.IsFalse(_ruleManager.CanPlayCards(spadeThree, tenCards), "Spade 3 should fail due to Club binding");

            // 4. Try to play Club 3 (Strong enough AND correct suit)
            var clubThree = new List<Card> { new Card(Suit.Clubs, 3) };
            Assert.IsTrue(_ruleManager.CanPlayCards(clubThree, tenCards), "Club 3 should pass");
        }

        [Test]
        public void SuitBinding_Activates()
        {
            var field = new List<Card> { new Card(Suit.Spades, 3) };
            var played = new List<Card> { new Card(Suit.Spades, 4) };

            var result = _ruleManager.CommitPlay(played, field);
            
            Assert.IsTrue(result.HasFlag(GameEvent.SuitBind));
            Assert.IsTrue(_ruleManager.IsSuitBound);
        }
        
        [Test]
        public void SuitBinding_RestrictsPlay()
        {
            // Activate binding
            var initialField = new List<Card> { new Card(Suit.Spades, 3) };
            var play1 = new List<Card> { new Card(Suit.Spades, 4) };
            _ruleManager.CommitPlay(play1, initialField);
            
            Assert.IsTrue(_ruleManager.IsSuitBound);

            // Try to play non-matching suit (Hearts) against Spades
            var fieldNow = play1;
            var playHearts = new List<Card> { new Card(Suit.Hearts, 5) };
            
            // Rank 5 > 4, so normally ok, but suit bound to Spades
            Assert.IsFalse(_ruleManager.CanPlayCards(playHearts, fieldNow));

            // Try to play matching suit (Spades)
            var playSpades = new List<Card> { new Card(Suit.Spades, 6) };
            Assert.IsTrue(_ruleManager.CanPlayCards(playSpades, fieldNow));
        }
    }
}
