#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Applies the AdMob app id and Android launcher icon whenever the project reloads.</summary>
[InitializeOnLoad]
internal static class AdMobProjectConfigurator
{
    private const string AndroidAppId = "ca-app-pub-1200547097832665~8919119189";
    private const string IconPath = "Assets/_Game/Development/Icon/Logo_1.png";

    static AdMobProjectConfigurator()
    {
        EditorApplication.delayCall += ApplyProjectSettings;
    }

    [MenuItem("Tools/Glow Rings/Apply Mobile Setup")]
    private static void ApplyProjectSettings()
    {
        ConfigureEmbeddedAndroidJdk();
        ApplyLauncherIcon();
        ApplyAdMobAppId();
    }

    private static void ConfigureEmbeddedAndroidJdk()
    {
        string jdkPath = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "AndroidPlayer",
            "OpenJDK");

        if (!Directory.Exists(jdkPath))
        {
            return;
        }

        Environment.SetEnvironmentVariable(
            "JAVA_HOME",
            jdkPath,
            EnvironmentVariableTarget.Process);
    }

    private static void ApplyLauncherIcon()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

        if (icon == null)
        {
            Debug.LogWarning("Glow Rings: Logo_1 launcher icon bulunamadı.");
            return;
        }

        int[] androidSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);

        if (androidSizes != null && androidSizes.Length > 0)
        {
            Texture2D[] androidIcons = new Texture2D[androidSizes.Length];

            for (int i = 0; i < androidIcons.Length; i++)
            {
                androidIcons[i] = icon;
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, androidIcons);
        }

        int[] defaultSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown);

        if (defaultSizes != null && defaultSizes.Length > 0)
        {
            Texture2D[] defaultIcons = new Texture2D[defaultSizes.Length];

            for (int i = 0; i < defaultIcons.Length; i++)
            {
                defaultIcons[i] = icon;
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, defaultIcons);
        }
    }

    private static void ApplyAdMobAppId()
    {
        Type settingsType = FindType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");

        if (settingsType == null)
        {
            return;
        }

        MethodInfo loadInstance = settingsType.GetMethod(
            "LoadInstance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        UnityEngine.Object settings = loadInstance?.Invoke(null, null) as UnityEngine.Object;

        if (settings == null)
        {
            Debug.LogWarning("Glow Rings: Google Mobile Ads ayar varlığı oluşturulamadı.");
            return;
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty androidAppId = serializedSettings.FindProperty("adMobAndroidAppId");

        if (androidAppId == null)
        {
            Debug.LogWarning("Glow Rings: AdMob Android App ID alanı bulunamadı.");
            return;
        }

        if (androidAppId.stringValue == AndroidAppId)
        {
            return;
        }

        androidAppId.stringValue = AndroidAppId;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Glow Rings: AdMob Android uygulama kimliği ayarlandı.");
    }

    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(fullName, false);

            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
#endif
