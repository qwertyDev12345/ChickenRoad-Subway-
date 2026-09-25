using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Applies to both the Editor Build button and CiBuild.BuildIOS.
public sealed class IosBuildOptimization : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS) Configure();
    }

    [MenuItem("Build/Configure Compact iOS Build")]
    public static void Configure()
    {
        var target = NamedBuildTarget.iOS;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Master);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Medium);
        PlayerSettings.stripEngineCode = true;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite) continue;
            var settings = importer.GetPlatformTextureSettings("iPhone");
            if (settings.overridden && settings.maxTextureSize <= 1024
                && settings.format == TextureImporterFormat.ASTC_6x6) continue;
            settings.name = "iPhone";
            settings.overridden = true;
            settings.maxTextureSize = System.Math.Min(importer.maxTextureSize, 1024);
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.compressionQuality = 100;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
}
