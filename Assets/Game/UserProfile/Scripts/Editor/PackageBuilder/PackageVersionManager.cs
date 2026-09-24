using System.IO;
using UnityEditor;
using UnityEngine;

namespace UserProfile.PackageBuilder
{
    public class PackageVersionManager : MonoBehaviour
    {
        public string versionFilePath = "Assets/Game/UserProfile/UserProfileVersion.txt";
        
        public string initialVersion = "1.0.0";
        
        public string GetCurrentVersion()
        {
            if (File.Exists(versionFilePath))
            {
                return File.ReadAllText(versionFilePath);
            }
            else
            {
                File.WriteAllText(versionFilePath, initialVersion);
                
                AssetDatabase.Refresh();
                
                return initialVersion;
            }
        }
        
        public string IncreaseVersion(VersionType type)
        {
            string currentVersion = GetCurrentVersion();
            
            string[] versionComponents = currentVersion.Split('.');
            
            int major = int.Parse(versionComponents[0]);
            int minor = int.Parse(versionComponents[1]);
            int patch = int.Parse(versionComponents[2]);

            switch (type)
            {
                case VersionType.Major:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case VersionType.Minor:
                    minor++;
                    patch = 0;
                    break;
                case VersionType.Patch:
                    patch++;
                    break;
            }

            var newVersion = $"{major}.{minor}.{patch}";

            return newVersion;
        }

        public void UpdateVersion(string newVersion)
        {
            File.WriteAllText(versionFilePath, newVersion);
            
            AssetDatabase.Refresh();
        }
    }
}