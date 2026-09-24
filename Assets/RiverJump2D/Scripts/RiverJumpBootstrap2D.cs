using UnityEngine;

namespace RiverJump
{
    public sealed partial class RiverJumpBootstrap
    {
        private const float WorldWidth2D = 7.2f;
        private Sprite safeIslandStrip2D;
        private Sprite landingIndicatorSprite2D;
        private Material waterMaterial2D;

        private void BuildWorld()
        {
            foreach (var existingCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(existingCamera.gameObject);
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(light.gameObject);

            RenderSettings.fog = false;
            sceneryRoot = new GameObject("River Jump 2D World").transform;

            CreateWater2D();
            CreateBank2D("Start Bank", 0, 3f, false);
            CreateBank2D("Safe Island 1", 7, 1.55f, true);
            CreateBank2D("Safe Island 2", 14, 1.55f, true);
            CreateBank2D("Finish Bank", FinishRow, 4.2f, false);
            CreateWaterDecorations2D();

            for (int row = 1; row < FinishRow; row++)
            {
                if (row == 7 || row == 14) continue;
                CreateLane2D(row);
            }
            CreateCoefficientMarkers2D();

            var chickenObject = new GameObject("Chicken");
            chickenObject.transform.SetParent(sceneryRoot);
            chickenObject.transform.position = new Vector3(0f, ChickenController.StartRowY, -1f);
            var chickenArtwork = CreateSprite2D("Chicken Artwork", LoadSprite2D("Chicken"), Vector3.zero, 20);
            ChickenRoad.Skins.ChickenSkinService.Apply(chickenArtwork.GetComponent<SpriteRenderer>());
            chickenArtwork.transform.SetParent(chickenObject.transform, false);
            chickenArtwork.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            ScaleSprite2D(chickenArtwork, 1.32f, 1.43f);
            chicken = chickenObject.AddComponent<ChickenController>();
            CreateLandingIndicator2D();

            var cameraObject = new GameObject("Portrait 2D Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.55f, 0.78f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.transform.position = new Vector3(0f, 3.45f, -10f);
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<PortraitCamera2D>();
            follow.target = chickenObject.transform;
        }

        private void CreateWater2D()
        {
            float bottom = -4f;
            float top = (FinishRow + 5) * ChickenController.RowSpacing;
            const float tileSize = WorldWidth2D + 0.08f;
            Sprite waterSprite = LoadSprite2D("Water");
            Shader waterShader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            waterMaterial2D = new Material(waterShader)
            {
                name = "Animated Painted Water",
                mainTexture = waterSprite.texture
            };
            var surfaceAnimator = sceneryRoot.gameObject.AddComponent<WaterSurfaceAnimator2D>();
            surfaceAnimator.waterMaterial = waterMaterial2D;
            int index = 0;
            for (float y = bottom + tileSize * 0.5f; y < top + tileSize * 0.5f; y += tileSize)
            {
                var water = CreateSprite2D($"Painted River {++index}", waterSprite, new Vector3(0f, y, 0f), -20);
                ScaleSprite2D(water, tileSize, tileSize);
                // Mirrored vertical tiling makes adjacent edges use the exact same pixels.
                var waterRenderer = water.GetComponent<SpriteRenderer>();
                waterRenderer.flipY = index % 2 == 0;
                waterRenderer.sharedMaterial = waterMaterial2D;
            }
        }

        private void CreateBank2D(string name, int row, float height, bool safe)
        {
            float y = row * ChickenController.RowSpacing;
            if (row == 0) y -= 0.85f;
            else if (row == FinishRow) y += 1.35f;

            var bank = CreateSprite2D(name, LoadSprite2D("Grass"), new Vector3(0f, y, 0f), -8);
            var bankRenderer = bank.GetComponent<SpriteRenderer>();
            bankRenderer.drawMode = SpriteDrawMode.Tiled;
            bankRenderer.size = new Vector2(WorldWidth2D, height);

            if (safe)
            {
                var island = CreateSprite2D(name + " Shoreline", GetSafeIslandStrip2D(), new Vector3(0f, row * ChickenController.RowSpacing - 0.04f, -0.1f), -6);
                ScaleSprite2D(island, 6.9f, 1.55f);
                CreateDecoration2D("Left Reeds", "Reeds", new Vector2(-2.85f, row * ChickenController.RowSpacing + 0.24f), 0.62f, 0.92f, -4);
                CreateDecoration2D("Right Reeds", "Reeds", new Vector2(2.82f, row * ChickenController.RowSpacing + 0.22f), 0.62f, 0.92f, -4, true);
            }
            else
            {
                float centerY = row == 0 ? 0.15f : row * ChickenController.RowSpacing;
                CreateDecoration2D(name + " Bush L", "Bush", new Vector2(-2.72f, centerY), 1.05f, 0.78f, -4);
                CreateDecoration2D(name + " Bush R", "Bush", new Vector2(2.72f, centerY + 0.18f), 1.05f, 0.78f, -4, true);
                CreateDecoration2D(name + " Reeds", "Reeds", new Vector2(-1.82f, centerY + 0.1f), 0.62f, 0.92f, -4);
            }
        }

        private Sprite GetSafeIslandStrip2D()
        {
            if (safeIslandStrip2D != null) return safeIslandStrip2D;
            Sprite source = LoadSprite2D("Island");
            Rect sourceRect = source.textureRect;
            var stripRect = new Rect(
                sourceRect.x + sourceRect.width * 0.02f,
                sourceRect.y + sourceRect.height * 0.18f,
                sourceRect.width * 0.96f,
                sourceRect.height * 0.32f);
            safeIslandStrip2D = Sprite.Create(
                source.texture,
                stripRect,
                new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            safeIslandStrip2D.name = "Safe Island Shoreline Strip";
            return safeIslandStrip2D;
        }

        private void CreateLandingIndicator2D()
        {
            var indicator = CreateSprite2D(
                "Landing Target",
                GetLandingIndicatorSprite2D(),
                new Vector3(0f, ChickenController.RowSpacing, -0.5f),
                12);
            ScaleSprite2D(indicator, 0.92f, 0.4f);
            var controller = indicator.AddComponent<LandingIndicator2D>();
            controller.chicken = chicken;
            controller.indicator = indicator.GetComponent<SpriteRenderer>();
        }

        private Sprite GetLandingIndicatorSprite2D()
        {
            if (landingIndicatorSprite2D != null) return landingIndicatorSprite2D;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Landing Target Ring",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.12f);
                    float glow = Mathf.Clamp01(1f - distance) * 0.22f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Max(ring * 0.92f, glow));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            landingIndicatorSprite2D = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            landingIndicatorSprite2D.name = "Landing Target Ring";
            return landingIndicatorSprite2D;
        }

        private void CreateWaterDecorations2D()
        {
            for (int row = 2; row < FinishRow; row += 3)
            {
                if (row == 7 || row == 14) continue;
                float side = row % 2 == 0 ? -1f : 1f;
                CreateDecoration2D("Lily Pads", "Lilies", new Vector2(side * 2.7f, row * ChickenController.RowSpacing + 0.55f), 0.82f, 0.58f, -12, side < 0f);
            }
        }

        private void CreateLane2D(int row)
        {
            float direction = row % 2 == 0 ? 1f : -1f;
            float difficultyT = Mathf.InverseLerp(1f, FinishRow - 1f, row);
            float speedMagnitude = Mathf.Lerp(0.78f, 1.92f, Mathf.SmoothStep(0f, 1f, difficultyT));
            float speed = speedMagnitude * direction;
            string spriteName;
            float width;
            float height;
            bool crocodile = false;

            if (row % 5 == 0)
            {
                spriteName = "Crocodile";
                width = 3.05f;
                height = 1.34f;
                crocodile = true;
            }
            else if (row % 6 == 0)
            {
                spriteName = "Raft";
                width = 2.7f;
                height = 1.16f;
            }
            else if (row % 4 == 0)
            {
                spriteName = "Boat";
                width = 2.85f;
                height = 1.25f;
            }
            else
            {
                spriteName = "Log";
                width = 2.65f;
                height = 1.08f;
            }

            float sizeByDifficulty = Mathf.Lerp(1.12f, 0.92f, difficultyT);
            width *= sizeByDifficulty;
            height *= Mathf.Lerp(1.04f, 0.95f, difficultyT);

            const int count = 2;
            float spacing = Mathf.Lerp(4.55f, 5.05f, difficultyT);
            float start = -spacing * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var platform = CreateSprite2D(spriteName, LoadSprite2D(spriteName), new Vector3(start + spacing * i + (row % 3) * 0.28f, row * ChickenController.RowSpacing, -0.2f), 0);
                ScaleSprite2D(platform, width, height);
                var spriteRenderer = platform.GetComponent<SpriteRenderer>();
                bool hasDirectionalFront = spriteName == "Crocodile" || spriteName == "Boat";
                spriteRenderer.flipX = hasDirectionalFront && speed > 0f;
                if (spriteName == "Log")
                {
                    // The painted log has a baked perspective tilt. Counter-rotate it so
                    // its visible upper surface travels horizontally along the lane.
                    platform.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
                }

                var collider = platform.AddComponent<BoxCollider2D>();
                const float minimumSafeWindow = 0.9f;
                float desiredColliderWidth = Mathf.Max(width * 0.78f, speedMagnitude * minimumSafeWindow);
                collider.size = new Vector2(
                    spriteRenderer.sprite.bounds.size.x * desiredColliderWidth / width,
                    spriteRenderer.sprite.bounds.size.y * 0.48f);
                collider.offset = new Vector2(0f, spriteRenderer.sprite.bounds.size.y * 0.02f);

                var mover = platform.AddComponent<RiverPlatform2D>();
                mover.speed = speed;
                mover.difficulty = row;
                mover.wrapEdge = spacing;
                mover.wrapDistance = spacing * count;
                mover.safeWindowSeconds = desiredColliderWidth / speedMagnitude;
                CreatePlatformWake2D(platform, mover, row * 0.37f + i * 0.61f);
                if (crocodile)
                {
                    var diving = platform.AddComponent<DivingCrocodile2D>();
                    diving.phase = row * 0.43f + i * 0.8f;
                }
            }
        }

        private void CreatePlatformWake2D(GameObject platform, RiverPlatform2D mover, float phase)
        {
            var wake = CreateSprite2D("Platform Wake", GetLandingIndicatorSprite2D(), platform.transform.position, -1);
            ScaleSprite2D(wake, 1.12f, 0.24f);
            wake.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.97f, 1f, 0.25f);
            var wakeController = wake.AddComponent<PlatformWake2D>();
            wakeController.target = mover;
            wakeController.wakeRenderer = wake.GetComponent<SpriteRenderer>();
            wakeController.phase = phase;
        }

