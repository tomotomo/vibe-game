using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Daifugo.Core;
using Daifugo.Game;
using Daifugo.UI;
using System.Linq;

namespace Daifugo.Tests.PlayMode
{
    public class GameControllerTests
    {
        private GameObject gameControllerGameObject;
        private GameController gameController;
        private GameObject uiManagerGameObject;
        private UIManager uiManager;

        [SetUp]
        public void SetUp()
        {
            // Setup UIManager
            uiManagerGameObject = new GameObject("UIManager");
            uiManagerGameObject.SetActive(false); // Prevent Awake
            uiManager = uiManagerGameObject.AddComponent<UIManager>();
            
            // UI Mocking - Create dummy objects for panels and texts
            uiManager.TitlePanel = new GameObject("TitlePanel");
            uiManager.RulePanel = new GameObject("RulePanel");
            uiManager.GamePanel = new GameObject("GamePanel");
            uiManager.ResultPanel = new GameObject("ResultPanel");
            
            uiManager.MessageText = uiManagerGameObject.AddComponent<UnityEngine.UI.Text>();
            uiManager.FieldArea = new GameObject("FieldArea").transform;
            uiManager.FieldArea.SetParent(uiManagerGameObject.transform);
            // Dice texts
            uiManager.Dice1Text = uiManagerGameObject.AddComponent<UnityEngine.UI.Text>();
            uiManager.Dice2Text = uiManagerGameObject.AddComponent<UnityEngine.UI.Text>();
            // Hand areas
            uiManager.PlayerHandArea = new GameObject("PlayerHandArea").transform;
            uiManager.CpuHandArea = new GameObject("CpuHandArea").transform;
            
            // Buttons
            uiManager.PlayButton = CreateButton("PlayButton");
            uiManager.PassButton = CreateButton("PassButton");
            uiManager.RetryButton = CreateButton("RetryButton");
            uiManager.ExitButton = CreateButton("ExitButton");
            uiManager.StartGameButton = CreateButton("StartGameButton");
            uiManager.ToRuleButton = CreateButton("ToRuleButton");
            uiManager.ToTitleButtonFromRule = CreateButton("ToTitleButtonFromRule");
            uiManager.ToTitleButtonFromGame = CreateButton("ToTitleButtonFromGame");

            // Activate UIManager (Awake will run now)
            uiManagerGameObject.SetActive(true);

            // Setup GameController
            gameControllerGameObject = new GameObject("GameController");
            gameControllerGameObject.SetActive(false); // Prevent Start
            gameController = gameControllerGameObject.AddComponent<GameController>();
            
            // Set required references
            gameController._uiManager = uiManager;
            
            // Activate GameController (Start will run now)
            gameControllerGameObject.SetActive(true);
        }

        private UnityEngine.UI.Button CreateButton(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(uiManagerGameObject.transform);
            return go.AddComponent<UnityEngine.UI.Button>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(gameControllerGameObject);
            Object.Destroy(uiManagerGameObject);
        }

        [UnityTest]
        public IEnumerator CpuPass_FieldIsCleared_PlayerBecomesLead()
        {
            // Wait for Start() to complete (frames)
            yield return null;

            // Arrange
            gameController._gameActive = true;
            // RuleManager is initialized in Start(), but let's reset to be sure
            if (gameController._ruleManager == null) gameController._ruleManager = new RuleManager();
            
            // Set up state: CPU Turn, Field has cards
            gameController._isPlayerTurn = false;
            gameController._fieldCards = new List<Card> { new Card(Suit.Diamonds, 13) }; // King
            gameController._ruleManager.CommitPlay(gameController._fieldCards, new List<Card>());
            
            // Act
            // Call ProcessPass(false) -> CPU passed
            yield return gameController.ProcessPass(false);

            // Assert
            // 1. Field should be cleared (empty list)
            Assert.IsNotNull(gameController._fieldCards);
            Assert.AreEqual(0, gameController._fieldCards.Count, "Field cards count should be 0.");
            
            // 2. UI Field should be cleared (Mock check or state check)
            // UIManager.UpdateField(null) calls Destroy on children.
            // Destroy happens at end of frame. Wait one frame.
            yield return null;
            Assert.AreEqual(0, uiManager.FieldArea.childCount, "UI Field Area should have 0 children.");

            // 3. Player should be lead
            Assert.IsTrue(gameController._isPlayerTurn, "Should be player's turn.");
            // Lead means Pass button is inactive
            Assert.IsFalse(uiManager.PassButton.gameObject.activeSelf, "Pass button should be inactive (Lead).");
            Assert.IsTrue(uiManager.PlayButton.gameObject.activeSelf, "Play button should be active.");
            
            Debug.Log("Test Passed: Field cleared correctly after CPU pass.");
        }
    }
}
