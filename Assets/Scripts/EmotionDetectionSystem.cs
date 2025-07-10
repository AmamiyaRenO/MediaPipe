using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 情感检测系统 - 整合语音和姿态分析的核心管理器
/// </summary>
public class EmotionDetectionSystem : MonoBehaviour
{
    [Header("=== System Configuration ===")]
    [Tooltip("Enable emotion detection")]
    public bool enableEmotionDetection = true;
    
    [Tooltip("Emotion analysis update frequency (seconds)")]
    public float analysisInterval = 1.0f;
    
    [Tooltip("Emotion state smoothing factor (0-1)")]
    [Range(0f, 1f)]
    public float emotionSmoothingFactor = 0.3f;
    
    [Header("=== Input Source Selection ===")]
    [Tooltip("Use voice analysis for emotion detection")]
    public bool useVoiceInput = true;
    
    [Tooltip("Use pose analysis for emotion detection")]
    public bool usePoseInput = true;
    
    [Tooltip("Voice analysis weight when both inputs are enabled")]
    [Range(0f, 1f)]
    public float voiceInputWeight = 0.6f;
    
    [Tooltip("Pose analysis weight when both inputs are enabled")]
    [Range(0f, 1f)]
    public float poseInputWeight = 0.4f;
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("Voice emotion analyzer")]
    public VoiceEmotionAnalyzer voiceAnalyzer;
    
    [Tooltip("Pose emotion analyzer")]
    public PoseEmotionAnalyzer poseAnalyzer;
    
    [Tooltip("Advanced logging system")]
    public AdvancedLoggingSystem loggingSystem;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show detailed emotion analysis logs")]
    public bool showEmotionLogs = false;
    
    [Tooltip("Display current emotion state on UI")]
    public bool showEmotionUI = false;
    
    // Emotion state enumeration
    public enum EmotionState
    {
        Neutral,     // Neutral
        Happy,       // Happy
        Excited,     // Excited
        Frustrated,  // Frustrated
        Angry,       // Angry
        Calm,        // Calm
        Focused,     // Focused
        Stressed     // Stressed
    }
    
    // Emotion data structure
    [System.Serializable]
    public class EmotionData
    {
        public EmotionState primaryEmotion = EmotionState.Neutral;
        public float confidence = 0f;           // Confidence 0-1
        public float arousal = 0f;              // Arousal -1 to 1
        public float valence = 0f;              // Valence -1 to 1 (negative to positive)
        public float intensity = 0f;            // Intensity 0-1
        public DateTime timestamp = DateTime.Now;
        
        // Dimension weights
        public float voiceWeight = 0.5f;        // Voice analysis weight
        public float poseWeight = 0.5f;         // Pose analysis weight
        
        public EmotionData()
        {
            timestamp = DateTime.Now;
        }
        
        public override string ToString()
        {
            return $"Emotion: {primaryEmotion}, Confidence: {confidence:F2}, Arousal: {arousal:F2}, Valence: {valence:F2}";
        }
    }
    
    // Current emotion state
    public EmotionData CurrentEmotion { get; private set; }
    
    // Emotion history records (last 10 states)
    private Queue<EmotionData> emotionHistory = new Queue<EmotionData>();
    private const int MAX_HISTORY_COUNT = 10;
    
    // Emotion change events
    public Action<EmotionData> OnEmotionChanged;
    public Action<EmotionState, EmotionState> OnEmotionStateChanged; // Old state, new state
    
    // Internal timer
    private float lastAnalysisTime = 0f;
    
    // Temporary emotion data storage
    private EmotionData tempVoiceEmotion = new EmotionData();
    private EmotionData tempPoseEmotion = new EmotionData();
    
    void Awake()
    {
        InitializeComponents();
        CurrentEmotion = new EmotionData();
    }
    
    void Start()
    {
        if (showEmotionLogs)
            Debug.Log("🧠 Emotion Detection System Initialized");
            
        // Subscribe to component events
        SubscribeToAnalyzerEvents();
        
        // Default to voice-only mode for debugging voice functionality
        // You can change this in the Inspector or call SetVoiceOnlyMode() at runtime
        if (useVoiceInput && !usePoseInput)
        {
            Debug.Log("🧠 Starting in Voice-Only mode for voice feature debugging");
        }
    }
    
    void Update()
    {
        if (!enableEmotionDetection) return;
        
        // Perform emotion analysis periodically
        if (Time.time - lastAnalysisTime >= analysisInterval)
        {
            PerformEmotionAnalysis();
            lastAnalysisTime = Time.time;
        }
    }
    
    /// <summary>
    /// Initialize components
    /// </summary>
    private void InitializeComponents()
    {
        // Auto-find components
        if (voiceAnalyzer == null)
            voiceAnalyzer = GetComponent<VoiceEmotionAnalyzer>();
            
        if (poseAnalyzer == null)
            poseAnalyzer = GetComponent<PoseEmotionAnalyzer>();
            
        if (loggingSystem == null)
            loggingSystem = FindObjectOfType<AdvancedLoggingSystem>();
    }
    
    /// <summary>
    /// Subscribe to analyzer events
    /// </summary>
    private void SubscribeToAnalyzerEvents()
    {
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnVoiceEmotionDetected += OnVoiceEmotionReceived;
        }
        
        if (poseAnalyzer != null)
        {
            poseAnalyzer.OnPoseEmotionDetected += OnPoseEmotionReceived;
        }
    }
    
    /// <summary>
    /// Perform comprehensive emotion analysis
    /// </summary>
    private void PerformEmotionAnalysis()
    {
        var newEmotion = AnalyzeEmotionFromMultipleSources();
        
        // Apply smoothing
        newEmotion = ApplyEmotionSmoothing(CurrentEmotion, newEmotion);
        
        // Check if emotion state has changed significantly
        bool emotionChanged = HasEmotionChanged(CurrentEmotion, newEmotion);
        EmotionState oldState = CurrentEmotion.primaryEmotion;
        
        // Update current emotion state
        CurrentEmotion = newEmotion;
        
        // Add to history
        AddToEmotionHistory(newEmotion);
        
        // Trigger events
        if (emotionChanged)
        {
            OnEmotionChanged?.Invoke(CurrentEmotion);
            OnEmotionStateChanged?.Invoke(oldState, CurrentEmotion.primaryEmotion);
            
            if (showEmotionLogs)
            {
                Debug.Log($"🧠 Emotion State Changed: {oldState} → {CurrentEmotion.primaryEmotion} (Confidence: {CurrentEmotion.confidence:F2})");
            }
        }
        
        // Log to logging system
        if (loggingSystem != null)
        {
            loggingSystem.LogEmotionEvent(CurrentEmotion);
        }
    }
    
    /// <summary>
    /// Analyze comprehensive emotion from selected sources
    /// </summary>
    private EmotionData AnalyzeEmotionFromMultipleSources()
    {
        var combinedEmotion = new EmotionData();
        
        float voiceWeight = 0f;
        float poseWeight = 0f;
        bool hasVoiceData = false;
        bool hasPoseData = false;
        
        // Collect voice emotion data (only if enabled)
        if (useVoiceInput && voiceAnalyzer != null && voiceAnalyzer.HasRecentData())
        {
            tempVoiceEmotion = voiceAnalyzer.GetCurrentEmotion();
            voiceWeight = voiceInputWeight;
            hasVoiceData = true;
            
            if (showEmotionLogs)
                Debug.Log($"🎤 Using voice input: Arousal={tempVoiceEmotion.arousal:F2}, Valence={tempVoiceEmotion.valence:F2}");
        }
        
        // Collect pose emotion data (only if enabled)
        if (usePoseInput && poseAnalyzer != null && poseAnalyzer.HasRecentData())
        {
            tempPoseEmotion = poseAnalyzer.GetCurrentEmotion();
            poseWeight = poseInputWeight;
            hasPoseData = true;
            
            if (showEmotionLogs)
                Debug.Log($"🤸 Using pose input: Arousal={tempPoseEmotion.arousal:F2}, Valence={tempPoseEmotion.valence:F2}");
        }
        
        // Handle different input scenarios
        if (hasVoiceData && hasPoseData)
        {
            // Both sources available - combine with weights
            float totalWeight = voiceWeight + poseWeight;
            if (totalWeight > 0)
            {
                voiceWeight /= totalWeight;
                poseWeight /= totalWeight;
                
                combinedEmotion.arousal = (tempVoiceEmotion.arousal * voiceWeight) + (tempPoseEmotion.arousal * poseWeight);
                combinedEmotion.valence = (tempVoiceEmotion.valence * voiceWeight) + (tempPoseEmotion.valence * poseWeight);
                combinedEmotion.intensity = (tempVoiceEmotion.intensity * voiceWeight) + (tempPoseEmotion.intensity * poseWeight);
                combinedEmotion.confidence = (tempVoiceEmotion.confidence * voiceWeight) + (tempPoseEmotion.confidence * poseWeight);
            }
        }
        else if (hasVoiceData && !hasPoseData)
        {
            // Voice only
            combinedEmotion.arousal = tempVoiceEmotion.arousal;
            combinedEmotion.valence = tempVoiceEmotion.valence;
            combinedEmotion.intensity = tempVoiceEmotion.intensity;
            combinedEmotion.confidence = tempVoiceEmotion.confidence;
            voiceWeight = 1.0f;
            poseWeight = 0.0f;
            
            if (showEmotionLogs)
                Debug.Log("🧠 Using voice-only emotion analysis");
        }
        else if (!hasVoiceData && hasPoseData)
        {
            // Pose only
            combinedEmotion.arousal = tempPoseEmotion.arousal;
            combinedEmotion.valence = tempPoseEmotion.valence;
            combinedEmotion.intensity = tempPoseEmotion.intensity;
            combinedEmotion.confidence = tempPoseEmotion.confidence;
            voiceWeight = 0.0f;
            poseWeight = 1.0f;
            
            if (showEmotionLogs)
                Debug.Log("🧠 Using pose-only emotion analysis");
        }
        else
        {
            // No data available - maintain neutral state
            if (showEmotionLogs)
                Debug.Log("🧠 No emotion data available - maintaining neutral state");
            return combinedEmotion; // Returns neutral state
        }
        
        // Store weight information
        combinedEmotion.voiceWeight = voiceWeight;
        combinedEmotion.poseWeight = poseWeight;
        
        // Determine primary emotion state based on arousal and valence
        combinedEmotion.primaryEmotion = DetermineEmotionState(combinedEmotion.arousal, combinedEmotion.valence, combinedEmotion.intensity);
        
        if (showEmotionLogs)
        {
            Debug.Log($"🧠 Combined Emotion: {combinedEmotion.primaryEmotion} (Voice: {voiceWeight:F2}, Pose: {poseWeight:F2})");
        }
        
        return combinedEmotion;
    }
    
    /// <summary>
    /// Determine emotion state based on arousal and valence
    /// </summary>
    private EmotionState DetermineEmotionState(float arousal, float valence, float intensity)
    {
        // If intensity is very low, default to neutral
        if (intensity < 0.2f)
            return EmotionState.Neutral;
        
        // Emotion classification based on arousal-valence model
        if (valence > 0.3f) // Positive emotions
        {
            if (arousal > 0.5f)
                return EmotionState.Excited;
            else if (arousal > 0.2f)
                return EmotionState.Happy;
            else
                return EmotionState.Calm;
        }
        else if (valence < -0.3f) // Negative emotions
        {
            if (arousal > 0.6f)
                return EmotionState.Angry;
            else if (arousal > 0.3f)
                return EmotionState.Frustrated;
            else
                return EmotionState.Stressed;
        }
        else // Neutral zone
        {
            if (arousal > 0.4f)
                return EmotionState.Focused;
            else
                return EmotionState.Neutral;
        }
    }
    
    /// <summary>
    /// Apply emotion smoothing
    /// </summary>
    private EmotionData ApplyEmotionSmoothing(EmotionData current, EmotionData newEmotion)
    {
        var smoothed = new EmotionData();
        
        // Smooth continuous values
        smoothed.arousal = Mathf.Lerp(current.arousal, newEmotion.arousal, emotionSmoothingFactor);
        smoothed.valence = Mathf.Lerp(current.valence, newEmotion.valence, emotionSmoothingFactor);
        smoothed.intensity = Mathf.Lerp(current.intensity, newEmotion.intensity, emotionSmoothingFactor);
        smoothed.confidence = Mathf.Lerp(current.confidence, newEmotion.confidence, emotionSmoothingFactor);
        
        // Re-determine primary emotion state
        smoothed.primaryEmotion = DetermineEmotionState(smoothed.arousal, smoothed.valence, smoothed.intensity);
        
        // Maintain weight information
        smoothed.voiceWeight = newEmotion.voiceWeight;
        smoothed.poseWeight = newEmotion.poseWeight;
        
        return smoothed;
    }
    
    /// <summary>
    /// Check if emotion has changed significantly
    /// </summary>
    private bool HasEmotionChanged(EmotionData current, EmotionData newEmotion)
    {
        // Emotion state changed
        if (current.primaryEmotion != newEmotion.primaryEmotion)
            return true;
            
        // Significant intensity change (over 0.3)
        if (Mathf.Abs(current.intensity - newEmotion.intensity) > 0.3f)
            return true;
            
        // Significant arousal or valence change (over 0.4)
        if (Mathf.Abs(current.arousal - newEmotion.arousal) > 0.4f || 
            Mathf.Abs(current.valence - newEmotion.valence) > 0.4f)
            return true;
            
        return false;
    }
    
    /// <summary>
    /// Add to emotion history
    /// </summary>
    private void AddToEmotionHistory(EmotionData emotion)
    {
        emotionHistory.Enqueue(emotion);
        
        // Maintain history count limit
        while (emotionHistory.Count > MAX_HISTORY_COUNT)
        {
            emotionHistory.Dequeue();
        }
    }
    
    /// <summary>
    /// Voice emotion data receive callback
    /// </summary>
    private void OnVoiceEmotionReceived(EmotionData voiceEmotion)
    {
        tempVoiceEmotion = voiceEmotion;
        
        if (showEmotionLogs)
        {
            Debug.Log($"🎤 Voice Emotion: {voiceEmotion}");
        }
    }
    
    /// <summary>
    /// Pose emotion data receive callback
    /// </summary>
    private void OnPoseEmotionReceived(EmotionData poseEmotion)
    {
        tempPoseEmotion = poseEmotion;
        
        if (showEmotionLogs)
        {
            Debug.Log($"🤸 Pose Emotion: {poseEmotion}");
        }
    }
    
    // === Public Interface ===
    
    /// <summary>
    /// Get emotion history
    /// </summary>
    public Queue<EmotionData> GetEmotionHistory()
    {
        return new Queue<EmotionData>(emotionHistory);
    }
    
    /// <summary>
    /// Get average emotion state (based on history)
    /// </summary>
    public EmotionData GetAverageEmotion()
    {
        if (emotionHistory.Count == 0)
            return new EmotionData();
            
        var average = new EmotionData();
        float arousalSum = 0f, valenceSum = 0f, intensitySum = 0f, confidenceSum = 0f;
        
        foreach (var emotion in emotionHistory)
        {
            arousalSum += emotion.arousal;
            valenceSum += emotion.valence;
            intensitySum += emotion.intensity;
            confidenceSum += emotion.confidence;
        }
        
        int count = emotionHistory.Count;
        average.arousal = arousalSum / count;
        average.valence = valenceSum / count;
        average.intensity = intensitySum / count;
        average.confidence = confidenceSum / count;
        
        average.primaryEmotion = DetermineEmotionState(average.arousal, average.valence, average.intensity);
        
        return average;
    }
    
    /// <summary>
    /// Manually trigger emotion analysis
    /// </summary>
    public void TriggerEmotionAnalysis()
    {
        PerformEmotionAnalysis();
    }
    
    /// <summary>
    /// Reset emotion state
    /// </summary>
    public void ResetEmotionState()
    {
        CurrentEmotion = new EmotionData();
        emotionHistory.Clear();
        
        if (showEmotionLogs)
            Debug.Log("🧠 Emotion State Reset");
    }
    
    // === Input Source Control Methods ===
    
    /// <summary>
    /// Enable voice-only analysis mode
    /// </summary>
    public void SetVoiceOnlyMode()
    {
        useVoiceInput = true;
        usePoseInput = false;
        
        if (showEmotionLogs)
            Debug.Log("🧠 Switched to Voice-Only emotion analysis mode");
    }
    
    /// <summary>
    /// Enable pose-only analysis mode
    /// </summary>
    public void SetPoseOnlyMode()
    {
        useVoiceInput = false;
        usePoseInput = true;
        
        if (showEmotionLogs)
            Debug.Log("🧠 Switched to Pose-Only emotion analysis mode");
    }
    
    /// <summary>
    /// Enable combined analysis mode
    /// </summary>
    public void SetCombinedMode()
    {
        useVoiceInput = true;
        usePoseInput = true;
        
        if (showEmotionLogs)
            Debug.Log("🧠 Switched to Combined emotion analysis mode");
    }
    
    /// <summary>
    /// Set voice input weight (automatically adjusts pose weight)
    /// </summary>
    public void SetVoiceWeight(float weight)
    {
        voiceInputWeight = Mathf.Clamp01(weight);
        poseInputWeight = 1.0f - voiceInputWeight;
        
        if (showEmotionLogs)
            Debug.Log($"🧠 Voice weight set to {voiceInputWeight:F2}, Pose weight set to {poseInputWeight:F2}");
    }
    
    /// <summary>
    /// Get current input mode as string
    /// </summary>
    public string GetCurrentInputMode()
    {
        if (useVoiceInput && usePoseInput)
            return $"Combined (Voice: {voiceInputWeight:F1}, Pose: {poseInputWeight:F1})";
        else if (useVoiceInput)
            return "Voice Only";
        else if (usePoseInput)
            return "Pose Only";
        else
            return "No Input";
    }
    
    /// <summary>
    /// Check if any input source is enabled
    /// </summary>
    public bool HasActiveInput()
    {
        return useVoiceInput || usePoseInput;
    }
    
    /// <summary>
    /// Simple UI display
    /// </summary>
    void OnGUI()
    {
        if (!showEmotionUI || CurrentEmotion == null) return;
        
        GUI.Box(new Rect(10, 10, 350, 160), "Emotion Detection Status");
        
        GUI.Label(new Rect(20, 35, 330, 20), $"Current Emotion: {GetEmotionDisplayName(CurrentEmotion.primaryEmotion)}");
        GUI.Label(new Rect(20, 55, 330, 20), $"Confidence: {CurrentEmotion.confidence:F2}");
        GUI.Label(new Rect(20, 75, 330, 20), $"Arousal: {CurrentEmotion.arousal:F2}");
        GUI.Label(new Rect(20, 95, 330, 20), $"Valence: {CurrentEmotion.valence:F2}");
        GUI.Label(new Rect(20, 115, 330, 20), $"Intensity: {CurrentEmotion.intensity:F2}");
        GUI.Label(new Rect(20, 135, 330, 20), $"Input Mode: {GetCurrentInputMode()}");
        
        // Quick toggle buttons
        if (GUI.Button(new Rect(370, 20, 80, 25), "Voice Only"))
        {
            SetVoiceOnlyMode();
        }
        if (GUI.Button(new Rect(370, 50, 80, 25), "Pose Only"))
        {
            SetPoseOnlyMode();
        }
        if (GUI.Button(new Rect(370, 80, 80, 25), "Combined"))
        {
            SetCombinedMode();
        }
    }
    
    /// <summary>
    /// Get emotion state display name in English
    /// </summary>
    private string GetEmotionDisplayName(EmotionState emotion)
    {
        switch (emotion)
        {
            case EmotionState.Neutral: return "Neutral";
            case EmotionState.Happy: return "Happy";
            case EmotionState.Excited: return "Excited";
            case EmotionState.Frustrated: return "Frustrated";
            case EmotionState.Angry: return "Angry";
            case EmotionState.Calm: return "Calm";
            case EmotionState.Focused: return "Focused";
            case EmotionState.Stressed: return "Stressed";
            default: return emotion.ToString();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnVoiceEmotionDetected -= OnVoiceEmotionReceived;
        }
        
        if (poseAnalyzer != null)
        {
            poseAnalyzer.OnPoseEmotionDetected -= OnPoseEmotionReceived;
        }
    }
} 