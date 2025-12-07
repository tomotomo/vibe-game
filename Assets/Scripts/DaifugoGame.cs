using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

namespace Daifugo
{
    public class DaifugoGame : MonoBehaviour
    {
        // Settings
        private const int HandSize = 7;

        // State
        private List<Card> deck;
        private List<Card> humanHand;
        private List<Card> cpuHand;
        private List<Card> fieldCards; // The active top cards on the pile
        
        private bool isPlayerTurn;
        private bool isRevolution;
        private bool isSuitBinding; // "Shibari"
        private Suit? boundSuit; // If binding is active, which suit? (Only relevant for singles usually, or logic needs to handle multiples)
        
        private bool gameEnded;
        
        // UI References
        private GameObject canvasObj;
        private Text statusText;
        private Transform fieldArea;
        private Transform playerArea;
        private Transform cpuArea;
        private Button playButton;
        private Button passButton;
        
        private List<int> selectedIndices = new List<int>();

        void Start()
        {
            SetupUI();
            StartGame();
        }

        void StartGame()
        {
            // Reset State
            gameEnded = false;
            isRevolution = false;
            isSuitBinding = false;
            boundSuit = null;
            fieldCards = new List<Card>();
            selectedIndices.Clear();
            
            // Create Deck
            deck = new List<Card>();
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            {
                for (int i = 1; i <= 13; i++)
                {
                    int rank = i;
                    if (i == 1) rank = 14; // A
                    if (i == 2) rank = 15; // 2
                    deck.Add(new Card(s, rank));
                }
            }

            // Shuffle
            for (int i = 0; i < deck.Count; i++)
            {
                Card temp = deck[i];
                int randomIndex = UnityEngine.Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }

            // Deal
            humanHand = new List<Card>();
            cpuHand = new List<Card>();
            for (int i = 0; i < HandSize; i++)
            {
                humanHand.Add(deck[0]); deck.RemoveAt(0);
                cpuHand.Add(deck[0]); deck.RemoveAt(0);
            }

            // Sort Hands
            SortHand(humanHand);
            SortHand(cpuHand);

            // Determine First Player (Random or specific? Spec says nothing. Random.)
            isPlayerTurn = UnityEngine.Random.value > 0.5f;

            UpdateUI();
            
            if (!isPlayerTurn)
            {
                StartCoroutine(CPUTurn());
            }
            else
            {
                statusText.text = "Your Turn";
            }
        }

        void SortHand(List<Card> hand)
        {
            hand.Sort((a, b) => a.rank.CompareTo(b.rank));
        }

        // --- Logic ---

        // Check if selected cards can be played
        bool CanPlay(List<Card> selected)
        {
            if (selected.Count == 0) return false;

            // 1. All selected cards must have same rank
            int r = selected[0].rank;
            foreach (var c in selected)
            {
                if (c.rank != r) return false;
            }

            // 2. If field is empty, any same-rank combo is valid
            if (fieldCards.Count == 0) return true;

            // 3. Must match count
            if (selected.Count != fieldCards.Count) return false;

            // 4. Must be stronger
            int fieldRank = fieldCards[0].rank;
            int myStrength = selected[0].GetStrength(isRevolution);
            int fieldStrength = fieldCards[0].GetStrength(isRevolution);

            if (myStrength <= fieldStrength) return false;

            // 5. Shibari (Suit Binding) Check
            if (isSuitBinding && boundSuit.HasValue)
            {
                // Simple Shibari: Single card binding.
                // If multiple cards, usually binding requires matching ALL suits.
                // For simplified impl, let's check if the selected cards CONTAIN the bound suit?
                // Actually, standard binding: "If the suits played match the suits on the field, subsequent plays must match those suits".
                // Simplification for 1v1: If single card played, check suit.
                
                if (selected.Count == 1)
                {
                    if (selected[0].suit != boundSuit.Value) return false;
                }
                else
                {
                    // For pairs/triples, strict binding requires matching the exact suit combination.
                    // This is complex. Let's stick to Single card binding for MVP or check exact match if binding is active.
                    // If we are in a "Bound" state, we must match field suits.
                    // Checking which suits are on field:
                    // This logic gets complicated.
                    // Let's implement: "Lock" happens if play matches field suits.
                    // If lock is active, play must match field suits.
                    
                    // Let's implement this later in "CheckBind".
                    // Here, we just check if we are violating an EXISTING lock.
                    if (isSuitBinding)
                    {
                        // Check if current play matches the suits of the field
                        // (Requires sorting suits to compare)
                        var selectedSuits = selected.Select(c => c.suit).OrderBy(s => s).ToList();
                        var fieldSuits = fieldCards.Select(c => c.suit).OrderBy(s => s).ToList();
                        
                        for(int i=0; i<selected.Count; i++)
                        {
                            if (selectedSuits[i] != fieldSuits[i]) return false;
                        }
                    }
                }
            }

            return true;
        }

