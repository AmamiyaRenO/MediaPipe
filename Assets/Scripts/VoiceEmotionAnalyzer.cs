using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Voice Emotion Analyzer - Analyzes user emotion state based on voice characteristics
/// </summary>
public class VoiceEmotionAnalyzer : MonoBehaviour
{
    [Header("=== Analysis Configuration ===")]
    [Tooltip("Enable voice emotion analysis")]
    public bool enableVoiceAnalysis = true;
    
    [Tooltip("Audio data buffer size")]
    public int bufferSize = 1024;
    
    [Tooltip("Data validity period (seconds)")]
    public float dataValidityPeriod = 5.0f;
    
    [Header("=== Emotion Detection Thresholds ===")]
    [Tooltip("Volume threshold for voice detection (0.01-0.1)")]
    public float volumeThreshold = 0.02f; // 降低阈值提高灵敏度
    
    [Tooltip("High frequency energy threshold (excitement detection)")]
    public float highFreqThreshold = 0.5f;
    
    [Tooltip("Minimum speech frames for validation")]
    public int minSpeechFrames = 3; // 新增：需要连续帧才算有效语音
    
    [Tooltip("Speech rate detection window size")]
    public int speechRateWindow = 10;
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("Voice processor component")]
    public VoiceProcessor voiceProcessor;
    
    [Tooltip("Vosk speech recognizer")]
    public VoskSpeechToText voskSpeech;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show analysis logs in console")]
    public bool showAnalysisLogs = false;
    
    [Tooltip("Display voice analysis data on UI")]
    public bool showVoiceUI = true;
    
    public Action<EmotionDetectionSystem.EmotionData> OnVoiceEmotionDetected;
    
    // 音频数据相关
    private float[] audioBuffer;
    private List<float> volumeHistory = new List<float>();
    private List<float> pitchHistory = new List<float>();
    private List<float> speechRateHistory = new List<float>();
    
    // 语音活动检测相关 - 新增
    private List<bool> voiceActivityHistory = new List<bool>();
    private int consecutiveSilenceFrames = 0;
    private int consecutiveVoiceFrames = 0;
    private float noiseFloor = 0.01f;
    
    // 分析结果
    private EmotionDetectionSystem.EmotionData currentVoiceEmotion;
    private DateTime lastAnalysisTime;
    private bool hasRecentData = false;
    
    // 内部分析参数
    private float currentVolume = 0f;
    private float currentPitch = 0f;
    private float currentSpeechRate = 0f;
    private float averageVolume = 0f;
    private float volumeVariance = 0f;
    
    // 频谱分析相关
    private float[] spectrum = new float[256];
    private float lowFreqEnergy = 0f;
    private float midFreqEnergy = 0f;
    private float highFreqEnergy = 0f;
    
    // 语音识别相关
    private List<DateTime> speechEventTimes = new List<DateTime>();
    private string lastRecognizedText = "";
    private DateTime lastSpeechTime;
    
    // 性能优化参数 - 新增
    private const int MAX_HISTORY_LENGTH = 50; // 严格限制历史长度
    private const int MAX_SPEECH_EVENTS = 20; // 限制语音事件历史
    private float lastCleanupTime = 0f;
    private const float CLEANUP_INTERVAL = 30f; // 30秒清理一次

    void Awake()
    {
        InitializeComponents();
        InitializeAudioBuffer();
        currentVoiceEmotion = new EmotionDetectionSystem.EmotionData();
        lastAnalysisTime = DateTime.Now;
        
        // 初始化噪声基线
        StartCoroutine(CalibrateNoiseFloor());
    }

    void Start()
    {
        SetupVoiceProcessorEvents();
        
        if (showAnalysisLogs)
            Debug.Log("🎤 Voice Emotion Analyzer initialized with enhanced VAD");
    }

    void Update()
    {
        if (!enableVoiceAnalysis) return;
        
        // 定期清理历史数据
        if (Time.time - lastCleanupTime >= CLEANUP_INTERVAL)
        {
            CleanupHistoryData();
            lastCleanupTime = Time.time;
        }
        
        // 定期分析音频数据
        AnalyzeAudioFeatures();
        
        // 检查数据有效性
        UpdateDataValidity();
        
        // 快捷键强制启用UI
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceEnableUI();
        }
    }
    
    /// <summary>
    /// 噪声基线校准协程
    /// </summary>
    private System.Collections.IEnumerator CalibrateNoiseFloor()
    {
        yield return new WaitForSeconds(2f); // 等待2秒进行校准
        
        if (volumeHistory.Count > 10)
        {
            // 使用最低的10%音量作为噪声基线
            var sortedVolumes = new List<float>(volumeHistory);
            sortedVolumes.Sort();
            int calibrationCount = Mathf.Max(1, sortedVolumes.Count / 10);
            noiseFloor = sortedVolumes.Take(calibrationCount).Average();
            
            if (showAnalysisLogs)
                Debug.Log($"🎤 Noise floor calibrated: {noiseFloor:F4}");
        }
    }
    
    /// <summary>
    /// 清理历史数据防止内存泄漏
    /// </summary>
    private void CleanupHistoryData()
    {
        // 限制各种历史数据的长度
        TrimList(volumeHistory, MAX_HISTORY_LENGTH);
        TrimList(pitchHistory, MAX_HISTORY_LENGTH);
        TrimList(speechRateHistory, MAX_HISTORY_LENGTH);
        TrimList(voiceActivityHistory, MAX_HISTORY_LENGTH);
        
        // 清理过期的语音事件
        DateTime cutoffTime = DateTime.Now.AddSeconds(-dataValidityPeriod * 2);
        speechEventTimes.RemoveAll(time => time < cutoffTime);
        
        // 限制语音事件总数
        if (speechEventTimes.Count > MAX_SPEECH_EVENTS)
        {
            speechEventTimes.RemoveRange(0, speechEventTimes.Count - MAX_SPEECH_EVENTS);
        }
        
        if (showAnalysisLogs)
        {
            Debug.Log($"🎤 Cleaned up history data. Counts: Vol={volumeHistory.Count}, " +
                     $"Pitch={pitchHistory.Count}, Events={speechEventTimes.Count}");
        }
    }
    
    /// <summary>
    /// 限制列表长度的辅助方法
    /// </summary>
    private void TrimList<T>(List<T> list, int maxLength)
    {
        while (list.Count > maxLength)
        {
            list.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        if (voiceProcessor == null)
            voiceProcessor = FindObjectOfType<VoiceProcessor>();
            
        if (voskSpeech == null)
            voskSpeech = FindObjectOfType<VoskSpeechToText>();
    }
    
    /// <summary>
    /// 初始化音频缓冲区
    /// </summary>
    private void InitializeAudioBuffer()
    {
        audioBuffer = new float[bufferSize];
    }
    
    /// <summary>
    /// 设置语音处理器事件
    /// </summary>
    private void SetupVoiceProcessorEvents()
    {
        if (voiceProcessor != null)
        {
            voiceProcessor.OnFrameCaptured += OnAudioFrameCaptured;
        }
        
        if (voskSpeech != null)
        {
            voskSpeech.OnTranscriptionResult += OnSpeechRecognized;
        }
    }
    
    /// <summary>
    /// 音频帧捕获回调
    /// </summary>
    private void OnAudioFrameCaptured(short[] audioData)
    {
        if (!enableVoiceAnalysis || audioData == null || audioData.Length == 0)
            return;
        
        // 转换为float数组并存储
        int copyLength = Mathf.Min(audioData.Length, audioBuffer.Length);
        for (int i = 0; i < copyLength; i++)
        {
            audioBuffer[i] = audioData[i] / (float)short.MaxValue;
        }
        
        // 立即分析当前帧
        AnalyzeCurrentFrame();
        hasRecentData = true;
        lastAnalysisTime = DateTime.Now;
    }
    
    /// <summary>
    /// 语音识别结果回调
    /// </summary>
    private void OnSpeechRecognized(string recognizedText)
    {
        if (!enableVoiceAnalysis) return;
        
        lastRecognizedText = recognizedText;
        lastSpeechTime = DateTime.Now;
        speechEventTimes.Add(DateTime.Now);
        
        // 计算语速
        CalculateSpeechRate();
        
        // 分析语音内容情感（基于关键词）
        AnalyzeSpeechContent(recognizedText);
        
        if (showAnalysisLogs)
        {
            Debug.Log($"🎤 Speech recognized: {recognizedText}, Current speech rate: {currentSpeechRate:F2}");
        }
    }
    
    /// <summary>
    /// 分析当前音频帧 - 优化版本，加入语音活动检测
    /// </summary>
    private void AnalyzeCurrentFrame()
    {
        // 计算音量
        currentVolume = CalculateVolume();
        volumeHistory.Add(currentVolume);
        
        // 语音活动检测 - 更智能的检测
        bool isVoiceActive = DetectVoiceActivity(currentVolume);
        voiceActivityHistory.Add(isVoiceActive);
        
        // 只有在检测到有效语音时才进行详细分析
        if (isVoiceActive && consecutiveVoiceFrames >= minSpeechFrames)
        {
            // 计算基频（音调估计）
            currentPitch = EstimatePitch();
            pitchHistory.Add(currentPitch);
            
            // 频谱分析
            PerformSpectrumAnalysis();
            
            // 计算统计特征
            CalculateAudioStatistics();
        }
        else
        {
            // 静音时添加默认值
            pitchHistory.Add(0f);
        }
        
        // 保持历史数据数量限制（在这里只是保险，主要清理在CleanupHistoryData中）
        if (volumeHistory.Count > MAX_HISTORY_LENGTH * 2)
        {
            TrimList(volumeHistory, MAX_HISTORY_LENGTH);
            TrimList(pitchHistory, MAX_HISTORY_LENGTH);
            TrimList(voiceActivityHistory, MAX_HISTORY_LENGTH);
        }
    }
    
    /// <summary>
    /// 语音活动检测 - 新增方法
    /// </summary>
    private bool DetectVoiceActivity(float volume)
    {
        // 动态阈值：噪声基线 + 固定阈值
        float dynamicThreshold = noiseFloor + volumeThreshold;
        bool isAboveThreshold = volume > dynamicThreshold;
        
        if (isAboveThreshold)
        {
            consecutiveVoiceFrames++;
            consecutiveSilenceFrames = 0;
        }
        else
        {
            consecutiveSilenceFrames++;
            consecutiveVoiceFrames = 0;
        }
        
        // 需要连续多帧超过阈值才认为是语音
        bool isValidVoice = consecutiveVoiceFrames >= minSpeechFrames;
        
        if (showAnalysisLogs && isValidVoice && consecutiveVoiceFrames == minSpeechFrames)
        {
            Debug.Log($"🎤 Voice activity detected! Volume: {volume:F4}, Threshold: {dynamicThreshold:F4}");
        }
        
        return isValidVoice;
    }
    
    /// <summary>
    /// 计算音量（RMS）
    /// </summary>
    private float CalculateVolume()
    {
        float sum = 0f;
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            sum += audioBuffer[i] * audioBuffer[i];
        }
        return Mathf.Sqrt(sum / audioBuffer.Length);
    }
    
    /// <summary>
    /// 估计基频（简单的自相关方法）
    /// </summary>
    private float EstimatePitch()
    {
        // 简化的基频估计 - 在实际应用中可能需要更复杂的算法
        const int minPeriod = 20;  // 最小周期 (对应高频)
        const int maxPeriod = 200; // 最大周期 (对应低频)
        
        float maxCorrelation = 0f;
        int bestPeriod = minPeriod;
        
        for (int period = minPeriod; period < maxPeriod && period < audioBuffer.Length / 2; period++)
        {
            float correlation = 0f;
            for (int i = 0; i < audioBuffer.Length - period; i++)
            {
                correlation += audioBuffer[i] * audioBuffer[i + period];
            }
            
            correlation /= (audioBuffer.Length - period);
            
            if (correlation > maxCorrelation)
            {
                maxCorrelation = correlation;
                bestPeriod = period;
            }
        }
        
        // 转换为频率 (Hz) - 假设采样率为16kHz
        float sampleRate = 16000f;
        return sampleRate / bestPeriod;
    }
    
    /// <summary>
    /// 执行频谱分析
    /// </summary>
    private void PerformSpectrumAnalysis()
    {
        // 简化的频谱分析 - 计算不同频段的能量
        int spectrumSize = Mathf.Min(spectrum.Length, audioBuffer.Length / 2);
        
        // 计算功率谱（简化版本）
        for (int i = 0; i < spectrumSize; i++)
        {
            if (i * 2 + 1 < audioBuffer.Length)
            {
                float real = audioBuffer[i * 2];
                float imag = i * 2 + 1 < audioBuffer.Length ? audioBuffer[i * 2 + 1] : 0;
                spectrum[i] = real * real + imag * imag;
            }
        }
        
        // 计算不同频段的能量
        int lowEnd = spectrumSize / 8;     // 低频段
        int midEnd = spectrumSize / 2;     // 中频段
        
        lowFreqEnergy = 0f;
        midFreqEnergy = 0f;
        highFreqEnergy = 0f;
        
        for (int i = 0; i < lowEnd; i++)
            lowFreqEnergy += spectrum[i];
        for (int i = lowEnd; i < midEnd; i++)
            midFreqEnergy += spectrum[i];
        for (int i = midEnd; i < spectrumSize; i++)
            highFreqEnergy += spectrum[i];
        
        // 归一化
        float totalEnergy = lowFreqEnergy + midFreqEnergy + highFreqEnergy;
        if (totalEnergy > 0)
        {
            lowFreqEnergy /= totalEnergy;
            midFreqEnergy /= totalEnergy;
            highFreqEnergy /= totalEnergy;
        }
    }
    
    /// <summary>
    /// 计算音频统计特征
    /// </summary>
    private void CalculateAudioStatistics()
    {
        if (volumeHistory.Count > 0)
        {
            averageVolume = volumeHistory.Average();
            
            // 计算音量方差（情感强度指标）
            float sumSquaredDiff = 0f;
            foreach (float vol in volumeHistory)
            {
                float diff = vol - averageVolume;
                sumSquaredDiff += diff * diff;
            }
            volumeVariance = sumSquaredDiff / volumeHistory.Count;
        }
    }
    
    /// <summary>
    /// 计算语速
    /// </summary>
    private void CalculateSpeechRate()
    {
        // 清理过期的语音事件
        DateTime cutoffTime = DateTime.Now.AddSeconds(-10); // 10秒窗口
        speechEventTimes.RemoveAll(time => time < cutoffTime);
        
        // 计算语速（每分钟单词数的简化版本）
        if (speechEventTimes.Count >= 2)
        {
            var recentEvents = speechEventTimes.TakeLast(speechRateWindow).ToList();
            if (recentEvents.Count >= 2)
            {
                TimeSpan timeSpan = recentEvents.Last() - recentEvents.First();
                if (timeSpan.TotalSeconds > 0)
                {
                    currentSpeechRate = (recentEvents.Count - 1) / (float)timeSpan.TotalMinutes;
                }
            }
        }
        
        speechRateHistory.Add(currentSpeechRate);
        if (speechRateHistory.Count > 20)
            speechRateHistory.RemoveAt(0);
    }
    
    /// <summary>
    /// 分析语音内容的情感倾向
    /// </summary>
    private void AnalyzeSpeechContent(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        text = text.ToLower();
        
        // 简单的关键词情感分析
        float contentValence = 0f;
        float contentArousal = 0f;
        
        // 正面词汇
        string[] positiveWords = { "good", "great", "excellent", "amazing", "wonderful", "happy", "love", "like", "yes", "好", "棒", "很好", "喜欢" };
        // 负面词汇
        string[] negativeWords = { "bad", "terrible", "awful", "hate", "no", "stop", "wrong", "error", "坏", "糟糕", "不好", "讨厌", "停止" };
        // 高兴奋度词汇
        string[] excitedWords = { "wow", "amazing", "incredible", "fantastic", "excited", "哇", "太棒了", "兴奋", "激动" };
        
        foreach (string word in positiveWords)
        {
            if (text.Contains(word))
                contentValence += 0.2f;
        }
        
        foreach (string word in negativeWords)
        {
            if (text.Contains(word))
                contentValence -= 0.2f;
        }
        
        foreach (string word in excitedWords)
        {
            if (text.Contains(word))
                contentArousal += 0.3f;
        }
        
        // 更新当前情感数据的内容分析部分
        currentVoiceEmotion.valence = Mathf.Lerp(currentVoiceEmotion.valence, contentValence, 0.3f);
        currentVoiceEmotion.arousal = Mathf.Lerp(currentVoiceEmotion.arousal, contentArousal, 0.3f);
    }
    
    /// <summary>
    /// 分析音频特征
    /// </summary>
    private void AnalyzeAudioFeatures()
    {
        if (!hasRecentData || volumeHistory.Count < 10) return;
        
        var emotion = new EmotionDetectionSystem.EmotionData();
        
        // 基于音量分析兴奋度 (arousal)
        float volumeArousal = 0f;
        if (averageVolume > volumeThreshold)
        {
            volumeArousal = Mathf.Clamp01((averageVolume - volumeThreshold) * 2f);
            // 音量变化大表示情感强烈
            volumeArousal += Mathf.Clamp01(volumeVariance * 10f);
        }
        
        // 基于高频能量分析兴奋度
        float freqArousal = Mathf.Clamp01(highFreqEnergy * 2f);
        
        // 基于语速分析兴奋度
        float speechArousal = 0f;
        if (speechRateHistory.Count > 0)
        {
            float avgSpeechRate = speechRateHistory.Average();
            speechArousal = Mathf.Clamp01((avgSpeechRate - 60f) / 120f); // 正常语速60词/分钟
        }
        
        // 综合兴奋度
        emotion.arousal = (volumeArousal * 0.4f + freqArousal * 0.3f + speechArousal * 0.3f);
        
        // 基于音调分析情感价值 (valence)
        float pitchValence = 0f;
        if (pitchHistory.Count > 5)
        {
            float avgPitch = pitchHistory.TakeLast(10).Average();
            float pitchVariance = 0f;
            var recentPitches = pitchHistory.TakeLast(10);
            foreach (float pitch in recentPitches)
            {
                float diff = pitch - avgPitch;
                pitchVariance += diff * diff;
            }
            pitchVariance /= recentPitches.Count();
            
            // 较高的音调通常表示积极情感
            if (avgPitch > 150f) // 基线频率
            {
                pitchValence = Mathf.Clamp01((avgPitch - 150f) / 200f);
            }
            else
            {
                pitchValence = -Mathf.Clamp01((150f - avgPitch) / 100f);
            }
            
            // 音调变化大可能表示情感丰富
            emotion.intensity = Mathf.Clamp01(pitchVariance / 1000f);
        }
        
        emotion.valence = pitchValence;
        
        // 计算置信度
        float dataQuality = Mathf.Clamp01(averageVolume / 0.5f);
        float historyQuality = Mathf.Clamp01(volumeHistory.Count / 50f);
        emotion.confidence = (dataQuality + historyQuality) * 0.5f;
        
        // 情感强度基于多个因素
        emotion.intensity = Mathf.Clamp01((volumeVariance * 10f + emotion.arousal + Mathf.Abs(emotion.valence)) / 3f);
        
        // 更新当前情感状态
        currentVoiceEmotion = emotion;
        
        // 触发情感检测事件
        OnVoiceEmotionDetected?.Invoke(emotion);
        
        if (showAnalysisLogs && emotion.confidence > 0.3f)
        {
            Debug.Log($"🎤 Voice emotion analysis: Arousal={emotion.arousal:F2}, Valence={emotion.valence:F2}, Intensity={emotion.intensity:F2}, Confidence={emotion.confidence:F2}");
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
            Debug.Log("🎤 Voice data expired, waiting for new audio input...");
        }
    }
    
    // === Public Interface ===
    
    /// <summary>
    /// 是否有最近的有效数据
    /// </summary>
    public bool HasRecentData()
    {
        return hasRecentData && enableVoiceAnalysis;
    }
    
    /// <summary>
    /// 获取当前语音情感数据
    /// </summary>
    public EmotionDetectionSystem.EmotionData GetCurrentEmotion()
    {
        return currentVoiceEmotion;
    }
    
    /// <summary>
    /// 获取当前音频特征
    /// </summary>
    public (float volume, float pitch, float speechRate) GetCurrentAudioFeatures()
    {
        return (averageVolume, currentPitch, currentSpeechRate);
    }
    
    /// <summary>
    /// 重置分析数据
    /// </summary>
    public void ResetAnalysisData()
    {
        volumeHistory.Clear();
        pitchHistory.Clear();
        speechRateHistory.Clear();
        speechEventTimes.Clear();
        voiceActivityHistory.Clear(); // 新增
        
        currentVolume = 0f;
        currentPitch = 0f;
        currentSpeechRate = 0f;
        averageVolume = 0f;
        volumeVariance = 0f;
        
        // 重置语音活动检测状态
        consecutiveSilenceFrames = 0;
        consecutiveVoiceFrames = 0;
        
        currentVoiceEmotion = new EmotionDetectionSystem.EmotionData();
        hasRecentData = false;
        
        if (showAnalysisLogs)
            Debug.Log("🎤 Voice analysis data reset with VAD state");
    }
    
    /// <summary>
    /// 强制启用UI显示 - 用于调试
    /// </summary>
    public void ForceEnableUI()
    {
        showVoiceUI = true;
        showAnalysisLogs = true;
        
        Debug.Log("🎤 Voice UI forcefully enabled for debugging");
    }
    
    /// <summary>
    /// 简单的UI显示
    /// </summary>
    void OnGUI()
    {
        if (!showVoiceUI) return;
        
        GUI.Box(new Rect(Screen.width - 320, 10, 300, 160), "Voice Analysis Status");
        
        // 显示基本状态
        GUI.Label(new Rect(Screen.width - 310, 35, 280, 20), $"Analysis Enabled: {enableVoiceAnalysis}");
        GUI.Label(new Rect(Screen.width - 310, 55, 280, 20), $"Has Recent Data: {hasRecentData}");
        
        if (hasRecentData)
        {
            // 显示详细数据
            GUI.Label(new Rect(Screen.width - 310, 75, 280, 20), $"Volume: {averageVolume:F3}");
            GUI.Label(new Rect(Screen.width - 310, 95, 280, 20), $"Pitch: {currentPitch:F1} Hz");
            GUI.Label(new Rect(Screen.width - 310, 115, 280, 20), $"Speech Rate: {currentSpeechRate:F1} words/minute");
            GUI.Label(new Rect(Screen.width - 310, 135, 280, 20), $"Emotion Intensity: {currentVoiceEmotion.intensity:F2}");
            GUI.Label(new Rect(Screen.width - 310, 155, 280, 20), $"Confidence: {currentVoiceEmotion.confidence:F2}");
        }
        else
        {
            // 显示等待状态
            GUI.Label(new Rect(Screen.width - 310, 75, 280, 20), "Waiting for voice input...");
            GUI.Label(new Rect(Screen.width - 310, 95, 280, 20), $"Volume Threshold: {volumeThreshold:F3}");
            GUI.Label(new Rect(Screen.width - 310, 115, 280, 20), $"Noise Floor: {noiseFloor:F4}");
            GUI.Label(new Rect(Screen.width - 310, 135, 280, 20), $"Voice Frames Needed: {minSpeechFrames}");
            
            // 显示当前音量（即使没有有效数据）
            if (volumeHistory.Count > 0)
            {
                float currentVol = volumeHistory.LastOrDefault();
                GUI.Label(new Rect(Screen.width - 310, 155, 280, 20), $"Current Volume: {currentVol:F4}");
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (voiceProcessor != null)
        {
            voiceProcessor.OnFrameCaptured -= OnAudioFrameCaptured;
        }
        
        if (voskSpeech != null)
        {
            voskSpeech.OnTranscriptionResult -= OnSpeechRecognized;
        }
    }
} 