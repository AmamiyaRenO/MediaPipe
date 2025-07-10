using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 性能监控器 - 自动检测性能问题并采取措施防止崩溃
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("=== Performance Thresholds ===")]
    [Tooltip("FPS threshold for performance warnings")]
    public float warningFPSThreshold = 45f;
    
    [Tooltip("FPS threshold for critical performance")]
    public float criticalFPSThreshold = 25f;
    
    [Tooltip("Memory usage threshold (MB)")]
    public float memoryWarningThreshold = 1024f;
    
    [Tooltip("Memory usage critical threshold (MB)")]
    public float memoryCriticalThreshold = 1536f;
    
    [Header("=== Auto-Management Settings ===")]
    [Tooltip("Enable automatic performance adjustments")]
    public bool enableAutoManagement = true;
    
    [Tooltip("Auto-disable features when performance is poor")]
    public bool autoDisableFeatures = true;
    
    [Tooltip("Force garbage collection when memory is high")]
    public bool forceGCOnHighMemory = true;
    
    [Tooltip("Performance check interval (seconds)")]
    public float checkInterval = 2f;
    
    [Header("=== Component References ===")]
    public DataVisualizationDashboard dashboard;
    public VoiceEmotionAnalyzer voiceAnalyzer;
    public PoseEmotionAnalyzer poseAnalyzer;
    public EmotionDetectionSystem emotionSystem;
    
    [Header("=== Debug Settings ===")]
    [Tooltip("Show performance warnings in console")]
    public bool showWarnings = true;
    
    [Tooltip("Show performance info in GUI")]
    public bool showPerformanceGUI = true;
    
    // 性能数据
    private float currentFPS = 60f;
    private float averageFPS = 60f;
    private float currentMemoryMB = 0f;
    private int frameCount = 0;
    private float frameTimer = 0f;
    private List<float> fpsHistory = new List<float>();
    
    // 系统状态
    private PerformanceLevel currentPerformanceLevel = PerformanceLevel.Good;
    private bool featuresDisabled = false;
    private float lastCheckTime = 0f;
    private float lastGCTime = 0f;
    
    // 统计数据
    private int warningCount = 0;
    private int criticalCount = 0;
    private int autoAdjustmentCount = 0;
    
    public enum PerformanceLevel
    {
        Good,
        Warning,
        Critical
    }
    
    public event Action<PerformanceLevel> OnPerformanceLevelChanged;
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        Debug.Log("🔧 Performance Monitor initialized");
        
        // 开始性能监控协程
        StartCoroutine(PerformanceCheckCoroutine());
    }
    
    void Update()
    {
        UpdateFPSCalculation();
    }
    
    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        if (dashboard == null)
            dashboard = FindObjectOfType<DataVisualizationDashboard>();
            
        if (voiceAnalyzer == null)
            voiceAnalyzer = FindObjectOfType<VoiceEmotionAnalyzer>();
            
        if (poseAnalyzer == null)
            poseAnalyzer = FindObjectOfType<PoseEmotionAnalyzer>();
            
        if (emotionSystem == null)
            emotionSystem = FindObjectOfType<EmotionDetectionSystem>();
    }
    
    /// <summary>
    /// 更新FPS计算
    /// </summary>
    private void UpdateFPSCalculation()
    {
        frameCount++;
        frameTimer += Time.unscaledDeltaTime;
        
        if (frameTimer >= 1.0f)
        {
            currentFPS = frameCount / frameTimer;
            fpsHistory.Add(currentFPS);
            
            // 保持FPS历史长度
            if (fpsHistory.Count > 30)
            {
                fpsHistory.RemoveAt(0);
            }
            
            // 计算平均FPS
            if (fpsHistory.Count > 0)
            {
                averageFPS = 0f;
                foreach (float fps in fpsHistory)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsHistory.Count;
            }
            
            frameCount = 0;
            frameTimer = 0f;
        }
    }
    
    /// <summary>
    /// 性能检查协程
    /// </summary>
    private IEnumerator PerformanceCheckCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            CheckPerformance();
            
            if (enableAutoManagement)
            {
                HandlePerformanceIssues();
            }
        }
    }
    
    /// <summary>
    /// 检查性能状态
    /// </summary>
    private void CheckPerformance()
    {
        // 更新内存使用
        currentMemoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        
        PerformanceLevel newLevel = EvaluatePerformanceLevel();
        
        if (newLevel != currentPerformanceLevel)
        {
            currentPerformanceLevel = newLevel;
            OnPerformanceLevelChanged?.Invoke(newLevel);
            
            if (showWarnings)
            {
                LogPerformanceChange(newLevel);
            }
        }
    }
    
    /// <summary>
    /// 评估性能等级
    /// </summary>
    private PerformanceLevel EvaluatePerformanceLevel()
    {
        bool fpsCritical = averageFPS < criticalFPSThreshold;
        bool fpsWarning = averageFPS < warningFPSThreshold;
        bool memoryCritical = currentMemoryMB > memoryCriticalThreshold;
        bool memoryWarning = currentMemoryMB > memoryWarningThreshold;
        
        if (fpsCritical || memoryCritical)
        {
            criticalCount++;
            return PerformanceLevel.Critical;
        }
        else if (fpsWarning || memoryWarning)
        {
            warningCount++;
            return PerformanceLevel.Warning;
        }
        
        return PerformanceLevel.Good;
    }
    
    /// <summary>
    /// 处理性能问题
    /// </summary>
    private void HandlePerformanceIssues()
    {
        switch (currentPerformanceLevel)
        {
            case PerformanceLevel.Warning:
                HandleWarningLevel();
                break;
                
            case PerformanceLevel.Critical:
                HandleCriticalLevel();
                break;
                
            case PerformanceLevel.Good:
                HandleGoodLevel();
                break;
        }
    }
    
    /// <summary>
    /// 处理警告级别性能
    /// </summary>
    private void HandleWarningLevel()
    {
        // 减少更新频率
        if (dashboard != null)
        {
            dashboard.guiUpdateInterval = 1.0f;
            dashboard.updateInterval = 0.5f;
        }
        
        // 强制垃圾回收
        if (forceGCOnHighMemory && currentMemoryMB > memoryWarningThreshold)
        {
            if (Time.time - lastGCTime > 10f)
            {
                System.GC.Collect();
                lastGCTime = Time.time;
                
                if (showWarnings)
                    Debug.Log("🔧 Forced garbage collection due to high memory usage");
            }
        }
        
        autoAdjustmentCount++;
    }
    
    /// <summary>
    /// 处理危险级别性能
    /// </summary>
    private void HandleCriticalLevel()
    {
        if (!autoDisableFeatures) return;
        
        // 禁用非关键功能
        if (!featuresDisabled)
        {
            if (dashboard != null)
            {
                dashboard.showDashboard = false;
                dashboard.autoDisableOnLowFPS = true;
            }
            
            if (voiceAnalyzer != null)
            {
                voiceAnalyzer.showVoiceUI = false;
                voiceAnalyzer.showAnalysisLogs = false;
            }
            
            if (poseAnalyzer != null)
            {
                poseAnalyzer.showPoseUI = false;
                poseAnalyzer.showAnalysisLogs = false;
                poseAnalyzer.frameSkipping = 4; // 增加跳帧
            }
            
            featuresDisabled = true;
            autoAdjustmentCount++;
            
            if (showWarnings)
            {
                Debug.LogWarning("🔧 Critical performance detected! Disabled non-essential features to prevent crashes");
            }
        }
        
        // 强制垃圾回收
        if (Time.time - lastGCTime > 5f)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            lastGCTime = Time.time;
        }
    }
    
    /// <summary>
    /// 处理良好级别性能
    /// </summary>
    private void HandleGoodLevel()
    {
        // 恢复功能
        if (featuresDisabled)
        {
            if (dashboard != null)
            {
                dashboard.guiUpdateInterval = 0.5f;
                dashboard.updateInterval = 0.2f;
            }
            
            if (voiceAnalyzer != null)
            {
                voiceAnalyzer.showVoiceUI = true; // 恢复语音UI
                voiceAnalyzer.showAnalysisLogs = false; // 保持日志关闭以节省性能
            }
            
            if (poseAnalyzer != null)
            {
                poseAnalyzer.frameSkipping = 2; // 恢复正常跳帧
                poseAnalyzer.showPoseUI = false; // 姿态UI暂时保持关闭
            }
            
            featuresDisabled = false;
            
            if (showWarnings)
            {
                Debug.Log("🔧 Performance recovered! Re-enabled features");
            }
        }
    }
    
    /// <summary>
    /// 记录性能变化
    /// </summary>
    private void LogPerformanceChange(PerformanceLevel level)
    {
        string message = $"🔧 Performance level changed to {level}: " +
                        $"FPS={averageFPS:F1}, Memory={currentMemoryMB:F1}MB";
        
        switch (level)
        {
            case PerformanceLevel.Warning:
                Debug.LogWarning(message);
                break;
            case PerformanceLevel.Critical:
                Debug.LogError(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
    }
    
    /// <summary>
    /// 手动触发垃圾回收
    /// </summary>
    public void ForceGarbageCollection()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        Debug.Log("🔧 Manual garbage collection completed");
    }
    
    /// <summary>
    /// 获取性能统计
    /// </summary>
    public (float fps, float memory, PerformanceLevel level, int warnings, int criticals, int adjustments) GetPerformanceStats()
    {
        return (averageFPS, currentMemoryMB, currentPerformanceLevel, warningCount, criticalCount, autoAdjustmentCount);
    }
    
    /// <summary>
    /// 重置统计数据
    /// </summary>
    public void ResetStats()
    {
        warningCount = 0;
        criticalCount = 0;
        autoAdjustmentCount = 0;
        fpsHistory.Clear();
        
        Debug.Log("🔧 Performance statistics reset");
    }
    
    /// <summary>
    /// GUI显示
    /// </summary>
    void OnGUI()
    {
        if (!showPerformanceGUI) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleLeft;
        
        Color bgColor = Color.green;
        switch (currentPerformanceLevel)
        {
            case PerformanceLevel.Warning:
                bgColor = Color.yellow;
                break;
            case PerformanceLevel.Critical:
                bgColor = Color.red;
                break;
        }
        
        GUI.backgroundColor = bgColor;
        GUI.Box(new Rect(10, 10, 200, 80), $"Performance Monitor\nFPS: {averageFPS:F1}\nMemory: {currentMemoryMB:F0}MB\nLevel: {currentPerformanceLevel}", style);
        GUI.backgroundColor = Color.white;
        
        // 手动垃圾回收按钮
        if (GUI.Button(new Rect(10, 100, 100, 25), "Force GC"))
        {
            ForceGarbageCollection();
        }
        
        // 重置统计按钮
        if (GUI.Button(new Rect(115, 100, 100, 25), "Reset Stats"))
        {
            ResetStats();
        }
    }
    
    void OnDestroy()
    {
        StopAllCoroutines();
        
        if (showWarnings)
        {
            Debug.Log($"🔧 Performance Monitor shutdown. Final stats: " +
                     $"Warnings={warningCount}, Criticals={criticalCount}, Adjustments={autoAdjustmentCount}");
        }
    }
} 