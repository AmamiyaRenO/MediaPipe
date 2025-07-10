using UnityEngine;

/// <summary>
/// 语音设置辅助脚本 - 自动配置语音处理器参数
/// </summary>
public class VoiceSetupHelper : MonoBehaviour
{
    [Header("=== Voice Configuration ===")]
    [Tooltip("Automatically setup voice processor on start")]
    public bool autoSetupOnStart = true;
    
    [Tooltip("Enable auto voice detection")]
    public bool enableAutoDetect = true;
    
    [Tooltip("Minimum volume threshold for voice detection")]
    [Range(0.001f, 0.1f)]
    public float minimumVolumeThreshold = 0.01f;
    
    [Tooltip("Silence timeout in seconds")]
    [Range(0.5f, 3.0f)]
    public float silenceTimeout = 1.0f;
    
    [Tooltip("Show debug logs")]
    public bool showDebugLogs = true;
    
    [Header("=== Component References ===")]
    public VoiceProcessor voiceProcessor;
    public VoskSpeechToText voskSpeech;
    public VoiceEmotionAnalyzer voiceAnalyzer;
    
    private bool isSetupComplete = false;
    
    void Awake()
    {
        // 自动查找组件
        if (voiceProcessor == null)
            voiceProcessor = FindObjectOfType<VoiceProcessor>();
            
        if (voskSpeech == null)
            voskSpeech = FindObjectOfType<VoskSpeechToText>();
            
        if (voiceAnalyzer == null)
            voiceAnalyzer = FindObjectOfType<VoiceEmotionAnalyzer>();
    }
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupVoiceComponents();
        }
    }
    
    /// <summary>
    /// 设置语音组件
    /// </summary>
    public void SetupVoiceComponents()
    {
        if (isSetupComplete) return;
        
        if (showDebugLogs)
            Debug.Log("🎤 Setting up voice components...");
        
        // 设置VoiceProcessor
        if (voiceProcessor != null)
        {
            SetupVoiceProcessor();
        }
        else
        {
            Debug.LogError("❌ VoiceProcessor not found!");
            return;
        }
        
        // 设置VoskSpeechToText
        if (voskSpeech != null)
        {
            SetupVoskSpeech();
        }
        
        // 设置VoiceEmotionAnalyzer
        if (voiceAnalyzer != null)
        {
            SetupVoiceAnalyzer();
        }
        
        // 启动录音
        StartVoiceRecording();
        
        isSetupComplete = true;
        
        if (showDebugLogs)
            Debug.Log("✅ Voice setup complete!");
    }
    
    /// <summary>
    /// 设置语音处理器
    /// </summary>
    private void SetupVoiceProcessor()
    {
        // 使用反射设置私有字段
        var voiceProcessorType = typeof(VoiceProcessor);
        
        // 设置最小音量阈值
        var minimumSampleField = voiceProcessorType.GetField("_minimumSpeakingSampleValue", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (minimumSampleField != null)
        {
            minimumSampleField.SetValue(voiceProcessor, minimumVolumeThreshold);
            if (showDebugLogs)
                Debug.Log($"🎤 Set minimum volume threshold to: {minimumVolumeThreshold}");
        }
        
        // 设置静音超时
        var silenceTimerField = voiceProcessorType.GetField("_silenceTimer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (silenceTimerField != null)
        {
            silenceTimerField.SetValue(voiceProcessor, silenceTimeout);
            if (showDebugLogs)
                Debug.Log($"🎤 Set silence timeout to: {silenceTimeout}s");
        }
        
        // 设置自动检测
        var autoDetectField = voiceProcessorType.GetField("_autoDetect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (autoDetectField != null)
        {
            autoDetectField.SetValue(voiceProcessor, enableAutoDetect);
            if (showDebugLogs)
                Debug.Log($"🎤 Set auto detect to: {enableAutoDetect}");
        }
    }
    
    /// <summary>
    /// 设置Vosk语音识别
    /// </summary>
    private void SetupVoskSpeech()
    {
        if (showDebugLogs)
            Debug.Log("🎤 Vosk speech recognizer found");
    }
    
    /// <summary>
    /// 设置语音情感分析器
    /// </summary>
    private void SetupVoiceAnalyzer()
    {
        if (voiceAnalyzer.volumeThreshold > minimumVolumeThreshold)
        {
            voiceAnalyzer.volumeThreshold = minimumVolumeThreshold + 0.005f; // 稍微高一点
            if (showDebugLogs)
                Debug.Log($"🎤 Adjusted voice analyzer threshold to: {voiceAnalyzer.volumeThreshold}");
        }
        
        // 启用调试日志
        voiceAnalyzer.showAnalysisLogs = true;
        voiceAnalyzer.showVoiceUI = true;
    }
    
    /// <summary>
    /// 启动语音录音
    /// </summary>
    private void StartVoiceRecording()
    {
        if (voiceProcessor != null && !voiceProcessor.IsRecording)
        {
            // 启动录音并启用自动检测
            voiceProcessor.StartRecording(16000, 512, enableAutoDetect);
            
            if (showDebugLogs)
                Debug.Log("🎤 Voice recording started with auto-detect enabled");
        }
    }
    
    /// <summary>
    /// 手动重新设置
    /// </summary>
    [ContextMenu("Force Reconfigure Voice")]
    public void ForceReconfigure()
    {
        isSetupComplete = false;
        SetupVoiceComponents();
    }
    
    /// <summary>
    /// 调试信息
    /// </summary>
    void OnGUI()
    {
        if (!showDebugLogs) return;
        
        GUI.Box(new Rect(10, 200, 300, 100), "Voice Setup Debug");
        
        GUI.Label(new Rect(20, 225, 280, 20), $"Setup Complete: {isSetupComplete}");
        
        if (voiceProcessor != null)
        {
            GUI.Label(new Rect(20, 245, 280, 20), $"Is Recording: {voiceProcessor.IsRecording}");
            GUI.Label(new Rect(20, 265, 280, 20), $"Device: {voiceProcessor.CurrentDeviceName}");
        }
        
        // 重新配置按钮
        if (GUI.Button(new Rect(20, 280, 100, 20), "Reconfigure"))
        {
            ForceReconfigure();
        }
    }
} 