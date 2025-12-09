using System.Collections.Generic;
using System.Linq;

namespace Daifugo.Core
{
    public class RuleManager
    {
        // 革命状態かどうか（恒久的な革命：4枚出しなど）
        public bool IsRevolution { get; private set; }

        // Jバック状態かどうか（一時的な革命）
        public bool IsJBackActive { get; private set; }

        // スート縛り状態かどうか
        public bool IsSuitBound { get; private set; }

        // 現在の場のスート（縛り判定用）
        // 場が流れるたびにリセットされる
        public Suit? CurrentFieldSuit { get; private set; }
        
        // 複数枚出しの場合、全てのスートが一致した場合のみ縛りが発生するため、
        // 場のカードが複数枚のときは、それらが全て同じスートである必要がある。
        // 単に「縛りが発生しているか」と「縛りの対象スート」を管理すれば十分か。

        public RuleManager()
        {
            ResetRound();
        }

        public void ResetRound()
        {
            IsRevolution = false;
            ResetField();
        }

        public void ResetField()
        {
            IsSuitBound = false;
            IsJBackActive = false; // Jバック解除
            CurrentFieldSuit = null;
        }

        /// <summary>
        /// 現在の革命状態を取得（恒久革命とJバックのXOR）
        /// </summary>
        public bool IsEffectiveRevolution => IsRevolution ^ IsJBackActive;

        /// <summary>
        /// カードが出せるかどうかを判定する
        /// </summary>
        /// <param name="handCards">出すつもりのカード群</param>
        /// <param name="topFieldCards">現在場に出ている一番上のカード群（なければnull/empty）</param>
        /// <returns>出せるならtrue</returns>
        public bool CanPlayCards(List<Card> handCards, List<Card> topFieldCards)
        {
            if (handCards == null || handCards.Count == 0) return false;

            // 1. 出すカードが全て同じランクかチェック
            if (!AreAllSameRank(handCards)) return false;

            // 場のカードがない場合（親）
            if (topFieldCards == null || topFieldCards.Count == 0)
            {
                return true;
            }

            // 2. 枚数が一致しているかチェック
            if (handCards.Count != topFieldCards.Count) return false;

            // 3. 縛りチェック
            if (IsSuitBound)
            {
                // 縛りがある場合、出すカードのスートが縛りスートと一致（または含む）している必要がある
                // 単体出し: そのスートでなければならない
                // 複数出し: 場のスート構成と一致しなければならない... というのが厳密なルールだが、
                // 仕様書には「台札と同じスートのカードが出された場合...縛り発生」とある。
                // 縛り状態での出し方は「その場が流れるまで、そのスートしか出せなくなる」
                
                // 実装方針: 
                // 縛り発生中 (IsSuitBound == true) の場合、
                // 場に出ているカードのスート構成と、出すカードのスート構成が一致している必要がある。
                // 簡易化: 1枚出しならスート一致。複数枚なら、場のスート集合と出すスート集合が一致。
                
                // 今回は IsSuitBound が true のときは、直前のカード (topFieldCards) とスート構成が一致することを条件とする。
                if (!DoSuitsMatch(handCards, topFieldCards)) return false;
            }

            // 4. 強さチェック
            int handStrength = GetCardStrength(handCards[0]);
            int fieldStrength = GetCardStrength(topFieldCards[0]);

            if (IsEffectiveRevolution)
            {
                // 革命中: 弱いほうが勝つ
                return handStrength < fieldStrength;
            }
            else
            {
                // 通常: 強いほうが勝つ
                return handStrength > fieldStrength;
            }
        }