        public void OnPass()
        {
            if (!isPlayerTurn) return;
            
            // Player passes.
            // In 1v1, if one passes, the other leads. 
            // So field is cleared.
            statusText.text = "You Passed. CPU leads.";
            ClearField();
            isPlayerTurn = false;
            UpdateUI();
            StartCoroutine(CPUTurn());
        }

        public void OnPlay()
        {
            if (!isPlayerTurn) return;

            List<Card> selected = new List<Card>();
            foreach (int i in selectedIndices)
            {
                selected.Add(humanHand[i]);
            }

            if (CanPlay(selected))
            {
                PlayCards(humanHand, selected);
                selectedIndices.Clear();
                
                if (CheckWin(humanHand)) return;

                // Handle Turn Check (Special rules might keep turn)
                if (CheckSpecialTurnRules(selected))
                {
                    // Player plays again
                    statusText.text = "Special Effect! Play again.";
                    UpdateUI();
                    return; 
                }

                isPlayerTurn = false;
                UpdateUI();
                StartCoroutine(CPUTurn());
            }
            else
            {
                statusText.text = "Invalid Move";
            }
        }

        void PlayCards(List<Card> hand, List<Card> toPlay)
        {
            // Check Binding before updating field
            CheckSuitBinding(toPlay);

            // Move to field
            fieldCards = new List<Card>(toPlay);
            foreach (var c in toPlay)
            {
                hand.Remove(c);
            }

            // Check Revolution (11 Back)
            if (fieldCards[0].rank == 11)
            {
                isRevolution = true;
                statusText.text = "11 Back! Revolution Active.";
            }

            UpdateUI();
        }

