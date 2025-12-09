using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Daifugo.Core;

namespace Daifugo.Tests
{
    public class SmartPlayTests
    {
        private RuleManager _ruleManager;

        [SetUp]
        public void Setup()
        {
            _ruleManager = new RuleManager();
        }

        [Test]
        public void SinglePlay_Unique_ReturnsCard()
        {
            var field = new List<Card> { new Card(Suit.Spades, 3) };
            var hand = new List<Card> { new Card(Suit.Clubs, 4), new Card(Suit.Hearts, 5) };
            var clicked = hand[0]; // Club 4

            var result = SmartPlayLogic.GetCardsToPlay(clicked, hand, field, _ruleManager);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(clicked, result[0]);
        }

        [Test]
        public void SinglePlay_Ambiguous_SameRankInHand_ReturnsNull()
        {
            // Even for single play, if we have multiple cards of same rank (e.g. 4 Spades, 4 Hearts)
            // and field is 3. We click 4 Spades.
            // Requirement says: "出せる組み合わせが一意に定まる場合... 自動的に"
            // If I click 4 Spades, I clearly mean "Play 4 Spades".
            // My logic in SmartPlayLogic checks: "sameRankCards.Count == requiredCount".
            // Here required=1. sameRankCards=2.
            // So it returns NULL.
            // Is this correct? "Ambiguity" usually means "which combination".
            // But if I click a specific card for a SINGLE play, there is no ambiguity about WHICH card I want to play?
            // Wait, the spec says: "例：ペア出しでペア相手が1枚しかない場合など"
            // For single card, clicking it IS selecting it.
            // However, the logic implemented treats "having more cards of same rank than needed" as ambiguity.
            // Let's verify spec intent: "選択肢が複数ある場合は自動で出さず"
            // In single play, if I hold two 4s, and click one, do I have a choice?
            // Yes, I could play the OTHER 4. But I clicked THIS one.
            // Actually, for single play, Smart Play is just "Instant Play".
            // But my logic returns null if I have another 4.
            // Let's stick to the implemented logic (conservative) for now, or update if user wants "Instant Play for Single even if duplicates exist".
            // Current logic: If I have 4S and 4H, and click 4S -> It will NOT auto play.
            
            var field = new List<Card> { new Card(Suit.Spades, 3) };
            var hand = new List<Card> { new Card(Suit.Clubs, 4), new Card(Suit.Hearts, 4) };
            var clicked = hand[0]; // Club 4

            var result = SmartPlayLogic.GetCardsToPlay(clicked, hand, field, _ruleManager);

            // Expect null because logic says "Count > required" -> Ambiguous (conservative)
            Assert.IsNull(result); 
        }

        [Test]
        public void PairPlay_Unique_ReturnsPair()
        {
            var field = new List<Card> { new Card(Suit.Spades, 3), new Card(Suit.Hearts, 3) };
            var hand = new List<Card> { new Card(Suit.Clubs, 4), new Card(Suit.Diamonds, 4) }; // Only 2 fours
            var clicked = hand[0]; // Club 4

            var result = SmartPlayLogic.GetCardsToPlay(clicked, hand, field, _ruleManager);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(hand[0]));
            Assert.IsTrue(result.Contains(hand[1]));
        }

        [Test]
        public void PairPlay_Ambiguous_ReturnsNull()
        {
            var field = new List<Card> { new Card(Suit.Spades, 3), new Card(Suit.Hearts, 3) };
            var hand = new List<Card> { new Card(Suit.Clubs, 4), new Card(Suit.Diamonds, 4), new Card(Suit.Spades, 4) }; // 3 fours
            var clicked = hand[0]; // Club 4

            var result = SmartPlayLogic.GetCardsToPlay(clicked, hand, field, _ruleManager);

            Assert.IsNull(result);
        }

        [Test]
        public void PairPlay_SuitBound_Unique_ReturnsPair()
        {
            // Field: Spades 3, Hearts 3 (Suit Bound active scenario if we play Spades 4, Hearts 4)
            // But wait, "Suit Binding" is a state in RuleManager.
            // If RuleManager.IsSuitBound is true, we MUST follow suits.
            
            // Setup Binding
            var initialField = new List<Card> { new Card(Suit.Spades, 3), new Card(Suit.Hearts, 3) };
            // Manually set binding state? RuleManager internals are private set.
            // We can trigger it via CommitPlay.
            _ruleManager.CommitPlay(initialField, null); // Just to set some state? No, CommitPlay sets binding if PREVIOUS matches CURRENT.
            
            // To simulate IsSuitBound = true, we need to play matching suits on top of something.
            // Or use Reflection / Cheat for test.
            // Or just trust logic: pass a RuleManager that HAS IsSuitBound=true.
            // Since we can't easily set IsSuitBound from outside without playing, let's play a sequence.
            
            var f1 = new List<Card> { new Card(Suit.Spades, 3), new Card(Suit.Hearts, 3) };
            var f2 = new List<Card> { new Card(Suit.Spades, 4), new Card(Suit.Hearts, 4) };
            _ruleManager.CommitPlay(f2, f1); // Triggers Bind
            Assert.IsTrue(_ruleManager.IsSuitBound);

            // Now field is f2 (4S, 4H). We need to play 5S, 5H.
            // Hand has 5S, 5H, 5D. (3 cards)
            var hand = new List<Card> { new Card(Suit.Spades, 5), new Card(Suit.Hearts, 5), new Card(Suit.Diamonds, 5) };
            
            // If we click 5D -> It doesn't match binding suits (Spades/Hearts).
            // Logic should handle finding MATCHING cards.
            // 5D is not matching.
            // Wait, logic says: "Find all cards of same rank... Filter by bound suits".
            // If I click 5D, it is in sameRankCards.
            // Filter: fieldSuits={S, H}. matchingCards={5S, 5H}.
            // matchingCards count is 2. Required is 2.
            // So it IS unique valid combination!
            // Even if I clicked the WRONG suit card (5D)?
            // If I click 5D, but I MUST play 5S and 5H.
            // Does clicking 5D trigger playing 5S+5H?
            // Logic: "sameRankCards = {5S, 5H, 5D}".
            // "matchingCards = {5S, 5H}".
            // Result = {5S, 5H}.
            // So yes, clicking ANY 5 will result in auto-playing the ONLY valid 5s.
            // Unless CanPlayCards fails?
            // CanPlayCards checks if the CANDIDATES are valid. {5S, 5H} is valid.
            // So clicking 5D *should* work if logic allows it.
            // This seems "Smart".
            
            var clicked = hand[2]; // 5 Diamonds (Not in binding set)
            
            var result = SmartPlayLogic.GetCardsToPlay(clicked, hand, f2, _ruleManager);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(c => c.Suit == Suit.Spades));
            Assert.IsTrue(result.Any(c => c.Suit == Suit.Hearts));
        }
    }
}