        private void CreateCoefficientMarkers2D()
        {
            var coinSprite = LoadSprite2D("Coin");
            for (int row = 1; row <= FinishRow; row++)
            {
                var marker = new GameObject($"Next Multiplier {row}");
                marker.transform.SetParent(sceneryRoot);
                marker.transform.position = new Vector3(0f, row * ChickenController.RowSpacing, -0.6f);

                var glow = CreateSprite2D("Coin Glow", coinSprite, Vector3.zero, 14);
                glow.transform.SetParent(marker.transform, false);
                ScaleSprite2D(glow, 1.02f, 1.02f);
                glow.GetComponent<SpriteRenderer>().color = new Color(1f, 0.72f, 0.08f, 0.24f);

                var coin = CreateSprite2D("Gold Coin", coinSprite, Vector3.zero, 16);
                coin.transform.SetParent(marker.transform, false);
                ScaleSprite2D(coin, 0.78f, 0.78f);

                var shadowObject = new GameObject("Coefficient Shadow", typeof(TextMesh));
                shadowObject.transform.SetParent(marker.transform, false);
                shadowObject.transform.localPosition = new Vector3(0.025f, 0.68f, 0f);
                ConfigureMarkerText(shadowObject.GetComponent<TextMesh>(), $"+{row * 10}", new Color(0.12f, 0.07f, 0.015f, 0.92f), 17);

                var labelObject = new GameObject("Coefficient", typeof(TextMesh));
                labelObject.transform.SetParent(marker.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.72f, -0.01f);
                ConfigureMarkerText(labelObject.GetComponent<TextMesh>(), $"+{row * 10}", new Color(1f, 0.9f, 0.2f), 18);

                var nextObject = new GameObject("Next Label", typeof(TextMesh));
                nextObject.transform.SetParent(marker.transform, false);
                nextObject.transform.localPosition = new Vector3(0f, 1.01f, -0.02f);
                var nextText = nextObject.GetComponent<TextMesh>();
                ConfigureMarkerText(nextText, "NEXT", new Color(1f, 1f, 1f, 0.92f), 19);
                nextText.fontSize = 46;
                nextText.characterSize = 0.026f;

                var animation = marker.AddComponent<CoefficientMarker>();
                animation.phase = row * 0.41f;
                animation.twoDimensional = true;
                coefficientMarkers[row] = animation;
            }
        }

