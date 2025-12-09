using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Daifugo.Core;
using System;

namespace Daifugo.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _suitImageLarge;
        [SerializeField] private Image _suitImageSmall;
        [SerializeField] private Text _rankText;
        [SerializeField] private Image _highlightImage;

        public Card Data { get; private set; }
        public bool IsSelected { get; private set; }
        private Action<CardView> _onClick;

        public void Initialize(Card card, Sprite suitSprite, Action<CardView> onClick)
        {
            Data = card;
            _onClick = onClick;
            
            if (_suitImageLarge) _suitImageLarge.sprite = suitSprite;
            if (_suitImageSmall) _suitImageSmall.sprite = suitSprite;
            
            if (_rankText)
            {
                _rankText.text = GetRankString(card.Rank);
                // Simple color logic
                Color c = (card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds) ? Color.red : Color.black;
                _rankText.color = c;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_highlightImage) _highlightImage.enabled = selected;
            
            // Visual feedback: Move up slightly
            // Note: Assuming the parent has a LayoutGroup, localPosition might be overridden unless we use an intermediate container or padding.
            // For simplicity in this prototype, we'll try changing the rect transform pivot or padding, 
            // but changing localPosition directly usually works even in HorizontalLayoutGroup if we are just animating visual offset on a child element
            // or if we disable the layout control. 
            // Better approach for LayoutGroups: Change the 'transform.localPosition' of a *child* visual container, not the root of the prefab.
            // But let's assume direct modification for now. If LayoutGroup forces it back, we might need a LayoutElement.
            transform.localPosition = new Vector3(transform.localPosition.x, selected ? 20 : 0, 0);
        }

        private string GetRankString(int rank)
        {
            switch (rank)
            {
                case 1: return "A";
                case 11: return "J";
                case 12: return "Q";
                case 13: return "K";
                default: return rank.ToString();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(this);
        }
    }
}
