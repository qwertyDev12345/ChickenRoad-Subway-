using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChickenRoad.Skins;
using UserProfile;

namespace ChickenRoad.Menu
{
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private const string BalanceKey = "RiverJumpBalance";
        private const string DailySpinKey = "ChickenRoadDailySpinUtc";
        private const int DailyCoins = 100;
        private const string DailyClaimCountKey = "ChickenRoadDailyClaimCount";

        private Text balanceText;
        private Text dailyStatusText;
        private Text spinButtonText;
        private Button spinButton;
        private readonly List<RectTransform> entranceCards = new();
        private GameObject rewardOverlay;
        private Text rewardAmountText;
        private Text rewardHeaderText;
        private Text rewardCaptionText;
        private Image rewardArtwork;
        private Button rewardEquipButton;
        private Button rewardClaimButton;
        private Text rewardClaimText;
        private ChickenSkinDefinition pendingSkin;
        private GameObject skinOverlay;
        private Transform skinGrid;
        private Image skinPreview;
        private Text skinPreviewName;
        private Text skinBalanceText;
        private Text skinMessageText;
        private Image collectionButtonPortrait;
        private Text collectionProgressText;
        private Sprite roundedSprite;
        private bool spinning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu" || FindFirstObjectByType<MainMenuBootstrap>() != null) return;
            new GameObject("Main Menu").AddComponent<MainMenuBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(camera.gameObject);
            CreateMenuCamera();
            BuildInterface();
            RefreshState();
            StartCoroutine(PlayEntrance());
        }

        private static void CreateMenuCamera()
        {
            var cameraObject = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.018f, .055f, .075f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        private void Update()
        {
            if (!spinning && !CanSpinToday()) RefreshState();
        }

        private void BuildInterface()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var canvasObject = new GameObject("Menu UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            roundedSprite = CreateRoundedSprite();

            Image background = Image(canvas.transform, "Background", new Color(.018f, .055f, .075f), Vector2.zero, new Vector2(1080, 1920));
            Stretch(background.rectTransform);
            Image glow = Image(canvas.transform, "Top Glow", new Color(.03f, .42f, .42f, .24f), new Vector2(0, 730), new Vector2(1080, 600));
            glow.sprite = roundedSprite;

            var safeArea = new GameObject("Safe Area", typeof(RectTransform), typeof(RiverJump.SafeAreaFitter));
            safeArea.transform.SetParent(canvas.transform, false);
            Stretch(safeArea.GetComponent<RectTransform>());
            Transform content = safeArea.transform;

            Text title = Label(content, $"Hi, {UserProfileStorage.UserName}", 50, new Vector2(0, 850), new Vector2(960, 72));
            title.color = new Color(1f, .91f, .65f);
            AddShadow(title, new Vector2(0, -5), new Color(0, 0, 0, .45f));
            Text subtitle = Label(content, "CHOOSE YOUR ADVENTURE", 23, new Vector2(-125, 757), new Vector2(390, 45));
            subtitle.alignment = TextAnchor.MiddleLeft;
            subtitle.color = new Color(.55f, .86f, .86f);
            CreateSettingsButton(content);
            CreateSkinsButton(content);

            var balance = Panel(content, "Balance", new Color(.025f, .13f, .16f, .98f), new Vector2(0, 646), new Vector2(840, 112));
            AddShadow(balance, new Vector2(0, -7), new Color(0, 0, 0, .32f));
            Label(balance.transform, "TOTAL BALANCE", 26, new Vector2(-215, 0), new Vector2(330, 55)).color = new Color(.65f, .84f, .84f);
            Image coin = Image(balance.transform, "Coin", new Color(1f, .71f, .08f), new Vector2(90, 0), new Vector2(56, 56));
            coin.sprite = Resources.Load<Sprite>("RiverJump2D/Coin") ?? CreateCircleSprite();
            coin.preserveAspect = true;
            balanceText = Label(balance.transform, "0", 48, new Vector2(245, 0), new Vector2(260, 70));
            balanceText.color = new Color(1f, .82f, .16f);

            BuildDailyCard(content);
            BuildModeCards(content);
            Text footer = Label(content, "COME BACK EVERY DAY FOR FREE COINS", 22, new Vector2(0, -875), new Vector2(820, 45));
            footer.color = new Color(.42f, .68f, .7f);
            BuildRewardOverlay(content);
            BuildSkinOverlay(content);
        }

        private void BuildDailyCard(Transform parent)
        {
            var card = Panel(parent, "Daily Bonus", new Color(.035f, .16f, .19f, .98f), new Vector2(0, 245), new Vector2(920, 650));
            entranceCards.Add(card.GetComponent<RectTransform>());
            AddShadow(card, new Vector2(0, -10), new Color(0, 0, 0, .34f));
            Image accent = Image(card.transform, "Daily Accent", new Color(1f, .67f, .06f), new Vector2(0, 316), new Vector2(840, 8));
            accent.sprite = roundedSprite;
            Label(card.transform, "DAILY BONUS", 42, new Vector2(0, 270), new Vector2(600, 60)).color = new Color(1f, .88f, .35f);
            dailyStatusText = Label(card.transform, "+100 COINS EVERY DAY", 25, new Vector2(0, 218), new Vector2(700, 38));
            dailyStatusText.color = new Color(.68f, .88f, .88f);

            Image giftCircle = Image(card.transform, "Daily Gift", new Color(.08f, .29f, .31f), new Vector2(0, -20), new Vector2(340, 340));
            giftCircle.sprite = CreateCircleSprite();
            Image coinIcon = Image(card.transform, "Coin Icon", Color.white, new Vector2(0, 20), new Vector2(150, 150));
            coinIcon.sprite = Resources.Load<Sprite>("RiverJump2D/Coin") ?? CreateCircleSprite();
            Label(card.transform, "+100", 67, new Vector2(0, -105), new Vector2(270, 90)).color = new Color(1f, .84f, .18f);
            spinButton = Button(card.transform, "CLAIM DAILY GIFT", new Vector2(0, -278), new Vector2(530, 86), Spin);
            spinButtonText = spinButton.GetComponentInChildren<Text>();
        }

        private void CreateSkinsButton(Transform parent)
        {
            var card = Panel(parent, "Collection Button", new Color(.025f, .14f, .17f, .98f), new Vector2(285, 757), new Vector2(270, 82));
            AddShadow(card, new Vector2(0, -5), new Color(0, 0, 0, .35f));
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(1f, .67f, .08f, .9f);
            outline.effectDistance = new Vector2(2, -2);
            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(ShowSkins);
            var colors = button.colors;
            colors.highlightedColor = new Color(.05f, .25f, .28f);
            colors.pressedColor = new Color(.02f, .09f, .11f);
            button.colors = colors;

            collectionButtonPortrait = Image(card.transform, "Selected Skin", Color.white, new Vector2(-92, 0), new Vector2(68, 68));
            collectionButtonPortrait.sprite = ChickenSkinCatalog.GetSprite(ChickenSkinService.Selected.Id);
            collectionButtonPortrait.preserveAspect = true;
            Text label = Label(card.transform, "COLLECTION", 20, new Vector2(38, 15), new Vector2(170, 30));
            label.color = new Color(1f, .86f, .35f);
            int unlocked = 0;
            foreach (ChickenSkinDefinition skin in ChickenSkinCatalog.All)
                if (ChickenSkinService.IsUnlocked(skin.Id)) unlocked++;
            collectionProgressText = Label(card.transform, $"{unlocked} / {ChickenSkinCatalog.All.Length}  UNLOCKED", 14, new Vector2(38, -17), new Vector2(170, 24));
            collectionProgressText.color = new Color(.62f, .82f, .83f);
        }

        private void CreateSettingsButton(Transform parent)
        {
            var panel = Panel(parent, "Settings Button", new Color(.025f, .14f, .17f, .98f), new Vector2(-420, 757), new Vector2(82, 82));
            AddShadow(panel, new Vector2(0, -5), new Color(0, 0, 0, .35f));

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, .67f, .08f, .9f);
            outline.effectDistance = new Vector2(2, -2);

            var button = panel.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(() => SceneManager.LoadScene("Settings"));
            var colors = button.colors;
            colors.highlightedColor = new Color(.05f, .25f, .28f);
            colors.pressedColor = new Color(.02f, .09f, .11f);
            button.colors = colors;

            Image gear = Image(panel.transform, "Gear", new Color(1f, .78f, .16f), Vector2.zero, new Vector2(54, 54));
            gear.sprite = CreateGearSprite();
            gear.preserveAspect = true;
        }

        private void BuildModeCards(Transform parent)
        {
            Label(parent, "SELECT GAME MODE", 30, new Vector2(0, -139), new Vector2(700, 50)).color = new Color(.82f, .94f, .94f);
            CreateModeCard(parent, "RIVER JUMP", "JUMP ACROSS THE WILD RIVER", "RiverJump2D/Boat", new Vector2(0, -335), new Color(.04f, .38f, .48f), () => SceneManager.LoadScene("SampleScene"));
            CreateModeCard(parent, "ROAD CROSSING", "DODGE TRAFFIC • REACH THE FINISH", "RoadCrossing/CarYellow", new Vector2(0, -656), new Color(.55f, .22f, .08f), () => SceneManager.LoadScene("RoadCrossing"));
        }

        private void CreateModeCard(Transform parent, string title, string subtitle, string resource, Vector2 position, Color color, UnityEngine.Events.UnityAction action)
        {
            var card = Panel(parent, title, color, position, new Vector2(920, 270));
            entranceCards.Add(card.GetComponent<RectTransform>());
            AddShadow(card, new Vector2(0, -9), new Color(0, 0, 0, .35f));
            var cardButton = card.AddComponent<Button>();
            cardButton.targetGraphic = card.GetComponent<UnityEngine.UI.Image>();
            cardButton.onClick.AddListener(action);
            var cardColors = cardButton.colors;
            cardColors.highlightedColor = color * 1.12f;
            cardColors.pressedColor = color * .82f;
            cardButton.colors = cardColors;
            var art = new GameObject("Art", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            art.transform.SetParent(card.transform, false);
            var artRect = art.GetComponent<RectTransform>();
            artRect.anchoredPosition = new Vector2(-290, 14);
            artRect.sizeDelta = new Vector2(235, 145);
            art.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>(resource);
            art.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
            Text heading = Label(card.transform, title, 39, new Vector2(112, 48), new Vector2(510, 58));
            heading.alignment = TextAnchor.MiddleLeft;
            Text description = Label(card.transform, subtitle, 20, new Vector2(112, -4), new Vector2(510, 48));
            description.alignment = TextAnchor.MiddleLeft;
            description.color = new Color(.83f, .92f, .92f);
            var playPill = Panel(card.transform, "Play", new Color(.03f, .12f, .14f, .72f), new Vector2(175, -77), new Vector2(355, 70));
            Text playText = Label(playPill.transform, "PLAY  ›", 27, Vector2.zero, new Vector2(330, 56));
            playText.color = new Color(1f, .88f, .5f);
        }

        private void BuildRewardOverlay(Transform parent)
        {
            rewardOverlay = Panel(parent, "Reward Overlay", new Color(.005f, .025f, .035f, .93f), Vector2.zero, new Vector2(1080, 1920));
            var card = Panel(rewardOverlay.transform, "Reward Card", new Color(.035f, .16f, .19f), Vector2.zero, new Vector2(820, 790));
            AddShadow(card, new Vector2(0, -14), new Color(0, 0, 0, .5f));
            rewardHeaderText = Label(card.transform, "DAILY REWARD", 30, new Vector2(0, 280), new Vector2(620, 55));
            rewardHeaderText.color = new Color(.65f, .88f, .88f);
            rewardArtwork = Image(card.transform, "Reward Artwork", Color.white, new Vector2(0, 125), new Vector2(210, 210));
            rewardArtwork.sprite = Resources.Load<Sprite>("RiverJump2D/Coin") ?? CreateCircleSprite();
            rewardArtwork.preserveAspect = true;
            rewardAmountText = Label(card.transform, "+100", 92, new Vector2(0, -30), new Vector2(700, 120));
            rewardAmountText.color = new Color(1f, .82f, .16f);
            rewardCaptionText = Label(card.transform, "COINS ADDED TO YOUR BALANCE", 24, new Vector2(0, -112), new Vector2(700, 50));
            rewardCaptionText.color = new Color(.7f, .86f, .86f);
            rewardClaimButton = Button(card.transform, "CLAIM", new Vector2(0, -255), new Vector2(550, 110), HideReward);
            rewardClaimButton.GetComponentInChildren<Text>().fontSize = 36;
            rewardClaimText = rewardClaimButton.GetComponentInChildren<Text>();
            rewardEquipButton = Button(card.transform, "EQUIP", new Vector2(-190, -255), new Vector2(330, 105), EquipRewardSkin);
            rewardEquipButton.GetComponentInChildren<Text>().fontSize = 31;
            rewardEquipButton.gameObject.SetActive(false);
            rewardOverlay.SetActive(false);
        }

        private void BuildSkinOverlay(Transform parent)
        {
            // A full, unsliced background keeps the underlying menu out of the collection.
            skinOverlay = Image(parent, "Skin Collection", new Color(.006f, .03f, .04f, 1f), Vector2.zero, new Vector2(1080, 1920)).gameObject;
            Label(skinOverlay.transform, "CHICKEN COLLECTION", 52, new Vector2(0, 820), new Vector2(850, 80)).color = new Color(1f, .86f, .28f);
            Button close = Button(skinOverlay.transform, "×", new Vector2(430, 825), new Vector2(86, 72), HideSkins);
            close.GetComponentInChildren<Text>().fontSize = 42;

            var balancePanel = Panel(skinOverlay.transform, "Collection Balance", new Color(.025f, .13f, .16f), new Vector2(0, 720), new Vector2(720, 86));
            Label(balancePanel.transform, "BALANCE", 23, new Vector2(-175, 0), new Vector2(230, 45)).color = new Color(.62f, .82f, .83f);
            skinBalanceText = Label(balancePanel.transform, "0", 37, new Vector2(155, 0), new Vector2(300, 55));
            skinBalanceText.color = new Color(1f, .82f, .16f);

            var previewPanel = Panel(skinOverlay.transform, "Selected Preview", new Color(.035f, .16f, .19f), new Vector2(0, 505), new Vector2(860, 290));
            skinPreview = Image(previewPanel.transform, "Chicken Preview", Color.white, new Vector2(-250, 0), new Vector2(230, 230));
            skinPreview.sprite = Resources.Load<Sprite>("RiverJump2D/Chicken");
            skinPreview.preserveAspect = true;
            skinPreviewName = Label(previewPanel.transform, "CLASSIC", 38, new Vector2(125, 35), new Vector2(470, 65));
            var equippedBadge = Panel(previewPanel.transform, "Equipped Badge", new Color(.07f, .33f, .25f), new Vector2(125, -35), new Vector2(290, 48));
            Label(equippedBadge.transform, "EQUIPPED", 23, Vector2.zero, new Vector2(270, 40)).color = new Color(.65f, 1f, .78f);
            Label(previewPanel.transform, "ACTIVE IN BOTH GAME MODES", 18, new Vector2(125, -91), new Vector2(470, 35)).color = new Color(.62f, .82f, .83f);
            skinMessageText = Label(skinOverlay.transform, string.Empty, 22, new Vector2(0, -700), new Vector2(850, 62));
            skinMessageText.color = new Color(1f, .78f, .18f);
            Label(skinOverlay.transform, "YOUR SKINS", 25, new Vector2(-285, 322), new Vector2(310, 42)).alignment = TextAnchor.MiddleLeft;
            Label(skinOverlay.transform, "SELECT OWNED • BUY WITH COINS", 18, new Vector2(175, 322), new Vector2(530, 42)).color = new Color(.62f, .82f, .83f);

            var gridObject = new GameObject("Skin Grid", typeof(RectTransform));
            gridObject.transform.SetParent(skinOverlay.transform, false);
            skinGrid = gridObject.transform;
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(.5f, .5f);
            gridRect.anchoredPosition = new Vector2(0, -135);
            gridRect.sizeDelta = new Vector2(920, 860);
            skinOverlay.SetActive(false);
        }

        private void ShowSkins()
        {
            skinOverlay.SetActive(true);
            skinMessageText.text = "EXCLUSIVE SKINS UNLOCK EVERY 7 DAILY CLAIMS";
            RefreshSkinCollection();
        }

        private void HideSkins()
        {
            skinOverlay.SetActive(false);
            RefreshCollectionButton();
        }

        private void RefreshCollectionButton()
        {
            collectionButtonPortrait.sprite = ChickenSkinCatalog.GetSprite(ChickenSkinService.Selected.Id);
            int unlocked = 0;
            foreach (ChickenSkinDefinition skin in ChickenSkinCatalog.All)
                if (ChickenSkinService.IsUnlocked(skin.Id)) unlocked++;
            collectionProgressText.text = $"{unlocked} / {ChickenSkinCatalog.All.Length}  UNLOCKED";
        }

        private void RefreshSkinCollection()
        {
            for (int i = skinGrid.childCount - 1; i >= 0; i--) Destroy(skinGrid.GetChild(i).gameObject);
            ChickenSkinDefinition selected = ChickenSkinService.Selected;
            skinPreview.sprite = ChickenSkinCatalog.GetSprite(selected.Id);
            skinPreview.color = Color.white;
            skinPreviewName.text = selected.Name.ToUpperInvariant();
            skinPreviewName.color = Color.white;
            int availableCoins = PlayerPrefs.GetInt(BalanceKey, 1000);
            skinBalanceText.text = FormatNumber(availableCoins);

            ChickenSkinDefinition[] skins = ChickenSkinCatalog.All;
            for (int i = 0; i < skins.Length; i++)
            {
                ChickenSkinDefinition skin = skins[i];
                int column = i % 2;
                int row = i / 2;
                Vector2 position = new Vector2(column == 0 ? -220 : 220, 305 - row * 205);
                Color rarity = ChickenSkinService.RarityColor(skin.Rarity);
                bool unlocked = ChickenSkinService.IsUnlocked(skin.Id);
                bool isSelected = selected.Id == skin.Id;
                var card = Panel(skinGrid, skin.Name, isSelected ? new Color(.045f, .23f, .19f) : new Color(.035f, .11f, .14f), position, new Vector2(410, 180));
                var outline = card.AddComponent<Outline>();
                outline.effectColor = isSelected ? new Color(.3f, .9f, .6f) : new Color(rarity.r, rarity.g, rarity.b, .35f);
                outline.effectDistance = new Vector2(2, -2);
                string capturedId = skin.Id;

                Image art = Image(card.transform, "Preview", Color.white, new Vector2(-120, 5), new Vector2(130, 145));
                art.sprite = ChickenSkinCatalog.GetSprite(skin.Id);
                art.preserveAspect = true;
                Text name = Label(card.transform, skin.Name.ToUpperInvariant(), 21, new Vector2(75, 57), new Vector2(230, 34));
                name.color = Color.white;
                Label(card.transform, skin.Rarity.ToString().ToUpperInvariant(), 15, new Vector2(75, 25), new Vector2(230, 25)).color = rarity;
                bool canBuy = availableCoins >= skin.Price;
                string state = isSelected ? "EQUIPPED" : unlocked ? "SELECT" : skin.DailyExclusive ? "DAY 7 GIFT" : $"BUY  {FormatNumber(skin.Price)}";
                var action = Button(card.transform, state, new Vector2(75, -16), new Vector2(222, 46), () => OnSkinPressed(capturedId));
                action.GetComponentInChildren<Text>().fontSize = 19;
                action.GetComponent<Image>().color = Color.white;
                var actionColors = action.colors;
                Color actionColor = isSelected ? new Color(.08f, .36f, .25f) : unlocked ? new Color(.055f, .36f, .4f) : skin.DailyExclusive ? new Color(.18f, .15f, .26f) : canBuy ? new Color(.85f, .48f, .045f) : new Color(.2f, .25f, .27f);
                actionColors.normalColor = actionColor;
                actionColors.highlightedColor = Color.Lerp(actionColor, Color.white, .12f);
                actionColors.pressedColor = Color.Lerp(actionColor, Color.black, .2f);
                actionColors.disabledColor = actionColor;
                action.colors = actionColors;
                action.interactable = !isSelected && (unlocked || (!skin.DailyExclusive && canBuy));
                string hint = isSelected ? "IN USE" : unlocked ? "OWNED" : skin.DailyExclusive ? "EVERY 7TH CLAIM" : canBuy ? "UNLOCK & EQUIP" : $"NEED {FormatNumber(skin.Price - availableCoins)} MORE";
                Label(card.transform, hint, 14, new Vector2(75, -59), new Vector2(230, 28)).color = new Color(.65f, .79f, .8f);
            }
        }

        private void OnSkinPressed(string id)
        {
            ChickenSkinDefinition skin = ChickenSkinCatalog.Get(id);
            if (ChickenSkinService.IsUnlocked(id))
            {
                ChickenSkinService.Select(id);
                skinMessageText.text = skin.Name.ToUpperInvariant() + " EQUIPPED";
            }
            else if (skin.DailyExclusive)
            {
                skinMessageText.text = "CLAIM 7 DAILY GIFTS TO UNLOCK THIS SKIN";
            }
            else if (!ChickenSkinService.TryBuy(id))
            {
                skinMessageText.text = "NOT ENOUGH COINS";
            }
            else
            {
                skinMessageText.text = skin.Name.ToUpperInvariant() + " UNLOCKED!";
            }
            RefreshSkinCollection();
            RefreshCollectionButton();
            balanceText.text = FormatNumber(PlayerPrefs.GetInt(BalanceKey, 1000));
        }

        private void Spin()
        {
            if (spinning || !CanSpinToday()) return;
            spinning = true;
            spinButton.interactable = false;
            int claimCount = PlayerPrefs.GetInt(DailyClaimCountKey, 0) + 1;
            int balance = PlayerPrefs.GetInt(BalanceKey, 1000) + DailyCoins;
            ChickenSkinDefinition wonSkin = claimCount % 7 == 0 ? ChickenSkinService.GetLockedDailySkin() : null;
            PlayerPrefs.SetInt(BalanceKey, balance);
            PlayerPrefs.SetInt(DailyClaimCountKey, claimCount);
            if (wonSkin != null)
            {
                ChickenSkinService.Unlock(wonSkin.Id);
                pendingSkin = wonSkin;
            }
            else
                pendingSkin = null;
            PlayerPrefs.SetString(DailySpinKey, TodayKey());
            PlayerPrefs.Save();
            spinning = false;
            balanceText.text = FormatNumber(balance);
            dailyStatusText.text = wonSkin != null ? "+100 COINS AND A NEW SKIN!" : "+100 COINS ADDED!";
            dailyStatusText.color = new Color(1f, .86f, .2f);
            spinButtonText.text = "CLAIMED TODAY";
            if (wonSkin != null) ConfigureSkinReward(wonSkin);
            else ConfigureCoinReward(DailyCoins);
            rewardOverlay.SetActive(true);
            StartCoroutine(PopRewardCard());
        }

        private void ConfigureCoinReward(int reward)
        {
            rewardHeaderText.text = "DAILY REWARD";
            rewardHeaderText.color = new Color(.65f, .88f, .88f);
            rewardArtwork.sprite = Resources.Load<Sprite>("RiverJump2D/Coin") ?? CreateCircleSprite();
            rewardArtwork.color = Color.white;
            rewardAmountText.text = $"+{reward}";
            rewardAmountText.fontSize = 92;
            rewardAmountText.color = new Color(1f, .82f, .16f);
            rewardCaptionText.text = "COINS ADDED TO YOUR BALANCE";
            rewardClaimButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -255);
            rewardClaimButton.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 110);
            rewardClaimText.text = "CLAIM";
            rewardEquipButton.gameObject.SetActive(false);
        }

        private void ConfigureSkinReward(ChickenSkinDefinition skin)
        {
            Color rarity = ChickenSkinService.RarityColor(skin.Rarity);
            rewardHeaderText.text = "NEW SKIN UNLOCKED";
            rewardHeaderText.color = rarity;
            rewardArtwork.sprite = ChickenSkinCatalog.GetSprite(skin.Id);
            rewardArtwork.color = Color.white;
            rewardAmountText.text = skin.Name.ToUpperInvariant();
            rewardAmountText.fontSize = 52;
            rewardAmountText.color = rarity;
            rewardCaptionText.text = "+100 COINS  •  DAY 7 SKIN GIFT";
            rewardClaimButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(190, -255);
            rewardClaimButton.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 105);
            rewardClaimText.text = "LATER";
            rewardEquipButton.gameObject.SetActive(true);
        }

        private void EquipRewardSkin()
        {
            if (pendingSkin != null) ChickenSkinService.Select(pendingSkin.Id);
            pendingSkin = null;
            HideReward();
        }

        private void HideReward()
        {
            rewardOverlay.SetActive(false);
        }

        private IEnumerator PopRewardCard()
        {
            Transform card = rewardOverlay.transform.Find("Reward Card");
            card.localScale = new Vector3(.72f, .72f, 1f);
            for (float elapsed = 0; elapsed < .34f; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / .34f);
                float scale = Mathf.Lerp(.72f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                card.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            card.localScale = Vector3.one;
        }

        private IEnumerator PlayEntrance()
        {
            foreach (RectTransform card in entranceCards)
            {
                Vector2 target = card.anchoredPosition;
                card.anchoredPosition = target + new Vector2(0, -45);
                card.localScale = new Vector3(.96f, .96f, 1f);
                var group = card.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                for (float elapsed = 0; elapsed < .25f; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.Clamp01(elapsed / .25f);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    card.anchoredPosition = Vector2.Lerp(target + new Vector2(0, -45), target, eased);
                    card.localScale = Vector3.Lerp(new Vector3(.96f, .96f, 1f), Vector3.one, eased);
                    group.alpha = eased;
                    yield return null;
                }
                card.anchoredPosition = target;
                card.localScale = Vector3.one;
                group.alpha = 1f;
                yield return new WaitForSecondsRealtime(.04f);
            }
        }

        private void RefreshState()
        {
            balanceText.text = FormatNumber(PlayerPrefs.GetInt(BalanceKey, 1000));
            bool available = CanSpinToday();
            spinButton.interactable = available;
            spinButtonText.text = available ? "CLAIM DAILY GIFT" : "COME BACK TOMORROW";
            if (!available && !dailyStatusText.text.StartsWith("YOU "))
            {
                dailyStatusText.text = "TODAY'S BONUS IS ALREADY CLAIMED";
                dailyStatusText.color = new Color(.58f, .76f, .77f);
            }
        }

        private static bool CanSpinToday() => PlayerPrefs.GetString(DailySpinKey, string.Empty) != TodayKey();
        private static string TodayKey() => DateTime.Now.ToString("yyyy-MM-dd");
        private static string FormatNumber(int value) => value.ToString("N0").Replace(',', ' ');

        private GameObject Panel(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var image = Image(parent, name, color, position, size);
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            return image.gameObject;
        }

        private static UnityEngine.UI.Image Image(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            return image;
        }

        private static Text Label(Transform parent, string value, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private Button Button(Transform parent, string value, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(parent, value + " Button", new Color(.93f, .55f, .06f), position, size);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .69f, .16f);
            colors.pressedColor = new Color(.78f, .4f, .03f);
            colors.disabledColor = new Color(.2f, .31f, .32f, .8f);
            button.colors = colors;
            Label(go.transform, value, 31, Vector2.zero, size - new Vector2(20, 10));
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AddShadow(Graphic graphic, Vector2 distance, Color color)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = distance;
            shadow.effectColor = color;
            shadow.useGraphicAlpha = true;
        }

        private static void AddShadow(GameObject gameObject, Vector2 distance, Color color)
        {
            AddShadow(gameObject.GetComponent<Graphic>(), distance, color);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(64f - distance));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        private static Sprite CreateGearSprite()
        {
            const int size = 128;
            const int teeth = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * .5f, (size - 1) * .5f);

            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float radius = point.magnitude;
                float angle = Mathf.Atan2(point.y, point.x);
                float toothWave = Mathf.Cos(angle * teeth);
                float outerRadius = toothWave > .35f ? 58f : 49f;
                float outerAlpha = Mathf.Clamp01(outerRadius + .8f - radius);
                float innerAlpha = Mathf.Clamp01(radius - 20f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, outerAlpha * innerAlpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const float radius = 13f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - (31.5f - radius));
                float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - (31.5f - radius));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }
    }
}
