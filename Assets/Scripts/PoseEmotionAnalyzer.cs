using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;

/// <summary>
/// Pose Emotion Analyzer - Analyzes user emotion state based on body posture and movements
/// </summary>
public class PoseEmotionAnalyzer : MonoBehaviour
{
    [Header("=== Analysis Configuration ===")]
    [Tooltip("Enable pose emotion analysis")]
    public bool enablePoseAnalysis = true;
    
    [Tooltip("Pose data validity period (seconds)")]
    public float dataValidityPeriod = 3.0f;
    
    [Tooltip("Movement analysis history window size")]
    public int movementHistorySize = 10; // 减少历史窗口大小
    
    [Header("=== Performance Settings ===")]
    [Tooltip("Analysis update frequency (seconds)")]
    public float analysisUpdateInterval = 0.5f; // 新增：减少分析频率
    
    [Tooltip("Skip frames for performance")]
    public int frameSkipping = 2; // 新增：跳帧处理
    
    [Tooltip("Auto-disable when FPS too low")]
    public bool autoDisableOnLowFPS = true;
    
    [Tooltip("FPS threshold for auto-disable")]
    public float lowFPSThreshold = 25f;
    
    [Header("=== Emotion Detection Parameters ===")]
    [Tooltip("Body stability threshold (detecting calmness)")]
    public float stabilityThreshold = 0.3f;
    
    [Tooltip("Arm openness threshold (detecting openness)")]
    public float opennessThreshold = 0.6f;
    
