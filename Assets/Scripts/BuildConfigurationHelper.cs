using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>
/// Helper class to configure build settings for MediaPipe project
/// </summary>
public class BuildConfigurationHelper
{
    [MenuItem("MediaPipe Tools/Configure Build Settings")]
    public static void ConfigureBuildSettings()
    {
        Debug.Log("[BuildConfigurationHelper] Configuring build settings...");
        
        // Configure player settings
        ConfigurePlayerSettings();
        
        // Configure quality settings
        ConfigureQualitySettings();
        
        // Log configuration complete
        Debug.Log("[BuildConfigurationHelper] Build configuration complete!");
        
        // Save the project
        AssetDatabase.SaveAssets();
    }
    
    private static void ConfigurePlayerSettings()
    {
        // Set camera usage description
        PlayerSettings.iOS.cameraUsageDescription = "This application uses the camera for pose detection and emotion analysis.";
        PlayerSettings.macOS.cameraUsageDescription = "This application uses the camera for pose detection and emotion analysis.";
        
        // Set microphone usage description
        PlayerSettings.iOS.microphoneUsageDescription = "This application uses the microphone for voice recognition and emotion analysis.";
        PlayerSettings.macOS.microphoneUsageDescription = "This application uses the microphone for voice recognition and emotion analysis.";
        
        // Enable run in background for continuous audio processing
        PlayerSettings.runInBackground = true;
        
        // Configure Android settings
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;
        
        // Set minimum API levels for better hardware support
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23; // API 23 for runtime permissions
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        
        // Configure WebGL settings (disable WebGL microphone if not supported)
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            Debug.LogWarning("[BuildConfigurationHelper] WebGL builds may have limited microphone support");
        }
        
        // Configure Windows settings for better audio support
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
        {
            // Ensure proper graphics API
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new UnityEngine.Rendering.GraphicsDeviceType[] {
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D12
            });
        }
        
        Debug.Log("[BuildConfigurationHelper] Player settings configured");
    }
    
    private static void ConfigureQualitySettings()
    {
        // Ensure adequate performance for MediaPipe processing
        QualitySettings.vSyncCount = 0; // Disable VSync for better performance
        QualitySettings.antiAliasing = 0; // Disable AA for better performance
        
        Debug.Log("[BuildConfigurationHelper] Quality settings configured");
    }
    
    [MenuItem("MediaPipe Tools/Validate Build Requirements")]
    public static void ValidateBuildRequirements()
    {
        Debug.Log("[BuildConfigurationHelper] Validating build requirements...");
        
        bool isValid = true;
        
        // Check StreamingAssets
        if (!System.IO.Directory.Exists("Assets/StreamingAssets"))
        {
            Debug.LogError("[BuildConfigurationHelper] StreamingAssets folder missing!");
            isValid = false;
        }
        
        // Check Vosk model
        if (!System.IO.File.Exists("Assets/StreamingAssets/vosk-model-small-en-us-0.15.zip"))
        {
            Debug.LogError("[BuildConfigurationHelper] Vosk model file missing!");
            isValid = false;
        }
        
        // Check MediaPipe package
        if (!System.IO.Directory.Exists("Packages/com.github.homuler.mediapipe"))
        {
            Debug.LogError("[BuildConfigurationHelper] MediaPipe package missing!");
            isValid = false;
        }
        
        // Check permissions settings
        if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
        {
            Debug.LogWarning("[BuildConfigurationHelper] iOS camera usage description not set!");
        }
        
        if (string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
        {
            Debug.LogWarning("[BuildConfigurationHelper] iOS microphone usage description not set!");
        }
        
        if (isValid)
        {
            Debug.Log("[BuildConfigurationHelper] ✓ All build requirements validated!");
        }
        else
        {
            Debug.LogError("[BuildConfigurationHelper] ✗ Build validation failed! Please fix the issues above.");
        }
    }
    
    [MenuItem("MediaPipe Tools/Generate Build Report")]
    public static void GenerateBuildReport()
    {
        Debug.Log("=== MediaPipe Build Report ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Target Platform: {EditorUserBuildSettings.activeBuildTarget}");
        Debug.Log($"Development Build: {EditorUserBuildSettings.development}");
        Debug.Log($"Script Debugging: {EditorUserBuildSettings.allowDebugging}");
        
        // MediaPipe specific info
        Debug.Log($"MediaPipe Package Present: {System.IO.Directory.Exists("Packages/com.github.homuler.mediapipe")}");
        Debug.Log($"Vosk Model Present: {System.IO.File.Exists("Assets/StreamingAssets/vosk-model-small-en-us-0.15.zip")}");
        
        // Permissions
        Debug.Log($"iOS Camera Description: {PlayerSettings.iOS.cameraUsageDescription}");
        Debug.Log($"iOS Microphone Description: {PlayerSettings.iOS.microphoneUsageDescription}");
        Debug.Log($"macOS Camera Description: {PlayerSettings.macOS.cameraUsageDescription}");
        Debug.Log($"macOS Microphone Description: {PlayerSettings.macOS.microphoneUsageDescription}");
        
        Debug.Log("==============================");
    }
}
#endif 