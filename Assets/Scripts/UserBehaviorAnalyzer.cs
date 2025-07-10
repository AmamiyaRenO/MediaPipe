using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// User Behavior Analyzer - Advanced user profile and behavior pattern detection system
/// </summary>
public class UserBehaviorAnalyzer : MonoBehaviour
{
    [Header("=== Analysis Configuration ===")]
    [Tooltip("Enable behavior analysis")]
    public bool enableBehaviorAnalysis = true;
    
    [Tooltip("Analysis interval (seconds)")]
    public float analysisInterval = 60f;
    
    [Tooltip("Learning curve window size")]
    public int learningCurveWindow = 10;
    
    [Header("=== Behavior Detection Thresholds ===")]
    [Tooltip("Active user threshold (commands/minute)")]
    [Range(0f, 10f)]
    public float activeUserThreshold = 3f;
    
    [Tooltip("Emotion stability threshold")]
    [Range(0f, 1f)]
    public float emotionStabilityThreshold = 0.7f;
    
    [Tooltip("Learning progress threshold")]
    [Range(0f, 1f)]
    public float learningProgressThreshold = 0.2f;
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("Emotion detection system")]
    public EmotionDetectionSystem emotionSystem;
    
    [Tooltip("Advanced logging system")]
    public AdvancedLoggingSystem loggingSystem;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show analysis logs")]
    public bool showAnalysisLogs = false;
    
    [Tooltip("Display behavior analysis on UI")]
    public bool showBehaviorUI = false;
    
    // 用户类型枚举
    public enum UserType
    {
        Beginner,       // 初学者
        Casual,         // 休闲用户
        Active,         // 活跃用户
        Expert,         // 专家用户
        Struggling      // 困难用户
    }
    
    // 游戏模式枚举
    public enum GameplayPattern
    {
        Exploratory,    // 探索型
        Goal_Oriented,  // 目标导向型
        Social,         // 社交型
        Competitive,    // 竞争型
        Relaxed         // 放松型
    }
    
    // 学习状态枚举
    public enum LearningState
    {
        Learning,       // 学习中
        Mastering,      // 熟练中
        Plateaued,      // 平台期
        Improving,      // 进步中
        Regressing      // 退步中
    }
    
    // 用户行为档案
    [System.Serializable]
    public class UserProfile
    {
        public UserType userType = UserType.Beginner;
        public GameplayPattern gameplayPattern = GameplayPattern.Exploratory;
        public LearningState learningState = LearningState.Learning;
        
        // 统计数据
        public float totalPlayTime;
        public int totalSessions;
        public float averageSessionLength;
        public float commandsPerMinute;
        public float emotionStability;
        public float learningProgress;
        public float engagementLevel;
        
        // 偏好分析
        public Dictionary<string, float> preferredCommands;
        public Dictionary<string, float> emotionDistribution;
        public List<float> performanceHistory;
        
        // 时间模式
        public Dictionary<int, float> playTimeByHour; // 按小时统计
        public Dictionary<DayOfWeek, float> playTimeByDayOfWeek;
        
        public DateTime lastAnalysisTime;
        
        public UserProfile()
        {
            preferredCommands = new Dictionary<string, float>();
            emotionDistribution = new Dictionary<string, float>();
            performanceHistory = new List<float>();
            playTimeByHour = new Dictionary<int, float>();
            playTimeByDayOfWeek = new Dictionary<DayOfWeek, float>();
            lastAnalysisTime = DateTime.Now;
        }
    }
    
    // 行为模式
    [System.Serializable]
    public class BehaviorPattern
    {
        public string patternName;
        public string description;
        public float confidence;
        public DateTime detectedTime;
        public Dictionary<string, object> characteristics;
        
        public BehaviorPattern(string name, string desc, float conf)
        {
            patternName = name;
            description = desc;
            confidence = conf;
            detectedTime = DateTime.Now;
            characteristics = new Dictionary<string, object>();
        }
    }
    
    // 当前用户档案
    public UserProfile CurrentProfile { get; private set; }
    
    // 检测到的行为模式
    private List<BehaviorPattern> detectedPatterns = new List<BehaviorPattern>();
    
    // 分析历史
    private Queue<float> recentPerformance = new Queue<float>();
    private Queue<float> recentEngagement = new Queue<float>();
    private Queue<float> recentEmotionStability = new Queue<float>();
    
    // 内部状态
    private float lastAnalysisTime = 0f;
    private DateTime sessionStartTime;
    private int currentSessionCommands = 0;
    
    // 事件委托
    public Action<UserProfile> OnProfileUpdated;
    public Action<BehaviorPattern> OnPatternDetected;
    public Action<UserType> OnUserTypeChanged;
    
    void Awake()
    {
        InitializeProfile();
        sessionStartTime = DateTime.Now;
    }
    
    void Start()
    {
        // 订阅系统事件
        SubscribeToEvents();
        
        if (showAnalysisLogs)
            Debug.Log("🔍 User Behavior Analyzer initialized");
    }
    
    void Update()
    {
        if (!enableBehaviorAnalysis) return;
        
        // 定期执行行为分析
        if (Time.time - lastAnalysisTime >= analysisInterval)
        {
            PerformBehaviorAnalysis();
            lastAnalysisTime = Time.time;
        }
        
        // 实时更新基础统计
        UpdateBasicStats();
    }
    
    /// <summary>
    /// 初始化用户档案
    /// </summary>
    private void InitializeProfile()
    {
        CurrentProfile = new UserProfile();
        sessionStartTime = DateTime.Now;
    }
    
    /// <summary>
    /// 订阅系统事件
    /// </summary>
    private void SubscribeToEvents()
    {
        if (emotionSystem != null)
        {
            emotionSystem.OnEmotionChanged += OnEmotionChanged;
            emotionSystem.OnEmotionStateChanged += OnEmotionStateChanged;
        }
        
        if (loggingSystem != null)
        {
            loggingSystem.OnLogEntryAdded += OnLogEntryAdded;
            loggingSystem.OnStatsUpdated += OnStatsUpdated;
        }
    }
    
    /// <summary>
    /// 执行行为分析
    /// </summary>
    private void PerformBehaviorAnalysis()
    {
        // 分析用户类型
        AnalyzeUserType();
        
        // 分析游戏模式
        AnalyzeGameplayPattern();
        
        // 分析学习状态
        AnalyzeLearningState();
        
        // 检测行为模式
        DetectBehaviorPatterns();
        
        // 更新时间模式
        UpdateTimePatterns();
        
        // 触发档案更新事件
        CurrentProfile.lastAnalysisTime = DateTime.Now;
        OnProfileUpdated?.Invoke(CurrentProfile);
        
        if (showAnalysisLogs)
        {
            Debug.Log($"🔍 User behavior analysis completed - Type: {CurrentProfile.userType}, " +
                     $"Pattern: {CurrentProfile.gameplayPattern}, Learning State: {CurrentProfile.learningState}");
        }
    }
    
    /// <summary>
    /// 分析用户类型
    /// </summary>
    private void AnalyzeUserType()
    {
        UserType newType = CurrentProfile.userType;
        
        // 基于游戏时间和指令频率判断
        if (CurrentProfile.totalPlayTime < 300f) // 5分钟以下
        {
            newType = UserType.Beginner;
        }
        else if (CurrentProfile.commandsPerMinute < 1f)
        {
            newType = UserType.Casual;
        }
        else if (CurrentProfile.commandsPerMinute > activeUserThreshold)
        {
            if (CurrentProfile.emotionStability > emotionStabilityThreshold)
                newType = UserType.Expert;
            else
                newType = UserType.Active;
        }
        else if (CurrentProfile.emotionStability < 0.3f && CurrentProfile.learningProgress < 0.1f)
        {
            newType = UserType.Struggling;
        }
        
        // 检查类型是否发生变化
        if (newType != CurrentProfile.userType)
        {
            UserType oldType = CurrentProfile.userType;
            CurrentProfile.userType = newType;
            OnUserTypeChanged?.Invoke(newType);
            
            if (showAnalysisLogs)
                Debug.Log($"🔍 User type changed: {oldType} → {newType}");
        }
    }
    
    /// <summary>
    /// 分析游戏模式
    /// </summary>
    private void AnalyzeGameplayPattern()
    {
        var stats = loggingSystem?.GetCurrentSessionStats();
        if (stats == null) return;
        
        // 基于语音指令分布判断游戏模式
        if (stats.voiceCommandFrequency != null && stats.voiceCommandFrequency.Count > 0)
        {
            var commands = stats.voiceCommandFrequency;
            
            // 探索型：多样化的指令使用
            float commandDiversity = CalculateCommandDiversity(commands);
            
            // 目标导向型：重复使用特定指令
            float commandFocus = CalculateCommandFocus(commands);
            
            // 竞争型：高频率使用
            float commandFrequency = stats.totalVoiceCommands / (stats.totalSessionTime / 60f);
            
            // 判断主要模式
            if (commandDiversity > 0.7f)
                CurrentProfile.gameplayPattern = GameplayPattern.Exploratory;
            else if (commandFocus > 0.8f)
                CurrentProfile.gameplayPattern = GameplayPattern.Goal_Oriented;
            else if (commandFrequency > 5f)
                CurrentProfile.gameplayPattern = GameplayPattern.Competitive;
            else if (CurrentProfile.emotionStability > 0.8f)
                CurrentProfile.gameplayPattern = GameplayPattern.Relaxed;
            else
                CurrentProfile.gameplayPattern = GameplayPattern.Social;
        }
    }
    
    /// <summary>
    /// 分析学习状态
    /// </summary>
    private void AnalyzeLearningState()
    {
        if (recentPerformance.Count < 3) return;
        
        var performances = recentPerformance.ToArray();
        
        // 计算学习趋势
        float trend = CalculateTrend(performances);
        float stability = CalculateStability(performances);
        
        // 判断学习状态
        if (trend > learningProgressThreshold)
        {
            CurrentProfile.learningState = LearningState.Improving;
        }
        else if (trend < -learningProgressThreshold)
        {
            CurrentProfile.learningState = LearningState.Regressing;
        }
        else if (stability > 0.8f && performances.Average() > 0.7f)
        {
            CurrentProfile.learningState = LearningState.Mastering;
        }
        else if (stability > 0.9f)
        {
            CurrentProfile.learningState = LearningState.Plateaued;
        }
        else
        {
            CurrentProfile.learningState = LearningState.Learning;
        }
        
        CurrentProfile.learningProgress = trend;
    }
    
    /// <summary>
    /// 检测行为模式
    /// </summary>
    private void DetectBehaviorPatterns()
    {
        // 检测快速学习模式
        if (CurrentProfile.learningProgress > 0.3f && CurrentProfile.totalPlayTime < 600f)
        {
            DetectPattern("Fast Learner", "User shows rapid learning and adaptation", 0.8f);
        }
        
        // 检测情感稳定模式
        if (CurrentProfile.emotionStability > 0.9f)
        {
            DetectPattern("Emotion Stability", "User maintains stable emotional state", 0.9f);
        }
        
        // 检测高活跃度模式
        if (CurrentProfile.commandsPerMinute > activeUserThreshold * 2)
        {
            DetectPattern("High Engagement", "User exhibits high engagement", 0.85f);
        }
        
        // 检测需要帮助模式
        if (CurrentProfile.userType == UserType.Struggling && 
            CurrentProfile.emotionStability < 0.3f)
        {
            DetectPattern("Needs Help", "User may require additional guidance and support", 0.75f);
        }
        
        // 检测专注模式
        if (CurrentProfile.gameplayPattern == GameplayPattern.Goal_Oriented && 
            CurrentProfile.emotionStability > 0.7f)
        {
            DetectPattern("Focused Mode", "User exhibits high focus and goal-oriented behavior", 0.8f);
        }
    }
    
    /// <summary>
    /// 检测特定行为模式
    /// </summary>
    private void DetectPattern(string name, string description, float confidence)
    {
        // 检查是否已存在相似模式
        var existingPattern = detectedPatterns.FirstOrDefault(p => p.patternName == name);
        
        if (existingPattern != null)
        {
            // 更新现有模式的置信度
            existingPattern.confidence = Mathf.Lerp(existingPattern.confidence, confidence, 0.3f);
            existingPattern.detectedTime = DateTime.Now;
        }
        else
        {
            // 创建新模式
            var newPattern = new BehaviorPattern(name, description, confidence);
            detectedPatterns.Add(newPattern);
            OnPatternDetected?.Invoke(newPattern);
            
            if (showAnalysisLogs)
                Debug.Log($"🔍 Detected behavior pattern: {name} (Confidence: {confidence:F2})");
        }
        
        // 清理旧模式
        CleanupOldPatterns();
    }
    
    /// <summary>
    /// 更新时间模式
    /// </summary>
    private void UpdateTimePatterns()
    {
        DateTime now = DateTime.Now;
        int currentHour = now.Hour;
        DayOfWeek currentDay = now.DayOfWeek;
        
        // 更新小时统计
        if (CurrentProfile.playTimeByHour.ContainsKey(currentHour))
            CurrentProfile.playTimeByHour[currentHour] += analysisInterval;
        else
            CurrentProfile.playTimeByHour[currentHour] = analysisInterval;
        
        // 更新星期统计
        if (CurrentProfile.playTimeByDayOfWeek.ContainsKey(currentDay))
            CurrentProfile.playTimeByDayOfWeek[currentDay] += analysisInterval;
        else
            CurrentProfile.playTimeByDayOfWeek[currentDay] = analysisInterval;
    }
    
    /// <summary>
    /// 更新基础统计
    /// </summary>
    private void UpdateBasicStats()
    {
        CurrentProfile.totalPlayTime = Time.time;
        
        if (loggingSystem != null)
        {
            var stats = loggingSystem.GetCurrentSessionStats();
            CurrentProfile.commandsPerMinute = stats.totalVoiceCommands / (stats.totalSessionTime / 60f);
            CurrentProfile.averageSessionLength = stats.totalSessionTime;
        }
        
        // 更新参与度（基于指令频率和情感强度）
        if (emotionSystem != null && emotionSystem.CurrentEmotion != null)
        {
            var emotion = emotionSystem.CurrentEmotion;
            CurrentProfile.engagementLevel = (CurrentProfile.commandsPerMinute / 10f + emotion.intensity) / 2f;
        }
    }
    
    /// <summary>
    /// 计算指令多样性
    /// </summary>
    private float CalculateCommandDiversity(Dictionary<string, int> commands)
    {
        if (commands.Count == 0) return 0f;
        
        int totalCommands = commands.Values.Sum();
        float entropy = 0f;
        
        foreach (var count in commands.Values)
        {
            float probability = (float)count / totalCommands;
            if (probability > 0)
                entropy -= probability * Mathf.Log(probability, 2);
        }
        
        // 归一化到0-1范围
        float maxEntropy = Mathf.Log(commands.Count, 2);
        return maxEntropy > 0 ? entropy / maxEntropy : 0f;
    }
    
    /// <summary>
    /// 计算指令专注度
    /// </summary>
    private float CalculateCommandFocus(Dictionary<string, int> commands)
    {
        if (commands.Count == 0) return 0f;
        
        int totalCommands = commands.Values.Sum();
        int maxCommandCount = commands.Values.Max();
        
        return (float)maxCommandCount / totalCommands;
    }
    
    /// <summary>
    /// 计算趋势
    /// </summary>
    private float CalculateTrend(float[] values)
    {
        if (values.Length < 2) return 0f;
        
        float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
        int n = values.Length;
        
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }
        
        float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
    
    /// <summary>
    /// 计算稳定性
    /// </summary>
    private float CalculateStability(float[] values)
    {
        if (values.Length < 2) return 0f;
        
        float mean = values.Average();
        float variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
        float stdDev = Mathf.Sqrt(variance);
        
        // 稳定性 = 1 - 标准差（归一化）
        return Mathf.Clamp01(1f - stdDev);
    }
    
    /// <summary>
    /// 清理旧的行为模式
    /// </summary>
    private void CleanupOldPatterns()
    {
        DateTime cutoffTime = DateTime.Now.AddMinutes(-30); // 保留30分钟内的模式
        detectedPatterns.RemoveAll(p => p.detectedTime < cutoffTime || p.confidence < 0.3f);
    }
    
    // === 事件处理 ===
    
    private void OnEmotionChanged(EmotionDetectionSystem.EmotionData emotion)
    {
        // 更新情感分布统计
        string emotionKey = emotion.primaryEmotion.ToString();
        if (CurrentProfile.emotionDistribution.ContainsKey(emotionKey))
            CurrentProfile.emotionDistribution[emotionKey] += emotion.intensity;
        else
            CurrentProfile.emotionDistribution[emotionKey] = emotion.intensity;
        
        // 更新情感稳定性
        recentEmotionStability.Enqueue(emotion.confidence);
        if (recentEmotionStability.Count > 10)
            recentEmotionStability.Dequeue();
            
        CurrentProfile.emotionStability = recentEmotionStability.Average();
    }
    
    private void OnEmotionStateChanged(EmotionDetectionSystem.EmotionState oldState, 
                                      EmotionDetectionSystem.EmotionState newState)
    {
        // 记录情感状态变化
        if (showAnalysisLogs)
            Debug.Log($"🔍 Emotion state change recorded: {oldState} → {newState}");
    }
    
    private void OnLogEntryAdded(AdvancedLoggingSystem.LogEntry entry)
    {
        if (entry.eventType == AdvancedLoggingSystem.LogEventType.Voice)
        {
            currentSessionCommands++;
            
            // 更新偏好指令统计
            if (entry.customData.ContainsKey("command"))
            {
                string command = entry.customData["command"].ToString();
                if (CurrentProfile.preferredCommands.ContainsKey(command))
                    CurrentProfile.preferredCommands[command]++;
                else
                    CurrentProfile.preferredCommands[command] = 1;
            }
        }
    }
    
    private void OnStatsUpdated(AdvancedLoggingSystem.SessionStats stats)
    {
        // 更新性能历史
        float performance = CalculateSessionPerformance(stats);
        recentPerformance.Enqueue(performance);
        if (recentPerformance.Count > learningCurveWindow)
            recentPerformance.Dequeue();
        
        CurrentProfile.performanceHistory.Add(performance);
        
        // 更新参与度历史
        float engagement = CalculateEngagement(stats);
        recentEngagement.Enqueue(engagement);
        if (recentEngagement.Count > 10)
            recentEngagement.Dequeue();
    }
    
    /// <summary>
    /// 计算会话表现
    /// </summary>
    private float CalculateSessionPerformance(AdvancedLoggingSystem.SessionStats stats)
    {
        // 基于多个因素计算综合表现
        float commandEfficiency = Mathf.Clamp01(stats.totalVoiceCommands / (stats.totalSessionTime / 60f) / 5f);
        float emotionQuality = Mathf.Clamp01(stats.averageConfidence);
        float stability = Mathf.Clamp01(CurrentProfile.emotionStability);
        
        return (commandEfficiency + emotionQuality + stability) / 3f;
    }
    
    /// <summary>
    /// 计算参与度
    /// </summary>
    private float CalculateEngagement(AdvancedLoggingSystem.SessionStats stats)
    {
        float actionDensity = stats.totalGameActions / (stats.totalSessionTime / 60f);
        float emotionIntensity = stats.averageEmotionIntensity;
        
        return Mathf.Clamp01((actionDensity / 3f + emotionIntensity) / 2f);
    }
    
    // === 公共接口 ===
    
    /// <summary>
    /// 获取检测到的行为模式
    /// </summary>
    public List<BehaviorPattern> GetDetectedPatterns()
    {
        return new List<BehaviorPattern>(detectedPatterns);
    }
    
    /// <summary>
    /// 获取推荐建议
    /// </summary>
    public List<string> GetRecommendations()
    {
        var recommendations = new List<string>();
        
        switch (CurrentProfile.userType)
        {
            case UserType.Beginner:
                recommendations.Add("Suggest trying more basic voice commands");
                recommendations.Add("Keep a relaxed mindset and slowly get familiar with the system");
                break;
                
            case UserType.Struggling:
                recommendations.Add("Try lowering the game difficulty");
                recommendations.Add("Suggest taking a break and adjusting your state");
                recommendations.Add("Consider seeking help or checking tutorials");
                break;
                
            case UserType.Expert:
                recommendations.Add("Try higher difficulty challenges");
                recommendations.Add("Share experiences to help other users");
                break;
                
            case UserType.Active:
                recommendations.Add("Maintain your current engagement");
                recommendations.Add("Try exploring new features");
                break;
                
            case UserType.Casual:
                recommendations.Add("Enjoy a relaxed gaming experience");
                recommendations.Add("Try more interactive features");
                break;
        }
        
        return recommendations;
    }
    
    /// <summary>
    /// 重置用户档案
    /// </summary>
    public void ResetProfile()
    {
        CurrentProfile = new UserProfile();
        detectedPatterns.Clear();
        recentPerformance.Clear();
        recentEngagement.Clear();
        recentEmotionStability.Clear();
        
        if (showAnalysisLogs)
            Debug.Log("🔍 User profile reset");
    }
    
    /// <summary>
    /// 简单的UI显示
    /// </summary>
    void OnGUI()
    {
        if (!showBehaviorUI) return;
        
        GUI.Box(new Rect(Screen.width - 330, 10, 310, 300), "User Behavior Analysis");
        
        float y = 35;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"User Type: {GetUserTypeDisplayName(CurrentProfile.userType)}");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Gameplay Pattern: {GetGameplayPatternDisplayName(CurrentProfile.gameplayPattern)}");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Learning State: {GetLearningStateDisplayName(CurrentProfile.learningState)}");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Command Frequency: {CurrentProfile.commandsPerMinute:F1}/minute");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Emotion Stability: {CurrentProfile.emotionStability:F2}");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Learning Progress: {CurrentProfile.learningProgress:F2}");
        y += 20;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"Engagement: {CurrentProfile.engagementLevel:F2}");
        
        y += 30;
        GUI.Label(new Rect(Screen.width - 320, y, 300, 20), "Detected Behavior Patterns:");
        y += 20;
        
        foreach (var pattern in detectedPatterns.Take(3))
        {
            GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"• {pattern.patternName} ({pattern.confidence:F2})");
            y += 20;
        }
    }
    
    /// <summary>
    /// 获取用户类型显示名称
    /// </summary>
    private string GetUserTypeDisplayName(UserType type)
    {
        switch (type)
        {
            case UserType.Beginner: return "Beginner";
            case UserType.Casual: return "Casual User";
            case UserType.Active: return "Active User";
            case UserType.Expert: return "Expert User";
            case UserType.Struggling: return "Struggling User";
            default: return type.ToString();
        }
    }
    
    /// <summary>
    /// 获取游戏模式显示名称
    /// </summary>
    private string GetGameplayPatternDisplayName(GameplayPattern pattern)
    {
        switch (pattern)
        {
            case GameplayPattern.Exploratory: return "Exploratory";
            case GameplayPattern.Goal_Oriented: return "Goal-Oriented";
            case GameplayPattern.Social: return "Social";
            case GameplayPattern.Competitive: return "Competitive";
            case GameplayPattern.Relaxed: return "Relaxed";
            default: return pattern.ToString();
        }
    }
    
    /// <summary>
    /// 获取学习状态显示名称
    /// </summary>
    private string GetLearningStateDisplayName(LearningState state)
    {
        switch (state)
        {
            case LearningState.Learning: return "Learning";
            case LearningState.Mastering: return "Mastering";
            case LearningState.Plateaued: return "Plateaued";
            case LearningState.Improving: return "Improving";
            case LearningState.Regressing: return "Regressing";
            default: return state.ToString();
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (emotionSystem != null)
        {
            emotionSystem.OnEmotionChanged -= OnEmotionChanged;
            emotionSystem.OnEmotionStateChanged -= OnEmotionStateChanged;
        }
        
        if (loggingSystem != null)
        {
            loggingSystem.OnLogEntryAdded -= OnLogEntryAdded;
            loggingSystem.OnStatsUpdated -= OnStatsUpdated;
        }
    }
} 