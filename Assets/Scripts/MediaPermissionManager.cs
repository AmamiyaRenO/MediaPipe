using System.Collections;
using UnityEngine;

/// <summary>
/// Manages camera and microphone permissions for built applications
/// </summary>
public class MediaPermissionManager : MonoBehaviour
{
    [Header("=== Permission Settings ===")]
    [Tooltip("Auto request permissions on start")]
    public bool autoRequestOnStart = true;
    
    [Tooltip("Show permission status in UI")]
    public bool showPermissionUI = true;
    
    [Header("=== Permission Status ===")]
    public bool hasCameraPermission = false;
    public bool hasMicrophonePermission = false;
    
    [Header("=== Events ===")]
    public UnityEngine.Events.UnityEvent OnCameraPermissionGranted;
    public UnityEngine.Events.UnityEvent OnMicrophonePermissionGranted;
    public UnityEngine.Events.UnityEvent OnPermissionDenied;
    
    private bool isRequestingPermissions = false;
    private Rect uiRect = new Rect(10, 10, 300, 150);
    
    void Start()
    {
        if (autoRequestOnStart)
        {
            RequestPermissions();
        }
    }
    
    /// <summary>
    /// Request both camera and microphone permissions
    /// </summary>
    public void RequestPermissions()
    {
        if (!isRequestingPermissions)
        {
            StartCoroutine(RequestPermissionsCoroutine());
        }
    }
    
    private IEnumerator RequestPermissionsCoroutine()
    {
        isRequestingPermissions = true;
        Debug.Log("[MediaPermissionManager] Starting permission requests...");
        
        // Request camera permission
        yield return StartCoroutine(RequestCameraPermission());
        
        // Request microphone permission
        yield return StartCoroutine(RequestMicrophonePermission());
        
        isRequestingPermissions = false;
        
        // Log final status
        Debug.Log($"[MediaPermissionManager] Final status - Camera: {hasCameraPermission}, Microphone: {hasMicrophonePermission}");
        
        // Notify other systems if all permissions granted
        if (hasCameraPermission && hasMicrophonePermission)
        {
            Debug.Log("[MediaPermissionManager] All permissions granted! Systems can now initialize.");
        }
    }
    
    private IEnumerator RequestCameraPermission()
    {
        Debug.Log("[MediaPermissionManager] Requesting camera permission...");
        
        // Check if already granted
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            hasCameraPermission = true;
            OnCameraPermissionGranted?.Invoke();
            Debug.Log("[MediaPermissionManager] Camera permission already granted");
            yield break;
        }
        
        // Request permission
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        
        // Check result
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            hasCameraPermission = true;
            OnCameraPermissionGranted?.Invoke();
            Debug.Log("[MediaPermissionManager] Camera permission granted!");
        }
        else
        {
            hasCameraPermission = false;
            OnPermissionDenied?.Invoke();
            Debug.LogWarning("[MediaPermissionManager] Camera permission denied!");
        }
    }
    
    private IEnumerator RequestMicrophonePermission()
    {
        Debug.Log("[MediaPermissionManager] Requesting microphone permission...");
        
        // Check if already granted
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            hasMicrophonePermission = true;
            OnMicrophonePermissionGranted?.Invoke();
            Debug.Log("[MediaPermissionManager] Microphone permission already granted");
            yield break;
        }
        
        // Request permission
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        
        // Check result
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            hasMicrophonePermission = true;
            OnMicrophonePermissionGranted?.Invoke();
            Debug.Log("[MediaPermissionManager] Microphone permission granted!");
        }
        else
        {
            hasMicrophonePermission = false;
            OnPermissionDenied?.Invoke();
            Debug.LogWarning("[MediaPermissionManager] Microphone permission denied!");
        }
    }
    
    /// <summary>
    /// Check current permission status without requesting
    /// </summary>
    public void UpdatePermissionStatus()
    {
        hasCameraPermission = Application.HasUserAuthorization(UserAuthorization.WebCam);
        hasMicrophonePermission = Application.HasUserAuthorization(UserAuthorization.Microphone);
        
        Debug.Log($"[MediaPermissionManager] Current status - Camera: {hasCameraPermission}, Microphone: {hasMicrophonePermission}");
    }
    
    /// <summary>
    /// Force re-request permissions
    /// </summary>
    public void ForceRequestPermissions()
    {
        hasCameraPermission = false;
        hasMicrophonePermission = false;
        RequestPermissions();
    }
    
    void OnGUI()
    {
        if (!showPermissionUI) return;
        
        GUI.Box(uiRect, "Media Permissions");
        
        GUILayout.BeginArea(new Rect(uiRect.x + 10, uiRect.y + 25, uiRect.width - 20, uiRect.height - 35));
        
        // Permission status
        GUILayout.Label($"Camera: {(hasCameraPermission ? "✓ Granted" : "✗ Denied")}");
        GUILayout.Label($"Microphone: {(hasMicrophonePermission ? "✓ Granted" : "✗ Denied")}");
        
        // Request button
        if (GUILayout.Button("Request Permissions"))
        {
            RequestPermissions();
        }
        
        // Status info
        if (isRequestingPermissions)
        {
            GUILayout.Label("Requesting permissions...");
        }
        else if (!hasCameraPermission || !hasMicrophonePermission)
        {
            GUILayout.Label("Some permissions missing!");
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Get device information for debugging
    /// </summary>
    public void LogDeviceInfo()
    {
        Debug.Log("=== Media Device Information ===");
        Debug.Log($"Camera devices: {WebCamTexture.devices.Length}");
        foreach (var device in WebCamTexture.devices)
        {
            Debug.Log($"Camera: {device.name} (Front: {device.isFrontFacing})");
        }
        
        Debug.Log($"Microphone devices: {Microphone.devices.Length}");
        foreach (var device in Microphone.devices)
        {
            Debug.Log($"Microphone: {device}");
        }
        Debug.Log("================================");
    }
} 