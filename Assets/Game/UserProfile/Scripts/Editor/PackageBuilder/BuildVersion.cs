using UnityEditor;
using UnityEngine;

namespace UserProfile.PackageBuilder
{
    public class BuildVersion : EditorWindow
    {
        private static EditorWindow _window;

        [MenuItem("User Profile/About", false, 1)]
        private static void Init()
        {
            _window = GetWindow<BuildVersion>("About User Profile");

            _window.name = "About User Profile";
            
            _window.maxSize = new Vector2(200f, 60f);
            _window.minSize = new Vector2(200f, 60f);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("BOX");
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Version: " + GetCurrentVersion(), new GUIStyle(EditorStyles.boldLabel) 
                { alignment = TextAnchor.MiddleCenter, fontSize = 20 }, GUILayout.Height(30));
            
            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }
        
        private string GetCurrentVersion()
        {
            var versionManager = new PackageVersionManager();
            
            return versionManager.GetCurrentVersion();
        }
    }
}
