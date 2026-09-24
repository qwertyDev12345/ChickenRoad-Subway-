using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UserProfile;

namespace RiverJump
{
    public sealed partial class RiverJumpBootstrap : MonoBehaviour
    {
        public static RiverJumpBootstrap Instance { get; private set; }

        private readonly List<Material> materials = new();
        private readonly Dictionary<string, Material> materialCache = new();
        private readonly Dictionary<int, CoefficientMarker> coefficientMarkers = new();
        private Transform sceneryRoot;
        private ChickenController chicken;
        private Text multiplierText;
        private Text coinsText;
        private Text messageText;
        private Text payoutText;
        private Text cashOutLabel;
        private Text menuBalanceText;
        private Text menuMaxWinText;
        private Text menuPlayButtonText;
        private Button menuPlayButton;
        private Text resultTitleText;
        private Text resultDetailsText;
        private Text resultBadgeText;
        private Text resultAmountText;
        private Text resultAmountCaptionText;
        private Text resultBalanceText;
        private Text resultReplayButtonText;
        private Image resultCard;
        private Image resultAccentBar;
        private Image resultIcon;
        private Image resultUserIcon;
        private Text soundButtonText;
        private Text vibrationButtonText;
        private Text qualityButtonText;
        private Slider progressSlider;
        private Button cashOutButton;
        private GameObject gameplayHud;
        private GameObject menuOverlay;
        private GameObject resultOverlay;
        private GameObject settingsOverlay;
        private GameObject tutorialOverlay;
        private GameObject privacyOverlay;
        private AudioSource audioSource;
        private Sprite roundedUiSprite;
        private int balance;
        private int potentialPayout;
        private int bestRow;
        private int comboCount;
        private int bestCombo;
        private int roundBonus;
        private bool soundEnabled;
        private bool vibrationEnabled;
        private bool worldMotionPaused;
        private Coroutine payoutImpactRoutine;
        private int qualityLevel;
        private static bool retryAfterReload;
        private const int FinishRow = 20;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SampleScene") return;
            CreateGame();
        }

