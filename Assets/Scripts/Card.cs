using System;
using System.Collections.Generic;
using UnityEngine;

namespace Daifugo
{
    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club
    }

    [System.Serializable]
    public class Card : IComparable<Card>
    {
        public Suit suit;
        public int rank; // 3, 4, ..., 9, 10, 11(J), 12(Q), 13(K), 14(A), 15(2)

        public Card(Suit s, int r)
        {
            suit = s;
            rank = r;
        }

        public int GetStrength(bool isReverse)
        {
            // Standard: 3 is weak (3), 2 is strong (15)
            // Reverse: 3 is strong, 2 is weak
            if (isReverse)
            {
                // Invert strength
                // 3 -> 15, 15 -> 3
                return 18 - rank; 
            }
            return rank;
        }

        public override string ToString()
        {
            string r = rank.ToString();
            if (rank == 11) r = "J";
            if (rank == 12) r = "Q";
            if (rank == 13) r = "K";
            if (rank == 14) r = "A";
            if (rank == 15) r = "2";

            string s;
            switch (suit)
            {
                case Suit.Spade: s = "♠️"; break;
                case Suit.Heart: s = "♥️"; break;
                case Suit.Diamond: s = "♦️"; break;
                case Suit.Club: s = "♣️"; break;
                default: s = "?"; break;
            }
            return $"{s}{r}";
        }

        public int CompareTo(Card other)
        {
            // Default sort by rank
            if (rank != other.rank)
                return rank.CompareTo(other.rank);
            return suit.CompareTo(other.suit);
        }
    }
}
