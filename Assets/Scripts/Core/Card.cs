using System;

namespace Daifugo.Core
{
    [Serializable]
    public class Card : IEquatable<Card>, IComparable<Card>
    {
        public Suit Suit { get; private set; }
        public int Rank { get; private set; } // 1 (Ace) to 13 (King)

        public Card(Suit suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// 大富豪的な強さを取得する (3=0, ... K=10, A=11, 2=12)
        /// 革命時は RuleManager 側で反転させるか、別のメソッドで評価する。
        /// ここでは標準的な強さを返す。
        /// </summary>
        public int GetStrength()
        {
            if (Rank == 2) return 12;
            if (Rank == 1) return 11;
            return Rank - 3; // 3->0, 4->1, ... 13->10
        }

        public override string ToString()
        {
            return $"{Suit} {Rank}";
        }

        public bool Equals(Card other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Card)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Suit, Rank);
        }

        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            // Default sort by strength
            int strengthDiff = GetStrength().CompareTo(other.GetStrength());
            if (strengthDiff != 0) return strengthDiff;
            return Suit.CompareTo(other.Suit);
        }
    }
}
