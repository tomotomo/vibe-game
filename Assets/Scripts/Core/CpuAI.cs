using System.Collections.Generic;
using System.Linq;

namespace Daifugo.Core
{
    public class CpuAI
    {
        private RuleManager _ruleManager;

        public CpuAI(RuleManager ruleManager)
        {
            _ruleManager = ruleManager;
        }

        /// <summary>
        /// CPUの出すカードを決定する
        /// </summary>
        /// <param name="hand">CPUの手札</param>
        /// <param name="fieldTopCards">場のトップカード（なければnull/empty）</param>
        /// <returns>出すカードのリスト。パスならnullを返す。</returns>
        public List<Card> DecideMove(List<Card> hand, List<Card> fieldTopCards)
        {
            if (hand == null || hand.Count == 0) return null;

            // 1. 全候補の列挙
            var candidates = GenerateCandidates(hand, fieldTopCards);

            if (candidates.Count == 0) return null; // 出せるカードがない -> Pass

            // 2. 戦略的フィルタリング (Special Card Holding)
            var filteredCandidates = FilterCandidatesByStrategy(hand, candidates);

            // フィルタの結果、候補がなくなったらパス (温存戦略のため)
            if (filteredCandidates.Count == 0) return null;

            // 3. 最弱のカードを選ぶ
            // 革命中は「強さが低い＝数値的には強い」だが、RuleManagerの定義では
            // Normal: Strength high is strong.
            // Revolution: Strength low is strong.
            // "弱いカードから出す" ->
            // Normal: Strengthが低いものを出す
            // Revolution: Strengthが高いものを出す (RevolutionではStrengthが高い＝ランクが低い＝弱い)
            
            // つまり、常に「勝てるギリギリの弱さ」ではなく「手持ちの中で最も弱いもの」を出すなら、
            // Normal: 3, 4, ... -> Min Strength
            // Revolution: 2, A, K ... -> Max Strength (which represents 3, 4...)
            
            // Card.GetStrength() は固定値 (3=0...2=12)
            // 革命中: 3(0) is Strongest, 2(12) is Weakest.
            // 通常中: 2(12) is Strongest, 3(0) is Weakest.
            
            // 「弱いカードから出す」
            // 通常: Strengthが小さいものを選ぶ
            // 革命: Strengthが大きいものを選ぶ (Rankが2に近いもの＝革命下で弱い)

            filteredCandidates.Sort((a, b) =>
            {
                int strengthA = a[0].GetStrength();
                int strengthB = b[0].GetStrength();

                if (_ruleManager.IsEffectiveRevolution)
                {
                    // 革命中: 弱い＝Strengthが大きい (例: 2(12), A(11)...)
                    // Descending sort
                    return strengthB.CompareTo(strengthA);
                }
                else
                {
                    // 通常: 弱い＝Strengthが小さい (例: 3(0), 4(1)...)
                    // Ascending sort
                    return strengthA.CompareTo(strengthB);
                }
            });

            // 最も弱い候補を返す
            return filteredCandidates[0];
        }

        private List<List<Card>> GenerateCandidates(List<Card> hand, List<Card> fieldTopCards)
        {
            var candidates = new List<List<Card>>();
            
            // 手札をランクごとにグループ化
            var groupedHand = hand.GroupBy(c => c.Rank);

            foreach (var group in groupedHand)
            {
                var cards = group.ToList();
                int count = cards.Count;

                // 組み合わせ生成 (1枚〜4枚)
                // 場に出ている枚数と同じでなければならない（場がある場合）
                // 場がない場合（親）は、1〜4枚の全ての組み合わせが候補
                
                int requiredCount = (fieldTopCards != null && fieldTopCards.Count > 0) ? fieldTopCards.Count : 0;

                if (requiredCount > 0)
                {
                    // 枚数指定あり
                    if (count >= requiredCount)
                    {
                        var combs = GetCombinations(cards, requiredCount);
                        foreach (var comb in combs)
                        {
                            if (_ruleManager.CanPlayCards(comb, fieldTopCards))
                            {
                                candidates.Add(comb);
                            }
                        }
                    }
                }
                else
                {
                    // 自由（親）: 1枚〜持っている枚数までの全ての組み合わせ
                    for (int k = 1; k <= count; k++)
                    {
                        var combs = GetCombinations(cards, k);
                        foreach (var comb in combs)
                        {
                            // 親なのでCanPlayCardsは常にtrueのはずだが念のため
                            if (_ruleManager.CanPlayCards(comb, null))
                            {
                                candidates.Add(comb);
                            }
                        }
                    }
                }
            }

            return candidates;
        }

        private List<List<Card>> FilterCandidatesByStrategy(List<Card> hand, List<List<Card>> candidates)
        {
            var allowed = new List<List<Card>>();
            var specialRanks = new HashSet<int> { 1, 2, 5, 8 }; // A, 2, 5, 8

            foreach (var cand in candidates)
            {
                // これを出したら上がる？
                if (cand.Count == hand.Count)
                {
                    allowed.Add(cand);
                    continue;
                }

                // 特別なカードを含んでいるか？
                int rank = cand[0].Rank;
                if (specialRanks.Contains(rank))
                {
                    // 手札にそのランクのカードが何枚あるか
                    int countInHand = hand.Count(c => c.Rank == rank);
                    // 今回出す枚数
                    int countToPlay = cand.Count;

                    // 残りがあるならOK
                    if (countInHand > countToPlay)
                    {
                        allowed.Add(cand);
                    }
                    else
                    {
                        // 使い切ってしまう -> 温存 (リストに追加しない)
                    }
                }
                else
                {
                    // 特別なカードでないなら無条件OK
                    allowed.Add(cand);
                }
            }

            return allowed;
        }

        // 簡易的な組み合わせ生成 (nCk)
        private List<List<Card>> GetCombinations(List<Card> list, int length)
        {
            if (length == 1) return list.Select(t => new List<Card> { t }).ToList();
            if (length == list.Count) return new List<List<Card>> { new List<Card>(list) };

            // 今回の要件では、ペアはスート違いの同じランク。
            // 4枚持っていて2枚出す場合、どのスートを出すかも重要（縛り回避など）。
            // 全組み合わせを返す。
            
            var result = new List<List<Card>>();
            CombinationsRecursive(list, length, 0, new List<Card>(), result);
            return result;
        }

        private void CombinationsRecursive(List<Card> list, int length, int index, List<Card> current, List<List<Card>> result)
        {
            if (current.Count == length)
            {
                result.Add(new List<Card>(current));
                return;
            }

            for (int i = index; i < list.Count; i++)
            {
                current.Add(list[i]);
                CombinationsRecursive(list, length, i + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}
