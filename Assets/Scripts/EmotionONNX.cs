using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;

public class EmotionSentis : MonoBehaviour
{
    /* ─── MODEL ─────────────────────────────────────── */
    [Header("Sentis Model")]
    public ModelAsset modelAsset;                    // 拖入 model.onnx.asset
    public BackendType backend = BackendType.GPUCompute;

    /* ─── AUDIO ─────────────────────────────────────── */
    [Header("Audio Settings")]
    [Tooltip("实时使用默认麦克风（16 kHz mono）。关闭后只跑 Test Clip 一次。")]
    public bool useMic = true;
    [Tooltip("要使用的输入设备索引（Microphone.devices）。-1 为默认首个设备")]
    public int deviceIndex = -1;
    public AudioClip testClip;                       // 离线测试用
    public int windowSeconds = 2;                    // 推理窗口长度 (s)

    /* ─── INTERNAL STATE ────────────────────────────── */
    private Worker worker;
    private AudioClip micClip;
    private string micDevice;                        // 当前使用的麦克风设备名
    private int lastSamplePos;
    // 固定容量环形缓冲（零GC）存放单声道16k样本
    private float[] ringBuf;
    private int ringCapacity;                        // sampleRate * windowSeconds
    private int ringWrite;                           // 写指针
    private int ringCount;                           // 已写入样本数（<= capacity）
    private float nextAnalyzeTime;
    private float[] analysisBuffer;                  // 预分配分析缓冲

    // 设备采样率与下采样（解决C920e等48k设备导致的卡顿）
    private int deviceSampleRate = sampleRate;       // 实际录制采样率（例如C920e为48000）
    private int downsampleFactor = 1;                // 例如 48000/16000=3
    private bool needDownsample = false;
    private int deviceChannels = 1;                  // 实际通道数（C920e通常为2）
    private const int framesPerChunk = 1024;         // 分块读取帧数（每帧含N通道）
    private float[] micChunk;                        // 分块读缓冲（大小=framesPerChunk*deviceChannels）
    private AudioSource audioSrc;                    // 通过音频线程回调获取数据
    private readonly object ringLock = new object(); // 保护环形缓冲

    // 日志控制（默认关闭，避免Editor卡顿）
    [Header("Logging")]
    [Tooltip("输出控制台日志。建议仅在调试时打开。")]
    public bool logToConsole = false;
    [Tooltip("只在情感变化时输出日志，并限制最短间隔。")]
    public bool logOnlyOnChange = true;
    [Tooltip("连续两次日志输出的最短间隔（秒）")]
    public float minLogInterval = 2f;
    private float lastLogTimeSec = 0f;
    private int lastLoggedIdx = -1;

    private const int sampleRate = 16000;
    private static readonly string[] labels =
    {
        "angry","calm","disgust","fearful",
        "happy","neutral","sad","surprised"
    };