        private static void CreateGame()
        {
            if (FindFirstObjectByType<RiverJumpBootstrap>() != null) return;
            var root = new GameObject("River Jump");
            root.AddComponent<RiverJumpBootstrap>();
        }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            balance = PlayerPrefs.GetInt("RiverJumpBalance", 1000);
            soundEnabled = PlayerPrefs.GetInt("RiverJumpSound", 1) == 1;
            vibrationEnabled = PlayerPrefs.GetInt("RiverJumpVibration", 1) == 1;
            qualityLevel = PlayerPrefs.GetInt("RiverJumpQuality", 1);
            ApplyQuality();
            BuildWorld();
            BuildInterface();
            SetupAudio();
            chicken.IsRoundOver = true;
            if (retryAfterReload)
            {
                retryAfterReload = false;
                StartRound();
            }
            else
            {
                ShowMenu();
                if (PlayerPrefs.GetInt("RiverJumpTutorialSeen", 0) == 0) ShowTutorial();
            }
        }

        private void BuildWorld3D()
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(camera.gameObject);
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(light.gameObject);

            RenderSettings.ambientLight = new Color(0.62f, 0.72f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.82f, 0.88f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 48f;
            ApplyQuality();
            sceneryRoot = new GameObject("Procedural Scenery").transform;
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1f, 0.93f, 0.78f);
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var fillObject = new GameObject("Soft Fill Light");
            var fillLight = fillObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.35f;
            fillLight.color = new Color(0.35f, 0.63f, 1f);
            fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);

            var water = Primitive("Water", PrimitiveType.Cube, new Vector3(0, -0.45f, 18), new Vector3(13, 0.7f, 34), CreateWaterMaterial());
            Destroy(water.GetComponent<Collider>());
            water.transform.SetParent(sceneryRoot);
            water.AddComponent<WaterBob>().amplitude = 0.025f;
            CreateWaterDetails();

            CreateBank("Start Bank", 0f, 5f);
            CreateBank("Island 1", 7f, 2f);
            CreateBank("Island 2", 14f, 2f);
            CreateBank("Finish Bank", 21.5f, 4f);

            for (int row = 1; row < FinishRow; row++)
            {
                if (row == 7 || row == 14) continue;
                CreateLane(row);
            }
            CreateCoefficientMarkers();

            var chickenObject = new GameObject("Chicken");
            chickenObject.transform.position = new Vector3(0, 0.55f, 0);
            chicken = chickenObject.AddComponent<ChickenController>();
            BuildChicken(chickenObject.transform);

            var cameraObject = new GameObject("Portrait Camera");
            cameraObject.tag = "MainCamera";
            var cam = cameraObject.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.48f, 0.82f, 0.95f);
            cameraObject.AddComponent<AudioListener>();
            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            SetupPostProcessing();
            var follow = cameraObject.AddComponent<PortraitCamera>();
            follow.target = chickenObject.transform;
            cameraObject.transform.position = new Vector3(0, 10.5f, -7f);
            cameraObject.transform.rotation = Quaternion.Euler(53f, 0, 0);
        }

        private Material CreateWaterMaterial()
        {
            var shader = Shader.Find("RiverJump/StylizedRiver") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "Stylized River" };
            if (material.HasProperty("_ShallowColor")) material.SetColor("_ShallowColor", new Color(0.08f, 0.76f, 0.92f));
            if (material.HasProperty("_DeepColor")) material.SetColor("_DeepColor", new Color(0.015f, 0.24f, 0.66f));
            if (material.HasProperty("_FoamColor")) material.SetColor("_FoamColor", new Color(0.78f, 0.98f, 1f));
            if (material.HasProperty("_WaveScale")) material.SetFloat("_WaveScale", 1.35f);
            if (material.HasProperty("_WaveSpeed")) material.SetFloat("_WaveSpeed", 1.15f);
            if (material.HasProperty("_WaveHeight")) material.SetFloat("_WaveHeight", 0.055f);
            materials.Add(material);
            return material;
        }

        private void SetupPostProcessing()
        {
            var volumeObject = new GameObject("River Jump Post Processing");
            volumeObject.transform.SetParent(transform);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = volume.profile.Add<Bloom>();
            bloom.active = true;
            bloom.intensity.Override(0.34f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.62f);

            var color = volume.profile.Add<ColorAdjustments>();
            color.active = true;
            color.postExposure.Override(0.08f);
            color.contrast.Override(10f);
            color.saturation.Override(13f);

            var vignette = volume.profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.72f);
        }

        private void CreateBank(string name, float z, float depth)
        {
            var bank = Primitive(name, PrimitiveType.Cube, new Vector3(0, 0, z), new Vector3(13, 0.7f, depth), MaterialWithColor("Grass", new Color(0.25f, 0.73f, 0.25f)));
            bank.transform.SetParent(sceneryRoot);
            if (name.StartsWith("Island")) CreateStylizedIslandChain(z);
            var sand = Primitive("Sandy Edge", PrimitiveType.Cube, new Vector3(0, 0.12f, z + depth * 0.48f), new Vector3(13, 0.18f, 0.18f), MaterialWithColor("Sand", new Color(0.92f, 0.75f, 0.38f)));
            sand.transform.SetParent(sceneryRoot);
            Destroy(sand.GetComponent<Collider>());
            for (int i = -5; i <= 5; i += 2)
            {
                CreateReedCluster(new Vector3(i, 0.34f, z + depth * 0.36f));
            }
            if (depth >= 4f)
            {
                CreateTree(new Vector3(-4.9f, 0.35f, z - depth * 0.2f), 0.85f);
                CreateTree(new Vector3(4.8f, 0.35f, z + depth * 0.12f), 1.05f);
            }
            CreateFlowers(z, depth);
            CreateBankDetails(z, depth);
        }

        private void CreateStylizedIslandChain(float z)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedIsland");
            if (generatedPrefab == null) return;

            for (int i = 0; i < 4; i++)
            {
                var island = Instantiate(generatedPrefab, new Vector3(-4.5f + i * 3f, 0, z), Quaternion.identity);
                island.name = $"Stylized Safe Island {i + 1}";
                island.transform.SetParent(sceneryRoot);
                foreach (var collider in island.GetComponentsInChildren<Collider>()) Destroy(collider);
            }
        }

        private void CreateBankDetails(float z, float depth)
        {
            var stoneMaterial = MaterialWithColor("River Stones", new Color(0.38f, 0.48f, 0.46f));
            var lightStone = MaterialWithColor("Light River Stones", new Color(0.56f, 0.63f, 0.57f));
            for (int i = 0; i < 10; i++)
            {
                float x = -5.8f + i * 1.28f;
                float edgeZ = z + depth * 0.47f + Mathf.Sin(i * 2.1f) * 0.08f;
                var rock = Primitive("River Stone", PrimitiveType.Sphere, new Vector3(x, 0.32f, edgeZ), new Vector3(0.28f + i % 3 * 0.07f, 0.17f, 0.22f), i % 2 == 0 ? stoneMaterial : lightStone);
                rock.transform.rotation = Quaternion.Euler(i * 11f, i * 29f, i * 7f);
                rock.transform.SetParent(sceneryRoot);
                Destroy(rock.GetComponent<Collider>());
            }

            var bushMaterial = MaterialWithColor("Bush", new Color(0.08f, 0.5f, 0.15f));
            for (int i = 0; i < 4; i++)
            {
                float x = -4.4f + i * 2.9f;
                float bushZ = z - depth * 0.25f + (i % 2) * 0.35f;
                for (int lobe = -1; lobe <= 1; lobe++)
                {
                    var bush = Primitive("Bush", PrimitiveType.Sphere, new Vector3(x + lobe * 0.22f, 0.52f + (lobe == 0 ? 0.18f : 0), bushZ), new Vector3(0.4f, 0.42f, 0.36f), bushMaterial);
                    bush.transform.SetParent(sceneryRoot);
                    Destroy(bush.GetComponent<Collider>());
                }
            }

            var grassMaterial = MaterialWithColor("Fresh Grass", new Color(0.08f, 0.62f, 0.18f));
            for (int i = 0; i < 14; i++)
            {
                float x = -5.4f + (i * 1.73f) % 10.8f;
                float grassZ = z - depth * 0.38f + (i % 4) * 0.22f;
                var blade = Primitive("Grass Blade", PrimitiveType.Cube, new Vector3(x, 0.55f, grassZ), new Vector3(0.045f, 0.38f + (i % 3) * 0.07f, 0.035f), grassMaterial);
                blade.transform.rotation = Quaternion.Euler(0, i * 31f, (i % 2 == 0 ? -1 : 1) * 9f);
                blade.transform.SetParent(sceneryRoot);
                Destroy(blade.GetComponent<Collider>());
            }
        }

        private void CreateWaterDetails()
        {
            var foamMaterial = MaterialWithColor("Foam", new Color(0.66f, 0.94f, 1f));
            for (int z = 2; z < 20; z += 2)
            {
                for (int x = -5; x <= 5; x += 3)
                {
                    var ripple = Primitive("Water Ripple", PrimitiveType.Cube, new Vector3(x + (z % 3) * 0.35f, -0.055f, z), new Vector3(1.15f, 0.025f, 0.08f), foamMaterial);
                    ripple.transform.SetParent(sceneryRoot);
                    Destroy(ripple.GetComponent<Collider>());
                    var bob = ripple.AddComponent<WaterBob>();
                    bob.amplitude = 0.045f;
                    bob.speed = 1.4f + z * 0.02f;
                    bob.phase = x * 0.7f;
                }
            }
            for (int i = 0; i < 18; i++)
            {
                float x = -5.4f + (i * 2.17f) % 10.8f;
                float z = 1.4f + (i * 3.73f) % 18f;
                var pad = Primitive("Lily Pad", PrimitiveType.Cylinder, new Vector3(x, -0.015f, z), new Vector3(0.42f, 0.025f, 0.42f), MaterialWithColor("Lily", new Color(0.18f, 0.63f, 0.29f)));
                pad.transform.SetParent(sceneryRoot);
                Destroy(pad.GetComponent<Collider>());
            }
        }

        private void CreateCoefficientMarkers()
        {
            var gold = MaterialWithColor("Coin Gold", new Color(1f, 0.67f, 0.04f));
            var brightGold = MaterialWithColor("Coin Highlight", new Color(1f, 0.9f, 0.2f));
            for (int row = 1; row <= FinishRow; row++)
            {
                var marker = new GameObject($"Score +{row * 10}");
                marker.transform.SetParent(sceneryRoot);
                marker.transform.position = new Vector3(0f, 1.55f, row);

                var coin = Primitive("Gold Coin", PrimitiveType.Cylinder, marker.transform.position, new Vector3(0.43f, 0.08f, 0.43f), gold);
                coin.transform.rotation = Quaternion.Euler(90f, 0, 0);
                coin.transform.SetParent(marker.transform, true);
                Destroy(coin.GetComponent<Collider>());
                var inset = Primitive("Coin Face", PrimitiveType.Cylinder, marker.transform.position + Vector3.back * 0.09f, new Vector3(0.3f, 0.018f, 0.3f), brightGold);
                inset.transform.rotation = Quaternion.Euler(90f, 0, 0);
                inset.transform.SetParent(marker.transform, true);
                Destroy(inset.GetComponent<Collider>());

                var coinSymbol = new GameObject("Coin Symbol", typeof(TextMesh));
                coinSymbol.transform.SetParent(marker.transform, false);
                coinSymbol.transform.localPosition = new Vector3(0, 0, -0.115f);
                var symbolText = coinSymbol.GetComponent<TextMesh>();
                symbolText.text = "+";
                symbolText.anchor = TextAnchor.MiddleCenter;
                symbolText.alignment = TextAlignment.Center;
                symbolText.fontSize = 54;
                symbolText.characterSize = 0.048f;
                symbolText.fontStyle = FontStyle.Bold;
                symbolText.color = new Color(0.62f, 0.3f, 0.015f);

                var labelObject = new GameObject("Coefficient", typeof(TextMesh));
                labelObject.transform.SetParent(marker.transform, false);
                labelObject.transform.localPosition = new Vector3(0, 0.61f, 0);
                var label = labelObject.GetComponent<TextMesh>();
                label.text = $"{row * 10}";
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 48;
                label.characterSize = 0.036f;
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(1f, 0.95f, 0.58f);
                var renderer = label.GetComponent<MeshRenderer>();
                renderer.sortingOrder = 10;
                var markerAnimation = marker.AddComponent<CoefficientMarker>();
                markerAnimation.phase = row * 0.41f;
                coefficientMarkers[row] = markerAnimation;
            }
        }

        private void CreateLane(int row)
        {
            float speed = Mathf.Min(2.15f, 1.08f + row * 0.045f) * (row % 2 == 0 ? 1 : -1);
            int count = row < 11 ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                float x = -5.2f + i * (10.4f / Mathf.Max(1, count - 1)) + (row % 3) * 0.45f;
                if (row % 5 == 0)
                    CreateCrocodile(new Vector3(x, 0.15f, row), speed, row);
                else if (row % 6 == 0)
                    CreateRaft(new Vector3(x, 0.18f, row), speed, row);
                else if (row % 4 == 0)
                    CreateBoat(new Vector3(x, 0.18f, row), speed, row);
                else
                    CreateLog(new Vector3(x, 0.12f, row), speed, row);
            }
        }

        private void CreateLog(Vector3 position, float speed, int row)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedLog");
            if (generatedPrefab != null)
            {
                var generated = Instantiate(generatedPrefab, position, Quaternion.identity);
                generated.name = "Stylized Log";
                ConfigurePlatform(generated, speed, row);
                return;
            }
            var log = Primitive("Log", PrimitiveType.Cylinder, position, new Vector3(0.5f, 1.3f, 0.5f), MaterialWithColor("Log", new Color(0.43f, 0.22f, 0.08f)));
            log.transform.rotation = Quaternion.Euler(0, 0, 90);
            var endMaterial = MaterialWithColor("Log Rings", new Color(0.73f, 0.46f, 0.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                var end = Primitive("Cut Rings", PrimitiveType.Cylinder, position + Vector3.right * side * 0.66f, new Vector3(0.42f, 0.035f, 0.42f), endMaterial);
                end.transform.rotation = Quaternion.Euler(0, 0, 90);
                end.transform.SetParent(log.transform, true);
                Destroy(end.GetComponent<Collider>());
            }
            var branch = Primitive("Branch", PrimitiveType.Cylinder, position + new Vector3(0.12f, 0.3f, 0), new Vector3(0.09f, 0.32f, 0.09f), MaterialWithColor("Dark Bark", new Color(0.3f, 0.13f, 0.045f)));
            branch.transform.rotation = Quaternion.Euler(0, 0, -28f);
            branch.transform.SetParent(log.transform, true);
            Destroy(branch.GetComponent<Collider>());
            for (int i = -1; i <= 1; i++)
            {
                var moss = Primitive("Moss", PrimitiveType.Sphere, position + new Vector3(i * 0.38f, 0.28f, -0.14f), new Vector3(0.28f, 0.08f, 0.2f), MaterialWithColor("Moss", new Color(0.2f, 0.48f, 0.12f)));
                moss.transform.SetParent(log.transform, true);
                Destroy(moss.GetComponent<Collider>());
            }
            ConfigurePlatform(log, speed, row);
        }

        private void CreateBoat(Vector3 position, float speed, int row)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedBoat");
            if (generatedPrefab != null)
            {
                var generated = Instantiate(generatedPrefab, position, Quaternion.identity);
                generated.name = "Stylized Boat";
                CreateWake(generated, speed);
                ConfigurePlatform(generated, speed, row);
                return;
            }
            var boat = Primitive("Boat", PrimitiveType.Cube, position, new Vector3(2.2f, 0.35f, 0.8f), MaterialWithColor("Boat", new Color(0.93f, 0.34f, 0.18f)));
            Primitive("Seat", PrimitiveType.Cube, position + Vector3.up * 0.3f, new Vector3(1.15f, 0.18f, 0.48f), MaterialWithColor("Seat", new Color(1f, 0.75f, 0.25f))).transform.SetParent(boat.transform, true);
            for (int side = -1; side <= 1; side += 2)
            {
                var rail = Primitive("Boat Rail", PrimitiveType.Cube, position + new Vector3(0, 0.34f, side * 0.37f), new Vector3(1.95f, 0.18f, 0.11f), MaterialWithColor("Boat Trim", new Color(1f, 0.82f, 0.28f)));
                rail.transform.SetParent(boat.transform, true);
                Destroy(rail.GetComponent<Collider>());
            }
            var bow = Primitive("Bow", PrimitiveType.Cube, position + new Vector3(Mathf.Sign(speed) * 1.1f, 0.05f, 0), new Vector3(0.55f, 0.42f, 0.72f), MaterialWithColor("Boat", new Color(0.93f, 0.34f, 0.18f)));
            bow.transform.rotation = Quaternion.Euler(0, 45f, 0);
            bow.transform.SetParent(boat.transform, true);
            Destroy(bow.GetComponent<Collider>());
            var paddle = Primitive("Paddle", PrimitiveType.Cylinder, position + new Vector3(0, 0.48f, 0), new Vector3(0.055f, 1.05f, 0.055f), MaterialWithColor("Paddle Wood", new Color(0.34f, 0.16f, 0.055f)));
            paddle.transform.rotation = Quaternion.Euler(64f, 0, 68f);
            paddle.transform.SetParent(boat.transform, true);
            Destroy(paddle.GetComponent<Collider>());
            CreateWake(boat, speed);
            ConfigurePlatform(boat, speed, row);
        }

        private void CreateCrocodile(Vector3 position, float speed, int row)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedCrocodile");
            if (generatedPrefab != null)
            {
                var generated = Instantiate(generatedPrefab, position, speed < 0 ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity);
                generated.name = "Stylized Crocodile";
                CreateWake(generated, speed);
                ConfigurePlatform(generated, speed, row);
                generated.AddComponent<DivingCrocodile>().phase = row * 0.37f;
                return;
            }
            var croc = Primitive("Crocodile", PrimitiveType.Cube, position, new Vector3(2.4f, 0.38f, 0.78f), MaterialWithColor("Croc", new Color(0.18f, 0.52f, 0.16f)));
            var head = Primitive("Head", PrimitiveType.Cube, position + new Vector3(Mathf.Sign(speed) * 1.2f, 0.05f, 0), new Vector3(0.8f, 0.48f, 0.9f), MaterialWithColor("Croc", new Color(0.18f, 0.52f, 0.16f)));
            head.transform.SetParent(croc.transform, true);
            var darkGreen = MaterialWithColor("Croc Scales", new Color(0.08f, 0.32f, 0.11f));
            for (int i = -2; i <= 2; i++)
            {
                var scale = Primitive("Back Scale", PrimitiveType.Sphere, position + new Vector3(i * 0.4f, 0.3f, 0), new Vector3(0.2f, 0.22f, 0.32f), darkGreen);
                scale.transform.SetParent(croc.transform, true);
                Destroy(scale.GetComponent<Collider>());
            }
            float direction = Mathf.Sign(speed);
            var eyeWhite = MaterialWithColor("Eye White", new Color(1f, 0.96f, 0.72f));
            var pupil = MaterialWithColor("Pupil", new Color(0.025f, 0.035f, 0.025f));
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = Primitive("Eye", PrimitiveType.Sphere, position + new Vector3(direction * 1.36f, 0.36f, side * 0.31f), Vector3.one * 0.2f, eyeWhite);
                eye.transform.SetParent(croc.transform, true);
                Destroy(eye.GetComponent<Collider>());
                var dot = Primitive("Pupil", PrimitiveType.Sphere, position + new Vector3(direction * 1.48f, 0.38f, side * 0.31f), Vector3.one * 0.09f, pupil);
                dot.transform.SetParent(croc.transform, true);
                Destroy(dot.GetComponent<Collider>());
            }
            var tail = Primitive("Tail", PrimitiveType.Cylinder, position - Vector3.right * direction * 1.45f, new Vector3(0.3f, 0.7f, 0.3f), darkGreen);
            tail.transform.rotation = Quaternion.Euler(0, 0, 90);
            tail.transform.SetParent(croc.transform, true);
            Destroy(tail.GetComponent<Collider>());
            var jaw = Primitive("Lower Jaw", PrimitiveType.Cube, position + new Vector3(direction * 1.55f, -0.07f, 0), new Vector3(0.85f, 0.16f, 0.78f), MaterialWithColor("Croc Jaw", new Color(0.12f, 0.39f, 0.12f)));
            jaw.transform.SetParent(croc.transform, true);
            Destroy(jaw.GetComponent<Collider>());
            var teeth = MaterialWithColor("Croc Teeth", new Color(1f, 0.96f, 0.76f));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    var tooth = Primitive("Tooth", PrimitiveType.Sphere, position + new Vector3(direction * (1.34f + i * 0.19f), 0.12f, side * 0.37f), new Vector3(0.055f, 0.1f, 0.055f), teeth);
                    tooth.transform.SetParent(croc.transform, true);
                    Destroy(tooth.GetComponent<Collider>());
                }
            }
            CreateWake(croc, speed);
            ConfigurePlatform(croc, speed, row);
            croc.AddComponent<DivingCrocodile>().phase = row * 0.37f;
        }

        private void CreateRaft(Vector3 position, float speed, int row)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedRaft");
            if (generatedPrefab == null)
            {
                CreateLog(position, speed, row);
                return;
            }
            var generated = Instantiate(generatedPrefab, position, Quaternion.identity);
            generated.name = "Stylized Raft";
            CreateWake(generated, speed);
            ConfigurePlatform(generated, speed, row);
        }

        private void ConfigurePlatform(GameObject platform, float speed, int row)
        {
            var mover = platform.AddComponent<RiverPlatform>();
            mover.speed = speed;
            mover.difficulty = row;
        }

        private void CreateWake(GameObject platform, float speed)
        {
            var foam = MaterialWithColor("Wake Foam", new Color(0.72f, 0.96f, 1f));
            float behind = -Mathf.Sign(speed);
            for (int side = -1; side <= 1; side += 2)
            {
                var wake = Primitive("Wake", PrimitiveType.Cube, platform.transform.position + new Vector3(behind * 1.25f, -0.18f, side * 0.31f), new Vector3(0.85f, 0.025f, 0.07f), foam);
                wake.transform.SetParent(platform.transform, true);
                wake.transform.rotation = Quaternion.Euler(0, side * behind * 12f, 0);
                Destroy(wake.GetComponent<Collider>());
                var pulse = wake.AddComponent<WaterBob>();
                pulse.amplitude = 0.025f;
                pulse.speed = 2.6f;
                pulse.phase = side;
            }
        }

        private void BuildChicken(Transform root)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedChicken");
            if (generatedPrefab != null)
            {
                var model = Instantiate(generatedPrefab, root);
                model.name = "Stylized Chicken Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 0.72f;
                foreach (var childCollider in model.GetComponentsInChildren<Collider>()) childCollider.enabled = false;
                var generatedCollider = root.gameObject.AddComponent<CapsuleCollider>();
                generatedCollider.center = new Vector3(0, 0.66f, 0);
                generatedCollider.height = 1.55f;
                generatedCollider.radius = 0.43f;
                root.gameObject.AddComponent<ChickenAnimation>();
                return;
            }
            var white = MaterialWithColor("Chicken White", new Color(1f, 0.96f, 0.82f));
            var red = MaterialWithColor("Comb Red", new Color(0.9f, 0.08f, 0.07f));
            var orange = MaterialWithColor("Beak", new Color(1f, 0.57f, 0.05f));
            var black = MaterialWithColor("Chicken Eyes", new Color(0.035f, 0.045f, 0.055f));
            var body = Primitive("Body", PrimitiveType.Sphere, root.position + new Vector3(0, 0.42f, 0), new Vector3(0.78f, 0.9f, 0.72f), white);
            body.transform.SetParent(root, true);
            var head = Primitive("Head", PrimitiveType.Sphere, root.position + new Vector3(0, 0.98f, 0.15f), new Vector3(0.58f, 0.58f, 0.58f), white);
            head.transform.SetParent(root, true);
            var beak = Primitive("Beak", PrimitiveType.Cube, root.position + new Vector3(0, 0.94f, 0.5f), new Vector3(0.25f, 0.18f, 0.28f), orange);
            beak.transform.SetParent(root, true);
            beak.transform.rotation = Quaternion.Euler(18f, 45f, 0);
            var wattle = Primitive("Wattle", PrimitiveType.Sphere, root.position + new Vector3(0, 0.75f, 0.4f), new Vector3(0.2f, 0.28f, 0.16f), red);
            wattle.transform.SetParent(root, true);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = Primitive("Eye", PrimitiveType.Sphere, root.position + new Vector3(side * 0.2f, 1.06f, 0.38f), Vector3.one * 0.115f, black);
                eye.transform.SetParent(root, true);
                var highlight = Primitive("Eye Highlight", PrimitiveType.Sphere, root.position + new Vector3(side * 0.225f, 1.095f, 0.465f), Vector3.one * 0.035f, MaterialWithColor("Eye Shine", Color.white));
                highlight.transform.SetParent(root, true);
                var wing = Primitive("Wing", PrimitiveType.Sphere, root.position + new Vector3(side * 0.43f, 0.55f, -0.02f), new Vector3(0.25f, 0.52f, 0.4f), MaterialWithColor("Wing Cream", new Color(0.94f, 0.88f, 0.68f)));
                wing.transform.rotation = Quaternion.Euler(0, 0, side * -18f);
                wing.transform.SetParent(root, true);
                var leg = Primitive("Leg", PrimitiveType.Cylinder, root.position + new Vector3(side * 0.2f, -0.03f, 0.05f), new Vector3(0.07f, 0.25f, 0.07f), orange);
                leg.transform.SetParent(root, true);
                for (int toe = -1; toe <= 1; toe++)
                {
                    var foot = Primitive("Toe", PrimitiveType.Cylinder, root.position + new Vector3(side * 0.2f + toe * 0.08f, -0.25f, 0.18f), new Vector3(0.035f, 0.16f, 0.035f), orange);
                    foot.transform.rotation = Quaternion.Euler(72f, 0, toe * 12f);
                    foot.transform.SetParent(root, true);
                }
            }
            for (int i = -1; i <= 1; i++)
            {
                var comb = Primitive("Comb", PrimitiveType.Sphere, root.position + new Vector3(i * 0.13f, 1.3f, 0.08f), Vector3.one * 0.2f, red);
                comb.transform.SetParent(root, true);
            }
            for (int i = -1; i <= 1; i++)
            {
                var tail = Primitive("Tail Feather", PrimitiveType.Capsule, root.position + new Vector3(i * 0.18f, 0.62f + Mathf.Abs(i) * 0.08f, -0.48f), new Vector3(0.16f, 0.38f, 0.16f), white);
                tail.transform.rotation = Quaternion.Euler(-42f, 0, i * 18f);
                tail.transform.SetParent(root, true);
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>()) Destroy(collider);
            var capsule = root.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.55f, 0);
            capsule.height = 1.4f;
            capsule.radius = 0.38f;
            root.gameObject.AddComponent<ChickenAnimation>();
        }

        private void CreateReedCluster(Vector3 position)
        {
            for (int i = -1; i <= 1; i++)
            {
                var reed = Primitive("Reed", PrimitiveType.Cylinder, position + new Vector3(i * 0.15f, i == 0 ? 0.25f : 0, i * 0.06f), new Vector3(0.055f, 0.52f + i * 0.05f, 0.055f), MaterialWithColor("Reed", new Color(0.09f, 0.47f, 0.16f)));
                reed.transform.SetParent(sceneryRoot);
                reed.transform.rotation = Quaternion.Euler(0, 0, i * 9f);
                Destroy(reed.GetComponent<Collider>());
                var tip = Primitive("Reed Tip", PrimitiveType.Capsule, reed.transform.position + Vector3.up * 0.58f, new Vector3(0.08f, 0.18f, 0.08f), MaterialWithColor("Reed Tip", new Color(0.38f, 0.2f, 0.07f)));
                tip.transform.SetParent(sceneryRoot);
                Destroy(tip.GetComponent<Collider>());
            }
        }

        private void CreateTree(Vector3 position, float scale)
        {
            var generatedPrefab = Resources.Load<GameObject>("Generated/StylizedVegetation");
            if (generatedPrefab != null)
            {
                var generated = Instantiate(generatedPrefab, position, Quaternion.identity);
                generated.name = "Stylized River Vegetation";
                generated.transform.localScale = Vector3.one * scale;
                generated.transform.SetParent(sceneryRoot);
                foreach (var collider in generated.GetComponentsInChildren<Collider>()) Destroy(collider);
                return;
            }

            var trunk = Primitive("Tree Trunk", PrimitiveType.Cylinder, position + Vector3.up * scale, new Vector3(0.3f * scale, scale, 0.3f * scale), MaterialWithColor("Tree Bark", new Color(0.34f, 0.17f, 0.06f)));
            trunk.transform.SetParent(sceneryRoot);
            Destroy(trunk.GetComponent<Collider>());
            var leaves = MaterialWithColor("Tree Leaves", new Color(0.12f, 0.58f, 0.2f));
            for (int i = 0; i < 3; i++)
            {
                var crown = Primitive("Tree Crown", PrimitiveType.Sphere, position + new Vector3((i - 1) * 0.42f * scale, (2f + (i % 2) * 0.25f) * scale, 0), Vector3.one * (0.92f * scale), leaves);
                crown.transform.SetParent(sceneryRoot);
                Destroy(crown.GetComponent<Collider>());
            }
        }

        private void CreateFlowers(float z, float depth)
        {
            var colors = new[] { new Color(1f, 0.35f, 0.55f), new Color(1f, 0.86f, 0.12f), new Color(0.65f, 0.42f, 1f) };
            for (int i = 0; i < 9; i++)
            {
                float x = -5.3f + (i * 1.37f) % 10.6f;
                float localZ = z - depth * 0.3f + (i % 3) * 0.32f;
                var flower = Primitive("Flower", PrimitiveType.Sphere, new Vector3(x, 0.48f, localZ), Vector3.one * 0.13f, MaterialWithColor("Flower " + (i % 3), colors[i % 3]));
                flower.transform.SetParent(sceneryRoot);
                Destroy(flower.GetComponent<Collider>());
            }
        }

        private void BuildInterface()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(transform);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            foreach (var oldModule in eventSystem.GetComponents<BaseInputModule>())
            {
                if (oldModule is InputSystemUIInputModule) continue;
                oldModule.enabled = false;
                Destroy(oldModule);
            }
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<SafeAreaFitter>();
            roundedUiSprite = CreateRoundedSprite();

            gameplayHud = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayHud.transform.SetParent(canvas.transform, false);
            Stretch(gameplayHud.GetComponent<RectTransform>());
            Transform hud = gameplayHud.transform;

            var topPanel = UIBlock(hud, "Top HUD Backdrop", new Color(0.025f, 0.095f, 0.14f, 0.9f));
            var topRect = topPanel.rectTransform;
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0, 158);

            coinsText = CreateText(hud, "Coins", $"BALANCE  {balance}", 42, TextAnchor.MiddleLeft, new Vector2(38, -28), new Vector2(430, 70), new Vector2(0, 1));
            multiplierText = CreateText(hud, "Score", "SCORE 0", 46, TextAnchor.MiddleCenter, new Vector2(0, -38), new Vector2(360, 100), new Vector2(0.5f, 1));
            payoutText = CreateText(hud, "Progress Reward", "FREE RUN  •  +5 COINS / STEP", 28, TextAnchor.MiddleLeft, new Vector2(38, -92), new Vector2(700, 48), new Vector2(0, 1));
            progressSlider = CreateProgress(hud);
            messageText = CreateText(hud, "Message", "TAP TO JUMP", 44, TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(860, 110), new Vector2(0.5f, 0));
            cashOutButton = CreateButton(hud, "END RUN", new Vector2(0, 70), new Vector2(520, 125));
            cashOutLabel = cashOutButton.GetComponentInChildren<Text>();
            cashOutButton.onClick.AddListener(CashOut);
            cashOutButton.gameObject.SetActive(false);

            var hint = CreateText(hud, "Control Hint", "TAP  •  SPACE  •  ↑", 30, TextAnchor.MiddleCenter, new Vector2(0, 22), new Vector2(620, 60), new Vector2(0.5f, 0));
            hint.color = new Color(1f, 1f, 1f, 0.72f);

            BuildMenu(canvas.transform);
            BuildResultOverlay(canvas.transform);
            BuildSettingsOverlay(canvas.transform);
            BuildTutorialOverlay(canvas.transform);
            BuildPrivacyOverlay(canvas.transform);
        }

        private void BuildMenu(Transform canvas)
        {
            menuOverlay = CreateOverlay(canvas, "Main Menu", new Color(0.005f, 0.045f, 0.07f, 0.9f));
            CreateBackdropGlow(menuOverlay.transform, "Aqua Glow", new Vector2(-390, 650), new Vector2(620, 620), new Color(0.02f, 0.72f, 0.82f, 0.11f));
            CreateBackdropGlow(menuOverlay.transform, "Gold Glow", new Vector2(430, -690), new Vector2(560, 560), new Color(1f, 0.62f, 0.04f, 0.1f));

            var card = CreateCard(menuOverlay.transform, Vector2.zero, new Vector2(940, 1700));
            card.color = new Color(0.025f, 0.13f, 0.18f, 0.96f);
            Transform content = card.transform;

            var menuChicken = CreateSpriteImage(content, "Menu Chicken", "RiverJump2D/Chicken", new Vector2(0, 660), new Vector2(215, 215));
            ChickenRoad.Skins.ChickenSkinService.Apply(menuChicken);
            var title = CreateText(content, "Title", "RIVER JUMP", 88, TextAnchor.MiddleCenter, new Vector2(0, 510), new Vector2(860, 125), new Vector2(0.5f, 0.5f));
            title.color = new Color(1f, 0.95f, 0.82f);
            var tagline = CreateText(content, "Tagline", "TIME YOUR JUMPS  •  REACH THE SHORE", 27, TextAnchor.MiddleCenter, new Vector2(0, 430), new Vector2(760, 52), new Vector2(0.5f, 0.5f));
            tagline.color = new Color(1f, 0.73f, 0.12f);

            var balancePanel = CreateUiPanel(content, "Balance Panel", new Vector2(0, 320), new Vector2(780, 110), new Color(0.015f, 0.08f, 0.12f, 0.92f), new Color(0.2f, 0.72f, 0.78f, 0.22f));
            var balanceLabel = CreateText(balancePanel.transform, "Balance Label", "BALANCE", 27, TextAnchor.MiddleLeft, new Vector2(-215, 0), new Vector2(250, 70), new Vector2(0.5f, 0.5f));
            balanceLabel.color = new Color(0.62f, 0.82f, 0.86f);
            menuBalanceText = CreateText(balancePanel.transform, "Menu Balance", balance.ToString("N0", CultureInfo.InvariantCulture), 52, TextAnchor.MiddleRight, new Vector2(215, 0), new Vector2(280, 82), new Vector2(0.5f, 0.5f));
            menuBalanceText.color = new Color(1f, 0.84f, 0.18f);

            var betPanel = CreateUiPanel(content, "Run Instructions", new Vector2(0, 55), new Vector2(820, 390), new Color(0.035f, 0.2f, 0.25f, 0.92f), new Color(0.24f, 0.88f, 0.84f, 0.3f));
            CreateText(betPanel.transform, "Instructions", "TAP TO HOP ACROSS THE RIVER\nLAND ON MOVING PLATFORMS\nEVERY RUN IS FREE", 37, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(760, 310), new Vector2(0.5f, 0.5f));
            var winPanel = CreateUiPanel(content, "Goal Panel", new Vector2(0, -205), new Vector2(780, 102), new Color(0.13f, 0.1f, 0.025f, 0.9f), new Color(1f, 0.72f, 0.08f, 0.42f));
            menuMaxWinText = CreateText(winPanel.transform, "Maximum Win", string.Empty, 34, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(730, 70), new Vector2(0.5f, 0.5f));
            menuMaxWinText.color = new Color(1f, 0.84f, 0.18f);

            menuPlayButton = CreateCenteredButton(content, string.Empty, new Vector2(0, -360), new Vector2(720, 136), StartRound);
            menuPlayButton.gameObject.name = "Play Button";
            StyleButton(menuPlayButton, new Color(1f, 0.62f, 0.035f), new Color(1f, 0.76f, 0.18f), new Color(0.88f, 0.45f, 0.02f));
            menuPlayButtonText = menuPlayButton.GetComponentInChildren<Text>();
            menuPlayButtonText.fontSize = 46;

            var table = CreateText(content, "Route Hint", "SAFE ISLANDS AT STEPS 7 AND 14\nREACH STEP 20 FOR BONUS COINS", 25, TextAnchor.MiddleCenter, new Vector2(0, -545), new Vector2(820, 110), new Vector2(0.5f, 0.5f));
            table.color = new Color(0.57f, 0.79f, 0.83f);
            var tutorialButton = CreateCenteredButton(content, "HOW TO PLAY", new Vector2(0, -690), new Vector2(430, 86), ShowTutorial);
            tutorialButton.gameObject.name = "How To Play Button";
            StyleButton(tutorialButton, new Color(1f, 0.62f, 0.035f), new Color(1f, 0.76f, 0.18f), new Color(0.88f, 0.45f, 0.02f));
            tutorialButton.GetComponentInChildren<Text>().fontSize = 29;
            UpdateBetMenuVisuals();
        }

        private void BuildResultOverlay(Transform canvas)
        {
            resultOverlay = CreateOverlay(canvas, "Result", new Color(0.004f, 0.035f, 0.055f, 0.93f));
            CreateBackdropGlow(resultOverlay.transform, "Result Glow", new Vector2(0, 420), new Vector2(820, 820), new Color(1f, 0.65f, 0.04f, 0.09f));
            resultCard = CreateCard(resultOverlay.transform, Vector2.zero, new Vector2(900, 1390));
            resultCard.color = new Color(0.025f, 0.13f, 0.18f, 0.98f);
            Transform content = resultCard.transform;

            resultAccentBar = UIBlock(content, "Result Accent", new Color(1f, 0.67f, 0.06f));
            resultAccentBar.sprite = roundedUiSprite;
            resultAccentBar.type = Image.Type.Sliced;
            var accentRect = resultAccentBar.rectTransform;
            accentRect.anchorMin = new Vector2(0.5f, 1f);
            accentRect.anchorMax = new Vector2(0.5f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = new Vector2(0, -14);
            accentRect.sizeDelta = new Vector2(820, 18);

            resultIcon = CreateSpriteImage(content, "Result Coin", "RiverJump2D/Coin", new Vector2(0, 530), new Vector2(145, 145));
            resultBadgeText = CreateText(content, "Result Badge", "ROUND COMPLETE", 24, TextAnchor.MiddleCenter, new Vector2(0, 414), new Vector2(560, 46), new Vector2(0.5f, 0.5f));
            resultBadgeText.color = new Color(0.65f, 0.85f, 0.88f);
            resultUserIcon = CreateSpriteImage(content, "Result User Icon", "default_user", new Vector2(-340, 319), new Vector2(88, 88));
            resultUserIcon.sprite = UserProfileStorage.UserIcon;
            resultTitleText = CreateText(content, "Result Title", "NICE WORK", 46, TextAnchor.MiddleLeft, new Vector2(90, 319), new Vector2(600, 105), new Vector2(0.5f, 0.5f));
            resultAmountText = CreateText(content, "Result Amount", "+0", 104, TextAnchor.MiddleCenter, new Vector2(0, 181.5f), new Vector2(800, 130), new Vector2(0.5f, 0.5f));
            resultAmountText.color = new Color(1f, 0.83f, 0.12f);
            resultAmountCaptionText = CreateText(content, "Result Amount Caption", "COINS EARNED", 25, TextAnchor.MiddleCenter, new Vector2(0, 74), new Vector2(600, 45), new Vector2(0.5f, 0.5f));
            resultAmountCaptionText.color = new Color(0.7f, 0.88f, 0.9f);

            var statsPanel = CreateUiPanel(content, "Round Statistics", new Vector2(0, -103.5f), new Vector2(760, 270), new Color(0.012f, 0.075f, 0.11f, 0.95f), new Color(0.2f, 0.7f, 0.76f, 0.24f));
            resultDetailsText = CreateText(statsPanel.transform, "Result Details", string.Empty, 31, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(710, 230), new Vector2(0.5f, 0.5f));
            resultDetailsText.color = new Color(0.88f, 0.96f, 0.97f);

            var balancePanel = CreateUiPanel(content, "Result Balance Panel", new Vector2(0, -302.5f), new Vector2(620, 88), new Color(0.08f, 0.27f, 0.29f, 0.88f), new Color(0.28f, 0.84f, 0.72f, 0.28f));
            resultBalanceText = CreateText(balancePanel.transform, "Result Balance", string.Empty, 31, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(580, 58), new Vector2(0.5f, 0.5f));

            var replayButton = CreateCenteredButton(content, string.Empty, new Vector2(0, -430.5f), new Vector2(680, 128), RetryRound);
            replayButton.gameObject.name = "Play Again Button";
            StyleButton(replayButton, new Color(1f, 0.62f, 0.035f), new Color(1f, 0.76f, 0.18f), new Color(0.88f, 0.45f, 0.02f));
            resultReplayButtonText = replayButton.GetComponentInChildren<Text>();
            resultReplayButtonText.fontSize = 42;

            var menuButton = CreateCenteredButton(content, "MAIN MENU", new Vector2(0, -561.5f), new Vector2(500, 94), ReturnToMenu);
            menuButton.gameObject.name = "Result Main Menu Button";
            StyleButton(menuButton, new Color(0.07f, 0.31f, 0.36f), new Color(0.09f, 0.42f, 0.46f), new Color(0.04f, 0.22f, 0.27f));
            menuButton.GetComponentInChildren<Text>().fontSize = 31;

            var footer = CreateText(content, "Result Footer", "KEEP PRACTICING YOUR TIMING", 22, TextAnchor.MiddleCenter, new Vector2(0, -649.5f), new Vector2(760, 42), new Vector2(0.5f, 0.5f));
            footer.color = new Color(0.48f, 0.69f, 0.73f);
            resultOverlay.SetActive(false);
        }

        private void BuildSettingsOverlay(Transform canvas)
        {
            settingsOverlay = CreateOverlay(canvas, "Settings", new Color(0.025f, 0.14f, 0.2f, 0.99f));
            CreateCard(settingsOverlay.transform, Vector2.zero, new Vector2(900, 1450));
            CreateText(settingsOverlay.transform, "Settings Title", "SETTINGS", 78, TextAnchor.MiddleCenter, new Vector2(0, 460), new Vector2(800, 130), new Vector2(0.5f, 0.5f));
            soundButtonText = CreateCenteredButton(settingsOverlay.transform, string.Empty, new Vector2(0, 245), new Vector2(600, 115), ToggleSound).GetComponentInChildren<Text>();
            vibrationButtonText = CreateCenteredButton(settingsOverlay.transform, string.Empty, new Vector2(0, 95), new Vector2(600, 115), ToggleVibration).GetComponentInChildren<Text>();
            qualityButtonText = CreateCenteredButton(settingsOverlay.transform, string.Empty, new Vector2(0, -55), new Vector2(600, 115), CycleQuality).GetComponentInChildren<Text>();
            CreateCenteredButton(settingsOverlay.transform, "PRIVACY", new Vector2(0, -205), new Vector2(600, 100), ShowPrivacy);
            CreateCenteredButton(settingsOverlay.transform, "RESET PROGRESS", new Vector2(0, -335), new Vector2(600, 100), ResetProgress);
            CreateCenteredButton(settingsOverlay.transform, "BACK", new Vector2(0, -480), new Vector2(460, 100), HideSettings);
            UpdateSettingsLabels();
            settingsOverlay.SetActive(false);
        }

        private void BuildTutorialOverlay(Transform canvas)
        {
            tutorialOverlay = CreateOverlay(canvas, "Tutorial", new Color(0.01f, 0.08f, 0.12f, 1f));
            CreateCard(tutorialOverlay.transform, Vector2.zero, new Vector2(940, 1320));
            CreateText(tutorialOverlay.transform, "Tutorial Title", "HOW TO PLAY", 78, TextAnchor.MiddleCenter, new Vector2(0, 480), new Vector2(850, 130), new Vector2(0.5f, 0.5f));
            CreateText(tutorialOverlay.transform, "Tutorial Body", "1. Tap to jump to the next row.\n\n2. Time each jump while platforms align.\n\n3. Reach step 20 for a finish bonus.\n\n4. Each completed step earns 5 coins.\n\nControls: tap, click, SPACE, W, or UP arrow.", 39, TextAnchor.MiddleCenter, new Vector2(0, 80), new Vector2(900, 650), new Vector2(0.5f, 0.5f));
            CreateCenteredButton(tutorialOverlay.transform, "GOT IT", new Vector2(0, -390), new Vector2(500, 120), CloseTutorial);
            tutorialOverlay.SetActive(false);
        }

        private void BuildPrivacyOverlay(Transform canvas)
        {
            privacyOverlay = CreateOverlay(canvas, "Privacy", new Color(0.02f, 0.11f, 0.16f, 1f));
            CreateCard(privacyOverlay.transform, Vector2.zero, new Vector2(920, 1200));
            CreateText(privacyOverlay.transform, "Privacy Title", "PRIVACY", 60, TextAnchor.MiddleCenter, new Vector2(0, 460), new Vector2(940, 120), new Vector2(0.5f, 0.5f));
            CreateText(privacyOverlay.transform, "Privacy Text", "This River Jump prototype does not collect or transmit personal data. Balance, settings, and tutorial status are stored locally on the device using PlayerPrefs.\n\nBefore publishing, replace this notice with the final privacy policy and add the developer's policy link.", 38, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(900, 600), new Vector2(0.5f, 0.5f));
            CreateCenteredButton(privacyOverlay.transform, "BACK", new Vector2(0, -390), new Vector2(480, 110), HidePrivacy);
            privacyOverlay.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Vector2 anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = obj.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI" }, size);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.material = text.defaultMaterial;
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.42f);
            shadow.effectDistance = new Vector2(2, -3);
            return text;
        }

        private Slider CreateProgress(Transform parent)
        {
            var root = new GameObject("Progress", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-42, -35);
            rect.sizeDelta = new Vector2(30, 720);
            var background = UIBlock(root.transform, "Background", new Color(0, 0, 0, 0.28f));
            Stretch(background.rectTransform);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 6);
            var fill = UIBlock(fillArea.transform, "Fill", new Color(1f, 0.82f, 0.12f));
            Stretch(fill.rectTransform);
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.direction = Slider.Direction.BottomToTop;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = FinishRow;
            return slider;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            obj.GetComponent<Image>().color = new Color(1f, 0.65f, 0.06f);
            obj.GetComponent<Image>().sprite = roundedUiSprite;
            obj.GetComponent<Image>().type = Image.Type.Sliced;
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.12f, 0.05f, 0, 0.55f);
            shadow.effectDistance = new Vector2(0, -10);
            var text = CreateText(obj.transform, "Label", label, 48, TextAnchor.MiddleCenter, Vector2.zero, size, new Vector2(0.5f, 0.5f));
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var button = obj.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.72f);
            colors.pressedColor = new Color(0.82f, 0.53f, 0.05f);
            colors.selectedColor = new Color(1f, 0.86f, 0.56f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private Button CreateCenteredButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = CreateButton(parent, label, Vector2.zero, size);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            button.onClick.AddListener(action);
            return button;
        }

        private void StyleButton(Button button, Color normal, Color highlighted, Color pressed)
        {
            var image = button.GetComponent<Image>();
            image.color = Color.white;
            image.sprite = roundedUiSprite;
            image.type = Image.Type.Sliced;
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.4f, 0.42f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private Image CreateUiPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, Color outlineColor)
        {
            var panel = UIBlock(parent, name, color);
            panel.sprite = roundedUiSprite;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;
            var rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            return panel;
        }

        private Image CreateSpriteImage(Transform parent, string name, string resourcePath, Vector2 position, Vector2 size)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null) return null;
            var image = UIBlock(parent, name, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private void CreateBackdropGlow(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var glow = UIBlock(parent, name, color);
            glow.sprite = roundedUiSprite;
            glow.type = Image.Type.Sliced;
            glow.raycastTarget = false;
            var rect = glow.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, 22f);
        }

        private GameObject CreateOverlay(Transform parent, string name, Color color)
        {
            var image = UIBlock(parent, name, color);
            Stretch(image.rectTransform);
            return image.gameObject;
        }

        private Image CreateCard(Transform parent, Vector2 position, Vector2 size)
        {
            var card = UIBlock(parent, "Glass Card", new Color(0.06f, 0.25f, 0.31f, 0.9f));
            card.sprite = roundedUiSprite;
            card.type = Image.Type.Sliced;
            var rect = card.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.78f, 0.85f, 0.35f);
            outline.effectDistance = new Vector2(3, -3);
            return card;
        }

        private Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const float radius = 15f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Rounded UI Texture" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f) + Mathf.Max(x - (size - 1 - radius), 0f);
                    float dy = Mathf.Max(radius - y, 0f) + Mathf.Max(y - (size - 1 - radius), 0f);
                    float alpha = 1f - Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - radius + 1f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }

        private Image UIBlock(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private void ShowMenu()
        {
            gameplayHud.SetActive(false);
            resultOverlay.SetActive(false);
            settingsOverlay.SetActive(false);
            menuOverlay.SetActive(true);
            chicken.IsRoundOver = true;
            UpdateBetMenuVisuals();
        }

        private void UpdateBetMenuVisuals()
        {
            if (menuBalanceText != null)
                menuBalanceText.text = balance.ToString("N0", CultureInfo.InvariantCulture);
            if (menuMaxWinText != null)
                menuMaxWinText.text = "REACH STEP 20  •  +100 BONUS COINS";
            if (menuPlayButtonText != null)
                menuPlayButtonText.text = "START FREE RUN";
            if (menuPlayButton != null) menuPlayButton.interactable = true;
        }

        private void StartRound()
        {
            potentialPayout = 0;
            bestRow = 0;
            comboCount = 0;
            bestCombo = 0;
            roundBonus = 0;
            menuOverlay.SetActive(false);
            settingsOverlay.SetActive(false);
            resultOverlay.SetActive(false);
            gameplayHud.SetActive(true);
            SetWorldMotionPaused(false);
            chicken.IsRoundOver = false;
            coinsText.text = $"BALANCE  {balance}";
            multiplierText.text = "SCORE 0";
            payoutText.text = "+5 COINS PER STEP";
            messageText.text = "TAP TO JUMP";
            progressSlider.value = 0;
            cashOutButton.gameObject.SetActive(false);
            PlaySweep(470f, 700f, 0.14f, 0.18f);
        }

        private void RetryRound()
        {
            retryAfterReload = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToMenu()
        {
            retryAfterReload = false;
            SceneManager.LoadScene("MainMenu");
        }

        private void ShowSettings()
        {
            menuOverlay.SetActive(false);
            settingsOverlay.SetActive(true);
            UpdateSettingsLabels();
        }

        private void HideSettings()
        {
            settingsOverlay.SetActive(false);
            menuOverlay.SetActive(true);
        }

        private void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            PlayerPrefs.SetInt("RiverJumpSound", soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            UpdateSettingsLabels();
            PlayTone(580f, 0.08f, 0.18f);
        }

        private void ToggleVibration()
        {
            vibrationEnabled = !vibrationEnabled;
            PlayerPrefs.SetInt("RiverJumpVibration", vibrationEnabled ? 1 : 0);
            PlayerPrefs.Save();
            UpdateSettingsLabels();
        }

        private void CycleQuality()
        {
            qualityLevel = (qualityLevel + 1) % 3;
            PlayerPrefs.SetInt("RiverJumpQuality", qualityLevel);
            PlayerPrefs.Save();
            ApplyQuality();
            UpdateSettingsLabels();
        }

        private void ResetProgress()
        {
            balance = 1000;
            SaveBalance();
            UpdateBetMenuVisuals();
            UpdateSettingsLabels();
        }

        private void ShowPrivacy()
        {
            settingsOverlay.SetActive(false);
            privacyOverlay.SetActive(true);
        }

        private void HidePrivacy()
        {
            privacyOverlay.SetActive(false);
            settingsOverlay.SetActive(true);
        }

        private void CloseTutorial()
        {
            PlayerPrefs.SetInt("RiverJumpTutorialSeen", 1);
            PlayerPrefs.Save();
            tutorialOverlay.SetActive(false);
        }

        private void ShowTutorial()
        {
            tutorialOverlay.SetActive(true);
        }

        private void UpdateSettingsLabels()
        {
            if (soundButtonText != null) soundButtonText.text = $"SOUND: {(soundEnabled ? "ON" : "OFF")}";
            if (vibrationButtonText != null) vibrationButtonText.text = $"VIBRATION: {(vibrationEnabled ? "ON" : "OFF")}";
            if (qualityButtonText != null) qualityButtonText.text = $"QUALITY: {(qualityLevel == 0 ? "LOW" : qualityLevel == 1 ? "MEDIUM" : "HIGH")}";
        }

        private void ApplyQuality()
        {
            QualitySettings.antiAliasing = qualityLevel == 0 ? 0 : qualityLevel == 1 ? 2 : 4;
            QualitySettings.shadowDistance = qualityLevel == 0 ? 18f : qualityLevel == 1 ? 28f : 38f;
        }

        private void SaveBalance()
        {
            PlayerPrefs.SetInt("RiverJumpBalance", balance);
            PlayerPrefs.Save();
        }

        public void ReachedRow(int row, bool perfect = false, bool timedLanding = false)
        {
            if (row <= bestRow) return;
            bestRow = row;
            int perfectBonus = 0;
            if (timedLanding)
            {
                if (perfect)
                {
                    comboCount++;
                    bestCombo = Mathf.Max(bestCombo, comboCount);
                    perfectBonus = 5 * comboCount;
                    roundBonus += perfectBonus;
                    ShowPerfectFeedback(comboCount, perfectBonus);
                }
                else
                {
                    comboCount = 0;
                }
            }
            if (coefficientMarkers.TryGetValue(row, out var claimedMarker))
            {
                Vector3 rewardPosition = claimedMarker.transform.position;
                claimedMarker.Claim();
                StartCoroutine(FlyRewardCoinToWin(rewardPosition));
            }
            potentialPayout = row * 10 + roundBonus;
            coinsText.text = $"BALANCE  {balance}";
            multiplierText.text = $"SCORE {potentialPayout}";
            payoutText.text = $"+{row * 5} COINS EARNED";
            progressSlider.value = row;
            cashOutButton.gameObject.SetActive(row > 0);
            cashOutLabel.text = "END RUN";
            bool safe = row == 7 || row == 14 || row >= FinishRow;
            messageText.text = perfect
                ? $"PERFECT!  COMBO x{comboCount}  +{perfectBonus} PTS"
                : safe
                    ? "SAFE ISLAND!"
                    : $"NEXT STEP  {Mathf.Min(row + 1, FinishRow)}";
            if (!perfect)
                PlaySweep(680f + row * 14f, 770f + row * 14f, 0.085f, 0.12f);
            if (row >= FinishRow) CashOut();
        }

        public void Lose()
        {
            if (chicken == null || chicken.IsRoundOver) return;
            chicken.IsRoundOver = true;
            SetWorldMotionPaused(true);
            StartCoroutine(PlayFailureSting());
            Vibrate();
            StartCoroutine(EndRound("GAME OVER", true));
        }

        public void SpawnSplash(Vector3 position)
        {
            var objectRoot = new GameObject("Water Splash");
            objectRoot.transform.position = position + Vector3.up * 0.08f;
            var particles = objectRoot.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.55f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new Color(0.62f, 0.94f, 1f, 0.9f);
            main.gravityModifier = 0.85f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 16) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.2f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialWithColor("Splash Particles", new Color(0.68f, 0.95f, 1f));
            particles.Play();
            Destroy(objectRoot, 1.5f);
        }

        public void SpawnLandingEffect(Vector3 position)
        {
            SpawnParticleBurst(position + Vector3.up * 0.05f, new Color(0.78f, 0.92f, 0.65f), 8, 0.8f);
        }

        public void SpawnJumpEffect(Vector3 position)
        {
            SpawnParticleBurst(position, new Color(1f, 0.96f, 0.78f), 7, 0.65f);
            SpawnParticleBurst(position + Vector3.up * 0.08f, new Color(1f, 0.72f, 0.12f), 4, 0.42f);
        }

        public void SpawnDefeatEffect(Vector3 position)
        {
            SpawnParticleBurst(position, new Color(1f, 0.24f, 0.16f), 7, 1.05f);
            SpawnParticleBurst(position + Vector3.up * 0.08f, new Color(1f, 0.86f, 0.18f), 5, 0.7f);
        }

        private void ShowPerfectFeedback(int combo, int bonus)
        {
            var canvas = gameplayHud != null ? gameplayHud.GetComponentInParent<Canvas>() : null;
            if (canvas == null || chicken == null) return;
            var feedback = CreateText(
                canvas.transform,
                "Perfect Jump Feedback",
                $"PERFECT!\nCOMBO x{combo}   +{bonus} PTS",
                54,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -125f),
                new Vector2(760f, 170f),
                new Vector2(0.5f, 0.5f));
            feedback.color = new Color(1f, 0.88f, 0.16f);
            feedback.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            feedback.transform.SetAsLastSibling();
            SpawnParticleBurst(chicken.transform.position + Vector3.up * 0.75f, new Color(1f, 0.86f, 0.12f), 18, 1.45f);
            StartCoroutine(PlayPerfectChord(combo));
            StartCoroutine(AnimatePerfectFeedback(feedback));
        }

        private IEnumerator PlayPerfectChord(int combo)
        {
            float root = Mathf.Min(920f, 650f + combo * 24f);
            PlayTone(root, 0.09f, 0.14f);
            yield return new WaitForSeconds(0.045f);
            PlayTone(root * 1.25f, 0.1f, 0.15f);
            yield return new WaitForSeconds(0.045f);
            PlaySweep(root * 1.5f, root * 1.68f, 0.13f, 0.18f);
        }

        private IEnumerator AnimatePerfectFeedback(Text feedback)
        {
            RectTransform rect = feedback.rectTransform;
            const float duration = 0.72f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float entrance = Mathf.Clamp01(t / 0.18f);
                float scale = Mathf.Lerp(0.45f, 1.15f, Mathf.Sin(entrance * Mathf.PI * 0.5f));
                if (t > 0.35f) scale = Mathf.Lerp(1.15f, 0.92f, (t - 0.35f) / 0.65f);
                rect.localScale = Vector3.one * scale;
                rect.anchoredPosition = new Vector2(0f, -125f + t * 80f);
                Color color = feedback.color;
                color.a = t < 0.62f ? 1f : Mathf.InverseLerp(1f, 0.62f, t);
                feedback.color = color;
                yield return null;
            }
            Destroy(feedback.gameObject);
        }

        private IEnumerator FlyRewardCoinToWin(Vector3 worldPosition)
        {
            yield return new WaitForSeconds(0.12f);
            SpawnParticleBurst(worldPosition, new Color(1f, 0.72f, 0.06f), 26, 2.5f);

            var canvas = gameplayHud != null ? gameplayHud.GetComponentInParent<Canvas>() : null;
            var camera = Camera.main;
            Sprite coinSprite = Resources.Load<Sprite>("RiverJump2D/Coin");
            if (canvas == null || camera == null || coinSprite == null || payoutText == null) yield break;

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) yield break;
            Vector2 screenPosition = camera.WorldToScreenPoint(worldPosition);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 startPosition))
                yield break;

            Vector3 winWorldPosition = payoutText.rectTransform.TransformPoint(new Vector3(390f, -24f, 0f));
            Vector2 targetPosition = canvasRect.InverseTransformPoint(winWorldPosition);

            var flyingRoot = new GameObject("Flying Reward Coin", typeof(RectTransform));
            flyingRoot.transform.SetParent(canvas.transform, false);
            flyingRoot.transform.SetAsLastSibling();
            var flyingRect = flyingRoot.GetComponent<RectTransform>();
            flyingRect.anchorMin = flyingRect.anchorMax = new Vector2(0.5f, 0.5f);
            flyingRect.pivot = new Vector2(0.5f, 0.5f);
            flyingRect.anchoredPosition = startPosition;
            flyingRect.sizeDelta = new Vector2(150f, 150f);

            var glowObject = new GameObject("Reward Glow", typeof(RectTransform), typeof(Image));
            glowObject.transform.SetParent(flyingRoot.transform, false);
            var glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(154f, 154f);
            var glowImage = glowObject.GetComponent<Image>();
            glowImage.sprite = coinSprite;
            glowImage.preserveAspect = true;
            glowImage.color = new Color(1f, 0.72f, 0.08f, 0.28f);
            glowImage.raycastTarget = false;

            var coinObject = new GameObject("Reward Coin", typeof(RectTransform), typeof(Image));
            coinObject.transform.SetParent(flyingRoot.transform, false);
            var coinRect = coinObject.GetComponent<RectTransform>();
            coinRect.anchorMin = coinRect.anchorMax = new Vector2(0.5f, 0.5f);
            coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.sizeDelta = new Vector2(116f, 116f);
            var coinImage = coinObject.GetComponent<Image>();
            coinImage.sprite = coinSprite;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;

            Vector2 controlPosition = Vector2.Lerp(startPosition, targetPosition, 0.5f) + Vector2.up * 240f;
            const float duration = 0.58f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector2 firstLeg = Vector2.Lerp(startPosition, controlPosition, eased);
                Vector2 secondLeg = Vector2.Lerp(controlPosition, targetPosition, eased);
                flyingRect.anchoredPosition = Vector2.Lerp(firstLeg, secondLeg, eased);
                flyingRect.localRotation = Quaternion.Euler(0f, 0f, -eased * 720f);
                float pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.24f;
                flyingRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.48f, eased) * pop;
                Color glowColor = glowImage.color;
                glowColor.a = Mathf.Lerp(0.32f, 0.04f, eased);
                glowImage.color = glowColor;
                yield return null;
            }

            Destroy(flyingRoot);
            PlaySweep(860f, 1180f, 0.11f, 0.18f);
            if (payoutImpactRoutine != null) StopCoroutine(payoutImpactRoutine);
            payoutImpactRoutine = StartCoroutine(PulsePayoutText());
        }

        private IEnumerator PulsePayoutText()
        {
            RectTransform rect = payoutText.rectTransform;
            rect.localScale = Vector3.one;
            const float duration = 0.2f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                rect.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.2f);
                yield return null;
            }
            rect.localScale = Vector3.one;
            payoutImpactRoutine = null;
        }

        private void SpawnParticleBurst(Vector3 position, Color color, int count, float speed)
        {
            var objectRoot = new GameObject("Stylized Particle Burst");
            objectRoot.transform.position = position;
            var particles = objectRoot.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = color;
            main.gravityModifier = 0.7f;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.16f;
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            var particleMaterial = new Material(particleShader) { color = color };
            particleRenderer.material = particleMaterial;
            particleRenderer.sortingOrder = 30;
            particles.Play();
            Destroy(particleMaterial, 1.2f);
            Destroy(objectRoot, 1.2f);
        }

        private void CashOut()
        {
            if (bestRow == 0) return;
            chicken.IsRoundOver = true;
            SetWorldMotionPaused(true);
            StartCoroutine(PlayWinSting());
            Vibrate();
            StartCoroutine(EndRound("RUN COMPLETE", false));
        }

        private IEnumerator EndRound(string message, bool sink)
        {
            int earnedCoins = bestRow * 5 + (bestRow >= FinishRow ? 100 : 0);
            balance += earnedCoins;
            SaveBalance();
            messageText.text = message;
            cashOutButton.gameObject.SetActive(false);
            if (sink)
            {
                yield return chicken.PlayDefeatAnimation();
            }
            yield return new WaitForSeconds(0.75f);
            gameplayHud.SetActive(false);
            resultOverlay.SetActive(true);
            Color accent = sink ? new Color(1f, 0.31f, 0.2f) : new Color(1f, 0.68f, 0.06f);
            if (resultAccentBar != null) resultAccentBar.color = accent;
            if (resultCard != null)
            {
                var outline = resultCard.GetComponent<Outline>();
                if (outline != null) outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.48f);
            }
            if (resultIcon != null)
                resultIcon.color = sink ? new Color(0.72f, 0.78f, 0.8f, 0.76f) : Color.white;

            string comboValue = bestCombo > 0 ? $"x{bestCombo}" : "—";
            resultBadgeText.text = sink ? "ROUND OVER" : "ROUND COMPLETE";
            resultBadgeText.color = sink ? new Color(1f, 0.6f, 0.52f) : new Color(0.65f, 0.88f, 0.82f);
            resultUserIcon.gameObject.SetActive(!sink);
            if (!sink) resultUserIcon.sprite = UserProfileStorage.UserIcon;
            resultTitleText.text = sink ? "RUN ENDED" : $"NICE WORK, {UserProfileStorage.UserName}";
            resultTitleText.alignment = sink ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            resultTitleText.rectTransform.anchoredPosition = new Vector2(sink ? 0 : 90, 319);
            resultTitleText.rectTransform.sizeDelta = new Vector2(sink ? 820 : 600, 105);
            resultTitleText.color = sink ? new Color(1f, 0.48f, 0.36f) : new Color(1f, 0.95f, 0.82f);
            resultAmountText.text = $"+{earnedCoins.ToString("N0", CultureInfo.InvariantCulture)}";
            resultAmountText.color = new Color(1f, 0.83f, 0.12f);
            resultAmountCaptionText.text = "COINS EARNED";
            resultDetailsText.text =
                $"STEPS COMPLETED        {bestRow} / {FinishRow}\n" +
                $"SCORE                        {potentialPayout}\n" +
                $"PERFECT POINTS             +{roundBonus}\n" +
                $"BEST COMBO                  {comboValue}";
            resultBalanceText.text = $"BALANCE   {balance.ToString("N0", CultureInfo.InvariantCulture)}";
            resultBalanceText.color = sink ? new Color(0.82f, 0.91f, 0.92f) : new Color(1f, 0.88f, 0.28f);
            resultReplayButtonText.text = "PLAY AGAIN";
        }

        private void SetWorldMotionPaused(bool paused)
        {
            if (worldMotionPaused == paused) return;
            worldMotionPaused = paused;

            foreach (var mover in FindObjectsByType<RiverPlatform2D>(FindObjectsSortMode.None)) mover.enabled = !paused;
            foreach (var diver in FindObjectsByType<DivingCrocodile2D>(FindObjectsSortMode.None)) diver.enabled = !paused;
            foreach (var marker in FindObjectsByType<CoefficientMarker>(FindObjectsSortMode.None)) marker.enabled = !paused;
            foreach (var cameraFollow in FindObjectsByType<PortraitCamera2D>(FindObjectsSortMode.None)) cameraFollow.enabled = !paused;
            foreach (var wake in FindObjectsByType<PlatformWake2D>(FindObjectsSortMode.None)) wake.enabled = !paused;
            foreach (var water in FindObjectsByType<WaterSurfaceAnimator2D>(FindObjectsSortMode.None)) water.enabled = !paused;

            foreach (var mover in FindObjectsByType<RiverPlatform>(FindObjectsSortMode.None)) mover.enabled = !paused;
            foreach (var diver in FindObjectsByType<DivingCrocodile>(FindObjectsSortMode.None)) diver.enabled = !paused;
            foreach (var water in FindObjectsByType<WaterBob>(FindObjectsSortMode.None)) water.enabled = !paused;
            foreach (var cameraFollow in FindObjectsByType<PortraitCamera>(FindObjectsSortMode.None)) cameraFollow.enabled = !paused;
        }

        private void SetupAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void PlayTone(float frequency, float duration, float volume)
        {
            if (!soundEnabled || audioSource == null) return;
            int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
            }
            var clip = AudioClip.Create("River Jump Tone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip);
            Destroy(clip, duration + 0.2f);
        }

        private void PlaySweep(float startFrequency, float endFrequency, float duration, float volume)
        {
            if (!soundEnabled || audioSource == null) return;
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            var samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)Mathf.Max(1, sampleCount - 1);
                float eased = t * t * (3f - 2f * t);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, eased);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                float attack = Mathf.Clamp01(t / 0.08f);
                float release = Mathf.Pow(1f - t, 1.55f);
                float body = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.16f;
                samples[i] = body * attack * release * volume;
            }

            var clip = AudioClip.Create("River Jump Sweep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip);
            Destroy(clip, duration + 0.2f);
        }

        public void PlayJumpSound(bool dangerous)
        {
            if (dangerous)
                PlaySweep(330f, 145f, 0.16f, 0.18f);
            else
                PlaySweep(390f, 570f, 0.1f, 0.14f);
        }

        public void PlayLandingSound(bool nearMiss)
        {
            if (nearMiss)
                PlaySweep(770f, 300f, 0.15f, 0.17f);
            else
                PlaySweep(540f, 700f, 0.08f, 0.12f);
            Vibrate();
        }

        public void PlayCrocodileWarningSound() => PlaySweep(315f, 180f, 0.17f, 0.12f);

        public void PlaySafeWindowSound() => PlaySweep(690f, 810f, 0.055f, 0.06f);

        private IEnumerator PlayFailureSting()
        {
            PlaySweep(360f, 150f, 0.2f, 0.22f);
            yield return new WaitForSeconds(0.11f);
            PlaySweep(190f, 92f, 0.27f, 0.18f);
        }

        private IEnumerator PlayWinSting()
        {
            const float root = 620f;
            PlayTone(root, 0.12f, 0.16f);
            yield return new WaitForSeconds(0.06f);
            PlayTone(root * 1.25f, 0.13f, 0.17f);
            yield return new WaitForSeconds(0.06f);
            PlaySweep(root * 1.5f, root * 1.82f, 0.22f, 0.22f);
        }

        private void Vibrate()
        {
            if (vibrationEnabled && Application.isMobilePlatform) Handheld.Vibrate();
        }

        private Material MaterialWithColor(string name, Color color)
        {
            string key = name + ColorUtility.ToHtmlStringRGBA(color);
            if (materialCache.TryGetValue(key, out var cached)) return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            material.SetFloat("_Smoothness", name.Contains("Water") ? 0.72f : 0.18f);
            material.SetFloat("_Metallic", 0f);
            materials.Add(material);
            materialCache[key] = material;
            return material;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            obj.GetComponent<Renderer>().receiveShadows = true;
            return obj;
        }
    }
}
