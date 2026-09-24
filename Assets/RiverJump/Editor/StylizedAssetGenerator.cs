#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiverJump.Editor
{
    [InitializeOnLoad]
    public static class StylizedAssetGenerator
    {
        private const string Root = "Assets/RiverJump/Resources/Generated";
        private static readonly Dictionary<string, Material> Materials = new();
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;
        private static Mesh coneMesh;

        static StylizedAssetGenerator()
        {
            EditorApplication.delayCall += GenerateOnce;
        }

        private static void GenerateOnce()
        {
            if (SessionState.GetBool("RiverJump.StylizedAssets.v2", false)) return;
            SessionState.SetBool("RiverJump.StylizedAssets.v2", true);
            GenerateAll();
        }

        [MenuItem("River Jump/Regenerate Stylized Models")]
        public static void GenerateAll()
        {
            EnsureFolders();
            Materials.Clear();
            sphereMesh = SaveMesh("RJ_SoftSphere", CreateSphere(20, 12));
            cylinderMesh = SaveMesh("RJ_RoundedCylinder", CreateCylinder(18));
            coneMesh = SaveMesh("RJ_SoftCone", CreateCone(16));

            SavePrefab("StylizedChicken", BuildChicken());
            SavePrefab("StylizedCrocodile", BuildCrocodile());
            SavePrefab("StylizedLog", BuildLog());
            SavePrefab("StylizedBoat", BuildBoat());
            SavePrefab("StylizedRaft", BuildRaft());
            SavePrefab("StylizedIsland", BuildIsland());
            SavePrefab("StylizedVegetation", BuildVegetation());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("River Jump: stylized model prefabs generated in " + Root);
        }

        private static GameObject BuildChicken()
        {
            var root = new GameObject("Stylized Chicken");
            var ivory = Mat("Chicken Ivory", new Color(1f, 0.91f, 0.72f), 0.18f);
            var cream = Mat("Wing Cream", new Color(0.9f, 0.78f, 0.55f), 0.12f);
            var red = Mat("Comb Coral", new Color(0.93f, 0.12f, 0.08f), 0.2f);
            var orange = Mat("Beak Gold", new Color(1f, 0.48f, 0.025f), 0.4f);
            var dark = Mat("Eye Dark", new Color(0.035f, 0.025f, 0.018f), 0.8f);
            var brown = Mat("Brow Brown", new Color(0.18f, 0.075f, 0.025f), 0.22f);
            var blush = Mat("Cheek Blush", new Color(1f, 0.48f, 0.42f), 0.16f);

            Part(root, "Body", sphereMesh, new Vector3(0, 0.68f, 0), new Vector3(0.88f, 0.98f, 0.82f), ivory);
            Part(root, "Chest", sphereMesh, new Vector3(0, 0.68f, 0.48f), new Vector3(0.64f, 0.66f, 0.32f), ivory);
            Part(root, "Head", sphereMesh, new Vector3(0, 1.48f, 0.18f), new Vector3(0.72f, 0.7f, 0.68f), ivory);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Eye", sphereMesh, new Vector3(side * 0.25f, 1.58f, 0.72f), new Vector3(0.19f, 0.23f, 0.13f), dark);
                Part(root, "Eye Shine", sphereMesh, new Vector3(side * 0.29f, 1.66f, 0.82f), Vector3.one * 0.055f, Mat("Eye Shine", Color.white, 0.9f));
                Part(root, "Brow", sphereMesh, new Vector3(side * 0.25f, 1.83f, 0.68f), new Vector3(0.22f, 0.055f, 0.055f), brown, new Vector3(0, 0, side * -10f));
                Part(root, "Cheek", sphereMesh, new Vector3(side * 0.42f, 1.36f, 0.63f), new Vector3(0.17f, 0.08f, 0.055f), blush);
                Part(root, "Wing", sphereMesh, new Vector3(side * 0.67f, 0.82f, 0.02f), new Vector3(0.28f, 0.59f, 0.44f), cream, new Vector3(0, 0, side * -13f));
                Part(root, "Leg", cylinderMesh, new Vector3(side * 0.27f, -0.02f, 0.08f), new Vector3(0.075f, 0.28f, 0.075f), orange);
                for (int toe = -1; toe <= 1; toe++)
                    Part(root, "Toe", cylinderMesh, new Vector3(side * 0.27f + toe * 0.1f, -0.26f, 0.25f), new Vector3(0.038f, 0.2f, 0.038f), orange, new Vector3(72f, 0, toe * 14f));
            }
            Part(root, "Upper Beak", coneMesh, new Vector3(0, 1.42f, 0.91f), new Vector3(0.34f, 0.36f, 0.34f), orange, new Vector3(90f, 0, 0));
            Part(root, "Lower Beak", sphereMesh, new Vector3(0, 1.29f, 0.83f), new Vector3(0.28f, 0.12f, 0.28f), orange);
            Part(root, "Wattle", sphereMesh, new Vector3(0, 1.08f, 0.63f), new Vector3(0.19f, 0.29f, 0.16f), red);
            for (int i = 0; i < 3; i++)
                Part(root, "Comb", sphereMesh, new Vector3((i - 1) * 0.18f, 2.06f + (1 - Mathf.Abs(i - 1)) * 0.09f, 0.14f), new Vector3(0.21f, 0.3f, 0.18f), red, new Vector3(0, 0, (i - 1) * 12f));
            for (int i = -1; i <= 1; i++)
                Part(root, "Tail Feather", sphereMesh, new Vector3(i * 0.25f, 0.96f + (1 - Mathf.Abs(i)) * 0.18f, -0.66f), new Vector3(0.18f, 0.5f, 0.16f), ivory, new Vector3(-38f, 0, i * 20f));

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.82f, 0);
            collider.height = 1.75f;
            collider.radius = 0.55f;
            return root;
        }

        private static GameObject BuildCrocodile()
        {
            var root = new GameObject("Stylized Crocodile");
            var green = Mat("Croc Green", new Color(0.28f, 0.58f, 0.08f), 0.25f);
            var light = Mat("Croc Belly", new Color(0.58f, 0.7f, 0.2f), 0.16f);
            var dark = Mat("Croc Scales", new Color(0.12f, 0.37f, 0.045f), 0.18f);
            var eye = Mat("Croc Eye", new Color(0.08f, 0.04f, 0.01f), 0.8f);
            var tooth = Mat("Croc Tooth", new Color(1f, 0.92f, 0.69f), 0.15f);
            var stone = Mat("Croc Deck Stone", new Color(0.73f, 0.64f, 0.46f), 0.1f);
            Part(root, "Body", sphereMesh, Vector3.zero, new Vector3(1.5f, 0.42f, 0.72f), green);
            Part(root, "Head", sphereMesh, new Vector3(1.5f, 0.08f, 0), new Vector3(0.8f, 0.44f, 0.68f), green);
            Part(root, "Snout", sphereMesh, new Vector3(2.04f, -0.02f, 0), new Vector3(0.65f, 0.28f, 0.58f), light);
            Part(root, "Tail", coneMesh, new Vector3(-1.68f, 0, 0), new Vector3(0.65f, 1.1f, 0.65f), green, new Vector3(0, 0, 90f));
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Eye", sphereMesh, new Vector3(1.58f, 0.42f, side * 0.42f), new Vector3(0.2f, 0.2f, 0.17f), Mat("Croc Eye White", new Color(0.95f, 0.93f, 0.7f), 0.4f));
                Part(root, "Pupil", sphereMesh, new Vector3(1.73f, 0.45f, side * 0.48f), Vector3.one * 0.095f, eye);
                Part(root, "Foot", sphereMesh, new Vector3(0.7f, -0.22f, side * 0.72f), new Vector3(0.46f, 0.2f, 0.32f), green);
                Part(root, "Foot", sphereMesh, new Vector3(-0.75f, -0.22f, side * 0.72f), new Vector3(0.46f, 0.2f, 0.32f), green);
                for (int i = 0; i < 4; i++)
                    Part(root, "Tooth", coneMesh, new Vector3(1.62f + i * 0.23f, -0.04f, side * 0.46f), new Vector3(0.065f, 0.13f, 0.065f), tooth);
            }
            for (int i = -3; i <= 2; i++)
                Part(root, "Back Scale", sphereMesh, new Vector3(i * 0.4f, 0.4f, 0), new Vector3(0.2f, 0.2f, 0.32f), dark);
            for (int z = -1; z <= 1; z++)
                for (int x = -2; x <= 2; x++)
                    Part(root, "Deck Stone", sphereMesh, new Vector3(x * 0.48f, 0.48f, z * 0.34f), new Vector3(0.29f, 0.1f, 0.22f), stone);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.38f, 0);
            collider.size = new Vector3(2.7f, 0.35f, 1.25f);
            return root;
        }

        private static GameObject BuildLog()
        {
            var root = new GameObject("Stylized Log");
            var bark = Mat("Warm Bark", new Color(0.46f, 0.2f, 0.055f), 0.16f);
            var rings = Mat("Cut Wood", new Color(0.83f, 0.52f, 0.2f), 0.22f);
            var moss = Mat("Log Moss", new Color(0.29f, 0.55f, 0.08f), 0.1f);
            Part(root, "Log Body", cylinderMesh, Vector3.zero, new Vector3(0.48f, 1.45f, 0.48f), bark, new Vector3(0, 0, 90f));
            Part(root, "Cut End", cylinderMesh, new Vector3(1.46f, 0, 0), new Vector3(0.43f, 0.035f, 0.43f), rings, new Vector3(0, 0, 90f));
            Part(root, "Cut End", cylinderMesh, new Vector3(-1.46f, 0, 0), new Vector3(0.43f, 0.035f, 0.43f), rings, new Vector3(0, 0, 90f));
            Part(root, "Branch", cylinderMesh, new Vector3(0.45f, 0.38f, 0), new Vector3(0.11f, 0.48f, 0.11f), bark, new Vector3(0, 0, -42f));
            for (int i = -2; i <= 2; i++)
                Part(root, "Moss", sphereMesh, new Vector3(i * 0.46f, 0.43f, -0.08f), new Vector3(0.34f, 0.09f, 0.28f), moss);
            root.AddComponent<BoxCollider>().size = new Vector3(2.8f, 0.8f, 0.85f);
            return root;
        }

        private static GameObject BuildBoat()
        {
            var root = new GameObject("Stylized Boat");
            var hull = Mat("Boat Coral", new Color(0.86f, 0.18f, 0.08f), 0.28f);
            var trim = Mat("Boat Gold Trim", new Color(1f, 0.58f, 0.05f), 0.3f);
            var wood = Mat("Boat Interior", new Color(0.4f, 0.18f, 0.05f), 0.14f);
            var hullMesh = SaveMesh("RJ_BoatHull", CreateBoatHull(18));
            Part(root, "Hull", hullMesh, Vector3.zero, Vector3.one, hull);
            for (int side = -1; side <= 1; side += 2)
                Part(root, "Golden Rail", cylinderMesh, new Vector3(0, 0.38f, side * 0.62f), new Vector3(0.07f, 1.35f, 0.07f), trim, new Vector3(0, 0, 90f));
            for (int x = -1; x <= 1; x += 2)
                Part(root, "Seat", cylinderMesh, new Vector3(x * 0.48f, 0.3f, 0), new Vector3(0.08f, 0.58f, 0.08f), wood, new Vector3(90f, 0, 0));
            Part(root, "Paddle", cylinderMesh, new Vector3(0.35f, 0.55f, 0), new Vector3(0.055f, 1.25f, 0.055f), wood, new Vector3(70f, 10f, 65f));
            Part(root, "Paddle Blade", sphereMesh, new Vector3(-0.65f, 0.16f, -0.62f), new Vector3(0.2f, 0.42f, 0.07f), trim, new Vector3(25f, 0, 45f));
            root.AddComponent<BoxCollider>().size = new Vector3(2.65f, 0.75f, 1.25f);
            return root;
        }

        private static GameObject BuildRaft()
        {
            var root = new GameObject("Stylized Raft");
            var wood = Mat("Raft Wood", new Color(0.49f, 0.24f, 0.07f), 0.14f);
            var rope = Mat("Rope", new Color(0.72f, 0.48f, 0.2f), 0.12f);
            for (int z = -2; z <= 2; z++)
                Part(root, "Raft Log", cylinderMesh, new Vector3(0, 0, z * 0.28f), new Vector3(0.25f, 1.35f, 0.25f), wood, new Vector3(0, 0, 90f));
            for (int x = -1; x <= 1; x += 2)
                Part(root, "Rope Binding", cylinderMesh, new Vector3(x * 0.72f, 0.26f, 0), new Vector3(0.045f, 0.78f, 0.045f), rope, new Vector3(90f, 0, 0));
            root.AddComponent<BoxCollider>().size = new Vector3(2.7f, 0.5f, 1.45f);
            return root;
        }

        private static GameObject BuildIsland()
        {
            var root = new GameObject("Stylized Island");
            var islandMesh = SaveMesh("RJ_Island", CreateIsland(20));
            Part(root, "Island", islandMesh, Vector3.zero, Vector3.one, Mat("Island Earth", new Color(0.46f, 0.27f, 0.09f), 0.08f));
            Part(root, "Grass Top", sphereMesh, new Vector3(0, 0.38f, 0), new Vector3(1.7f, 0.25f, 1.15f), Mat("Island Grass", new Color(0.32f, 0.68f, 0.1f), 0.1f));
            for (int i = 0; i < 7; i++)
                Part(root, "Stone", sphereMesh, new Vector3(Mathf.Cos(i * 0.9f) * 1.55f, 0.08f, Mathf.Sin(i * 0.9f) * 0.9f), new Vector3(0.25f, 0.2f, 0.22f), Mat("River Stone", new Color(0.48f, 0.52f, 0.45f), 0.08f));
            root.AddComponent<BoxCollider>().center = new Vector3(0, 0.35f, 0);
            root.GetComponent<BoxCollider>().size = new Vector3(3.2f, 0.55f, 2f);
            return root;
        }

        private static GameObject BuildVegetation()
        {
            var root = new GameObject("Stylized Vegetation");
            var green = Mat("Leaf Green", new Color(0.25f, 0.62f, 0.08f), 0.08f);
            var dark = Mat("Leaf Dark", new Color(0.1f, 0.4f, 0.045f), 0.08f);
            var brown = Mat("Tree Wood", new Color(0.35f, 0.16f, 0.045f), 0.12f);
            Part(root, "Tree Trunk", cylinderMesh, new Vector3(0, 1.1f, 0), new Vector3(0.34f, 1.1f, 0.34f), brown);
            for (int i = 0; i < 9; i++)
                Part(root, "Tree Crown", sphereMesh, new Vector3(Mathf.Cos(i * 2.4f) * 0.58f, 2.35f + (i % 3) * 0.18f, Mathf.Sin(i * 2.4f) * 0.5f), new Vector3(0.62f, 0.58f, 0.56f), i % 2 == 0 ? green : dark);
            for (int i = 0; i < 7; i++)
                Part(root, "Reed", cylinderMesh, new Vector3(-2f + i * 0.18f, 0.58f + (i % 2) * 0.12f, 0), new Vector3(0.035f, 0.58f, 0.035f), green, new Vector3(0, 0, (i - 3) * 3f));
            return root;
        }

        private static GameObject Part(GameObject parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localEulerAngles = rotation;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return part;
        }

        private static Material Mat(string name, Color color, float smoothness)
        {
            if (Materials.TryGetValue(name, out var material)) return material;
            string path = Root + "/Materials/" + name.Replace(" ", "_") + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            Materials[name] = material;
            return material;
        }

        private static Mesh SaveMesh(string name, Mesh generated)
        {
            string path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                generated.name = name;
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }
            EditorUtility.CopySerialized(generated, existing);
            existing.name = name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(generated);
            return existing;
        }

        private static void SavePrefab(string name, GameObject root)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static Mesh CreateSphere(int segments, int rings)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= segments; x++)
                {
                    float u = x / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 p = new(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    vertices.Add(p * 0.5f);
                    normals.Add(p.normalized);
                    uv.Add(new Vector2(u, v));
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segments; x++)
                {
                    int a = y * (segments + 1) + x;
                    int b = a + segments + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            return MeshFrom(vertices, normals, uv, triangles);
        }

        private static Mesh CreateCylinder(int segments)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int y = 0; y <= 1; y++)
                for (int i = 0; i <= segments; i++)
                {
                    float a = i / (float)segments * Mathf.PI * 2f;
                    Vector3 n = new(Mathf.Cos(a), 0, Mathf.Sin(a));
                    vertices.Add(new Vector3(n.x * 0.5f, y * 2f - 1f, n.z * 0.5f)); normals.Add(n); uv.Add(new Vector2(i / (float)segments, y));
                }
            for (int i = 0; i < segments; i++)
            {
                int a = i, b = i + segments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1); triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            AddCap(vertices, normals, uv, triangles, segments, -1f, false);
            AddCap(vertices, normals, uv, triangles, segments, 1f, true);
            return MeshFrom(vertices, normals, uv, triangles);
        }

        private static Mesh CreateCone(int segments)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            vertices.Add(new Vector3(0, 1f, 0)); normals.Add(Vector3.up); uv.Add(new Vector2(0.5f, 1));
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = new(Mathf.Cos(a) * 0.5f, -1f, Mathf.Sin(a) * 0.5f);
                vertices.Add(p); normals.Add(new Vector3(p.x, 0.5f, p.z).normalized); uv.Add(new Vector2(i / (float)segments, 0));
            }
            for (int i = 0; i < segments; i++) { triangles.Add(0); triangles.Add(i + 2); triangles.Add(i + 1); }
            AddCap(vertices, normals, uv, triangles, segments, -1f, false);
            return MeshFrom(vertices, normals, uv, triangles);
        }

        private static Mesh CreateBoatHull(int segments)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(t) * (ring == 0 ? 1.45f : 1.12f);
                    float z = Mathf.Sin(t) * (ring == 0 ? 0.68f : 0.43f);
                    float y = ring == 0 ? 0.35f : -0.22f;
                    vertices.Add(new Vector3(x, y, z)); normals.Add(new Vector3(x, 0.45f, z).normalized); uv.Add(new Vector2(i / (float)segments, ring));
                }
            for (int i = 0; i < segments; i++)
            {
                int a = i, b = i + segments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1); triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            return MeshFrom(vertices, normals, uv, triangles);
        }

        private static Mesh CreateIsland(int segments)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments * Mathf.PI * 2f;
                    float wobble = 1f + Mathf.Sin(i * 2.31f) * 0.08f;
                    float radiusX = (ring == 0 ? 1.75f : 1.48f) * wobble;
                    float radiusZ = (ring == 0 ? 1.12f : 0.92f) * wobble;
                    vertices.Add(new Vector3(Mathf.Cos(t) * radiusX, ring == 0 ? 0.35f : -0.3f, Mathf.Sin(t) * radiusZ));
                    normals.Add(new Vector3(Mathf.Cos(t), 0.35f, Mathf.Sin(t)).normalized); uv.Add(new Vector2(i / (float)segments, ring));
                }
            for (int i = 0; i < segments; i++)
            {
                int a = i, b = i + segments + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1); triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
            return MeshFrom(vertices, normals, uv, triangles);
        }

        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles, int segments, float y, bool top)
        {
            int center = vertices.Count;
            vertices.Add(new Vector3(0, y, 0)); normals.Add(top ? Vector3.up : Vector3.down); uv.Add(new Vector2(0.5f, 0.5f));
            int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(a) * 0.5f, y, Mathf.Sin(a) * 0.5f)); normals.Add(top ? Vector3.up : Vector3.down); uv.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }
            for (int i = 0; i < segments; i++)
            {
                if (top) { triangles.Add(center); triangles.Add(start + i); triangles.Add(start + i + 1); }
                else { triangles.Add(center); triangles.Add(start + i + 1); triangles.Add(start + i); }
            }
        }

        private static Mesh MeshFrom(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/RiverJump", "Resources");
            EnsureFolder("Assets/RiverJump/Resources", "Generated");
            EnsureFolder(Root, "Meshes");
            EnsureFolder(Root, "Materials");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