        private static void ConfigureMarkerText(TextMesh text, string value, Color color, int sortingOrder)
        {
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.043f;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        }

        private void CreateDecoration2D(string name, string spriteName, Vector2 position, float width, float height, int sortingOrder, bool flip = false)
        {
            var decoration = CreateSprite2D(name, LoadSprite2D(spriteName), new Vector3(position.x, position.y, 0f), sortingOrder);
            ScaleSprite2D(decoration, width, height);
            decoration.GetComponent<SpriteRenderer>().flipX = flip;
        }

        private GameObject CreateSprite2D(string name, Sprite sprite, Vector3 position, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(SpriteRenderer));
            gameObject.transform.SetParent(sceneryRoot);
            gameObject.transform.position = position;
            var renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        private static void ScaleSprite2D(GameObject gameObject, float width, float height)
        {
            var sprite = gameObject.GetComponent<SpriteRenderer>().sprite;
            if (sprite == null) return;
            Vector2 size = sprite.bounds.size;
            gameObject.transform.localScale = new Vector3(width / Mathf.Max(0.01f, size.x), height / Mathf.Max(0.01f, size.y), 1f);
        }

        private static Sprite LoadSprite2D(string name)
        {
            var sprite = Resources.Load<Sprite>($"RiverJump2D/{name}");
            if (sprite == null) Debug.LogError($"River Jump 2D sprite is missing: {name}");
            return sprite;
        }
    }
}
