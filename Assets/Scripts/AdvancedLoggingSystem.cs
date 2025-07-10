using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using SimpleJSON;

/// <summary>
/// Advanced Logging System - Comprehensive user behavior data recording and analysis system
/// </summary>
public class AdvancedLoggingSystem : MonoBehaviour
{
    [Header("=== System Configuration ===")]
    [Tooltip("Enable logging")]
    public bool enableLogging = true;
    
    [Tooltip("Log file save path")]
    public string logFilePath = "UserBehaviorLogs";
    
    [Tooltip("Real-time write to file")]
    public bool realTimeWrite = false;
    
    [Tooltip("Buffer size for batch writing")]
    [Range(10, 1000)]
    public int bufferSize = 100;
    
    [Header("=== Log Types ===")]
    [Tooltip("Log emotion events")]
    public bool logEmotionEvents = true;
    
    [Tooltip("Log voice events")]
    public bool logVoiceEvents = true;
    
    [Tooltip("Log pose events")]
    public bool logPoseEvents = true;
    
    [Tooltip("Log game events")]
    public bool logGameEvents = true;
    
    [Tooltip("Log performance data")]
    public bool logPerformanceData = true;
    
    [Header("=== Analysis Configuration ===")]
    [Tooltip("Analysis interval (seconds)")]
    public float analysisInterval = 30f;
    
    [Tooltip("Data retention period (days)")]
    [Range(1, 365)]
    public int dataRetentionDays = 30;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show debug logs")]
    public bool showDebugLogs = false;
    
    [Tooltip("Display statistics UI")]
    public bool showStatsUI = false;
    
    // 日志事件类型枚举
    public enum LogEventType
    {
        Emotion,        // 情感事件
        Voice,          // 语音事件
        Pose,           // 姿态事件
        GameAction,     // 游戏行为
        SystemEvent,    // 系统事件
        Performance,    // 性能数据
        Error,          // 错误事件
        Debug           // 调试信息
    }
    
    // 日志条目数据结构
    [System.Serializable]
    public class LogEntry
    {
        public DateTime timestamp;
        public LogEventType eventType;
        public string eventName;
        public string eventData;
        public float sessionTime;
        public string userId;
        public Dictionary<string, object> customData;
        
        public LogEntry()
        {
            timestamp = DateTime.Now;
            customData = new Dictionary<string, object>();
        }
        
        public LogEntry(LogEventType type, string name, string data = "") : this()
        {
            eventType = type;
            eventName = name;
            eventData = data;
        }
        
        public string ToJson()
        {
            var json = new StringBuilder();
            json.Append("{");
            json.Append($"\"timestamp\":\"{timestamp:yyyy-MM-dd HH:mm:ss.fff}\",");
            json.Append($"\"eventType\":\"{eventType}\",");
            json.Append($"\"eventName\":\"{eventName}\",");
            json.Append($"\"eventData\":\"{EscapeJsonString(eventData)}\",");
            json.Append($"\"sessionTime\":{sessionTime:F2},");
            json.Append($"\"userId\":\"{userId}\"");
            
            if (customData != null && customData.Count > 0)
            {
                json.Append(",\"customData\":{");
                var customEntries = customData.Select(kvp => $"\"{kvp.Key}\":\"{kvp.Value}\"");
                json.Append(string.Join(",", customEntries));
                json.Append("}");
            }
            
            json.Append("}");
            return json.ToString();
        }
        
        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
    
    // 统计数据结构
    [System.Serializable]
    public class SessionStats
    {
        public float totalSessionTime;
        public int totalEmotionEvents;
        public int totalVoiceCommands;
        public int totalPoseEvents;
        public int totalGameActions;
        public Dictionary<string, int> emotionFrequency;
        public Dictionary<string, int> voiceCommandFrequency;
        public Dictionary<string, float> performanceMetrics;
        public float averageEmotionIntensity;
        public float averageConfidence;
        public DateTime sessionStartTime;
        public DateTime lastUpdateTime;
        
        public SessionStats()
        {
            emotionFrequency = new Dictionary<string, int>();
            voiceCommandFrequency = new Dictionary<string, int>();
            performanceMetrics = new Dictionary<string, float>();
            sessionStartTime = DateTime.Now;
            lastUpdateTime = DateTime.Now;
        }
    }
    
    // 内部变量
    private List<LogEntry> logBuffer = new List<LogEntry>();
    private SessionStats currentSessionStats = new SessionStats();
    private float sessionStartTime;
    private string currentUserId;
    private string logFileName;
    private float lastAnalysisTime;
    
