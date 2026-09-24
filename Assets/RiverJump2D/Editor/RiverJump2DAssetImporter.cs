#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiverJump.Editor
{
    [InitializeOnLoad]
    public static class RiverJump2DAssetImporter
    {
        private static readonly string[] ArtFolders =
        {
            "Assets/RiverJump2D/Resources/RiverJump2D",
            "Assets/RoadCrossing/Resources/RoadCrossing"
        };

        static RiverJump2DAssetImporter()
        {
            EditorApplication.delayCall += ConfigureOnce;
        }

        private static void ConfigureOnce()
        {
            if (SessionState.GetBool("RiverJump.2DImport.v2", false)) return;
            SessionState.SetBool("RiverJump.2DImport.v2", true);
            ConfigureSprites();
        }

        [MenuItem("River Jump/Configure 2D Sprites")]
        public static void ConfigureSprites()
        {
            foreach (string folder in ArtFolders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
                {
                    string path = file.Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    string name = Path.GetFileNameWithoutExtension(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 256f;
                    var textureSettings = new TextureImporterSettings();
                    importer.ReadTextureSettings(textureSettings);
                    textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(textureSettings);
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = name is "Water" or "Grass" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("River Jump: 2D sprites configured successfully.");
        }

        [MenuItem("River Jump/Render 2D Prototype Preview")]
        public static void RenderPrototypePreview()
        {
            ConfigureSprites();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] required = { "Chicken", "Crocodile", "Log", "Boat", "Raft", "Island", "Coin", "Water", "Grass", "Reeds", "Lilies", "Bush" };
            foreach (string name in required)
            {
                if (Resources.Load<Sprite>($"RiverJump2D/{name}") == null)
                    throw new FileNotFoundException($"River Jump 2D sprite failed to load: {name}");
            }

            var root = new GameObject("2D Preview Bootstrap");
            var bootstrap = root.AddComponent<RiverJumpBootstrap>();
            var buildWorld = typeof(RiverJumpBootstrap).GetMethod("BuildWorld", BindingFlags.Instance | BindingFlags.NonPublic);
            if (buildWorld == null) throw new System.MissingMethodException("RiverJumpBootstrap.BuildWorld");
            buildWorld.Invoke(bootstrap, null);

            int platformCount = 0;
            foreach (var spriteRenderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (spriteRenderer.GetComponent<RiverPlatform2D>() == null) continue;
                platformCount++;
                Debug.Log($"2D platform preview: {spriteRenderer.name}, sprite={spriteRenderer.sprite?.name}, position={spriteRenderer.transform.position}, scale={spriteRenderer.transform.localScale}, bounds={spriteRenderer.bounds.size}, sorting={spriteRenderer.sortingOrder}");
            }
            if (platformCount < 20) throw new MissingReferenceException($"Only {platformCount} 2D platforms were created.");
            Debug.Log($"River Jump: validated {platformCount} moving 2D platforms.");

            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null) throw new MissingReferenceException("2D preview camera was not created.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            RenderCameraPreview(camera, Path.Combine(projectRoot, "RiverJump2DPreview.png"));

            var chicken = Object.FindFirstObjectByType<ChickenController>();
            RiverPlatform2D landingPlatform = null;
            float bestDistance = float.MaxValue;
            foreach (var platform in Object.FindObjectsByType<RiverPlatform2D>(FindObjectsSortMode.None))
            {
                float rowDistance = Mathf.Abs(platform.transform.position.y - ChickenController.RowSpacing);
                if (rowDistance > 0.05f) continue;
                float distance = Mathf.Abs(platform.transform.position.x);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                landingPlatform = platform;
            }

            if (chicken == null || landingPlatform == null)
                throw new MissingReferenceException("Could not prepare the chicken-on-platform alignment preview.");

            chicken.transform.position = new Vector3(landingPlatform.transform.position.x, landingPlatform.transform.position.y, -1f);
            camera.transform.position = new Vector3(0f, chicken.transform.position.y + 3.45f, -10f);
            RenderCameraPreview(camera, Path.Combine(projectRoot, "RiverJump2DLandingPreview.png"));
            Debug.Log($"River Jump: chicken landing alignment validated on {landingPlatform.name} at {landingPlatform.transform.position}.");

            camera.transform.position = new Vector3(0f, 7f * ChickenController.RowSpacing, -10f);
            RenderCameraPreview(camera, Path.Combine(projectRoot, "RiverJump2DIslandPreview.png"));
        }

        [MenuItem("River Jump/Render Interface Previews")]
        public static void RenderInterfacePreviews()
        {
            ConfigureSprites();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Interface Preview Bootstrap");
            var bootstrap = root.AddComponent<RiverJumpBootstrap>();
            var bootstrapType = typeof(RiverJumpBootstrap);
            bootstrapType.GetField("balance", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(bootstrap, 800);
            bootstrapType.GetMethod("BuildWorld", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(bootstrap, null);
            bootstrapType.GetMethod("BuildInterface", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(bootstrap, null);

            var camera = Object.FindFirstObjectByType<Camera>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (camera == null || canvas == null)
                throw new MissingReferenceException("Interface preview camera or canvas was not created.");

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            Canvas.ForceUpdateCanvases();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            RenderCameraPreview(camera, Path.Combine(projectRoot, "RiverJumpMenuPreview.png"));

            var menu = GetPrivateField<GameObject>(bootstrap, "menuOverlay");
            var gameplay = GetPrivateField<GameObject>(bootstrap, "gameplayHud");
            var result = GetPrivateField<GameObject>(bootstrap, "resultOverlay");
            menu.SetActive(false);
            gameplay.SetActive(false);
            result.SetActive(true);
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultBadgeText").text = "ROUND COMPLETE";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultTitleText").text = "RUN COMPLETE";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultAmountText").text = "+35";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultAmountCaptionText").text = "COINS EARNED";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultDetailsText").text = "STEPS COMPLETED        7 / 20\nSCORE                        90\nPERFECT POINTS             +20\nBEST COMBO                  x3";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultBalanceText").text = "BALANCE   1 035";
            GetPrivateField<UnityEngine.UI.Text>(bootstrap, "resultReplayButtonText").text = "PLAY AGAIN";
            Canvas.ForceUpdateCanvases();
            RenderCameraPreview(camera, Path.Combine(projectRoot, "RiverJumpResultPreview.png"));
        }

        private static T GetPrivateField<T>(RiverJumpBootstrap bootstrap, string name) where T : class
        {
            var value = typeof(RiverJumpBootstrap).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as T;
            if (value == null) throw new MissingReferenceException($"Interface preview field was not created: {name}");
            return value;
        }

        private static void RenderCameraPreview(Camera camera, string output)
        {
            const int width = 540;
            const int height = 960;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var preview = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.targetTexture = renderTexture;
            for (int frame = 0; frame < 3; frame++) camera.Render();
            RenderTexture.active = renderTexture;
            preview.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            preview.Apply();

            File.WriteAllBytes(output, preview.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(preview);
            Object.DestroyImmediate(renderTexture);
            Debug.Log($"River Jump: 2D preview rendered to {output}");
        }
    }
}
#endif