    [Tooltip("Movement speed threshold (detecting activity level)")]
    public float movementThreshold = 0.1f;
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("MediaPipe pose landmarker runner")]
    public PoseLandmarkerRunner poseRunner;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show analysis logs in console")]
    public bool showAnalysisLogs = false;
    
    [Tooltip("Display pose analysis data on UI")]
    public bool showPoseUI = false;
    
    public Action<EmotionDetectionSystem.EmotionData> OnPoseEmotionDetected;
    
    // 姿态数据结构
    private struct PoseData
    {
        public Vector3 headPosition;
        public Vector3 leftShoulder;
        public Vector3 rightShoulder;
        public Vector3 leftElbow;
        public Vector3 rightElbow;
        public Vector3 leftWrist;
        public Vector3 rightWrist;
        public Vector3 leftHip;
        public Vector3 rightHip;
        public Vector3 bodyCenter;
        public DateTime timestamp;
        public bool isValid;
    }
    
    // 分析结果缓存
    private EmotionDetectionSystem.EmotionData currentPoseEmotion;
    private DateTime lastAnalysisTime;
    private DateTime lastUpdateTime;  // 新增：控制更新频率
    private bool hasRecentData = false;
    
    // 姿态历史数据 - 优化：使用固定大小
    private Queue<PoseData> poseHistory = new Queue<PoseData>();
    private List<float> movementSpeedHistory = new List<float>();
    private List<float> bodyOpennesHistory = new List<float>();
    private List<float> bodyStabilityHistory = new List<float>();
    
    // 当前分析参数
    private float currentMovementSpeed = 0f;
    private float currentBodyOpenness = 0f;
    private float currentBodyStability = 0f;
    private float currentBodyTilt = 0f;
    private float averageArmPosition = 0f;
    
    // 内部计算缓存
    private PoseData lastValidPose;
    private bool hasLastValidPose = false;
    
    // MediaPipe数据访问缓存
    private FieldInfo landmarkField;
    private bool landmarkFieldInitialized = false;
    
    // 性能优化参数 - 新增
    private int frameCounter = 0;
    private float currentFPS = 60f;
    private const int MAX_HISTORY_SIZE = 20; // 严格限制历史大小
    private const int MAX_POSE_QUEUE_SIZE = 15; // 限制姿态队列大小

    void Awake()
    {
        InitializeComponents();
        currentPoseEmotion = new EmotionDetectionSystem.EmotionData();
        lastAnalysisTime = DateTime.Now;
        lastUpdateTime = DateTime.Now;
    }

    void Start()
    {
        if (showAnalysisLogs)
            Debug.Log("🤸 Pose Emotion Analyzer initialized with performance optimizations");
    }

    void Update()
    {
        if (!enablePoseAnalysis) return;
        
        // 计算当前FPS
        currentFPS = 1f / Time.unscaledDeltaTime;
        
        // 自动禁用检查
        if (autoDisableOnLowFPS && currentFPS < lowFPSThreshold)
        {
            enablePoseAnalysis = false;
            Debug.LogWarning($"🤸 Pose analysis auto-disabled due to low FPS ({currentFPS:F1})");
            return;
        }
        
        // 跳帧处理以减少GPU负载
        frameCounter++;
        if (frameCounter % (frameSkipping + 1) != 0)
        {
            return;
        }
        
        // 控制分析更新频率
        if ((DateTime.Now - lastUpdateTime).TotalSeconds < analysisUpdateInterval)
        {
            return;
        }
        
        // 获取当前姿态数据
        var currentPose = ExtractPoseData();
        
        if (currentPose.isValid)
        {
            // 添加到历史记录
            AddPoseToHistory(currentPose);
            
            // 执行情感分析
            AnalyzePoseEmotion();
            
            hasRecentData = true;
            lastAnalysisTime = DateTime.Now;
        }
        else
        {
            // 检查数据有效性
            UpdateDataValidity();
        }
        
        lastUpdateTime = DateTime.Now;
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        if (poseRunner == null)
            poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
    }
    
    /// <summary>
    /// 提取当前姿态数据
    /// </summary>
    private PoseData ExtractPoseData()
    {
        var poseData = new PoseData();
        poseData.timestamp = DateTime.Now;
        poseData.isValid = false;
        
        if (poseRunner == null) return poseData;
        
        try
        {
            var result = poseRunner.LatestResult;
            
            // 正确检查MediaPipe结果是否有效
            if (!result.Equals(default(PoseLandmarkerResult)) && 
                result.poseLandmarks != null && 
                result.poseLandmarks.Count > 0)
            {
                var landmarksContainer = result.poseLandmarks[0]; // 使用第一个检测到的人
                
                // 初始化landmarks字段访问（仅一次）
                if (!landmarkFieldInitialized)
                {
                    InitializeLandmarkField(landmarksContainer);
                }
                
                if (landmarkField != null)
                {
                    var rawLandmarks = landmarkField.GetValue(landmarksContainer) as IList;
                    
                    if (rawLandmarks != null && rawLandmarks.Count >= 33) // MediaPipe姿态模型有33个关键点
                    {
                        // 提取关键姿态点（基于MediaPipe姿态模型的索引）
                        poseData.headPosition = ConvertLandmark(rawLandmarks[0]);      // 鼻子
                        poseData.leftShoulder = ConvertLandmark(rawLandmarks[11]);     // 左肩
                        poseData.rightShoulder = ConvertLandmark(rawLandmarks[12]);    // 右肩
                        poseData.leftElbow = ConvertLandmark(rawLandmarks[13]);        // 左肘
                        poseData.rightElbow = ConvertLandmark(rawLandmarks[14]);       // 右肘
                        poseData.leftWrist = ConvertLandmark(rawLandmarks[15]);        // 左腕
                        poseData.rightWrist = ConvertLandmark(rawLandmarks[16]);       // 右腕
                        poseData.leftHip = ConvertLandmark(rawLandmarks[23]);          // 左臀
                        poseData.rightHip = ConvertLandmark(rawLandmarks[24]);         // 右臀
                        
                        // 计算身体中心
                        poseData.bodyCenter = (poseData.leftShoulder + poseData.rightShoulder + 
                                             poseData.leftHip + poseData.rightHip) / 4f;
                        
                        poseData.isValid = true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (showAnalysisLogs)
                Debug.LogWarning($"🤸 Pose data extraction failed: {e.Message}");
        }
        
        return poseData;
    }
    
    /// <summary>
    /// 初始化landmarks字段访问
    /// </summary>
    private void InitializeLandmarkField(object landmarksContainer)
    {
        try
        {
            var containerType = landmarksContainer.GetType();
            landmarkField = containerType.GetField("landmarks", BindingFlags.Instance | BindingFlags.Public);
            
            if (landmarkField == null)
            {
                // 尝试其他可能的字段名
                landmarkField = containerType.GetField("landmark_", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            
            landmarkFieldInitialized = true;
            
            if (landmarkField == null)
            {
                Debug.LogError("❌ Could not find landmarks field");
            }
            else if (showAnalysisLogs)
            {
                Debug.Log("✅ Successfully initialized landmarks field access");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to initialize landmarks field: {e.Message}");
            landmarkFieldInitialized = true; // 避免重复尝试
        }
    }
    
    /// <summary>
    /// 转换MediaPipe地标点为Unity向量
    /// </summary>
    private Vector3 ConvertLandmark(object landmark)
    {
        try
        {
            // 使用反射访问x, y, z字段（MediaPipe landmark的结构）
            var landmarkType = landmark.GetType();
            
            float x = 0f, y = 0f, z = 0f;
            
            // 尝试获取x, y, z字段
            var xField = landmarkType.GetField("x", BindingFlags.Instance | BindingFlags.Public);
            var yField = landmarkType.GetField("y", BindingFlags.Instance | BindingFlags.Public);
            var zField = landmarkType.GetField("z", BindingFlags.Instance | BindingFlags.Public);
            
            if (xField != null) x = Convert.ToSingle(xField.GetValue(landmark));
            if (yField != null) y = Convert.ToSingle(yField.GetValue(landmark));
            if (zField != null) z = Convert.ToSingle(zField.GetValue(landmark));
            
            return new Vector3(x, y, z);
        }
        catch
        {
            return Vector3.zero;
        }
    }
    
    /// <summary>
    /// 添加姿态数据到历史记录 - 优化版本
    /// </summary>
    private void AddPoseToHistory(PoseData pose)
    {
        poseHistory.Enqueue(pose);
        
        // 严格限制历史大小防止内存泄漏
        int maxSize = Mathf.Min(movementHistorySize, MAX_POSE_QUEUE_SIZE);
        while (poseHistory.Count > maxSize)
        {
            poseHistory.Dequeue();
        }
        
        // 更新最后有效姿态
        lastValidPose = pose;
        hasLastValidPose = true;
    }
    
    /// <summary>
    /// 分析姿态情感
    /// </summary>
    private void AnalyzePoseEmotion()
    {
        if (poseHistory.Count < 5) return; // 需要足够的历史数据
        
        // 计算各种姿态特征
        CalculateMovementSpeed();
        CalculateBodyOpenness();
        CalculateBodyStability();
        CalculateBodyTilt();
        
        // 基于姿态特征推断情感
        var emotion = new EmotionDetectionSystem.EmotionData();
        
        // 分析兴奋度 (Arousal) - 基于动作速度和身体活跃度
        float movementArousal = Mathf.Clamp01(currentMovementSpeed / movementThreshold);
        float instabilityArousal = Mathf.Clamp01((1f - currentBodyStability) * 2f); // 不稳定表示高兴奋
        emotion.arousal = (movementArousal * 0.7f + instabilityArousal * 0.3f);
        
        // 分析情感价值 (Valence) - 基于身体开放性和姿态
        float opennessValence = 0f;
        if (currentBodyOpenness > opennessThreshold)
        {
            opennessValence = Mathf.Clamp01((currentBodyOpenness - opennessThreshold) / (1f - opennessThreshold));
        }
        else
        {
            opennessValence = -Mathf.Clamp01((opennessThreshold - currentBodyOpenness) / opennessThreshold);
        }
        
        // 身体倾斜也影响情感价值 - 轻微前倾可能表示积极参与
        float tiltValence = 0f;
        if (currentBodyTilt > 0 && currentBodyTilt < 15f) // 假设bodyTiltThreshold是15度
        {
            tiltValence = Mathf.Clamp01(currentBodyTilt / 15f * 0.5f);
        }
        else if (currentBodyTilt > 15f)
        {
            tiltValence = -Mathf.Clamp01((currentBodyTilt - 15f) / 15f);
        }
        
        emotion.valence = (opennessValence * 0.8f + tiltValence * 0.2f);
        
        // 计算情感强度 - 基于动作幅度和变化
        float movementIntensity = Mathf.Clamp01(currentMovementSpeed / 2f);
        float opennessIntensity = Mathf.Abs(currentBodyOpenness - 0.5f) * 2f; // 偏离中性状态的程度
        emotion.intensity = (movementIntensity + opennessIntensity + Mathf.Abs(emotion.valence) + emotion.arousal) / 4f;
        
        // 计算置信度 - 基于数据质量和一致性
        float dataQuality = Mathf.Clamp01(poseHistory.Count / (float)movementHistorySize);
        float consistencyScore = CalculateDataConsistency();
        emotion.confidence = (dataQuality + consistencyScore) * 0.5f;
        
        // 更新历史记录
        UpdateAnalysisHistory();
        
        // 更新当前情感状态
        currentPoseEmotion = emotion;
        
        // 触发事件
        OnPoseEmotionDetected?.Invoke(emotion);
        
        if (showAnalysisLogs && emotion.confidence > 0.4f)
        {
            Debug.Log($"🤸 Pose emotion analysis: Arousal={emotion.arousal:F2}, Valence={emotion.valence:F2}, " +
                     $"Intensity={emotion.intensity:F2}, Confidence={emotion.confidence:F2}, " +
                     $"Speed={currentMovementSpeed:F2}, Openness={currentBodyOpenness:F2}");
        }
    }
    
    /// <summary>
    /// 计算动作速度
    /// </summary>
    private void CalculateMovementSpeed()
    {
        if (poseHistory.Count < 2) return;
        
        var poses = poseHistory.ToArray();
        float totalSpeed = 0f;
        int validComparisons = 0;
        
        for (int i = 1; i < poses.Length; i++)
        {
            var current = poses[i];
            var previous = poses[i - 1];
            
            float deltaTime = (float)(current.timestamp - previous.timestamp).TotalSeconds;
            if (deltaTime > 0)
            {
                // 计算关键点的移动距离
                float headSpeed = Vector3.Distance(current.headPosition, previous.headPosition) / deltaTime;
                float leftHandSpeed = Vector3.Distance(current.leftWrist, previous.leftWrist) / deltaTime;
                float rightHandSpeed = Vector3.Distance(current.rightWrist, previous.rightWrist) / deltaTime;
                
                float frameSpeed = (headSpeed + leftHandSpeed + rightHandSpeed) / 3f;
                totalSpeed += frameSpeed;
                validComparisons++;
            }
        }
        
        if (validComparisons > 0)
        {
            currentMovementSpeed = totalSpeed / validComparisons;
        }
    }
    
    /// <summary>
    /// 计算身体开放性
    /// </summary>
    private void CalculateBodyOpenness()
    {
        if (!hasLastValidPose) return;
        
        var pose = lastValidPose;
        
        // 计算肩宽
        float shoulderWidth = Vector3.Distance(pose.leftShoulder, pose.rightShoulder);
        
        // 计算手臂张开程度
        Vector3 leftArmVector = pose.leftWrist - pose.leftShoulder;
        Vector3 rightArmVector = pose.rightWrist - pose.rightShoulder;
        Vector3 shoulderVector = pose.rightShoulder - pose.leftShoulder;
        
        // 计算手臂与肩膀的角度
        float leftArmAngle = Vector3.Angle(leftArmVector, shoulderVector);
        float rightArmAngle = Vector3.Angle(rightArmVector, -shoulderVector);
        
        // 计算平均手臂高度（相对于肩膀）
        float leftArmHeight = (pose.leftWrist.y - pose.leftShoulder.y) / shoulderWidth;
        float rightArmHeight = (pose.rightWrist.y - pose.rightShoulder.y) / shoulderWidth;
        averageArmPosition = (leftArmHeight + rightArmHeight) / 2f;
        
        // 综合开放性评分 (0-1)
        float angleOpenness = Mathf.Clamp01((leftArmAngle + rightArmAngle - 90f) / 180f);
        float heightOpenness = Mathf.Clamp01((averageArmPosition + 1f) / 2f);
        
        currentBodyOpenness = (angleOpenness + heightOpenness) / 2f;
    }
    
    /// <summary>
    /// 计算身体稳定性
    /// </summary>
    private void CalculateBodyStability()
    {
        if (poseHistory.Count < 10) return;
        
        var poses = poseHistory.ToArray();
        float totalVariance = 0f;
        
        // 计算身体中心位置的方差
        Vector3 avgCenter = Vector3.zero;
        foreach (var pose in poses)
        {
            avgCenter += pose.bodyCenter;
        }
        avgCenter /= poses.Length;
        
        foreach (var pose in poses)
        {
            float distance = Vector3.Distance(pose.bodyCenter, avgCenter);
            totalVariance += distance * distance;
        }
        
        float variance = totalVariance / poses.Length;
        
        // 稳定性越高，方差越小
        currentBodyStability = Mathf.Clamp01(1f - variance * 100f);
    }
    
    /// <summary>
    /// 计算身体倾斜角度
    /// </summary>
    private void CalculateBodyTilt()
    {
        if (!hasLastValidPose) return;
        
        var pose = lastValidPose;
        
        // 计算肩膀连线与水平面的角度
        Vector3 shoulderLine = pose.rightShoulder - pose.leftShoulder;
        Vector3 horizontal = Vector3.right;
        
        float shoulderTilt = Vector3.Angle(shoulderLine, horizontal);
        
        // 计算躯干倾斜（肩膀中心到臀部中心）
        Vector3 shoulderCenter = (pose.leftShoulder + pose.rightShoulder) / 2f;
        Vector3 hipCenter = (pose.leftHip + pose.rightHip) / 2f;
        Vector3 torsoLine = shoulderCenter - hipCenter;
        Vector3 vertical = Vector3.up;
        
        float torsoTilt = Vector3.Angle(torsoLine, vertical);
        
        currentBodyTilt = (shoulderTilt + torsoTilt) / 2f;
    }
    
    /// <summary>
    /// 计算数据一致性
    /// </summary>
    private float CalculateDataConsistency()
    {
        if (movementSpeedHistory.Count < 5) return 0.5f;
        
        // 计算最近数据的标准差，一致性高的数据标准差较小
        float avgSpeed = movementSpeedHistory.Average();
        float variance = movementSpeedHistory.Sum(x => (x - avgSpeed) * (x - avgSpeed)) / movementSpeedHistory.Count;
        float stdDev = Mathf.Sqrt(variance);
        
        // 一致性评分（标准差越小，一致性越高）
        return Mathf.Clamp01(1f - stdDev / 2f);
    }
    
    /// <summary>
    /// 更新分析历史 - 优化版本
    /// </summary>
    private void UpdateAnalysisHistory()
    {
        movementSpeedHistory.Add(currentMovementSpeed);
        bodyOpennesHistory.Add(currentBodyOpenness);
        bodyStabilityHistory.Add(currentBodyStability);
        
        // 严格限制历史记录大小防止内存泄漏
        int maxHistorySize = Mathf.Min(movementHistorySize, MAX_HISTORY_SIZE);
        
        while (movementSpeedHistory.Count > maxHistorySize)
        {
            movementSpeedHistory.RemoveAt(0);
        }
        
        while (bodyOpennesHistory.Count > maxHistorySize)
        {
            bodyOpennesHistory.RemoveAt(0);
        }
        
        while (bodyStabilityHistory.Count > maxHistorySize)
        {
            bodyStabilityHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 更新数据有效性
    /// </summary>
    private void UpdateDataValidity()
    {
        TimeSpan timeSinceLastData = DateTime.Now - lastAnalysisTime;
        hasRecentData = timeSinceLastData.TotalSeconds < dataValidityPeriod;
        
        if (!hasRecentData && showAnalysisLogs)
        {
            Debug.Log("🤸 Pose data expired, waiting for new pose input...");
        }
    }
    
    // === Public Interface ===
    
    /// <summary>
    /// 是否有最近的有效数据
    /// </summary>
    public bool HasRecentData()
    {
        return hasRecentData && enablePoseAnalysis;
    }
    
    /// <summary>
    /// 获取当前姿态情感数据
    /// </summary>
    public EmotionDetectionSystem.EmotionData GetCurrentEmotion()
    {
        return currentPoseEmotion;
    }
    
    /// <summary>
    /// 获取当前姿态特征
    /// </summary>
    public (float speed, float openness, float stability, float tilt) GetCurrentPoseFeatures()
    {
        return (currentMovementSpeed, currentBodyOpenness, currentBodyStability, currentBodyTilt);
    }
    
    /// <summary>
    /// 重置分析数据
    /// </summary>
    public void ResetAnalysisData()
    {
        poseHistory.Clear();
        movementSpeedHistory.Clear();
        bodyOpennesHistory.Clear();
        bodyStabilityHistory.Clear();
        currentPoseEmotion = new EmotionDetectionSystem.EmotionData();
        hasRecentData = false;
        hasLastValidPose = false;
        
        if (showAnalysisLogs)
            Debug.Log("🤸 Pose analysis data reset");
    }
    
    /// <summary>
    /// 简单的UI显示
    /// </summary>
    void OnGUI()
    {
        if (!showPoseUI || !hasRecentData) return;
        
        GUI.Box(new Rect(Screen.width - 320, 160, 300, 140), "Pose Analysis Status");
        
        GUI.Label(new Rect(Screen.width - 310, 185, 280, 20), $"Movement Speed: {currentMovementSpeed:F2}");
        GUI.Label(new Rect(Screen.width - 310, 205, 280, 20), $"Body Openness: {currentBodyOpenness:F2}");
        GUI.Label(new Rect(Screen.width - 310, 225, 280, 20), $"Body Stability: {currentBodyStability:F2}");
        GUI.Label(new Rect(Screen.width - 310, 245, 280, 20), $"Body Tilt: {currentBodyTilt:F1}°");
        GUI.Label(new Rect(Screen.width - 310, 265, 280, 20), $"Emotion Intensity: {currentPoseEmotion.intensity:F2}");
        GUI.Label(new Rect(Screen.width - 310, 285, 280, 20), $"Confidence: {currentPoseEmotion.confidence:F2}");
    }
} 