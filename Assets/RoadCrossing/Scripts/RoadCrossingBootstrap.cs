using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UserProfile;

namespace RoadCrossing
{
    public sealed class RoadCrossingBootstrap : MonoBehaviour
    {
        public static RoadCrossingBootstrap Instance { get; private set; }
        public const int FinishRow = 20;
        public const float RowSpacing = 1.55f;

        private readonly List<RoadLane> lanes = new();
        private readonly Dictionary<int, RoadCoinMarker> coinMarkers = new();
        private RoadChicken chicken;
        private Camera worldCamera;
        private Text stepText;
        private Text multiplierText;
        private Text balanceText;
        private Text payoutText;
        private Text messageText;
        private Text resultBadgeText;
        private Text resultTitleText;
        private Text resultAmountText;
        private Text resultAmountCaptionText;
        private Text resultDetailsText;
        private Text resultBalanceText;
        private Text menuBalanceText;
        private Text maximumWinText;
        private Text playButtonText;
        private Text cashOutText;
        private Text replayButtonText;
        private GameObject startPanel;
        private GameObject resultPanel;
        private GameObject settingsPanel;
        private GameObject tutorialPanel;
        private Image resultCardImage;
        private Image resultAccentBar;
        private Image resultIcon;
        private Image resultUserIcon;
        private Button cashOutButton;
        private Button playButton;
        private bool roundActive;
        private int currentRow;
        private int balance;
        private int potentialPayout;
        private Sprite roundedUiSprite;
        private const string TutorialSeenKey = "RoadCrossingTutorialSeen";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "RoadCrossing" || FindFirstObjectByType<RoadCrossingBootstrap>() != null) return;
            new GameObject("Chicken Road").AddComponent<RoadCrossingBootstrap>();
        }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            balance = PlayerPrefs.GetInt("RiverJumpBalance", 1000);
            BuildWorld();
            BuildUi();
            ShowStart();
            if (PlayerPrefs.GetInt(TutorialSeenKey, 0) == 0) ShowTutorial();
        }

        private void BuildWorld()
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(camera.gameObject);
            var root = new GameObject("Road World").transform;
            Sprite grassSprite = Resources.Load<Sprite>("RoadCrossing/Grass");
            CreateTexturedTile("Start Grass", grassSprite, new Vector2(0f, -1.2f), new Vector2(8f, 2.2f), -10, root);

            for (int row = 1; row <= FinishRow; row++)
            {
                float y = row * RowSpacing;
                CreateTile($"Road {row}", new Vector2(0f, y), new Vector2(8f, 1.42f), new Color(0.13f, 0.16f, 0.20f), -10, root);
                for (int dash = -3; dash <= 3; dash++)
                    CreateTile("Lane Mark", new Vector2(dash * 1.25f, y + 0.64f), new Vector2(0.7f, 0.06f), new Color(1f, 0.86f, 0.32f), -8, root);

                var laneObject = new GameObject($"Traffic Lane {row}");
                laneObject.transform.SetParent(root);
                var lane = laneObject.AddComponent<RoadLane>();
                lane.Configure(row, y, row % 2 == 0 ? 1 : -1, 2.2f + row * 0.075f);
                lanes.Add(lane);
            }

            CreateCoinMarkers(root);

            float finishY = (FinishRow + 1) * RowSpacing;
            CreateTexturedTile("Finish Grass", grassSprite, new Vector2(0f, finishY), new Vector2(8f, 1.7f), -10, root);
            for (int x = -4; x < 4; x++)
                CreateTile("Finish Check", new Vector2(x + 0.5f, finishY - 0.55f), new Vector2(1f, 0.25f), x % 2 == 0 ? Color.white : Color.black, -7, root);

            var chickenObject = new GameObject("Chicken");
            chickenObject.transform.SetParent(root);
            chickenObject.transform.position = new Vector3(0f, -0.35f, 0f);
            var renderer = chickenObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("RiverJump2D/Chicken");
            ChickenRoad.Skins.ChickenSkinService.Apply(renderer);
            renderer.sortingOrder = 20;
            FitSprite(chickenObject, 1.15f, 1.25f);
            chicken = chickenObject.AddComponent<RoadChicken>();

            var cameraObject = new GameObject("Road Camera");
            cameraObject.tag = "MainCamera";
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5.7f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.38f, 0.72f, 0.92f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 4.2f, -10f);
        }

        private void CreateCoinMarkers(Transform root)
        {
            Sprite coinSprite = Resources.Load<Sprite>("RiverJump2D/Coin");
            for (int row = 1; row <= FinishRow; row++)
            {
                var marker = new GameObject($"Next Score {row}");
                marker.transform.SetParent(root);
                marker.transform.position = new Vector3(0f, row * RowSpacing, 0f);

                var glow = new GameObject("Coin Glow");
                glow.transform.SetParent(marker.transform, false);
                var glowRenderer = glow.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = coinSprite;
                glowRenderer.color = new Color(1f, 0.72f, 0.08f, 0.25f);
                glowRenderer.sortingOrder = 14;
                FitSprite(glow, 1.04f, 1.04f);

                var coin = new GameObject("Gold Coin");
                coin.transform.SetParent(marker.transform, false);
                var coinRenderer = coin.AddComponent<SpriteRenderer>();
                coinRenderer.sprite = coinSprite;
                coinRenderer.sortingOrder = 16;
                FitSprite(coin, 0.78f, 0.78f);

                var coefficientPlate = CreateTile(
                    "Coefficient Plate",
                    Vector2.zero,
                    new Vector2(1.72f, 0.38f),
                    new Color(0.025f, 0.045f, 0.07f, 0.94f),
                    17,
                    marker.transform);
                coefficientPlate.transform.localPosition = new Vector3(0f, 0.66f, 0f);

                CreateWorldText(marker.transform, "Score Shadow", $"+{row * 10}", new Vector3(0.025f, 0.625f, 0f), 18, 0.052f, new Color(0f, 0f, 0f, 0.95f));
                CreateWorldText(marker.transform, "Score", $"+{row * 10}", new Vector3(0f, 0.66f, 0f), 19, 0.052f, new Color(1f, 0.88f, 0.12f));
                CreateWorldText(marker.transform, "Next Shadow", "NEXT", new Vector3(0.02f, 1.0f, 0f), 18, 0.03f, new Color(0f, 0f, 0f, 0.95f));
                CreateWorldText(marker.transform, "Next Label", "NEXT", new Vector3(0f, 1.025f, 0f), 19, 0.03f, Color.white);

                var controller = marker.AddComponent<RoadCoinMarker>();
                controller.Configure(row, row * 0.41f);
                coinMarkers[row] = controller;
            }
        }

        private static void CreateWorldText(Transform parent, string name, string value, Vector3 position, int order, float characterSize, Color color)
        {
            var textObject = new GameObject(name, typeof(TextMesh));
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            var text = textObject.GetComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.GetComponent<MeshRenderer>().sortingOrder = order;
        }

        private void BuildUi()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var canvasObject = new GameObject("Road UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            roundedUiSprite = CreateRoundedSprite();

            var top = Panel(canvas.transform, "Top HUD", new Color(0.04f, 0.07f, 0.11f, 0.92f), new Vector2(0, 765), new Vector2(980, 230));
            balanceText = Label(top.transform, "BALANCE  0", 31, new Vector2(-270, 65), new Vector2(400, 55));
            multiplierText = Label(top.transform, "SCORE 0", 42, new Vector2(270, 55), new Vector2(340, 75));
            stepText = Label(top.transform, "STEP 0 / 20", 30, new Vector2(-270, 5), new Vector2(400, 50));
            payoutText = Label(top.transform, "KEEP GOING!", 30, new Vector2(210, -25), new Vector2(500, 55));
            messageText = Label(canvas.transform, "TAP TO RUN", 40, new Vector2(0, -610), new Vector2(760, 80));
            cashOutButton = Button(canvas.transform, "END RUN", new Vector2(0, -790), new Vector2(600, 120), CashOut);
            cashOutText = cashOutButton.GetComponentInChildren<Text>();

            BuildBetMenu(canvas.transform);
            BuildSettingsMenu(canvas.transform);
            BuildResultPanel(canvas.transform);
            BuildTutorial(canvas.transform);
        }

        private void BuildBetMenu(Transform canvas)
        {
            startPanel = Panel(canvas, "Main Menu", new Color(0.005f, 0.035f, 0.055f, 0.9f), Vector2.zero, new Vector2(1080, 1920), false);
            var card = Panel(startPanel.transform, "Menu Card", new Color(0.025f, 0.13f, 0.18f, 0.98f), Vector2.zero, new Vector2(940, 1700));
            var content = card.transform;
            var menuChicken = CreateSpriteImage(content, "Menu Chicken", "RiverJump2D/Chicken", new Vector2(0, 660), new Vector2(215, 215));
            ChickenRoad.Skins.ChickenSkinService.Apply(menuChicken);
            var title = Label(content, "ROAD CROSSING", 78, new Vector2(0, 510), new Vector2(860, 125));
            title.color = new Color(1f, 0.95f, 0.82f);
            var tagline = Label(content, "DODGE TRAFFIC  •  REACH THE FINISH", 27, new Vector2(0, 430), new Vector2(760, 52));
            tagline.color = new Color(1f, 0.73f, 0.12f);

            var balancePanel = Panel(content, "Balance Panel", new Color(0.015f, 0.08f, 0.12f, 0.98f), new Vector2(0, 320), new Vector2(780, 110));
            Label(balancePanel.transform, "BALANCE", 27, new Vector2(-220, 0), new Vector2(240, 65));
            menuBalanceText = Label(balancePanel.transform, "0", 49, new Vector2(205, 0), new Vector2(300, 75));
            menuBalanceText.color = new Color(1f, 0.84f, 0.18f);

            var betPanel = Panel(content, "Run Instructions", new Color(0.035f, 0.2f, 0.25f, 0.98f), new Vector2(0, 55), new Vector2(820, 390));
            Label(betPanel.transform, "TAP TO CROSS EACH LANE\nAVOID CARS • COLLECT POINTS\nEVERY RUN IS FREE", 37, Vector2.zero, new Vector2(760, 310));
            var winPanel = Panel(content, "Goal Panel", new Color(0.13f, 0.1f, 0.025f, 0.98f), new Vector2(0, -205), new Vector2(780, 102));
            maximumWinText = Label(winPanel.transform, "", 31, Vector2.zero, new Vector2(720, 65));
            maximumWinText.color = new Color(1f, 0.84f, 0.18f);

            playButton = Button(content, "PLAY", new Vector2(0, -360), new Vector2(720, 136), StartRound);
            playButtonText = playButton.GetComponentInChildren<Text>();
            var footer = Label(content, "REACH THE FINISH FOR A BONUS\nEARN COINS TO UNLOCK SKINS", 25, new Vector2(0, -545), new Vector2(820, 110));
            footer.color = new Color(0.57f, 0.79f, 0.83f);
            var tutorialButton = Button(content, "HOW TO PLAY", new Vector2(0, -690), new Vector2(430, 86), ShowTutorial);
            tutorialButton.GetComponentInChildren<Text>().fontSize = 29;
            UpdateBetMenu();
        }

        private void BuildSettingsMenu(Transform canvas)
        {
            settingsPanel = Panel(canvas, "Settings Overlay", new Color(0.01f, 0.06f, 0.09f, 0.98f), Vector2.zero, new Vector2(1080, 1920), false);
            var card = Panel(settingsPanel.transform, "Settings Card", new Color(0.025f, 0.13f, 0.18f, 1f), Vector2.zero, new Vector2(900, 1050));
            Label(card.transform, "SETTINGS", 72, new Vector2(0, 360), new Vector2(760, 110));
            Button(card.transform, "RESET PROGRESS", new Vector2(0, 80), new Vector2(620, 110), ResetProgress);
            Button(card.transform, "BACK", new Vector2(0, -260), new Vector2(470, 100), HideSettings);
            settingsPanel.SetActive(false);
        }

        private void BuildResultPanel(Transform canvas)
        {
            resultPanel = Panel(canvas, "Result", new Color(0.004f, 0.035f, 0.055f, 0.94f), Vector2.zero, new Vector2(1080, 1920), false);
            var card = Panel(resultPanel.transform, "Result Card", new Color(0.025f, 0.13f, 0.18f, 1f), Vector2.zero, new Vector2(900, 1390));
            resultCardImage = card.GetComponent<Image>();
            Transform content = card.transform;

            var accent = Panel(content, "Result Accent", new Color(1f, 0.67f, 0.06f), new Vector2(0, 672), new Vector2(820, 18), false);
            resultAccentBar = accent.GetComponent<Image>();
            resultIcon = CreateSpriteImage(content, "Result Coin", "RiverJump2D/Coin", new Vector2(0, 530), new Vector2(145, 145));

            resultBadgeText = Label(content, "ROUND COMPLETE", 24, new Vector2(0, 414), new Vector2(560, 46));
            resultBadgeText.color = new Color(0.65f, 0.85f, 0.88f);
            resultUserIcon = CreateSpriteImage(content, "Result User Icon", "default_user", new Vector2(-340, 319), new Vector2(88, 88));
            resultUserIcon.sprite = UserProfileStorage.UserIcon;
            resultTitleText = Label(content, "NICE WORK", 46, new Vector2(90, 319), new Vector2(600, 105));
            resultTitleText.alignment = TextAnchor.MiddleLeft;
            resultAmountText = Label(content, "+0", 104, new Vector2(0, 181.5f), new Vector2(800, 130));
            resultAmountText.color = new Color(1f, 0.83f, 0.12f);
            resultAmountCaptionText = Label(content, "COINS EARNED", 25, new Vector2(0, 74), new Vector2(600, 45));
            resultAmountCaptionText.color = new Color(0.7f, 0.88f, 0.9f);

            var statsPanel = Panel(content, "Round Statistics", new Color(0.012f, 0.075f, 0.11f, 0.98f), new Vector2(0, -103.5f), new Vector2(760, 270));
            resultDetailsText = Label(statsPanel.transform, "", 31, Vector2.zero, new Vector2(710, 230));
            resultDetailsText.color = new Color(0.88f, 0.96f, 0.97f);

            var balancePanel = Panel(content, "Result Balance Panel", new Color(0.08f, 0.27f, 0.29f, 0.96f), new Vector2(0, -302.5f), new Vector2(620, 88));
            resultBalanceText = Label(balancePanel.transform, "", 31, Vector2.zero, new Vector2(580, 58));

            var replayButton = Button(content, "PLAY AGAIN", new Vector2(0, -430.5f), new Vector2(680, 128), StartRound);
            replayButtonText = replayButton.GetComponentInChildren<Text>();
            replayButtonText.fontSize = 42;
            var menuButton = Button(content, "MAIN MENU", new Vector2(0, -561.5f), new Vector2(500, 94), LoadMainMenu);
            menuButton.GetComponent<Image>().color = new Color(0.07f, 0.31f, 0.36f);
            menuButton.GetComponentInChildren<Text>().fontSize = 31;
            var footer = Label(content, "YOUR NEXT RUN COULD BE THE BIG ONE", 22, new Vector2(0, -649.5f), new Vector2(760, 42));
            footer.color = new Color(0.48f, 0.69f, 0.73f);
            resultPanel.SetActive(false);
        }

        private void BuildTutorial(Transform canvas)
        {
            tutorialPanel = Panel(canvas, "Tutorial Overlay", new Color(0.005f, 0.035f, 0.055f, 0.96f), Vector2.zero, new Vector2(1080, 1920), false);
            var card = Panel(tutorialPanel.transform, "Tutorial Card", new Color(0.025f, 0.13f, 0.18f, 1f), Vector2.zero, new Vector2(940, 1320));
            Label(card.transform, "HOW TO PLAY", 72, new Vector2(0, 480), new Vector2(820, 120));
            Label(
                card.transform,
                "1. Tap to move forward one lane.\n\n" +
                "2. Time each move and avoid the cars.\n\n" +
                "3. Every safe lane earns 10 coins.\n\n" +
                "4. Reach lane 20 to finish the run.\n\n" +
                "Controls: tap, click, SPACE, W, or UP arrow.",
                37,
                new Vector2(0, 70),
                new Vector2(850, 680));
            Button(card.transform, "GOT IT", new Vector2(0, -430), new Vector2(500, 112), CloseTutorial);
            tutorialPanel.SetActive(false);
        }

        private void ShowStart()
        {
            roundActive = false;
            chicken.InputEnabled = false;
            RefreshCoinMarkers();
            startPanel.SetActive(true);
            resultPanel.SetActive(false);
            settingsPanel.SetActive(false);
            cashOutButton.gameObject.SetActive(false);
            UpdateBetMenu();
        }

        private static void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void ShowSettings()
        {
            startPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void HideSettings()
        {
            settingsPanel.SetActive(false);
            startPanel.SetActive(true);
            UpdateBetMenu();
        }

        private void ShowTutorial()
        {
            tutorialPanel.SetActive(true);
        }

        private void CloseTutorial()
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
            tutorialPanel.SetActive(false);
        }

        private void ResetProgress()
        {
            balance = 1000;
            SaveBalance();
            UpdateBetMenu();
        }

        private void StartRound()
        {
            potentialPayout = 0;
            StopAllCoroutines();
            worldCamera.transform.position = new Vector3(0f, 4.2f, -10f);
            foreach (var lane in lanes) lane.ResetLane();
            foreach (var marker in coinMarkers.Values) marker.ResetMarker();
            currentRow = 0;
            chicken.ResetChicken();
            roundActive = true;
            chicken.InputEnabled = true;
            startPanel.SetActive(false);
            resultPanel.SetActive(false);
            cashOutButton.gameObject.SetActive(false);
            UpdateHud("TAP TO RUN");
            RefreshCoinMarkers();
        }

        public bool CanRun => roundActive;
        public float TrafficSpeedMultiplier => 1f + Mathf.Min(currentRow, FinishRow) * 0.055f;
        public float TrafficSpawnMultiplier => 1f + Mathf.Min(currentRow, FinishRow) * 0.035f;
        public RoadLane GetLane(int row) => row > 0 && row <= lanes.Count ? lanes[row - 1] : null;

        public void CompletedRow(int row)
        {
            if (!roundActive) return;
            currentRow = row;
            GetLane(row)?.Block();
            if (coinMarkers.TryGetValue(row, out var collectedCoin)) collectedCoin.Claim();
            potentialPayout = row * 10;
            UpdateHud(row == FinishRow ? "FINISH!" : "LANE SAFE — KEEP GOING");
            StartCoroutine(CameraFollow(row));
            RefreshCoinMarkers();
            if (row >= FinishRow) CashOut();
        }

        private void RefreshCoinMarkers()
        {
            int nextRow = roundActive ? currentRow + 1 : -1;
            foreach (var pair in coinMarkers)
                pair.Value.SetTarget(pair.Key == nextRow);
        }

        public void HitByCar()
        {
            if (!roundActive) return;
            roundActive = false;
            chicken.InputEnabled = false;
            StartCoroutine(HitSequence());
        }

        private IEnumerator HitSequence()
        {
            yield return chicken.PlayHit();
            EndRound(true, false);
        }

        private void CashOut()
        {
            if (!roundActive || currentRow == 0) return;
            EndRound(false, currentRow >= FinishRow);
        }

        private void EndRound(bool hit, bool finish)
        {
            roundActive = false;
            chicken.InputEnabled = false;
            int earnedCoins = currentRow * 5 + (finish ? 100 : 0);
            balance += earnedCoins;
            SaveBalance();
            RefreshCoinMarkers();
            cashOutButton.gameObject.SetActive(false);
            resultPanel.SetActive(true);
            if (replayButtonText != null) replayButtonText.text = "PLAY AGAIN";
            Color accent = hit ? new Color(1f, 0.31f, 0.2f) : new Color(1f, 0.68f, 0.06f);
            resultAccentBar.color = accent;
            var outline = resultCardImage.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.48f);
            resultIcon.color = hit ? new Color(0.72f, 0.78f, 0.8f, 0.76f) : Color.white;
            resultBadgeText.text = hit ? "ROUND OVER" : "ROUND COMPLETE";
            resultBadgeText.color = hit ? new Color(1f, 0.6f, 0.52f) : new Color(0.65f, 0.88f, 0.82f);
            resultUserIcon.gameObject.SetActive(!hit);
            if (!hit) resultUserIcon.sprite = UserProfileStorage.UserIcon;
            resultTitleText.text = hit ? "RUN ENDED" : $"NICE WORK, {UserProfileStorage.UserName}";
            resultTitleText.alignment = hit ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            resultTitleText.rectTransform.anchoredPosition = new Vector2(hit ? 0 : 90, 319);
            resultTitleText.rectTransform.sizeDelta = new Vector2(hit ? 820 : 600, 105);
            resultTitleText.color = hit ? new Color(1f, 0.48f, 0.36f) : new Color(1f, 0.95f, 0.82f);
            resultAmountText.text = $"+{FormatNumber(earnedCoins)}";
            resultAmountText.color = new Color(1f, 0.83f, 0.12f);
            resultAmountCaptionText.text = "COINS EARNED";
            resultDetailsText.text =
                $"STEPS COMPLETED        {currentRow} / {FinishRow}\n" +
                $"SCORE                       {FormatNumber(potentialPayout)}\n" +
                $"FINISH BONUS                {(finish ? 100 : 0)}\n" +
                $"TRAFFIC SPEED               x{TrafficSpeedMultiplier:0.00}";
            resultBalanceText.text = $"BALANCE   {FormatNumber(balance)}";
            resultBalanceText.color = hit ? new Color(0.82f, 0.91f, 0.92f) : new Color(1f, 0.88f, 0.28f);
        }

        private void UpdateHud(string message)
        {
            stepText.text = $"STEP {currentRow} / {FinishRow}";
            multiplierText.text = $"SCORE {FormatNumber(potentialPayout)}";
            balanceText.text = $"BALANCE  {FormatNumber(balance)}";
            payoutText.text = "FREE RUN  •  +5 COINS / LANE";
            messageText.text = message;
            cashOutButton.gameObject.SetActive(currentRow > 0);
            cashOutButton.interactable = currentRow > 0;
            cashOutText.text = "END RUN";
        }

        private void SaveBalance()
        {
            PlayerPrefs.SetInt("RiverJumpBalance", balance);
            PlayerPrefs.Save();
        }

        private void UpdateBetMenu()
        {
            if (menuBalanceText == null) return;
            menuBalanceText.text = FormatNumber(balance);
            maximumWinText.text = "GOAL: CROSS 20 LANES  •  +100 BONUS COINS";
            playButton.interactable = true;
            playButtonText.text = "START FREE RUN";
        }

        private static string FormatNumber(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        private IEnumerator CameraFollow(int row)
        {
            Vector3 start = worldCamera.transform.position;
            float targetY = Mathf.Max(4.2f, row * RowSpacing + 2.2f);
            Vector3 target = new Vector3(0f, targetY, -10f);
            for (float time = 0; time < 0.28f; time += Time.deltaTime)
            {
                worldCamera.transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / 0.28f));
                yield return null;
            }
            worldCamera.transform.position = target;
        }

        public static GameObject CreateTile(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent = null)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = new Vector3(position.x, position.y, 0f);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SolidSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            return gameObject;
        }

        private static GameObject CreateTexturedTile(string name, Sprite sprite, Vector2 position, Vector2 size, int order, Transform parent)
        {
            if (sprite == null)
                return CreateTile(name, position, size, new Color(0.25f, 0.67f, 0.27f), order, parent);
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = new Vector3(position.x, position.y, 0f);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
            return gameObject;
        }

        private static Sprite solidSprite;
        public static Sprite SolidSprite
        {
            get
            {
                if (solidSprite != null) return solidSprite;
                var texture = new Texture2D(1, 1) { name = "Road Pixel" };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                solidSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return solidSprite;
            }
        }

        private static void FitSprite(GameObject target, float width, float height)
        {
            var sprite = target.GetComponent<SpriteRenderer>().sprite;
            if (sprite == null) return;
            target.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        }

        private GameObject Panel(Transform parent, string name, Color color, Vector2 position, Vector2 size, bool outlined = true)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.sprite = roundedUiSprite;
            image.type = Image.Type.Sliced;
            if (outlined)
            {
                var outline = panel.AddComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.78f, 0.84f, 0.38f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            return panel;
        }

        private static Text Label(Transform parent, string value, int size, Vector2 position, Vector2 dimensions)
        {
            var gameObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(2f, -3f);
            return text;
        }

        private Button Button(Transform parent, string value, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var gameObject = Panel(parent, value, new Color(1f, 0.58f, 0.08f), position, size);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            button.onClick.AddListener(action);
            Label(gameObject.transform, value, 44, Vector2.zero, size);
            return button;
        }

        private Image CreateSpriteImage(Transform parent, string name, string path, Vector2 position, Vector2 size)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null) return null;
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const float radius = 15f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Road Rounded UI" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f) + Mathf.Max(x - (size - 1 - radius), 0f);
                    float dy = Mathf.Max(radius - y, 0f) + Mathf.Max(y - (size - 1 - radius), 0f);
                    float alpha = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) - radius + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }
    }
}