        void CheckSuitBinding(List<Card> toPlay)
        {
            if (fieldCards.Count == 0) 
            {
                isSuitBinding = false;
                boundSuit = null;
                return;
            }

            // If already bound, ignore (we already validated in CanPlay)
            if (isSuitBinding) return;

            // Check if this play TRIGGERS binding
            // Compare suits of toPlay vs fieldCards (previous)
            // Assumes same count (validated in CanPlay)
            
            var playSuits = toPlay.Select(c => c.suit).OrderBy(s => s).ToList();
            var fieldSuits = fieldCards.Select(c => c.suit).OrderBy(s => s).ToList();

            bool match = true;
            for(int i=0; i<playSuits.Count; i++)
            {
                if (playSuits[i] != fieldSuits[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                isSuitBinding = true;
                // For single card, easy to track. For multi, "isSuitBinding" flag implies we must match field suits.
                if (toPlay.Count == 1) boundSuit = toPlay[0].suit;
            }
        }

        bool CheckSpecialTurnRules(List<Card> played)
        {
            int r = played[0].rank;
            
            // 8 Cut
            if (r == 8)
            {
                ClearField();
                return true; // Play again
            }

            // 5 Skip
            if (r == 5)
            {
                // In 1v1, skip opponent = play again
                return true;
            }

            return false;
        }

        void ClearField()
        {
            fieldCards.Clear();
            isSuitBinding = false;
            boundSuit = null;
            // 11 Back usually lasts until field cleared? 
            // "11 Back" implies J is strong. Often it's temporary.
            // Let's reset Revolution if it was triggered by 11 Back?
            // Actually, J-Back usually means "Revolution while J is in play" or "Until sweep".
            // I'll assume it resets on sweep.
            if (isRevolution)
            {
                // Did we have a permanent Revolution (4 cards)? Not in spec.
                // Assuming 11 Back is the only revolution source here.
                isRevolution = false; 
            }
        }

        bool CheckWin(List<Card> hand)
        {
            if (hand.Count == 0)
            {
                gameEnded = true;
                string winner = (hand == humanHand) ? "You Win!" : "CPU Wins!";
                statusText.text = winner;
                playButton.interactable = false;
                passButton.interactable = false;
                return true;
            }
            return false;
        }

        IEnumerator CPUTurn()
        {
            yield return new WaitForSeconds(1.0f);

            // Simple AI: Find lowest valid play
            // Try 1 card, then 2 cards, etc? 
            // Just scan all combinations?
            // Since we sort hand, iterating is easier.
            
            List<Card> bestMove = null;

            // Group hand by rank
            var groups = cpuHand.GroupBy(c => c.rank).OrderBy(g => g.First().GetStrength(isRevolution));

            foreach (var g in groups)
            {
                var candidates = g.ToList();
                
                // Try to play as many as possible or match field?
                // If field is empty, play lowest single or pair?
                // Greedy: Play lowest possible that works.
                
                if (fieldCards.Count == 0)
                {
                    // Play lowest single (or pair if we have it?)
                    // Let's just play lowest single for simplicity or lowest set
                    bestMove = new List<Card> { candidates[0] };
                    // If we have pair, maybe play pair?
                    // Let's stick to simple legal moves.
                    break;
                }
                else
                {
                    // Must match count
                    if (candidates.Count >= fieldCards.Count)
                    {
                        var attempt = candidates.Take(fieldCards.Count).ToList();
                        if (CanPlay(attempt))
                        {
                            bestMove = attempt;
                            break;
                        }
                    }
                }
            }

            if (bestMove != null)
            {
                PlayCards(cpuHand, bestMove);
                if (CheckWin(cpuHand)) yield break;

                if (CheckSpecialTurnRules(bestMove))
                {
                    StartCoroutine(CPUTurn()); // CPU plays again
                }
                else
                {
                    isPlayerTurn = true;
                    statusText.text = "Your Turn";
                    UpdateUI();
                }
            }
            else
            {
                // Pass
                statusText.text = "CPU Passed. Your lead.";
                ClearField();
                isPlayerTurn = true;
                UpdateUI();
            }
        }

        // --- UI Handling ---
        
        public void ToggleCardSelection(int index)
        {
            if (!isPlayerTurn) return;

            if (selectedIndices.Contains(index))
                selectedIndices.Remove(index);
            else
                selectedIndices.Add(index);
            
            UpdateUI();
        }

        void UpdateUI()
        {
            if (gameEnded) return;

            // Update Field
            foreach (Transform t in fieldArea) Destroy(t.gameObject);
            if (fieldCards.Count > 0)
            {
                foreach (var c in fieldCards)
                {
                    CreateCardUI(c, fieldArea, false, -1);
                }
            }
            else
            {
                CreateTextUI("Empty", fieldArea);
            }

            // Update CPU
            foreach (Transform t in cpuArea) Destroy(t.gameObject);
            CreateTextUI($"CPU Hand: {cpuHand.Count}", cpuArea);

            // Update Player
            foreach (Transform t in playerArea) Destroy(t.gameObject);
            for (int i = 0; i < humanHand.Count; i++)
            {
                int index = i;
                bool isSelected = selectedIndices.Contains(i);
                CreateCardUI(humanHand[i], playerArea, true, index, isSelected);
            }

            // Buttons
            playButton.interactable = isPlayerTurn;
            passButton.interactable = isPlayerTurn;
        }

        void CreateCardUI(Card c, Transform parent, bool clickable, int index, bool selected = false)
        {
            GameObject cardObj = new GameObject(c.ToString());
            cardObj.transform.SetParent(parent);
            
            Image img = cardObj.AddComponent<Image>();
            img.color = selected ? Color.yellow : Color.white;
            
            // Layout
            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.minWidth = 50;
            le.minHeight = 70;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(cardObj.transform);
            Text t = textObj.AddComponent<Text>();
            t.text = c.ToString();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Common unity default font access? Or Arial.
            if (t.font == null) t.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleCenter;
            t.resizeTextForBestFit = true;
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (clickable)
            {
                Button b = cardObj.AddComponent<Button>();
                b.onClick.AddListener(() => ToggleCardSelection(index));
            }
        }

        void CreateTextUI(string content, Transform parent)
        {
            GameObject textObj = new GameObject("Info");
            textObj.transform.SetParent(parent);
            Text t = textObj.AddComponent<Text>();
            t.text = content;
            t.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            t.color = Color.white;
            t.fontSize = 20;
            LayoutElement le = textObj.AddComponent<LayoutElement>();
            le.minHeight = 30;
        }

        void SetupUI()
        {
            // EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // Canvas
            canvasObj = new GameObject("Canvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasObj.transform);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.4f, 0.1f); // Green table
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Main Layout
            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasObj.transform);
            VerticalLayoutGroup vlg = mainPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 20;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
            mainRt.anchorMin = Vector2.zero;
            mainRt.anchorMax = Vector2.one;
            mainRt.offsetMin = Vector2.zero;
            mainRt.offsetMax = Vector2.zero;

            // CPU Area
            GameObject cpuObj = new GameObject("CPU Area");
            cpuObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup cpuHlg = cpuObj.AddComponent<HorizontalLayoutGroup>();
            cpuHlg.childAlignment = TextAnchor.MiddleCenter;
            cpuHlg.childControlWidth = false;
            cpuHlg.childForceExpandWidth = false;
            cpuArea = cpuObj.transform;

            // Field Area
            GameObject fieldObj = new GameObject("Field Area");
            fieldObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup fieldHlg = fieldObj.AddComponent<HorizontalLayoutGroup>();
            fieldHlg.childAlignment = TextAnchor.MiddleCenter;
            fieldHlg.spacing = 10;
            fieldArea = fieldObj.transform;
            LayoutElement fieldLe = fieldObj.AddComponent<LayoutElement>();
            fieldLe.minHeight = 100;

            // Status Text
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(mainPanel.transform);
            statusText = statusObj.AddComponent<Text>();
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            statusText.fontSize = 24;
            statusText.color = Color.yellow;
            statusText.text = "Initializing...";
            
            // Player Area
            GameObject playerObj = new GameObject("Player Area");
            playerObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup playerHlg = playerObj.AddComponent<HorizontalLayoutGroup>();
            playerHlg.childAlignment = TextAnchor.MiddleCenter;
            playerHlg.spacing = 5;
            playerArea = playerObj.transform;
            LayoutElement playerLe = playerObj.AddComponent<LayoutElement>();
            playerLe.minHeight = 100;

            // Buttons Area
            GameObject btnObj = new GameObject("Buttons");
            btnObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup btnHlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.spacing = 20;

            // Pass Button
            GameObject passBtnObj = new GameObject("PassButton");
            passBtnObj.transform.SetParent(btnObj.transform);
            Image passImg = passBtnObj.AddComponent<Image>();
            passImg.color = Color.gray;
            passButton = passBtnObj.AddComponent<Button>();
            passButton.onClick.AddListener(OnPass);
            LayoutElement passLe = passBtnObj.AddComponent<LayoutElement>();
            passLe.minWidth = 100;
            passLe.minHeight = 40;
            GameObject passTxtObj = new GameObject("Text");
            passTxtObj.transform.SetParent(passBtnObj.transform);
            Text passTxt = passTxtObj.AddComponent<Text>();
            passTxt.text = "PASS";
            passTxt.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            passTxt.color = Color.black;
            passTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform passTxtRt = passTxtObj.GetComponent<RectTransform>();
            passTxtRt.anchorMin = Vector2.zero;
            passTxtRt.anchorMax = Vector2.one;
            passTxtRt.offsetMin = Vector2.zero;
            passTxtRt.offsetMax = Vector2.zero;

            // Play Button
            GameObject playBtnObj = new GameObject("PlayButton");
            playBtnObj.transform.SetParent(btnObj.transform);
            Image playImg = playBtnObj.AddComponent<Image>();
            playImg.color = Color.cyan;
            playButton = playBtnObj.AddComponent<Button>();
            playButton.onClick.AddListener(OnPlay);
            LayoutElement playLe = playBtnObj.AddComponent<LayoutElement>();
            playLe.minWidth = 100;
            playLe.minHeight = 40;
            GameObject playTxtObj = new GameObject("Text");
            playTxtObj.transform.SetParent(playBtnObj.transform);
            Text playTxt = playTxtObj.AddComponent<Text>();
            playTxt.text = "PLAY";
            playTxt.font = Resources.FindObjectsOfTypeAll<Font>()[0];
            playTxt.color = Color.black;
            playTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform playTxtRt = playTxtObj.GetComponent<RectTransform>();
            playTxtRt.anchorMin = Vector2.zero;
            playTxtRt.anchorMax = Vector2.one;
            playTxtRt.offsetMin = Vector2.zero;
            playTxtRt.offsetMax = Vector2.zero;
        }
    }
}
