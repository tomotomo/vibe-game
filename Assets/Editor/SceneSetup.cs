using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Daifugo.UI;
using Daifugo.Game;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Daifugo.Editor
{
    public class SceneSetup
    {
        [MenuItem("Daifugo/Setup Scene")]
        public static void Setup()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // 1. Setup Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.4f, 0.2f); // Dark Green Table
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 0, -10);

            // 2. Setup EventSystem
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 3. Setup Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 4. Create Card Prefab
            var cardPrefab = CreateCardPrefab();
            var cardBackPrefab = CreateCardBackPrefab();

            // 5. Setup Managers
            var gameControllerObj = new GameObject("GameController");
            var gameController = gameControllerObj.AddComponent<GameController>();
            
            var uiManagerObj = new GameObject("UIManager");
            var uiManager = uiManagerObj.AddComponent<UIManager>();
            
            // Link UI Manager to GameController using SerializedObject to avoid "private" issues if fields are private
            // But here fields are SerializeField so we can find them via SerializedObject or public property if available.
            // Since they are private/serialized, we use SerializedObject.
            var soGame = new SerializedObject(gameController);
            soGame.FindProperty("_uiManager").objectReferenceValue = uiManager;
            soGame.ApplyModifiedProperties();

            // 6. Build UI Hierarchy
            var font = Resources.Load<Font>("Fonts/RobotoMono-Regular");

            // --- Title Panel ---
            var titlePanel = CreatePanel(canvasObj.transform, "TitlePanel", Color.black);
            var titleText = CreateText(titlePanel.transform, "TitleText", "DAIFUGO", font, 60, new Vector2(0, 200));
            var startBtn = CreateButton(titlePanel.transform, "StartButton", "START GAME", font, new Vector2(0, 0));
            var toRuleBtn = CreateButton(titlePanel.transform, "RuleButton", "RULES", font, new Vector2(0, -100));
            
            // --- Rule Panel ---
            var rulePanel = CreatePanel(canvasObj.transform, "RulePanel", new Color(0,0,0,0.9f));
            rulePanel.SetActive(false);
            CreateText(rulePanel.transform, "RuleTitle", "RULES", font, 40, new Vector2(0, 350));
            CreateText(rulePanel.transform, "RuleBody", 
                "1 vs 1 Daifugo\n\n- 8 Cut\n- 5 Skip\n- J Back\n- Suit Bind\n\nWin by emptying your hand.", 
                font, 24, Vector2.zero).GetComponent<RectTransform>().sizeDelta = new Vector2(400, 600);
            var ruleBackBtn = CreateButton(rulePanel.transform, "BackButton", "BACK", font, new Vector2(0, -350));

            // --- Game Panel ---
            var gamePanel = CreatePanel(canvasObj.transform, "GamePanel", Color.clear); // Transparent
            gamePanel.SetActive(false);
            
            // Top: CPU Hand (Horizontal Layout)
            var cpuArea = CreateContainer(gamePanel.transform, "CpuArea", new Vector2(0, 350), new Vector2(500, 150));
            var cpuLayout = cpuArea.AddComponent<HorizontalLayoutGroup>();
            cpuLayout.childAlignment = TextAnchor.MiddleCenter;
            cpuLayout.spacing = -30; // Overlap
            
            // Center: Field (Horizontal Layout)
            var fieldArea = CreateContainer(gamePanel.transform, "FieldArea", new Vector2(0, 100), new Vector2(500, 200));
            var fieldLayout = fieldArea.AddComponent<HorizontalLayoutGroup>();
            fieldLayout.childAlignment = TextAnchor.MiddleCenter;
            fieldLayout.spacing = 10;

            // Info Texts
            var msgText = CreateText(gamePanel.transform, "MessageText", "Game Start", font, 30, new Vector2(0, 150));
            var cpuInfoText = CreateText(gamePanel.transform, "CpuInfoText", "CPU: 0", font, 24, new Vector2(0, 300));
            
            // Dice Texts (Center)
            var dice1 = CreateText(gamePanel.transform, "Dice1Text", "1", font, 50, new Vector2(-50, 50));
            var dice2 = CreateText(gamePanel.transform, "Dice2Text", "1", font, 50, new Vector2(50, 50));
            dice1.SetActive(false);
            dice2.SetActive(false);

            // Bottom: Player Hand (Grid/Horizontal)
            var playerArea = CreateContainer(gamePanel.transform, "PlayerArea", new Vector2(0, -200), new Vector2(500, 250));
            var playerLayout = playerArea.AddComponent<HorizontalLayoutGroup>();
            playerLayout.childAlignment = TextAnchor.MiddleCenter;
            playerLayout.spacing = -40; // Overlap

            // Footer: Buttons
            var btnArea = CreateContainer(gamePanel.transform, "ButtonArea", new Vector2(0, -400), new Vector2(500, 100));
            var btnLayout = btnArea.AddComponent<HorizontalLayoutGroup>();
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.spacing = 20;
            
            var playBtn = CreateButton(btnArea.transform, "PlayButton", "PLAY", font, Vector2.zero);
            var passBtn = CreateButton(btnArea.transform, "PassButton", "PASS", font, Vector2.zero);
            var exitBtn = CreateButton(btnArea.transform, "ExitButton", "EXIT", font, Vector2.zero);

            // --- Result Panel ---
            var resultPanel = CreatePanel(canvasObj.transform, "ResultPanel", new Color(0,0,0,0.8f));
            resultPanel.SetActive(false);
            var resText = CreateText(resultPanel.transform, "ResultText", "WIN!", font, 60, new Vector2(0, 200));
            var statsText = CreateText(resultPanel.transform, "StatsText", "Stats...", font, 30, Vector2.zero);
            var retryBtn = CreateButton(resultPanel.transform, "RetryButton", "RETRY", font, new Vector2(0, -200));
            var resQuitBtn = CreateButton(resultPanel.transform, "ResQuitButton", "TITLE", font, new Vector2(0, -300));

            // 7. Link UI Manager References
            var soUI = new SerializedObject(uiManager);
            
            // Panels
            soUI.FindProperty("TitlePanel").objectReferenceValue = titlePanel;
            soUI.FindProperty("RulePanel").objectReferenceValue = rulePanel;
            soUI.FindProperty("GamePanel").objectReferenceValue = gamePanel;
            soUI.FindProperty("ResultPanel").objectReferenceValue = resultPanel;

            // Areas
            soUI.FindProperty("CpuHandArea").objectReferenceValue = cpuArea.transform;
            soUI.FindProperty("PlayerHandArea").objectReferenceValue = playerArea.transform;
            soUI.FindProperty("FieldArea").objectReferenceValue = fieldArea.transform;

            // Texts
            soUI.FindProperty("MessageText").objectReferenceValue = msgText.GetComponent<Text>();
            soUI.FindProperty("CpuInfoText").objectReferenceValue = cpuInfoText.GetComponent<Text>();
            soUI.FindProperty("ResultText").objectReferenceValue = resText.GetComponent<Text>();
            soUI.FindProperty("StatsText").objectReferenceValue = statsText.GetComponent<Text>();
            soUI.FindProperty("Dice1Text").objectReferenceValue = dice1.GetComponent<Text>();
            soUI.FindProperty("Dice2Text").objectReferenceValue = dice2.GetComponent<Text>();

            // Buttons
            soUI.FindProperty("PlayButton").objectReferenceValue = playBtn.GetComponent<Button>();
            soUI.FindProperty("PassButton").objectReferenceValue = passBtn.GetComponent<Button>();
            soUI.FindProperty("RetryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
            soUI.FindProperty("ExitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
            soUI.FindProperty("StartGameButton").objectReferenceValue = startBtn.GetComponent<Button>();
            soUI.FindProperty("ToRuleButton").objectReferenceValue = toRuleBtn.GetComponent<Button>();
            soUI.FindProperty("ToTitleButtonFromRule").objectReferenceValue = ruleBackBtn.GetComponent<Button>();
            soUI.FindProperty("ToTitleButtonFromGame").objectReferenceValue = resQuitBtn.GetComponent<Button>();

            // Prefabs
            soUI.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab.GetComponent<CardView>();
            soUI.FindProperty("_cardBackPrefab").objectReferenceValue = cardBackPrefab;

            soUI.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
            Debug.Log("Scene Setup Complete!");
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string content, Font font, int fontSize, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 100);
            
            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Font font, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(160, 50);

            var img = go.AddComponent<Image>();
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = CreateText(go.transform, "Label", label, font, 20, Vector2.zero);
            txtObj.GetComponent<Text>().color = Color.black;
            txtObj.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;

            return go;
        }

        private static GameObject CreateCardPrefab()
        {
            var go = new GameObject("CardPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 120);

            var img = go.AddComponent<Image>();
            img.color = Color.white;

            // Simple layout
            // Rank Text (Top Left)
            var rankObj = CreateText(go.transform, "RankText", "A", null, 18, Vector2.zero);
            var rankRect = rankObj.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 1);
            rankRect.anchorMax = new Vector2(0, 1);
            rankRect.pivot = new Vector2(0, 1);
            rankRect.anchoredPosition = new Vector2(5, -5);
            rankRect.sizeDelta = new Vector2(30, 30);
            var rankTxt = rankObj.GetComponent<Text>();
            rankTxt.alignment = TextAnchor.UpperLeft;
            rankTxt.color = Color.black;

            // Suit Center
            var suitObj = new GameObject("SuitLarge");
            suitObj.transform.SetParent(go.transform, false);
            var suitRect = suitObj.AddComponent<RectTransform>();
            suitRect.anchorMin = Vector2.zero;
            suitRect.anchorMax = Vector2.one;
            suitRect.offsetMin = new Vector2(10, 10);
            suitRect.offsetMax = new Vector2(-10, -10);
            var suitImg = suitObj.AddComponent<Image>();
            suitImg.preserveAspect = true;

            // Highlight
            var hlObj = new GameObject("Highlight");
            hlObj.transform.SetParent(go.transform, false);
            var hlRect = hlObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            var hlImg = hlObj.AddComponent<Image>();
            hlImg.color = new Color(1, 1, 0, 0.3f);
            hlImg.enabled = false;

            var view = go.AddComponent<CardView>();
            // Use SerializedObject to set private fields
            var so = new SerializedObject(view);
            so.FindProperty("_backgroundImage").objectReferenceValue = img;
            so.FindProperty("_suitImageLarge").objectReferenceValue = suitImg;
            so.FindProperty("_rankText").objectReferenceValue = rankTxt;
            so.FindProperty("_highlightImage").objectReferenceValue = hlImg;
            so.ApplyModifiedProperties();

            // Save as Prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            
            var path = "Assets/Prefabs/CardView.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateCardBackPrefab()
        {
            var go = new GameObject("CardBackPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 120);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.8f); // Blue back

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            
            var path = "Assets/Prefabs/CardBack.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }
    }
}
