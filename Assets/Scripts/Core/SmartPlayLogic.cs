using System.Collections.Generic;
using System.Linq;

namespace Daifugo.Core
{
    public static class SmartPlayLogic
    {
        /// <summary>
        /// スマートプレイ判定を行う
        /// </summary>
        /// <param name="clickedCard">クリックされたカード</param>
        /// <param name="hand">プレイヤーの手札</param>
        /// <param name="fieldCards">場のカード（nullまたは空ならLead）</param>
        /// <param name="ruleManager">ルールマネージャー（縛り判定などに使用）</param>
        /// <returns>自動で出すべきカードのリスト。自動で出さない場合はnull。</returns>
        public static List<Card> GetCardsToPlay(Card clickedCard, List<Card> hand, List<Card> fieldCards, RuleManager ruleManager)
        {
            // Lead時（場にカードがない）はスマートプレイしない仕様（仕様書には "場にカードが出ている（Follow）状態で..." とある）
            if (fieldCards == null || fieldCards.Count == 0)
            {
                return null;
            }

            int requiredCount = fieldCards.Count;

            // Find all cards of same rank in hand
            var sameRankCards = hand.Where(c => c.Rank == clickedCard.Rank).ToList();

            // If we don't have enough cards of this rank, we can't play them anyway
            if (sameRankCards.Count < requiredCount)
            {
                return null;
            }

            List<Card> candidates = null;

            if (sameRankCards.Count == requiredCount)
            {
                // Case 1: Exact number of cards available. No ambiguity.
                candidates = sameRankCards;
            }
            else // sameRankCards.Count > requiredCount
            {
                // Case 2: More cards than needed. Ambiguous unless constrained by Suit Binding.
                if (ruleManager.IsSuitBound)
                {
                    // Filter by bound suits
                    // The field cards have specific suits. We must match them.
                    var fieldSuits = fieldCards.Select(c => c.Suit).ToList();
                    var matchingCards = sameRankCards.Where(c => fieldSuits.Contains(c.Suit)).ToList();

                    if (matchingCards.Count == requiredCount)
                    {
                        // Verify if suits match exactly (just in case of duplicate suits logic which shouldn't happen in single deck)
                        candidates = matchingCards;
                    }
                }
            }

            // If we identified a unique candidate set, validate
            if (candidates != null)
            {
                if (ruleManager.CanPlayCards(candidates, fieldCards))
                {
                    return candidates;
                }
            }

            return null;
        }
    }
}