    // 文件I/O相关
    private StringBuilder csvBuffer = new StringBuilder();
    private bool isWritingToFile = false;
    
    // 事件委托
    public Action<LogEntry> OnLogEntryAdded;
    public Action<SessionStats> OnStatsUpdated;
    
    void Awake()
    {
        InitializeLoggingSystem();
    }
    
    void Start()
    {
        StartNewSession();
        
        if (showDebugLogs)
            Debug.Log("📊 Advanced Logging System initialized");
    }
    
    void Update()
    {
        if (!enableLogging) return;
        
        // 更新会话时间
        currentSessionStats.totalSessionTime = Time.time - sessionStartTime;
        
        // 定期执行统计分析
        if (Time.time - lastAnalysisTime >= analysisInterval)
        {
            PerformAnalysis();
            lastAnalysisTime = Time.time;
        }
        
        // 定期写入文件（如果缓冲区满了或者启用了实时写入）
        if (logBuffer.Count >= bufferSize || (realTimeWrite && logBuffer.Count > 0))
        {
            WriteBufferToFile();
        }
        
        // 记录性能数据
        if (logPerformanceData && Time.frameCount % 60 == 0) // 每秒记录一次
        {
            LogPerformanceData();
        }
    }
    
    /// <summary>
    /// 初始化日志系统
    /// </summary>
    private void InitializeLoggingSystem()
    {
        sessionStartTime = Time.time;
        currentUserId = SystemInfo.deviceUniqueIdentifier;
        
        // 创建日志文件名（基于当前时间）
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logFileName = $"UserBehavior_{timestamp}.json";
        
        // 确保日志目录存在
        string logDirectory = Path.Combine(Application.persistentDataPath, logFilePath);
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        
        // 清理过期的日志文件
        CleanupOldLogFiles();
    }
    
    /// <summary>
    /// 开始新的会话
    /// </summary>
    private void StartNewSession()
    {
        currentSessionStats = new SessionStats();
        sessionStartTime = Time.time;
        
        // 记录会话开始事件
        LogGameEvent("SessionStart", "New user session started");
        
        if (showDebugLogs)
            Debug.Log($"📊 Starting new user session: {currentUserId}");
    }
    
    /// <summary>
    /// 记录情感事件
    /// </summary>
    public void LogEmotionEvent(EmotionDetectionSystem.EmotionData emotionData)
    {
        if (!enableLogging || !logEmotionEvents) return;
        
        var logEntry = new LogEntry(LogEventType.Emotion, "EmotionStateChange");
        logEntry.eventData = emotionData.ToString();
        logEntry.customData["primaryEmotion"] = emotionData.primaryEmotion.ToString();
        logEntry.customData["confidence"] = emotionData.confidence;
        logEntry.customData["arousal"] = emotionData.arousal;
        logEntry.customData["valence"] = emotionData.valence;
        logEntry.customData["intensity"] = emotionData.intensity;
        logEntry.customData["voiceWeight"] = emotionData.voiceWeight;
        logEntry.customData["poseWeight"] = emotionData.poseWeight;
        
        AddLogEntry(logEntry);
        
        // 更新统计
        currentSessionStats.totalEmotionEvents++;
        string emotionKey = emotionData.primaryEmotion.ToString();
        if (currentSessionStats.emotionFrequency.ContainsKey(emotionKey))
            currentSessionStats.emotionFrequency[emotionKey]++;
        else
            currentSessionStats.emotionFrequency[emotionKey] = 1;
            
        // 更新平均情感强度
        float totalIntensity = currentSessionStats.averageEmotionIntensity * (currentSessionStats.totalEmotionEvents - 1);
        currentSessionStats.averageEmotionIntensity = (totalIntensity + emotionData.intensity) / currentSessionStats.totalEmotionEvents;
        
        // 更新平均置信度
        float totalConfidence = currentSessionStats.averageConfidence * (currentSessionStats.totalEmotionEvents - 1);
        currentSessionStats.averageConfidence = (totalConfidence + emotionData.confidence) / currentSessionStats.totalEmotionEvents;
    }
    
    /// <summary>
    /// 记录语音事件
    /// </summary>
    public void LogVoiceEvent(string command, string recognizedText, float confidence = 0f)
    {
        if (!enableLogging || !logVoiceEvents) return;
        
        var logEntry = new LogEntry(LogEventType.Voice, "VoiceCommand");
        logEntry.eventData = $"Command: {command}, Text: {recognizedText}";
        logEntry.customData["command"] = command;
        logEntry.customData["recognizedText"] = recognizedText;
        logEntry.customData["confidence"] = confidence;
        
        AddLogEntry(logEntry);
        
        // 更新统计
        currentSessionStats.totalVoiceCommands++;
        if (currentSessionStats.voiceCommandFrequency.ContainsKey(command))
            currentSessionStats.voiceCommandFrequency[command]++;
        else
            currentSessionStats.voiceCommandFrequency[command] = 1;
    }
    
