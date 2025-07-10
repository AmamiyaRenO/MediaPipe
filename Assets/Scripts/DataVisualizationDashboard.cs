using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Data Visualization Dashboard - Real-time visualization panel for emotion analysis data
/// </summary>
public class DataVisualizationDashboard : MonoBehaviour
{
    [Header("=== Panel Configuration ===")]
    [Tooltip("Show dashboard panel")]
    public bool showDashboard = false;
    
    [Tooltip("Panel position")]
    public Vector2 dashboardPosition = new Vector2(10, 10);
    
    [Tooltip("Panel size")]
    public Vector2 dashboardSize = new Vector2(400, 600);
    
    [Tooltip("Update frequency (seconds)")]
    public float updateInterval = 0.2f;
    
    [Header("=== Display Options ===")]
    [Tooltip("Show emotion analysis")]
    public bool showEmotionAnalysis = true;
    
    [Tooltip("Show voice statistics")]
    public bool showVoiceStats = true;
    
    [Tooltip("Show pose analysis")]
    public bool showPoseAnalysis = true;
    
    [Tooltip("Show performance monitor")]
    public bool showPerformanceMonitor = true;
    
    [Tooltip("Show session statistics")]
    public bool showSessionStats = true;
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("Emotion detection system")]
    public EmotionDetectionSystem emotionSystem;
    
    [Tooltip("Voice emotion analyzer")]
    public VoiceEmotionAnalyzer voiceAnalyzer;
    
    [Tooltip("Pose emotion analyzer")]
    public PoseEmotionAnalyzer poseAnalyzer;
    
    [Tooltip("Advanced logging system")]
    public AdvancedLoggingSystem loggingSystem;
    
    [Header("=== Chart Configuration ===")]
    [Tooltip("Emotion history display length")]
    public int emotionHistoryLength = 15; // 减少历史长度
    
    [Tooltip("Performance chart display length")]
    public int performanceHistoryLength = 20; // 减少历史长度
    
    [Header("=== Performance Settings ===")]
    [Tooltip("GUI update frequency (seconds)")]
    public float guiUpdateInterval = 0.5f; // 新增：减少GUI更新频率
    
    [Tooltip("Auto-disable when FPS too low")]
    public bool autoDisableOnLowFPS = true;
    
    [Tooltip("FPS threshold for auto-disable")]
    public float lowFPSThreshold = 30f;
    
    // UI样式
    private GUIStyle titleStyle, headerStyle, dataStyle, chartStyle;
    private bool stylesInitialized = false;
    
    // 数据缓存
    private List<float> emotionArousals = new List<float>();
    private List<float> emotionValences = new List<float>();
    private List<float> emotionIntensities = new List<float>();
    private List<float> fpsHistory = new List<float>();
    private List<float> memoryHistory = new List<float>();
    
    // 内部状态
    private float lastUpdateTime = 0f;
    private float lastGUIUpdateTime = 0f; // 新增：GUI更新时间
    private Vector2 scrollPosition = Vector2.zero;
    private Rect dashboardRect;
    private bool shouldUpdateGUI = true; // 新增：GUI更新标志
    
    // 颜色定义
    private Color positiveColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    private Color negativeColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    private Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    private Color arousalColor = new Color(1f, 0.5f, 0f, 0.8f);
    private Color chartBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    
    // 纹理缓存 - 新增：避免重复创建纹理
    private Texture2D cachedBackgroundTexture;
    private Dictionary<Color, Texture2D> colorTextureCache = new Dictionary<Color, Texture2D>();
    
    // 性能监控 - 新增
    private float currentFPS = 60f;
    private int frameCount = 0;
    private float frameTimer = 0f;

    void Awake()
    {
        InitializeComponents();
        dashboardRect = new Rect(dashboardPosition.x, dashboardPosition.y, dashboardSize.x, dashboardSize.y);
    }

    void Start()
    {
        // 订阅事件
        SubscribeToEvents();
        
        Debug.Log("📊 Data Visualization Dashboard initialized with performance optimizations");
    }

    void Update()
    {
        if (!showDashboard) return;
        
        // 性能监控
        UpdatePerformanceMetrics();
        
        // 检查是否需要自动禁用
        if (autoDisableOnLowFPS && currentFPS < lowFPSThreshold)
        {
            showDashboard = false;
            Debug.LogWarning($"📊 Dashboard auto-disabled due to low FPS ({currentFPS:F1})");
            return;
        }
        
        // 定期更新数据
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDataCache();
            lastUpdateTime = Time.time;
        }
        
        // 控制GUI更新频率
        if (Time.time - lastGUIUpdateTime >= guiUpdateInterval)
        {
            shouldUpdateGUI = true;
            lastGUIUpdateTime = Time.time;
        }
        
        // 处理键盘快捷键
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDashboard = !showDashboard;
        }
    }
    
    /// <summary>
    /// 更新性能指标
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        frameCount++;
        frameTimer += Time.unscaledDeltaTime;
        
        if (frameTimer >= 1.0f)
        {
            currentFPS = frameCount / frameTimer;
            frameCount = 0;
            frameTimer = 0f;
        }
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        if (emotionSystem == null)
            emotionSystem = FindObjectOfType<EmotionDetectionSystem>();
            
        if (voiceAnalyzer == null)
            voiceAnalyzer = FindObjectOfType<VoiceEmotionAnalyzer>();
            
        if (poseAnalyzer == null)
            poseAnalyzer = FindObjectOfType<PoseEmotionAnalyzer>();
            
        if (loggingSystem == null)
            loggingSystem = FindObjectOfType<AdvancedLoggingSystem>();
    }

    /// <summary>
    /// 初始化UI样式 - 只能在OnGUI中调用
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        try
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.cyan;
            
            dataStyle = new GUIStyle(GUI.skin.label);
            dataStyle.fontSize = 12;
            dataStyle.normal.textColor = Color.white;
            
            chartStyle = new GUIStyle(GUI.skin.box);
            if (cachedBackgroundTexture == null)
            {
                cachedBackgroundTexture = MakeTex(2, 2, chartBgColor);
            }
            chartStyle.normal.background = cachedBackgroundTexture;
            
            stylesInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"📊 Failed to initialize GUI styles: {e.Message}");
            // 创建基本样式作为后备
            CreateFallbackStyles();
        }
    }
    
    /// <summary>
    /// 创建后备样式（无需GUI.skin）
    /// </summary>
    private void CreateFallbackStyles()
    {
        if (stylesInitialized) return;
        
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;
        
        dataStyle = new GUIStyle();
        dataStyle.fontSize = 12;
        dataStyle.normal.textColor = Color.white;
        
        chartStyle = new GUIStyle();
        if (cachedBackgroundTexture == null)
        {
            cachedBackgroundTexture = MakeTex(2, 2, chartBgColor);
        }
        chartStyle.normal.background = cachedBackgroundTexture;
        
        stylesInitialized = true;
    }
    
    /// <summary>
    /// 安全的GUILayout.Label辅助方法
    /// </summary>
    private void SafeLabel(string text, GUIStyle style = null, params GUILayoutOption[] options)
    {
        GUIStyle safeStyle = style ?? GUI.skin.label;
        if (options != null && options.Length > 0)
        {
            GUILayout.Label(text, safeStyle, options);
        }
        else
        {
            GUILayout.Label(text, safeStyle);
        }
    }

    /// <summary>
    /// 订阅系统事件
    /// </summary>
    private void SubscribeToEvents()
    {
        if (emotionSystem != null)
        {
            emotionSystem.OnEmotionChanged += OnEmotionChanged;
        }
        
        if (loggingSystem != null)
        {
            loggingSystem.OnStatsUpdated += OnStatsUpdated;
        }
    }

    /// <summary>
    /// 更新数据缓存
    /// </summary>
    private void UpdateDataCache()
    {
        // 更新情感数据
        if (emotionSystem != null && emotionSystem.CurrentEmotion != null)
        {
            var emotion = emotionSystem.CurrentEmotion;
            emotionArousals.Add(emotion.arousal);
            emotionValences.Add(emotion.valence);
            emotionIntensities.Add(emotion.intensity);
            
            // 保持历史长度限制 - 更严格的限制
            while (emotionArousals.Count > emotionHistoryLength)
            {
                emotionArousals.RemoveAt(0);
                emotionValences.RemoveAt(0);
                emotionIntensities.RemoveAt(0);
            }
        }
        
        // 更新性能数据 - 使用缓存的FPS而不是实时计算
        float memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        
        fpsHistory.Add(currentFPS);
        memoryHistory.Add(memoryUsage);
        
        while (fpsHistory.Count > performanceHistoryLength)
        {
            fpsHistory.RemoveAt(0);
            memoryHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 情感状态变化回调
    /// </summary>
    private void OnEmotionChanged(EmotionDetectionSystem.EmotionData newEmotion)
    {
        // 可以在这里添加特殊的处理逻辑
    }

    /// <summary>
    /// 统计更新回调
    /// </summary>
    private void OnStatsUpdated(AdvancedLoggingSystem.SessionStats stats)
    {
        // 可以在这里添加特殊的处理逻辑
    }

    /// <summary>
    /// GUI绘制主函数 - 优化版本
    /// </summary>
    void OnGUI()
    {
        if (!showDashboard || !shouldUpdateGUI) return;
        
        shouldUpdateGUI = false; // 重置更新标志
        
        // 确保在OnGUI中初始化样式
        if (!stylesInitialized)
        {
            InitializeStyles();
        }
        
        // 如果样式仍未初始化，使用后备方案
        if (!stylesInitialized)
        {
            CreateFallbackStyles();
        }
        
        // 绘制主面板背景
        GUI.Box(dashboardRect, "", GUI.skin.window);
        
        GUILayout.BeginArea(dashboardRect);
        
        // 标题栏
        GUILayout.BeginHorizontal();
        SafeLabel("Real-time Data Monitoring Panel", titleStyle);
        GUILayout.FlexibleSpace();
        
        // 显示当前FPS
        SafeLabel($"FPS: {currentFPS:F0}", dataStyle);
        
        if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
        {
            showDashboard = false;
        }
        GUILayout.EndHorizontal();
        
        // 滚动视图
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        // 各个面板
        if (showEmotionAnalysis)
            DrawEmotionPanel();
            
        if (showVoiceStats)
            DrawVoiceStatsPanel();
            
        if (showPoseAnalysis)
            DrawPoseAnalysisPanel();
            
        if (showPerformanceMonitor)
            DrawPerformancePanel();
            
        if (showSessionStats)
            DrawSessionStatsPanel();
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 绘制情感分析面板
    /// </summary>
    private void DrawEmotionPanel()
    {
        GUILayout.Space(10);
        GUILayout.Label("🧠 Emotion Analysis", headerStyle);
        
        if (emotionSystem != null && emotionSystem.CurrentEmotion != null)
        {
            var emotion = emotionSystem.CurrentEmotion;
            
            // 当前情感状态
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current Emotion: {GetEmotionDisplayName(emotion.primaryEmotion)}", dataStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Confidence: {emotion.confidence:F2}", dataStyle);
            GUILayout.EndHorizontal();
            
            // 情感维度显示
            DrawProgressBar("Arousal", emotion.arousal, arousalColor);
            DrawProgressBar("Valence", emotion.valence, emotion.valence > 0 ? positiveColor : negativeColor, true);
            DrawProgressBar("Intensity", emotion.intensity, neutralColor);
            
            // 权重信息
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Voice Weight: {emotion.voiceWeight:F2}", dataStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Pose Weight: {emotion.poseWeight:F2}", dataStyle);
            GUILayout.EndHorizontal();
            
            // 情感历史图表
            if (emotionArousals.Count > 1)
            {
                GUILayout.Space(5);
                GUILayout.Label("Emotion Trend Chart", dataStyle);
                DrawLineChart(emotionArousals, "Arousal", arousalColor, 120, 60);
                DrawLineChart(emotionValences, "Valence", neutralColor, 120, 60, true);
            }
        }
        else
        {
            GUILayout.Label("Waiting for emotion data...", dataStyle);
        }
    }
    
    /// <summary>
    /// 绘制语音统计面板
    /// </summary>
    private void DrawVoiceStatsPanel()
    {
        GUILayout.Space(10);
        GUILayout.Label("🎤 Voice Analysis", headerStyle);
        
        if (voiceAnalyzer != null && voiceAnalyzer.HasRecentData())
        {
            var (volume, pitch, speechRate) = voiceAnalyzer.GetCurrentAudioFeatures();
            var voiceEmotion = voiceAnalyzer.GetCurrentEmotion();
            
            GUILayout.Label($"Volume: {volume:F3}", dataStyle);
            GUILayout.Label($"Pitch: {pitch:F1} Hz", dataStyle);
            GUILayout.Label($"Speech Rate: {speechRate:F1} words/minute", dataStyle);
            
            GUILayout.Space(5);
            GUILayout.Label("Voice Emotion Analysis:", dataStyle);
            DrawProgressBar("Voice Arousal", voiceEmotion.arousal, arousalColor);
            DrawProgressBar("Voice Valence", voiceEmotion.valence, 
                           voiceEmotion.valence > 0 ? positiveColor : negativeColor, true);
        }
        else
        {
            GUILayout.Label("Waiting for voice data...", dataStyle);
        }
        
        // 语音指令统计
        if (loggingSystem != null)
        {
            var stats = loggingSystem.GetCurrentSessionStats();
            GUILayout.Space(5);
            GUILayout.Label($"Total Voice Commands: {stats.totalVoiceCommands}", dataStyle);
            
            if (stats.voiceCommandFrequency != null && stats.voiceCommandFrequency.Count > 0)
            {
                GUILayout.Label("Command Frequency:", dataStyle);
                foreach (var cmd in stats.voiceCommandFrequency.Take(5))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  {cmd.Key}", dataStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{cmd.Value}", dataStyle);
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
    
    /// <summary>
    /// 绘制姿态分析面板
    /// </summary>
    private void DrawPoseAnalysisPanel()
    {
        GUILayout.Space(10);
        GUILayout.Label("🤸 Pose Analysis", headerStyle);
        
        if (poseAnalyzer != null && poseAnalyzer.HasRecentData())
        {
            var (speed, openness, stability, tilt) = poseAnalyzer.GetCurrentPoseFeatures();
            var poseEmotion = poseAnalyzer.GetCurrentEmotion();
            
            GUILayout.Label($"Speed: {speed:F2}", dataStyle);
            GUILayout.Label($"Body Openness: {openness:F2}", dataStyle);
            GUILayout.Label($"Body Stability: {stability:F2}", dataStyle);
            GUILayout.Label($"Body Tilt: {tilt:F1}°", dataStyle);
            
            GUILayout.Space(5);
            GUILayout.Label("Pose Emotion Analysis:", dataStyle);
            DrawProgressBar("Pose Arousal", poseEmotion.arousal, arousalColor);
            DrawProgressBar("Pose Valence", poseEmotion.valence, 
                           poseEmotion.valence > 0 ? positiveColor : negativeColor, true);
        }
        else
        {
            GUILayout.Label("Waiting for pose data...", dataStyle);
        }
    }
    
    /// <summary>
    /// 绘制性能监控面板
    /// </summary>
    private void DrawPerformancePanel()
    {
        GUILayout.Space(10);
        GUILayout.Label("⚡ Performance Monitor", headerStyle);
        
        if (fpsHistory.Count > 0)
        {
            float currentFPS = fpsHistory.LastOrDefault();
            float avgFPS = fpsHistory.Average();
            float currentMemory = memoryHistory.LastOrDefault();
            
            GUILayout.Label($"Current FPS: {currentFPS:F1}", dataStyle);
            GUILayout.Label($"Average FPS: {avgFPS:F1}", dataStyle);
            GUILayout.Label($"Memory Usage: {currentMemory:F1} MB", dataStyle);
            
            // 性能图表
            if (fpsHistory.Count > 1)
            {
                GUILayout.Space(5);
                GUILayout.Label("FPS Trend", dataStyle);
                DrawLineChart(fpsHistory, "FPS", positiveColor, 120, 40);
            }
        }
    }
    
    /// <summary>
    /// 绘制会话统计面板
    /// </summary>
    private void DrawSessionStatsPanel()
    {
        GUILayout.Space(10);
        GUILayout.Label("📊 Session Statistics", headerStyle);
        
        if (loggingSystem != null)
        {
            var stats = loggingSystem.GetCurrentSessionStats();
            
            GUILayout.Label($"Session Duration: {stats.totalSessionTime:F0} seconds", dataStyle);
            GUILayout.Label($"Emotion Events: {stats.totalEmotionEvents}", dataStyle);
            GUILayout.Label($"Voice Commands: {stats.totalVoiceCommands}", dataStyle);
            GUILayout.Label($"Pose Events: {stats.totalPoseEvents}", dataStyle);
            GUILayout.Label($"Game Actions: {stats.totalGameActions}", dataStyle);
            
            GUILayout.Space(5);
            GUILayout.Label($"Average Emotion Intensity: {stats.averageEmotionIntensity:F2}", dataStyle);
            GUILayout.Label($"Average Confidence: {stats.averageConfidence:F2}", dataStyle);
            
            // 情感频率饼图（简化显示）
            if (stats.emotionFrequency != null && stats.emotionFrequency.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Emotion Distribution:", dataStyle);
                foreach (var emotion in stats.emotionFrequency.Take(5))
                {
                    float percentage = (float)emotion.Value / stats.totalEmotionEvents * 100f;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  {GetEmotionDisplayName(emotion.Key)}", dataStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{percentage:F1}%", dataStyle);
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
    
    /// <summary>
    /// 绘制进度条
    /// </summary>
    private void DrawProgressBar(string label, float value, Color color, bool centered = false)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, dataStyle ?? GUI.skin.label, GUILayout.Width(80));
        
        Rect barRect = GUILayoutUtility.GetRect(200, 16);
        GUI.Box(barRect, "", chartStyle ?? GUI.skin.box);
        
        if (centered)
        {
            // 居中显示（用于valence等有正负值的数据）
            float normalizedValue = (value + 1f) / 2f; // 转换到0-1范围
            float barWidth = barRect.width * Mathf.Abs(value);
            float barStart = value > 0 ? barRect.x + barRect.width * 0.5f : 
                                      barRect.x + barRect.width * 0.5f - barWidth;
            
            Rect fillRect = new Rect(barStart, barRect.y + 2, barWidth, barRect.height - 4);
            GUI.DrawTexture(fillRect, MakeTex(2, 2, color));
            
            // 中心线
            Rect centerLine = new Rect(barRect.x + barRect.width * 0.5f - 1, barRect.y, 2, barRect.height);
            GUI.DrawTexture(centerLine, MakeTex(2, 2, Color.white));
        }
        else
        {
            // 标准进度条
            float barWidth = barRect.width * Mathf.Clamp01(value);
            Rect fillRect = new Rect(barRect.x + 2, barRect.y + 2, barWidth - 4, barRect.height - 4);
            GUI.DrawTexture(fillRect, MakeTex(2, 2, color));
        }
        
        SafeLabel($"{value:F2}", dataStyle, GUILayout.Width(40));
        GUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 绘制简单的折线图
    /// </summary>
    private void DrawLineChart(List<float> data, string title, Color lineColor, int width, int height, bool centered = false)
    {
        if (data.Count < 2) return;
        
        Rect chartRect = GUILayoutUtility.GetRect(width, height);
        GUI.Box(chartRect, "", chartStyle);
        
        // 计算数据范围
        float minValue, maxValue;
        if (centered)
        {
            minValue = -1f;
            maxValue = 1f;
        }
        else
        {
            minValue = data.Min();
            maxValue = data.Max();
            if (Mathf.Approximately(minValue, maxValue))
            {
                minValue -= 0.1f;
                maxValue += 0.1f;
            }
        }
        
        // 绘制数据点连线
        Vector2 previousPoint = Vector2.zero;
        for (int i = 0; i < data.Count; i++)
        {
            float x = chartRect.x + (float)i / (data.Count - 1) * (chartRect.width - 4) + 2;
            float normalizedY = Mathf.InverseLerp(minValue, maxValue, data[i]);
            float y = chartRect.y + (1f - normalizedY) * (chartRect.height - 4) + 2;
            
            Vector2 currentPoint = new Vector2(x, y);
            
            if (i > 0)
            {
                DrawLine(previousPoint, currentPoint, lineColor);
            }
            
            previousPoint = currentPoint;
        }
        
        // 绘制中心线（如果是居中模式）
        if (centered)
        {
            float centerY = chartRect.y + chartRect.height * 0.5f;
            DrawLine(new Vector2(chartRect.x, centerY), 
                    new Vector2(chartRect.xMax, centerY), 
                    new Color(1f, 1f, 1f, 0.3f));
        }
    }
    
    /// <summary>
    /// 绘制线段
    /// </summary>
    private void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        
        Vector2[] points = new Vector2[4];
        points[0] = start + perpendicular;
        points[1] = start - perpendicular;
        points[2] = end - perpendicular;
        points[3] = end + perpendicular;
        
        // 简化的线段绘制（使用GUI.DrawTexture）
        Rect lineRect = new Rect(
            Mathf.Min(start.x, end.x) - 1,
            Mathf.Min(start.y, end.y) - 1,
            Mathf.Abs(end.x - start.x) + 2,
            Mathf.Abs(end.y - start.y) + 2
        );
        
        GUI.DrawTexture(lineRect, MakeTex(2, 2, color));
    }
    
    /// <summary>
    /// 创建纯色纹理 - 优化版本，使用缓存
    /// </summary>
    private Texture2D MakeTex(int width, int height, Color color)
    {
        // 使用颜色缓存避免重复创建
        if (colorTextureCache.ContainsKey(color))
        {
            return colorTextureCache[color];
        }
        
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = color;
            
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        
        // 限制缓存大小避免内存泄漏
        if (colorTextureCache.Count < 50)
        {
            colorTextureCache[color] = result;
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取情感状态的中文显示名称
    /// </summary>
    private string GetEmotionDisplayName(EmotionDetectionSystem.EmotionState emotion)
    {
        switch (emotion)
        {
            case EmotionDetectionSystem.EmotionState.Neutral: return "Neutral";
            case EmotionDetectionSystem.EmotionState.Happy: return "Happy";
            case EmotionDetectionSystem.EmotionState.Excited: return "Excited";
            case EmotionDetectionSystem.EmotionState.Frustrated: return "Frustrated";
            case EmotionDetectionSystem.EmotionState.Angry: return "Angry";
            case EmotionDetectionSystem.EmotionState.Calm: return "Calm";
            case EmotionDetectionSystem.EmotionState.Focused: return "Focused";
            case EmotionDetectionSystem.EmotionState.Stressed: return "Stressed";
            default: return emotion.ToString();
        }
    }
    
    /// <summary>
    /// 获取情感状态的中文显示名称（字符串版本）
    /// </summary>
    private string GetEmotionDisplayName(string emotionStr)
    {
        if (Enum.TryParse<EmotionDetectionSystem.EmotionState>(emotionStr, out var emotion))
        {
            return GetEmotionDisplayName(emotion);
        }
        return emotionStr;
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (emotionSystem != null)
        {
            emotionSystem.OnEmotionChanged -= OnEmotionChanged;
        }
        
        if (loggingSystem != null)
        {
            loggingSystem.OnStatsUpdated -= OnStatsUpdated;
        }
        
        // 清理纹理资源防止内存泄漏
        if (cachedBackgroundTexture != null)
        {
            DestroyImmediate(cachedBackgroundTexture);
            cachedBackgroundTexture = null;
        }
        
        foreach (var texture in colorTextureCache.Values)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }
        colorTextureCache.Clear();
        
        Debug.Log("📊 Data Visualization Dashboard resources cleaned up");
    }
} 