    /* ─── UNITY LIFECYCLE ───────────────────────────── */
    private void Awake()
    {
        // ① 加载模型 & 创建 Worker
        Model runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backend);
    }

    private void Start()
    {
        if (useMic)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("未检测到任何麦克风设备");
                return;
            }

            if (deviceIndex >= 0 && deviceIndex < Microphone.devices.Length)
                micDevice = Microphone.devices[deviceIndex];
            else
                micDevice = Microphone.devices[0];

            // 读取设备能力，优先用16k（模型目标），其次32k，再不行用设备最大；
            // 避免由驱动/系统隐式重采样造成的周期性停顿
            int minF, maxF;
            Microphone.GetDeviceCaps(micDevice, out minF, out maxF);
            int prefer1 = sampleRate;      // 16000
            int prefer2 = 32000;
            int prefer3 = 48000;
            deviceSampleRate = prefer1;
            if (!(minF == 0 && maxF == 0))
            {
                int lo = (minF == 0 ? prefer1 : minF);
                int hi = (maxF == 0 ? prefer3 : maxF);
                if (prefer1 < lo || prefer1 > hi)
                {
                    if (prefer2 >= lo && prefer2 <= hi) deviceSampleRate = prefer2;
                    else if (prefer3 >= lo && prefer3 <= hi) deviceSampleRate = prefer3;
                    else deviceSampleRate = hi; // 兜底
                }
            }

            // 计算是否需要下采样到模型所需的16k（若是32k或48k则抽取）
            needDownsample = (deviceSampleRate != sampleRate) && (deviceSampleRate % sampleRate == 0);
            downsampleFactor = needDownsample ? deviceSampleRate / sampleRate : 1;

            micClip = Microphone.Start(micDevice, true, 20, deviceSampleRate);   // 使用设备原生采样率
            lastSamplePos   = 0;
            nextAnalyzeTime = Time.time + windowSeconds;
            // 预分配分析缓冲，避免 ToArray 分配
            analysisBuffer = new float[sampleRate * windowSeconds];
            ringCapacity = sampleRate * windowSeconds;
            ringBuf = new float[ringCapacity];
            deviceChannels = Mathf.Max(1, micClip.channels);
            micChunk = new float[framesPerChunk * deviceChannels];

            // 创建一个静音AudioSource来驱动音频线程回调
            audioSrc = gameObject.GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.clip = micClip;
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
            audioSrc.mute = true;
            audioSrc.Play();
        }
        else if (testClip != null)
        {
            RunOnce(testClip);  // 只跑一次离线文件
        }
        else
        {
            Debug.LogWarning("没有可用音频源；请启用麦克风或指定 Test Clip。");
        }
    }

    private void Update()
    {
        if (!useMic || micClip == null) return;

        // 主线程不再拉取数据，由音频线程回调写入环形缓冲

        /* -------- 到时间就做一次情绪识别 -------- */
        if (Time.time >= nextAnalyzeTime && ringCount >= sampleRate * windowSeconds)
        {
            // 从环形缓冲复制最近 windowSeconds 的数据到 analysisBuffer
            int count = analysisBuffer.Length;
            int start = (ringWrite - count + ringCapacity) % ringCapacity;
            if (start + count <= ringCapacity)
            {
                Array.Copy(ringBuf, start, analysisBuffer, 0, count);
            }
            else
            {
                int first = ringCapacity - start;
                Array.Copy(ringBuf, start, analysisBuffer, 0, first);
                Array.Copy(ringBuf, 0, analysisBuffer, first, count - first);
            }
            RunOnce(analysisBuffer, count);
            nextAnalyzeTime += 1f;                   // 每秒跑一次
        }
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }

    /* ─── INFERENCE HELPERS ─────────────────────────── */
    private void RunOnce(AudioClip clip)
    {
        if (clip.frequency != sampleRate)
        {
            Debug.LogWarning($"Clip 需为 16 kHz；当前 {clip.frequency}");
            return;
        }

        float[] buf = new float[clip.samples];
        clip.GetData(buf, 0);
        RunOnce(buf);
    }

    private void RunOnce(float[] samples)
    {
        using var input = new Tensor<float>(
            new TensorShape(1, samples.Length), samples);

        worker.Schedule(input);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        /* softmax + argmax（全 float 版本） */
        float sum = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            logits[i] = Mathf.Exp(logits[i]);
            sum += logits[i];
        }

        int bestIdx = 0;
        float bestProb = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            logits[i] /= sum;
            if (logits[i] > bestProb)
            {
                bestProb = logits[i];
                bestIdx = i;
            }
        }

        Debug.Log($"Emotion: <b>{labels[bestIdx]}</b>  ({bestProb:P1})");
    }

    // 不分配版本：使用外部缓冲+长度
    private void RunOnce(float[] samples, int count)
    {
        using var input = new Tensor<float>(
            new TensorShape(1, count), samples);

        worker.Schedule(input);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // softmax + argmax（全 float 版本）
        float sum = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            logits[i] = Mathf.Exp(logits[i]);
            sum += logits[i];
        }

        int bestIdx = 0;
        float bestProb = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            logits[i] /= sum;
            if (logits[i] > bestProb)
            {
                bestProb = logits[i];
                bestIdx = i;
            }
        }

        // 受控日志，避免Editor卡顿
        if (logToConsole)
        {
            bool canLogByInterval = (Time.time - lastLogTimeSec) >= minLogInterval;
            bool changed = (bestIdx != lastLoggedIdx);
            if (!logOnlyOnChange || (changed && canLogByInterval))
            {
                Debug.Log($"Emotion: <b>{labels[bestIdx]}</b>  ({bestProb:P1})");
                lastLogTimeSec = Time.time;
                lastLoggedIdx = bestIdx;
            }
        }
    }

    // 在音频线程回调中读取数据，避免主线程拉取造成的节奏性阻塞
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!useMic || micClip == null) return;
        if (data == null || data.Length == 0) return;

        // data 长度 = 帧数 * channels（来自当前AudioSource的clip）
        int frames = data.Length / channels;

        if (needDownsample)
        {
            int step = downsampleFactor; // 例如 2(32k->16k) 或 3(48k->16k)
            for (int f = 0; f < frames; f += step)
            {
                float mono = 0f;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++) mono += data[baseIdx + c];
                mono /= channels;
                ringBuf[ringWrite] = mono;
                ringWrite = (ringWrite + 1) % ringCapacity;
                if (ringCount < ringCapacity) ringCount++;
            }
        }
        else
        {
            for (int f = 0; f < frames; f++)
            {
                float mono = 0f;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++) mono += data[baseIdx + c];
                mono /= channels;
                ringBuf[ringWrite] = mono;
                ringWrite = (ringWrite + 1) % ringCapacity;
                if (ringCount < ringCapacity) ringCount++;
            }
        }
    }
}