    /// <summary>
    /// 记录姿态事件
    /// </summary>
    public void LogPoseEvent(string eventName, Dictionary<string, float> poseFeatures)
    {
        if (!enableLogging || !logPoseEvents) return;
        
        var logEntry = new LogEntry(LogEventType.Pose, eventName);
        
        if (poseFeatures != null)
        {
            foreach (var feature in poseFeatures)
            {
                logEntry.customData[feature.Key] = feature.Value;
            }
            
            logEntry.eventData = string.Join(", ", poseFeatures.Select(f => $"{f.Key}: {f.Value:F2}"));
        }
        
        AddLogEntry(logEntry);
        currentSessionStats.totalPoseEvents++;
    }
    
    /// <summary>
    /// 记录游戏事件
    /// </summary>
    public void LogGameEvent(string eventName, string eventData = "", Dictionary<string, object> customData = null)
    {
        if (!enableLogging || !logGameEvents) return;
        
        var logEntry = new LogEntry(LogEventType.GameAction, eventName, eventData);
        
        if (customData != null)
        {
            foreach (var data in customData)
            {
                logEntry.customData[data.Key] = data.Value;
            }
        }
        
        AddLogEntry(logEntry);
        currentSessionStats.totalGameActions++;
        
        if (showDebugLogs)
            Debug.Log($"📊 Game event: {eventName} - {eventData}");
    }
    
    /// <summary>
    /// 记录系统事件
    /// </summary>
    public void LogSystemEvent(string eventName, string eventData = "")
    {
        if (!enableLogging) return;
        
        var logEntry = new LogEntry(LogEventType.SystemEvent, eventName, eventData);
        AddLogEntry(logEntry);
    }
    
    /// <summary>
    /// 记录错误事件
    /// </summary>
    public void LogError(string errorMessage, string stackTrace = "")
    {
        if (!enableLogging) return;
        
        var logEntry = new LogEntry(LogEventType.Error, "Error", errorMessage);
        logEntry.customData["stackTrace"] = stackTrace;
        AddLogEntry(logEntry);
        
        if (showDebugLogs)
            Debug.LogError($"📊 Error recorded: {errorMessage}");
    }
    
    /// <summary>
    /// 记录性能数据
    /// </summary>
    private void LogPerformanceData()
    {
        if (!enableLogging || !logPerformanceData) return;
        
        var logEntry = new LogEntry(LogEventType.Performance, "PerformanceSnapshot");
        
        // 收集性能指标
        float fps = 1f / Time.unscaledDeltaTime;
        float memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB
        
        logEntry.customData["fps"] = fps;
        logEntry.customData["memoryUsage"] = memoryUsage;
        logEntry.customData["frameTime"] = Time.unscaledDeltaTime * 1000f; // ms
        
        logEntry.eventData = $"FPS: {fps:F1}, Memory: {memoryUsage:F1}MB";
        
        AddLogEntry(logEntry);
        
        // 更新性能统计
        currentSessionStats.performanceMetrics["averageFPS"] = CalculateRunningAverage("fps", fps);
        currentSessionStats.performanceMetrics["averageMemory"] = CalculateRunningAverage("memory", memoryUsage);
    }
    
    /// <summary>
    /// 添加日志条目
    /// </summary>
    private void AddLogEntry(LogEntry entry)
    {
        entry.sessionTime = Time.time - sessionStartTime;
        entry.userId = currentUserId;
        
        logBuffer.Add(entry);
        
        // 触发事件
        OnLogEntryAdded?.Invoke(entry);
        
        if (showDebugLogs && entry.eventType != LogEventType.Performance) // 不显示性能日志以避免spam
        {
            Debug.Log($"📊 Log: [{entry.eventType}] {entry.eventName} - {entry.eventData}");
        }
    }
    
    /// <summary>
    /// 写入缓冲区到文件
    /// </summary>
    private void WriteBufferToFile()
    {
        if (isWritingToFile || logBuffer.Count == 0) return;
        
        isWritingToFile = true;
        
        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, logFilePath, logFileName);
            
