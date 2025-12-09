using UnityEngine;
using Daifugo.Core;
using Daifugo.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Daifugo.Game
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;

        private RuleManager _ruleManager;
        private CpuAI _cpuAI;

        private List<Card> _playerHand = new List<Card>();
        private List<Card> _cpuHand = new List<Card>();
        private HashSet<Card> _selectedCards = new HashSet<Card>();
        private List<Card> _fieldCards = new List<Card>(); // The most recent set of cards played

        private bool _isPlayerTurn;
        private bool _gameActive;
        
        // Stats
        private const string KEY_GAMES = "Stats_Games";
        private const string KEY_WINS = "Stats_Wins";
        private const string KEY_STREAK = "Stats_Streak";
        private const string KEY_MAX_STREAK = "Stats_MaxStreak";

        private void Start()
        {
            _ruleManager = new RuleManager();
            _cpuAI = new CpuAI(_ruleManager);

            if (_uiManager == null)
            {
                _uiManager = FindObjectOfType<UIManager>();
            }

            BindUIEvents();
            _uiManager.ShowTitle();
        }

        private void BindUIEvents()
        {
            _uiManager.OnStartGameClicked += StartGameSequence;
            _uiManager.OnToRuleClicked += _uiManager.ShowRule;
            _uiManager.OnToTitleClicked += QuitGame;
            _uiManager.OnPlayClicked += PlayerTryPlay;
            _uiManager.OnPassClicked += PlayerPass;
            _uiManager.OnRetryClicked += StartGameSequence;
            _uiManager.OnCardClicked += OnPlayerCardClicked;
        }

                private void StartGameSequence()
                {
                    StartCoroutine(GameStartRoutine());
                }
        
                private IEnumerator GameStartRoutine()
                {
                    _uiManager.ShowGame();
                    _gameActive = true;
                    _ruleManager.ResetRound();
                    _fieldCards.Clear();
                    _selectedCards.Clear();
                    _uiManager.UpdateField(_fieldCards);
                    _uiManager.UpdateMessage("Rolling Dice...");
                    
                    // Hide buttons during dice roll
                    _uiManager.ToggleButtons(false, false, false, false);
        
                    // Dice Animation
                    int d1 = 1;
                    int d2 = 1;
                    
                    // Simple animation loop
                    for (int i = 0; i < 20; i++)
                    {
                        d1 = Random.Range(1, 7);
                        d2 = Random.Range(1, 7);
                        _uiManager.UpdateDice(d1, d2, true);
                        yield return new WaitForSeconds(0.05f);
                    }
        
                    int totalCards = d1 + d2;
                    _uiManager.UpdateMessage($"Hand Size: {totalCards}");
                    yield return new WaitForSeconds(1.0f);
                    _uiManager.UpdateDice(0, 0, false); // Hide dice
        
                    DealCards(totalCards);
        
                    // Determine first player (Random or Rule? Usually loser deals/winner starts, but for first game let's randomize or fixed)
                    // Spec doesn't specify. Let's random.
                    _isPlayerTurn = Random.value > 0.5f;
        
                    if (_isPlayerTurn)
                    {
                        StartPlayerTurn(false);
                    }
                    else
                    {
                        StartCoroutine(CpuTurnRoutine(false));
                    }
                }
        
                private void DealCards(int count)
                {
                    // Deck generation
                    var deck = new List<Card>();
                    foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
                    {
                        if (s == Suit.None) continue;
                        for (int r = 1; r <= 13; r++)
                        {
                            deck.Add(new Card(s, r));
                        }
                    }
        
                    // Shuffle
                    for (int i = 0; i < deck.Count; i++)
                    {
                        var temp = deck[i];
                        int randomIndex = Random.Range(i, deck.Count);
                        deck[i] = deck[randomIndex];
                        deck[randomIndex] = temp;
                    }
        
                    // Distribute
                    _playerHand = deck.Take(count).ToList();
                    _cpuHand = deck.Skip(count).Take(count).ToList();
        
                    SortHand(_playerHand);
                    SortHand(_cpuHand);
        
                    UpdateHandViews();
                }
        
                private void SortHand(List<Card> hand)
                {
                    // Sort by strength (Daifugo order)
                    // Usually players like to see cards sorted by rank 3..K, A, 2
                    hand.Sort(); // Uses Card.CompareTo which uses Strength
                }
        
                private void UpdateHandViews()
                {
                    _uiManager.UpdatePlayerHand(_playerHand, _selectedCards);
                    _uiManager.UpdateCpuInfo(_cpuHand.Count);
                }
        
                // --- Turn Logic ---
        
                private void StartPlayerTurn(bool isNewRound)
                {
                    if (!_gameActive) return;
                    _isPlayerTurn = true;
                    
                    // Button Visibility:
                    // Lead: Play, Exit
                    // Follow: Play, Pass, Exit
                    bool canPass = !isNewRound;
                    _uiManager.ToggleButtons(true, canPass, false, true);
                    
                    _uiManager.PlayButton.interactable = true;
                    if (canPass) _uiManager.PassButton.interactable = true;
                    
                    if (isNewRound)
                    {
                        _uiManager.UpdateMessage("Your Turn (Lead)");
                        _fieldCards.Clear();
                        _ruleManager.ResetField();
                        _uiManager.UpdateField(null);
                    }
                    else
                    {
                        _uiManager.UpdateMessage("Your Turn");
                    }
                }
        
                private void OnPlayerCardClicked(Card card)
                {
                    if (!_isPlayerTurn) return;
        
                    if (_selectedCards.Contains(card))
                    {
                        _selectedCards.Remove(card);
                    }
                    else
                    {
                        _selectedCards.Add(card);
                    }
                    
                    // Smart Play Check (if enabled/implemented)
                    // Spec: "手札をタップすると、出せるカードであれば自動的に選択・決定して即座に出す" when following?
                    // "スマートプレイ: 場にカードが出ている（Follow）状態で、手札をタップすると..."
                    // Let's implement manual select then Play button first, Smart Play is UX enhancement.
                    // But spec says "手札をタップして選択/解除 -> PLAYボタン" AND "Smart Play"
                    // For now, let's just toggle selection.
                    
                    _uiManager.UpdatePlayerHand(_playerHand, _selectedCards);
                }
        
                private void PlayerTryPlay()
                {
                    if (!_isPlayerTurn) return;
        
                    var cardsToPlay = _selectedCards.ToList();
                    if (cardsToPlay.Count == 0) return;
        
                    // Check validity
                    if (_ruleManager.CanPlayCards(cardsToPlay, _fieldCards))
                    {
                        // Play
                        PlayCards(_playerHand, cardsToPlay, true);
                        _selectedCards.Clear();
                    }
                    else
                    {
                        _uiManager.UpdateMessage("Cannot play these cards!");
                    }
                }
        
                private void PlayerPass()
                {
                    if (!_isPlayerTurn) return;
                    // Cannot pass if lead (field empty)
                    if (_fieldCards == null || _fieldCards.Count == 0)
                    {
                         _uiManager.UpdateMessage("Cannot pass on lead!");
                         return;
                    }
        
                    _uiManager.UpdateMessage("You Passed");
                    _isPlayerTurn = false;
                    StartCoroutine(ProcessPass(true));
                }
        
                private void PlayCards(List<Card> hand, List<Card> cards, bool isPlayer)
                {
                    // Remove from hand
                    foreach (var c in cards)
                    {
                        // Remove exact instance or matching value
                        var item = hand.FirstOrDefault(h => h.Equals(c));
                        if (item != null) hand.Remove(item);
                    }
        
                    // Update Field
                    var prevField = _fieldCards.ToList(); // Copy
                    _fieldCards = cards;
                    
                    // Rule Update
                    var events = _ruleManager.CommitPlay(cards, prevField);
                    
                    UpdateHandViews();
                    _uiManager.UpdateField(_fieldCards);
        
                    string eventMsg = "";
                    if (events.HasFlag(GameEvent.Revolution)) eventMsg += "Revolution! ";
                    if (events.HasFlag(GameEvent.JBack)) eventMsg += "J-Back! ";
                    if (events.HasFlag(GameEvent.SuitBind)) eventMsg += "Suit Binding! ";
                    if (events.HasFlag(GameEvent.EightCut)) eventMsg += "8-Cut! ";
                    if (events.HasFlag(GameEvent.FiveSkip)) eventMsg += "5-Skip! ";
                    
                    if (!string.IsNullOrEmpty(eventMsg)) _uiManager.UpdateMessage(eventMsg);
        
                    // Win Check
                    if (hand.Count == 0)
                    {
                        GameEnd(isPlayer);
                        return;
                    }
        
                    // Special Effects Handling
                    if (events.HasFlag(GameEvent.EightCut))
                    {
                        // 8切り: Field Clear and same player plays again
                        StartCoroutine(EightCutRoutine(isPlayer));
                    }
                    else if (events.HasFlag(GameEvent.FiveSkip))
                    {
                        // 5 Skip: Skip opponent, same player plays again
                        // Effectively same as 8 cut but field remains?
                        // Spec: "対戦相手のターンをスキップし、続けて自分のターンとなる"
                        // Usually field remains. So the player can add more on top?
                        // Or does it mean it's their turn as if they were responding to themselves?
                        // Daifugo rules: if you skip opponent, you play against the current field.
                        // You must play stronger cards against your own 5.
                        // Unless 5 was the strongest?
                        // Let's assume field remains.
                        if (isPlayer) StartPlayerTurn(false);
                        else StartCoroutine(CpuTurnRoutine(false));
                    }
                    else
                    {
                        // Normal turn switch
                        if (isPlayer) StartCoroutine(CpuTurnRoutine(false));
                        else StartPlayerTurn(false);
                    }
                }
        
                private IEnumerator EightCutRoutine(bool isPlayer)
                {
                    yield return new WaitForSeconds(1.0f);
                    _fieldCards.Clear();
                    _ruleManager.ResetField();
                    _uiManager.UpdateField(null);
                    _uiManager.UpdateMessage("Field Cleared (8 Cut)");
                    
                    if (isPlayer) StartPlayerTurn(true);
                    else StartCoroutine(CpuTurnRoutine(true));
                }
        
                private IEnumerator ProcessPass(bool playerPassed)
                {
                    // If one passes, the other becomes lead?
                    // Spec: "パスした場合は場が流れる" -> Confirm: "自分（最後に出した側）が親になる"
                    // So if Player passed, CPU (who played last) becomes lead.
                    // If CPU passed, Player (who played last) becomes lead.
                    
                    // Hide buttons during transition
                    _uiManager.ToggleButtons(false, false, false, false);
                    
                    yield return new WaitForSeconds(0.5f);
                    _fieldCards.Clear();
                    _ruleManager.ResetField();
                    _uiManager.UpdateField(null);
                    _uiManager.UpdateMessage("Field Cleared");
                    yield return new WaitForSeconds(0.5f);
        
                    if (playerPassed)
                    {
                        // Player passed, so CPU won the round -> CPU Lead
                        StartCoroutine(CpuTurnRoutine(true));
                    }
                    else
                    {
                        // CPU passed, so Player won the round -> Player Lead
                        StartPlayerTurn(true);
                    }
                }
        
                private IEnumerator CpuTurnRoutine(bool isNewRound)
                {
                    _isPlayerTurn = false;
                    // CPU Turn: Hide all buttons per instruction
                    _uiManager.ToggleButtons(false, false, false, false);
                    
                    string msg = isNewRound ? "CPU Turn (Lead)" : "CPU Turn";
                    _uiManager.UpdateMessage(msg);
        
                    yield return new WaitForSeconds(1.0f); // Thinking time
        
                    List<Card> move = _cpuAI.DecideMove(_cpuHand, isNewRound ? null : _fieldCards);
        
                    if (move != null)
                    {
                        PlayCards(_cpuHand, move, false);
                    }
                    else
                    {
                        _uiManager.UpdateMessage("CPU Passed");
                        yield return new WaitForSeconds(0.5f);
                        StartCoroutine(ProcessPass(false));
                    }
                }
                
                private void GameEnd(bool playerWin)
                {
                    _gameActive = false;
                    
                    // Update Stats
                    int games = PlayerPrefs.GetInt(KEY_GAMES, 0) + 1;
                    int wins = PlayerPrefs.GetInt(KEY_WINS, 0);
                    int streak = PlayerPrefs.GetInt(KEY_STREAK, 0);
                    int maxStreak = PlayerPrefs.GetInt(KEY_MAX_STREAK, 0);
        
                    if (playerWin)
                    {
                        wins++;
                        streak++;
                        if (streak > maxStreak) maxStreak = streak;
                    }
                    else
                    {
                        streak = 0;
                    }
        
                    PlayerPrefs.SetInt(KEY_GAMES, games);
                    PlayerPrefs.SetInt(KEY_WINS, wins);
                    PlayerPrefs.SetInt(KEY_STREAK, streak);
                    PlayerPrefs.SetInt(KEY_MAX_STREAK, maxStreak);
                    PlayerPrefs.Save();
        
                    float winRate = (float)wins / games * 100f;
                    string stats = $"Games: {games}\nWins: {wins}\nWin Rate: {winRate:F1}%\nStreak: {streak}\nMax Streak: {maxStreak}";
        
                    _uiManager.ShowResult(playerWin, stats);
                    // Result panel has its own buttons managed by UIManager's ShowResult (mostly static on panel)
                    // But we can ensure game buttons are hidden
                    _uiManager.ToggleButtons(false, false, false, false);
                }
        
                private void QuitGame()
                {
                    _gameActive = false;
                    StopAllCoroutines();
                    _uiManager.ShowTitle();
                }
        private void Update()
        {
            // Debug Screenshot
            if (Input.GetKeyDown(KeyCode.S))
            {
                string filename = "screenshot/screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                ScreenCapture.CaptureScreenshot(filename);
                Debug.Log($"Screenshot saved: {filename}");
            }
        }
    }
}