        /// <summary>
        /// カードを出した後の状態更新（革命、縛り、8切り、5スキップなどの判定用結果を返す）
        /// </summary>
        /// <param name="playedCards">出されたカード</param>
        /// <param name="previousTopCards">出す前の場のトップカード（縛り判定用）</param>
        /// <returns>発生したイベント情報の構造体やEnumなどを返すと良いが、一旦voidで内部状態更新のみ実装</returns>
        public GameEvent CommitPlay(List<Card> playedCards, List<Card> previousTopCards)
        {
            GameEvent events = GameEvent.None;
            
            // 1. 革命判定 (Jバック含む)
            // Jバック: 11(J)が出されると一時的革命
            bool hasJack = playedCards.Any(c => c.Rank == 11);
            if (hasJack)
            {
                // Jバック発動。現在のJバック状態を反転、あるいはセットする？
                // 仕様: "その場が流れるまで一時的に「革命」状態となり"
                // Jが出るたびに革命状態が「発生」する。
                // すでにJバック中なら？ -> 革命状態が維持されると解釈するのが自然（上書き）
                // ただし、もしIsRevolution(恒久)中にJが出たら？ -> 反転して通常に戻る「革命返し」的な挙動？
                // ここではシンプルに「Jが出たら JBackActive = true」とする。
                // もしJBackActiveが既にtrueならtrueのまま。
                // これにより、IsEffectiveRevolution = IsRevolution ^ true となり、状態が反転する。
                
                // 質問の回答は「Jバックと革命の重複 -> A案: 反転して通常に戻る」だった。
                // つまり IsRevolution=true のときに J(IsJBackActive=true) になると、 XOR で false (通常) になる。
                // これは IsEffectiveRevolution プロパティで実現できている。
                
                // しかし、もし既に JBackActive=true のときに、さらに J を出したら？
                // 仕様「Jバック: 11(J)が出されると...革命状態となり」
                // 重ねてJを出しても「革命状態となる」べき。
                // つまり IsJBackActive は true に固定されるべき。
                
                // でも待てよ、Jバック中にJを出した場合、
                // 「革命状態」の中で「11」は強いので出せる。
                // 出したらどうなる？「革命状態となり」 -> 革命維持。
                
                // よって、Jが出たら IsJBackActive = true にするだけで良い。
                
                if (!IsJBackActive)
                {
                    IsJBackActive = true;
                    events |= GameEvent.JBack;
                    events |= GameEvent.Revolution; // UI演出用
                }
            }
            
            // 通常の革命 (4枚以上出し)
            if (playedCards.Count >= 4)
            {
                IsRevolution = !IsRevolution;
                events |= GameEvent.Revolution;
            }

            // 2. 縛り判定
            // "台札（場に出ているカード）と同じスートのカードが出された場合...そのスートしか出せなくなる"
            // "複数枚出しの場合は、全てのスートが一致した場合のみ発生"
            if (previousTopCards != null && previousTopCards.Count > 0)
            {
                if (!IsSuitBound) // まだ縛られていない場合のみチェック
                {
                    if (DoSuitsMatch(playedCards, previousTopCards))
                    {
                        IsSuitBound = true;
                        events |= GameEvent.SuitBind;
                    }
                }
            }

            // 3. 8切り
            if (playedCards.Any(c => c.Rank == 8))
            {
                events |= GameEvent.EightCut;
                // 場を流す処理はGameController側で行う想定だが、
                // RuleManagerとしては「8切り発生」を通知する。
            }

            // 4. 5スキップ
            if (playedCards.Any(c => c.Rank == 5))
            {
                events |= GameEvent.FiveSkip;
            }

            return events;
        }

        private bool AreAllSameRank(List<Card> cards)
        {
            if (cards.Count == 0) return false;
            int rank = cards[0].Rank;
            return cards.All(c => c.Rank == rank);
        }

        private int GetCardStrength(Card card)
        {
            return card.GetStrength();
        }

        private bool DoSuitsMatch(List<Card> cards1, List<Card> cards2)
        {
            if (cards1.Count != cards2.Count) return false;

            // スートの集合が一致するか確認
            // 枚数が少ないのでソートして比較でOK
            var suits1 = cards1.Select(c => c.Suit).OrderBy(s => s).ToList();
            var suits2 = cards2.Select(c => c.Suit).OrderBy(s => s).ToList();

            for (int i = 0; i < suits1.Count; i++)
            {
                if (suits1[i] != suits2[i]) return false;
            }
            return true;
        }
    }

    [System.Flags]
    public enum GameEvent
    {
        None = 0,
        Revolution = 1 << 0,
        EightCut = 1 << 1,
        FiveSkip = 1 << 2,
        JBack = 1 << 3,
        SuitBind = 1 << 4
    }
}