            using (StreamWriter writer = new StreamWriter(fullPath, true, Encoding.UTF8))
            {
                foreach (var entry in logBuffer)
                {
                    writer.WriteLine(entry.ToJson());
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"📊 {logBuffer.Count} log entries written to file");
        }
        catch (Exception e)
        {
            Debug.LogError($"📊 Failed to write log file: {e.Message}");
        }
        finally
        {
            logBuffer.Clear();
            isWritingToFile = false;
        }
    }
    
    /// <summary>
    /// 执行统计分析
    /// </summary>
    private void PerformAnalysis()
    {
        currentSessionStats.lastUpdateTime = DateTime.Now;
        
        // 触发统计更新事件
        OnStatsUpdated?.Invoke(currentSessionStats);
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 Session Stats - Duration: {currentSessionStats.totalSessionTime:F0}s, " +
                     $"Emotion Events: {currentSessionStats.totalEmotionEvents}, " +
                     $"Voice Commands: {currentSessionStats.totalVoiceCommands}, " +
                     $"Game Actions: {currentSessionStats.totalGameActions}");
        }
    }
    
    /// <summary>
    /// 计算运行平均值
    /// </summary>
    private float CalculateRunningAverage(string key, float newValue)
    {
        string countKey = key + "_count";
        string sumKey = key + "_sum";
        
        float count = currentSessionStats.performanceMetrics.ContainsKey(countKey) ? 
                     currentSessionStats.performanceMetrics[countKey] : 0f;
        float sum = currentSessionStats.performanceMetrics.ContainsKey(sumKey) ? 
                   currentSessionStats.performanceMetrics[sumKey] : 0f;
        
        count++;
        sum += newValue;
        
        currentSessionStats.performanceMetrics[countKey] = count;
        currentSessionStats.performanceMetrics[sumKey] = sum;
        
        return sum / count;
    }
    
    /// <summary>
    /// 清理过期的日志文件
    /// </summary>
    private void CleanupOldLogFiles()
    {
        try
        {
            string logDirectory = Path.Combine(Application.persistentDataPath, logFilePath);
            if (!Directory.Exists(logDirectory)) return;
            
            DateTime cutoffDate = DateTime.Now.AddDays(-dataRetentionDays);
            string[] logFiles = Directory.GetFiles(logDirectory, "*.json");
            
            foreach (string file in logFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                    if (showDebugLogs)
                        Debug.Log($"📊 Deleted expired log file: {fileInfo.Name}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"📊 Error cleaning up log files: {e.Message}");
        }
    }
    
    // === Public Interface ===
    
    /// <summary>
    /// 获取当前会话统计
    /// </summary>
    public SessionStats GetCurrentSessionStats()
    {
        return currentSessionStats;
    }
    
    /// <summary>
    /// 导出日志数据
    /// </summary>
    public string ExportLogData(DateTime startDate, DateTime endDate)
    {
        // 这里可以实现更复杂的日志导出逻辑
        WriteBufferToFile(); // 确保缓冲区数据已写入
        
        string logDirectory = Path.Combine(Application.persistentDataPath, logFilePath);
        string exportPath = Path.Combine(logDirectory, $"Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
        
        // 实际实现中可以根据日期范围过滤和合并日志文件
        return exportPath;
    }
    
    /// <summary>
    /// 强制写入所有缓冲数据
    /// </summary>
    public void FlushAllData()
    {
        WriteBufferToFile();
        PerformAnalysis();
        
        if (showDebugLogs)
            Debug.Log("📊 Flushed all log data");
    }
    
    /// <summary>
    /// 简单的UI显示
    /// </summary>
    void OnGUI()
    {
        if (!showStatsUI) return;
        
        GUI.Box(new Rect(10, 140, 300, 200), "Session Stats");
        
        float y = 165;
        GUI.Label(new Rect(20, y, 280, 20), $"Session Duration: {currentSessionStats.totalSessionTime:F0} seconds");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Emotion Events: {currentSessionStats.totalEmotionEvents}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Voice Commands: {currentSessionStats.totalVoiceCommands}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Pose Events: {currentSessionStats.totalPoseEvents}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Game Actions: {currentSessionStats.totalGameActions}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Average Emotion Intensity: {currentSessionStats.averageEmotionIntensity:F2}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Average Confidence: {currentSessionStats.averageConfidence:F2}");
        y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Buffer Entries: {logBuffer.Count}");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            LogSystemEvent("ApplicationPause", "Application paused");
            FlushAllData();
        }
        else
        {
            LogSystemEvent("ApplicationResume", "Application resumed");
        }
    }
    
    void OnApplicationQuit()
    {
        LogSystemEvent("ApplicationQuit", "Application closing");
        FlushAllData();
    }
    
    void OnDestroy()
    {
        FlushAllData();
    }
} 