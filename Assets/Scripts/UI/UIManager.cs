using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Daifugo.Core;
using System;
using System.Linq;

namespace Daifugo.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] public GameObject TitlePanel;
        [SerializeField] public GameObject RulePanel;
        [SerializeField] public GameObject GamePanel;
        [SerializeField] public GameObject ResultPanel;

        [Header("Game Elements")]
        [SerializeField] public Transform CpuHandArea;
        [SerializeField] public Transform PlayerHandArea;
        [SerializeField] public Transform FieldArea;
        [SerializeField] public Text MessageText;
        [SerializeField] public Text CpuInfoText;
        [SerializeField] public Text ResultText;
        [SerializeField] public Text StatsText; // For showing win/loss stats
        [SerializeField] public Text DiceResultText; // For dice animation

        [Header("Buttons")]
        [SerializeField] public Button PlayButton;
        [SerializeField] public Button PassButton;
        [SerializeField] public Button RetryButton;
        [SerializeField] public Button QuitButton;
        
        [Header("Title/Rule Buttons")]
        [SerializeField] public Button StartGameButton;
        [SerializeField] public Button ToRuleButton;
        [SerializeField] public Button ToTitleButtonFromRule;
        [SerializeField] public Button ToTitleButtonFromGame; // Quit button in game

        [Header("Prefabs")]
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private GameObject _cardBackPrefab; // For CPU hand

        // Resources
        private Dictionary<Suit, Sprite> _suitSprites;

        // Events
        public event Action OnStartGameClicked;
        public event Action OnToRuleClicked;
        public event Action OnToTitleClicked;
        public event Action OnPlayClicked;
        public event Action OnPassClicked;
        public event Action OnRetryClicked;
        public event Action<Card> OnCardClicked;

        private List<CardView> _playerCardViews = new List<CardView>();

        private void Awake()
        {
            LoadResources();
            BindButtons();
            ShowTitle();
        }

        private void LoadResources()
        {
            _suitSprites = new Dictionary<Suit, Sprite>();
            // Note: Ensure these paths match where we put the sprites
            _suitSprites[Suit.Spades] = Resources.Load<Sprite>("Cards/Spades");
            _suitSprites[Suit.Hearts] = Resources.Load<Sprite>("Cards/Hearts");
            _suitSprites[Suit.Diamonds] = Resources.Load<Sprite>("Cards/Diamonds");
            _suitSprites[Suit.Clubs] = Resources.Load<Sprite>("Cards/Clubs");
            
            // If sprites are not found, we might want to log error or use fallback
        }

        private void BindButtons()
        {
            if (StartGameButton) StartGameButton.onClick.AddListener(() => OnStartGameClicked?.Invoke());
            if (ToRuleButton) ToRuleButton.onClick.AddListener(() => OnToRuleClicked?.Invoke());
            if (ToTitleButtonFromRule) ToTitleButtonFromRule.onClick.AddListener(() => OnToTitleClicked?.Invoke());
            if (ToTitleButtonFromGame) ToTitleButtonFromGame.onClick.AddListener(() => OnToTitleClicked?.Invoke());
            
            if (PlayButton) PlayButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
            if (PassButton) PassButton.onClick.AddListener(() => OnPassClicked?.Invoke());
            if (RetryButton) RetryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (QuitButton) QuitButton.onClick.AddListener(() => OnToTitleClicked?.Invoke());
        }

        // --- State Management ---

        public void ShowTitle()
        {
            TitlePanel.SetActive(true);
            RulePanel.SetActive(false);
            GamePanel.SetActive(false);
            ResultPanel.SetActive(false);
        }

        public void ShowRule()
        {
            TitlePanel.SetActive(false);
            RulePanel.SetActive(true);
        }

        public void ShowGame()
        {
            TitlePanel.SetActive(false);
            RulePanel.SetActive(false);
            GamePanel.SetActive(true);
            ResultPanel.SetActive(false);
        }

        public void ShowResult(bool playerWin, string stats)
        {
            GamePanel.SetActive(false); // Or keep it visible in background
            ResultPanel.SetActive(true);
            
            if (ResultText) ResultText.text = playerWin ? "YOU WIN!" : "CPU WINS";
            if (StatsText) StatsText.text = stats;
        }

        // --- Game View Updates ---

        public void UpdateMessage(string msg)
        {
            if (MessageText) MessageText.text = msg;
        }

        public void UpdateCpuInfo(int cardCount)
        {
            if (CpuInfoText) CpuInfoText.text = $"CPU: {cardCount} cards";
            
            // Rebuild CPU hand visuals (Card Backs)
            foreach (Transform child in CpuHandArea) Destroy(child.gameObject);
            
            for (int i = 0; i < cardCount; i++)
            {
                if (_cardBackPrefab) Instantiate(_cardBackPrefab, CpuHandArea);
            }
        }

        public void UpdatePlayerHand(List<Card> hand, HashSet<Card> selectedCards)
        {
            // Clear existing
            foreach (Transform child in PlayerHandArea) Destroy(child.gameObject);
            _playerCardViews.Clear();

            // Create new
            foreach (var card in hand)
            {
                if (_cardPrefab)
                {
                    var view = Instantiate(_cardPrefab, PlayerHandArea);
                    Sprite s = _suitSprites.ContainsKey(card.Suit) ? _suitSprites[card.Suit] : null;
                    view.Initialize(card, s, OnCardViewClicked);
                    view.SetSelected(selectedCards.Contains(card));
                    _playerCardViews.Add(view);
                }
            }
        }

        public void UpdateField(List<Card> fieldCards)
        {
            // Clear existing
            foreach (Transform child in FieldArea) Destroy(child.gameObject);

            if (fieldCards == null) return;

            // Show cards on field
            foreach (var card in fieldCards)
            {
                if (_cardPrefab)
                {
                    var view = Instantiate(_cardPrefab, FieldArea);
                    Sprite s = _suitSprites.ContainsKey(card.Suit) ? _suitSprites[card.Suit] : null;
                    // Disable interaction for field cards
                    view.Initialize(card, s, null); 
                }
            }
        }

        private void OnCardViewClicked(CardView view)
        {
            OnCardClicked?.Invoke(view.Data);
        }
        
        // --- Dice Animation Helper ---
        public void ShowDiceResult(int result)
        {
            if (DiceResultText) DiceResultText.text = $"Dice: {result}";
        }
    }
}
