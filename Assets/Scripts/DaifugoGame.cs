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
        // HandSize determined at runtime

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
        
        private int matchCount = 0;
        private int winCount = 0;

        // UI References
        private GameObject canvasObj;
        private GameObject mainPanel; // Game UI
        private GameObject titlePanel; // Start Screen
        private GameObject rulesPanel; // Rules Overlay
        private Text statusText;
        private Text statsText; // New Stats UI
        private Transform fieldArea;
        private Transform playerArea;
        private Transform cpuArea;
        private Button playButton;
        private Button passButton;
        private Button retryButton;
        
        private List<int> selectedIndices = new List<int>();

        void Start()
        {
            SetupUI();
            // StartCoroutine(GameSequence()); // Wait for Start Button
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                TakeScreenshot();
            }
        }

        void TakeScreenshot()
        {
            // Application.dataPath points to 'Assets'. Parent is project root.
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string folderPath = System.IO.Path.Combine(projectRoot, "screenshot");

            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string fileName = $"screenshot_{sceneName}.png";
            string fullPath = System.IO.Path.Combine(folderPath, fileName);

            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"Screenshot saved to: {fullPath}");
        }

        public void OnStartGamePressed()
        {
            titlePanel.SetActive(false);
            mainPanel.SetActive(true);
            StartCoroutine(GameSequence());
        }

        public void OnRulesPressed()
        {
            rulesPanel.SetActive(true);
        }

        public void OnCloseRulesPressed()
        {
            rulesPanel.SetActive(false);
        }

        IEnumerator GameSequence()
        {
            // Dice Roll Phase
            statusText.text = "Rolling Dice to determine Hand Size...";
            yield return new WaitForSeconds(0.5f);

            // Create Dice Objects
            GameObject pDiceObj = new GameObject("PlayerDice");
            DiceAnimation pDice = pDiceObj.AddComponent<DiceAnimation>();
            // Move Dice UP to avoid overlap with center/status
            pDice.Setup(canvasObj.transform, new Vector2(-80, 100));

            GameObject cDiceObj = new GameObject("CPUDice");
            DiceAnimation cDice = cDiceObj.AddComponent<DiceAnimation>();
            cDice.Setup(canvasObj.transform, new Vector2(80, 100));

            int pRoll = UnityEngine.Random.Range(1, 7);
            int cRoll = UnityEngine.Random.Range(1, 7);
            
            pDice.Roll(pRoll);
            cDice.Roll(cRoll);

            yield return new WaitForSeconds(1.5f); // Wait for animation

            int total = pRoll + cRoll;

            statusText.text = $"Total: {total} Cards (You: {pRoll}, CPU: {cRoll})";
            yield return new WaitForSeconds(2.0f);

            // Cleanup Dice
            Destroy(pDiceObj);
            Destroy(cDiceObj);

            InitializeRound(total);
        }

        void InitializeRound(int handSize)
        {
            // Reset State
            gameEnded = false;
            if (retryButton != null) retryButton.gameObject.SetActive(false);
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
            
            // Ensure we don't exceed deck size (52 cards total, 26 max per person if dealing all, but here total * 2)
            // total is max 12, so 24 cards. Safe.
            for (int i = 0; i < handSize; i++)
            {
                if (deck.Count > 0) { humanHand.Add(deck[0]); deck.RemoveAt(0); }
                if (deck.Count > 0) { cpuHand.Add(deck[0]); deck.RemoveAt(0); }
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
                if (selected.Count == 1)
                {
                    if (selected[0].suit != boundSuit.Value) return false;
                }
                else
                {
                    if (isSuitBinding)
                    {
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
            
            statusText.text = "You Passed. CPU leads.";
            ClearField();
            isPlayerTurn = false;
            UpdateUI();
            StartCoroutine(CPUTurn());
        }

        // Unified Card Interaction
        public void OnCardClicked(int index)
        {
            if (!isPlayerTurn) return;

            // If field is empty (Lead), we use Selection Mode -> Play Button
            if (fieldCards.Count == 0)
            {
                ToggleCardSelection(index);
            }
            // If field has cards (Follow), we use Smart Instant Play
            else
            {
                SmartPlay(index);
            }
        }

        void ToggleCardSelection(int index)
        {
            if (selectedIndices.Contains(index))
                selectedIndices.Remove(index);
            else
                selectedIndices.Add(index);
            
            UpdateUI();
        }

        void SmartPlay(int index)
        {
            Card clickedCard = humanHand[index];
            List<Card> candidates = humanHand.Where(c => c.rank == clickedCard.rank).ToList();
            List<Card> toPlay = new List<Card>();

            int required = fieldCards.Count;
            if (candidates.Count >= required)
            {
                // Prioritize including the clicked card
                toPlay.Add(clickedCard);
                foreach(var c in candidates)
                {
                    if (toPlay.Count >= required) break;
                    if (c != clickedCard) toPlay.Add(c);
                }

                if (CanPlay(toPlay))
                {
                    ExecutePlay(toPlay);
                }
                else
                {
                    statusText.text = "Cannot play this.";
                }
            }
            else
            {
                statusText.text = "Need " + required + " cards.";
            }
        }

        public void OnPlayButtonPressed()
        {
            if (!isPlayerTurn) return;

            List<Card> selected = new List<Card>();
            foreach (int i in selectedIndices)
            {
                selected.Add(humanHand[i]);
            }

            if (CanPlay(selected))
            {
                ExecutePlay(selected);
            }
            else
            {
                statusText.text = "Invalid Move";
            }
        }

        void ExecutePlay(List<Card> toPlay)
        {
            PlayCards(humanHand, toPlay);
            selectedIndices.Clear();
            
            if (CheckWin(humanHand)) return;

            if (CheckSpecialTurnRules(toPlay))
            {
                statusText.text = "Special Effect! Play again.";
                UpdateUI();
                return; 
            }

            isPlayerTurn = false;
            UpdateUI();
            StartCoroutine(CPUTurn());
        }

        void PlayCards(List<Card> hand, List<Card> toPlay)
        {
            CheckSuitBinding(toPlay);

            fieldCards = new List<Card>(toPlay);
            foreach (var c in toPlay)
            {
                hand.Remove(c);
            }

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

            if (isSuitBinding) return;

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
                if (toPlay.Count == 1) boundSuit = toPlay[0].suit;
            }
        }

        bool CheckSpecialTurnRules(List<Card> played)
        {
            int r = played[0].rank;
            if (r == 8) { ClearField(); return true; }
            if (r == 5) return true;
            return false;
        }

        void ClearField()
        {
            fieldCards.Clear();
            isSuitBinding = false;
            boundSuit = null;
            if (isRevolution) isRevolution = false; 
        }

        bool CheckWin(List<Card> hand)
        {
            if (hand.Count == 0)
            {
                gameEnded = true;
                matchCount++;
                string winner;
                if (hand == humanHand)
                {
                    winner = "You Win!";
                    winCount++;
                }
                else
                {
                    winner = "CPU Wins!";
                }
                
                statusText.text = winner;
                UpdateStatsUI();
                
                playButton.gameObject.SetActive(false);
                passButton.interactable = false;
                if (retryButton != null) retryButton.gameObject.SetActive(true);
                return true;
            }
            return false;
        }

        void UpdateStatsUI()
        {
            if (statsText != null)
            {
                float rate = matchCount > 0 ? (float)winCount / matchCount * 100f : 0f;
                statsText.text = $"Matches: {matchCount}  Wins: {winCount}  Rate: {rate:F1}%";
            }
        }

        public void OnRetry()
        {
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            
            // Clean up UI
            foreach (Transform t in fieldArea) Destroy(t.gameObject);
            foreach (Transform t in playerArea) Destroy(t.gameObject);
            foreach (Transform t in cpuArea) Destroy(t.gameObject);

            StartCoroutine(GameSequence());
        }

        IEnumerator CPUTurn()
        {
            yield return new WaitForSeconds(1.0f); 
            
            List<Card> bestMove = null;
            var groups = cpuHand.GroupBy(c => c.rank).OrderBy(g => g.First().GetStrength(isRevolution));

            foreach (var g in groups)
            {
                var candidates = g.ToList();
                
                if (fieldCards.Count == 0)
                {
                    bestMove = new List<Card> { candidates[0] };
                    break;
                }
                else
                {
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
                    StartCoroutine(CPUTurn());
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
                statusText.text = "CPU Passed. Your lead.";
                ClearField();
                isPlayerTurn = true;
                UpdateUI();
            }
        }

        // --- UI Handling ---

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
            // Show Play button ONLY if it's Player Turn AND Field is Empty (Lead mode)
            bool showPlayButton = isPlayerTurn && fieldCards.Count == 0;
            playButton.gameObject.SetActive(showPlayButton);
            if (showPlayButton)
            {
                playButton.interactable = selectedIndices.Count > 0;
            }

            passButton.interactable = isPlayerTurn;
        }

        void CreateCardUI(Card c, Transform parent, bool clickable, int index, bool selected = false)
        {
            GameObject cardObj = new GameObject(c.ToString());
            cardObj.transform.SetParent(parent);
            
            Image img = cardObj.AddComponent<Image>();
            img.color = selected ? Color.yellow : Color.white;
            
            // Explicitly set size
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            if (cardRt == null) cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(70, 100);

            // Layout
            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = 70; // Set preferred size
            le.preferredHeight = 100;
            le.minWidth = 70;
            le.minHeight = 100;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(cardObj.transform);
            Text t = textObj.AddComponent<Text>();
            t.text = c.ToString();
            t.font = GetDefaultFont();
            t.raycastTarget = false; // Fix: Disable raycast to allow button click
            
            // Set text color based on suit
            if (c.suit == Suit.Heart || c.suit == Suit.Diamond)
            {
                t.color = Color.red;
            }
            else
            {
                t.color = Color.black;
            }
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
                b.onClick.AddListener(() => OnCardClicked(index));
            }
        }

        void CreateTextUI(string content, Transform parent)
        {
            GameObject textObj = new GameObject("Info");
            textObj.transform.SetParent(parent);
            Text t = textObj.AddComponent<Text>();
            t.text = content;
            t.font = GetDefaultFont();
            t.raycastTarget = false;
            t.color = Color.white;
            t.fontSize = 20;
            LayoutElement le = textObj.AddComponent<LayoutElement>();
            le.minHeight = 30;
        }

        // --- Helper ---
        Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) return f;
            
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts != null && allFonts.Length > 0) return allFonts[0];
            
            return null; // Should not happen in standard Unity env, but UI Text handles null font gracefully (no text).
        }

        void SetupUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            canvasObj = new GameObject("Canvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasObj.transform);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.4f, 0.1f);
            bgImg.raycastTarget = false; // Background shouldn't block? Actually it's fine, it's at back.
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Stats UI (Overlay on top left)
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(canvasObj.transform); // Attach to Canvas directly to be on top/independent
            statsText = statsObj.AddComponent<Text>();
            statsText.font = GetDefaultFont();
            statsText.fontSize = 18;
            statsText.color = Color.white;
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.raycastTarget = false;
            
            RectTransform statsRt = statsObj.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0, 1);
            statsRt.anchorMax = new Vector2(0, 1);
            statsRt.pivot = new Vector2(0, 1);
            statsRt.anchoredPosition = new Vector2(20, -20);
            statsRt.sizeDelta = new Vector2(400, 30);
            
            UpdateStatsUI();

            // Main Layout
            mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasObj.transform);
            mainPanel.SetActive(false); // Hide initially
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

            // --- Title Screen ---
            titlePanel = new GameObject("TitlePanel");
            titlePanel.transform.SetParent(canvasObj.transform);
            Image titleBg = titlePanel.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.4f, 0.1f); // Same green bg
            RectTransform titleRt = titlePanel.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup titleVlg = titlePanel.AddComponent<VerticalLayoutGroup>();
            titleVlg.childAlignment = TextAnchor.MiddleCenter;
            titleVlg.spacing = 30;

            GameObject titleTextObj = new GameObject("Title");
            titleTextObj.transform.SetParent(titlePanel.transform);
            Text titleText = titleTextObj.AddComponent<Text>();
            titleText.text = "DAIFUGO";
            titleText.font = GetDefaultFont();
            titleText.fontSize = 60;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.yellow;
            LayoutElement titleLe = titleTextObj.AddComponent<LayoutElement>();
            titleLe.minHeight = 80;

            // Start Button
            GameObject startBtnObj = new GameObject("StartButton");
            startBtnObj.transform.SetParent(titlePanel.transform);
            Image startImg = startBtnObj.AddComponent<Image>();
            startImg.color = Color.white;
            Button startBtn = startBtnObj.AddComponent<Button>();
            startBtn.onClick.AddListener(OnStartGamePressed);
            LayoutElement startLe = startBtnObj.AddComponent<LayoutElement>();
            startLe.minWidth = 200;
            startLe.minHeight = 60;
            GameObject startTxtObj = new GameObject("Text");
            startTxtObj.transform.SetParent(startBtnObj.transform);
            Text startTxt = startTxtObj.AddComponent<Text>();
            startTxt.text = "START GAME";
            startTxt.font = GetDefaultFont();
            startTxt.color = Color.black;
            startTxt.alignment = TextAnchor.MiddleCenter;
            startTxt.fontSize = 24;
            startTxt.raycastTarget = false;
            RectTransform startTxtRt = startTxtObj.GetComponent<RectTransform>();
            startTxtRt.anchorMin = Vector2.zero;
            startTxtRt.anchorMax = Vector2.one;
            startTxtRt.offsetMin = Vector2.zero;
            startTxtRt.offsetMax = Vector2.zero;

            // Rules Button
            GameObject rulesBtnObj = new GameObject("RulesButton");
            rulesBtnObj.transform.SetParent(titlePanel.transform);
            Image rulesImg = rulesBtnObj.AddComponent<Image>();
            rulesImg.color = Color.white;
            Button rulesBtn = rulesBtnObj.AddComponent<Button>();
            rulesBtn.onClick.AddListener(OnRulesPressed);
            LayoutElement rulesLe = rulesBtnObj.AddComponent<LayoutElement>();
            rulesLe.minWidth = 200;
            rulesLe.minHeight = 60;
            GameObject rulesTxtObj = new GameObject("Text");
            rulesTxtObj.transform.SetParent(rulesBtnObj.transform);
            Text rulesTxt = rulesTxtObj.AddComponent<Text>();
            rulesTxt.text = "RULES";
            rulesTxt.font = GetDefaultFont();
            rulesTxt.color = Color.black;
            rulesTxt.alignment = TextAnchor.MiddleCenter;
            rulesTxt.fontSize = 24;
            rulesTxt.raycastTarget = false;
            RectTransform rulesTxtRt = rulesTxtObj.GetComponent<RectTransform>();
            rulesTxtRt.anchorMin = Vector2.zero;
            rulesTxtRt.anchorMax = Vector2.one;
            rulesTxtRt.offsetMin = Vector2.zero;
            rulesTxtRt.offsetMax = Vector2.zero;

            // --- Rules Panel ---
            rulesPanel = new GameObject("RulesPanel");
            rulesPanel.transform.SetParent(canvasObj.transform);
            rulesPanel.SetActive(false);
            Image rulesBg = rulesPanel.AddComponent<Image>();
            rulesBg.color = new Color(0, 0, 0, 0.9f);
            RectTransform rulesRt = rulesPanel.GetComponent<RectTransform>();
            rulesRt.anchorMin = Vector2.zero;
            rulesRt.anchorMax = Vector2.one;
            rulesRt.offsetMin = Vector2.zero;
            rulesRt.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup rulesVlg = rulesPanel.AddComponent<VerticalLayoutGroup>();
            rulesVlg.childAlignment = TextAnchor.MiddleCenter;
            rulesVlg.padding = new RectOffset(50, 50, 50, 50);
            rulesVlg.spacing = 20;

            GameObject rulesContentObj = new GameObject("Content");
            rulesContentObj.transform.SetParent(rulesPanel.transform);
            Text rulesContent = rulesContentObj.AddComponent<Text>();
            rulesContent.font = GetDefaultFont();
            rulesContent.fontSize = 20;
            rulesContent.color = Color.white;
            rulesContent.text = "--- RULES ---\n\n" +
                                "8 CUT: Clears the field.\n" +
                                "5 SKIP: Skips opponent's turn.\n" +
                                "11 BACK: Temporary Revolution (J).\n" +
                                "SUIT BINDING: Matching suit locks suit.\n" +
                                "WIN: Empty your hand first.";
            rulesContent.alignment = TextAnchor.MiddleCenter;
            LayoutElement rulesContentLe = rulesContentObj.AddComponent<LayoutElement>();
            rulesContentLe.minHeight = 300;

            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(rulesPanel.transform);
            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = Color.red;
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(OnCloseRulesPressed);
            LayoutElement closeLe = closeBtnObj.AddComponent<LayoutElement>();
            closeLe.minWidth = 150;
            closeLe.minHeight = 50;
            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform);
            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.text = "CLOSE";
            closeTxt.font = GetDefaultFont();
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.raycastTarget = false;
            RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero;
            closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero;
            closeTxtRt.offsetMax = Vector2.zero;

            // CPU Area (continue with main panel children)
            GameObject cpuObj = new GameObject("CPU Area");
            cpuObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup cpuHlg = cpuObj.AddComponent<HorizontalLayoutGroup>();
            cpuHlg.childAlignment = TextAnchor.MiddleCenter;
            cpuHlg.childControlWidth = false;
            cpuHlg.childForceExpandWidth = false;
            cpuHlg.childControlHeight = false; 
            cpuHlg.childForceExpandHeight = false; 
            cpuArea = cpuObj.transform;

            GameObject fieldObj = new GameObject("Field Area");
            fieldObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup fieldHlg = fieldObj.AddComponent<HorizontalLayoutGroup>();
            fieldHlg.childAlignment = TextAnchor.MiddleCenter;
            fieldHlg.spacing = 10;
            fieldHlg.childControlHeight = false; 
            fieldHlg.childForceExpandHeight = false; 
            fieldHlg.childControlWidth = false; // Prevent horizontal stretch
            fieldHlg.childForceExpandWidth = false; // Prevent horizontal stretch
            fieldArea = fieldObj.transform;
            LayoutElement fieldLe = fieldObj.AddComponent<LayoutElement>();
            fieldLe.minHeight = 100;

            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(mainPanel.transform);
            statusText = statusObj.AddComponent<Text>();
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.font = GetDefaultFont();
            statusText.fontSize = 24;
            statusText.color = Color.yellow;
            statusText.text = "Initializing...";
            statusText.raycastTarget = false;
            
            GameObject playerObj = new GameObject("Player Area");
            playerObj.transform.SetParent(mainPanel.transform);
            HorizontalLayoutGroup playerHlg = playerObj.AddComponent<HorizontalLayoutGroup>();
            playerHlg.childAlignment = TextAnchor.MiddleCenter;
            playerHlg.spacing = 5;
            playerHlg.childControlHeight = false; 
            playerHlg.childForceExpandHeight = false; 
            playerHlg.childControlWidth = false; // Prevent horizontal stretch
            playerHlg.childForceExpandWidth = false; // Prevent horizontal stretch
            playerArea = playerObj.transform;
            LayoutElement playerLe = playerObj.AddComponent<LayoutElement>();
            playerLe.minHeight = 100;

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
            passTxt.font = GetDefaultFont();
            passTxt.color = Color.black;
            passTxt.alignment = TextAnchor.MiddleCenter;
            passTxt.raycastTarget = false;
            RectTransform passTxtRt = passTxtObj.GetComponent<RectTransform>();
            passTxtRt.anchorMin = Vector2.zero;
            passTxtRt.anchorMax = Vector2.one;
            passTxtRt.offsetMin = Vector2.zero;
            passTxtRt.offsetMax = Vector2.zero;

            // Play Button (Re-added)
            GameObject playBtnObj = new GameObject("PlayButton");
            playBtnObj.transform.SetParent(btnObj.transform);
            Image playImg = playBtnObj.AddComponent<Image>();
            playImg.color = Color.cyan;
            playButton = playBtnObj.AddComponent<Button>();
            playButton.onClick.AddListener(OnPlayButtonPressed);
            LayoutElement playLe = playBtnObj.AddComponent<LayoutElement>();
            playLe.minWidth = 100;
            playLe.minHeight = 40;
            GameObject playTxtObj = new GameObject("Text");
            playTxtObj.transform.SetParent(playBtnObj.transform);
            Text playTxt = playTxtObj.AddComponent<Text>();
            playTxt.text = "PLAY";
            playTxt.font = GetDefaultFont();
            playTxt.color = Color.black;
            playTxt.alignment = TextAnchor.MiddleCenter;
            playTxt.raycastTarget = false;
            RectTransform playTxtRt = playTxtObj.GetComponent<RectTransform>();
            playTxtRt.anchorMin = Vector2.zero;
            playTxtRt.anchorMax = Vector2.one;
            playTxtRt.offsetMin = Vector2.zero;
            playTxtRt.offsetMax = Vector2.zero;
            
            // Retry Button
            GameObject retryBtnObj = new GameObject("RetryButton");
            retryBtnObj.transform.SetParent(mainPanel.transform); // Put at bottom
            Image retryImg = retryBtnObj.AddComponent<Image>();
            retryImg.color = Color.green;
            retryButton = retryBtnObj.AddComponent<Button>();
            retryButton.onClick.AddListener(OnRetry);
            LayoutElement retryLe = retryBtnObj.AddComponent<LayoutElement>();
            retryLe.minHeight = 50;
            GameObject retryTxtObj = new GameObject("Text");
            retryTxtObj.transform.SetParent(retryBtnObj.transform);
            Text retryTxt = retryTxtObj.AddComponent<Text>();
            retryTxt.text = "RETRY";
            retryTxt.font = GetDefaultFont();
            retryTxt.color = Color.black;
            retryTxt.alignment = TextAnchor.MiddleCenter;
            retryTxt.fontSize = 24;
            retryTxt.raycastTarget = false;
            RectTransform retryTxtRt = retryTxtObj.GetComponent<RectTransform>();
            retryTxtRt.anchorMin = Vector2.zero;
            retryTxtRt.anchorMax = Vector2.one;
            retryTxtRt.offsetMin = Vector2.zero;
            retryTxtRt.offsetMax = Vector2.zero;
            
            retryButton.gameObject.SetActive(false);
        }
    }
}