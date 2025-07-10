using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 船体语音控制器 - 专门处理语音指令与船体控制的集成
/// </summary>
public class BoatVoiceController : MonoBehaviour
{
    [Header("=== Component Dependencies ===")]
    [Tooltip("Voice recognition component")]
    public VoskSpeechToText voiceRecognizer;
    
    [Tooltip("Boat controller")]
    public BoatController boatController;
    
    [Header("=== Voice Control Parameters ===")]
    [Tooltip("Enable voice control")]
    public bool enableVoiceControl = true;
    
    public float commandDuration = 2f;
    
    [Header("=== Debug Options ===")]
    [Tooltip("Show detailed logs")]
    public bool showDebugLogs = false;
    
    [Header("=== Supported Voice Commands ===")]
    [TextArea(5, 10)]
    public string commandHelp = @"
Supported Commands:
• forward / ahead / 前进 - Move forward
• backward / back / 后退 - Move backward  
• left / 左 - Turn left
• right / 右 - Turn right
• stop / 停止 - Stop movement
• faster / 加速 - Increase speed
• slower / 减速 - Decrease speed
";

    
    [SerializeField] private List<string> showCameraCommands = new List<string> { "show camera", "open camera", "camera on" };
    [SerializeField] private List<string> hideCameraCommands = new List<string> { "hide camera", "close camera", "camera off" };
    
    [SerializeField] private List<string> addCargoCommands = new List<string> { "add cargo", "load cargo", "add box", "add item" };
    
    [SerializeField] private List<string> morningSkyCommands = new List<string> { "morning", "sunrise", "early" };
    [SerializeField] private List<string> noonSkyCommands = new List<string> { "noon", "midday", "daytime" };
    [SerializeField] private List<string> eveningSkyCommands = new List<string> { "evening", "sunset", "dusk", "night" };
    
    [SerializeField] private List<string> startGameCommands = new List<string> { "start", "begin", "开始", "go", "play" };
    
    [SerializeField] private List<string> windLeftCommands = new List<string> { "wind left", "left wind", "turn wind left" };
    [SerializeField] private List<string> windRightCommands = new List<string> { "wind right", "right wind", "turn wind right" };
    [SerializeField] private List<string> windRandomCommands = new List<string> { "random wind", "change wind", "shuffle wind" };
    
    [Header("Camera View UI")]
    public GameObject cameraViewObject; // 拖Container Panel到这里
    
    [Header("Skybox Materials")]
    public Material skyboxMorning;
    public Material skyboxNoon;
    public Material skyboxEvening;
    
    // === 私有变量 ===
    private string lastRecognizedCommand = "";
    
    // 反射缓存（用于访问目标控制器的方法）
    private System.Reflection.MethodInfo setWindDirectionMethod;
    
    private CargoSpawner cargoSpawner;
    private GameStateManager gameStateManager;
    
    // === Unity生命周期 ===
    void Start()
    {
        InitializeComponents();
        SetupVoiceRecognition();
        CacheReflectionMethods();
        
        // 自动查找CargoSpawner
        cargoSpawner = FindObjectOfType<CargoSpawner>();
        if (cargoSpawner != null && showDebugLogs)
            Debug.Log("✅ Auto-found CargoSpawner");
            
        // 自动查找GameStateManager
        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager != null && showDebugLogs)
            Debug.Log("✅ Auto-found GameStateManager");

        if (showDebugLogs)
            Debug.Log("✅ Boat voice controller initialized");
    }
    
    void Update()
    {
        if (!enableVoiceControl) return;
    }
    
    // === 初始化方法 ===
    private void InitializeComponents()
    {
        // 自动查找组件
        if (voiceRecognizer == null)
        {
            voiceRecognizer = FindObjectOfType<VoskSpeechToText>();
            if (voiceRecognizer == null)
            {
                Debug.LogError("❌ VoskSpeechToText component not found! Please ensure a voice recognition component is in the scene.");
                enabled = false;
                return;
            }
        }
        
        if (boatController == null)
        {
            // 尝试查找BoatController
            boatController = FindObjectOfType<BoatController>();
            if (boatController == null)
            {
                Debug.LogError("❌ BoatController not found! Please specify it in the Inspector.");
                enabled = false;
                return;
            }
        }
    }
    
    private void SetupVoiceRecognition()
    {
        // 收集所有关键词
        List<string> allKeywords = new List<string>();
        allKeywords.AddRange(showCameraCommands);
        allKeywords.AddRange(hideCameraCommands);
        allKeywords.AddRange(addCargoCommands);
        allKeywords.AddRange(morningSkyCommands);
        allKeywords.AddRange(noonSkyCommands);
        allKeywords.AddRange(eveningSkyCommands);
        allKeywords.AddRange(startGameCommands);
        allKeywords.AddRange(windLeftCommands);
        allKeywords.AddRange(windRightCommands);
        allKeywords.AddRange(windRandomCommands);
        
        // 设置语音识别关键词
        voiceRecognizer.KeyPhrases = allKeywords;
        
        // 订阅事件
        voiceRecognizer.OnTranscriptionResult += OnSpeechRecognized;
        voiceRecognizer.OnStatusUpdated += OnVoiceStatusUpdated;
        
        if (showDebugLogs)
        {
            Debug.Log($"📋 Set {allKeywords.Count} voice keywords");
            Debug.Log($"🎤 Supported commands: {string.Join(", ", allKeywords)}");
        }
    }
    
    private void CacheReflectionMethods()
    {
        if (boatController == null) return;
        
        var controllerType = boatController.GetType();
        
        // 缓存风向设置方法（如果存在）
        setWindDirectionMethod = controllerType.GetMethod("SetWindDirection", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
    }
    
    // === 语音控制逻辑 ===
    
    private void OnSpeechRecognized(string recognizedText)
    {
        if (!enableVoiceControl) return;

        string command = recognizedText.ToLower().Trim();

        // 尝试解析JSON格式
        if (command.StartsWith("{") && command.Contains("\"text\""))
        {
            try
            {
                int idx = command.IndexOf("\"text\"");
                int start = command.IndexOf(":", idx) + 1;
                int quote1 = command.IndexOf("\"", start);
                int quote2 = command.IndexOf("\"", quote1 + 1);
                if (quote1 >= 0 && quote2 > quote1)
                {
                    command = command.Substring(quote1 + 1, quote2 - quote1 - 1).Trim();
                }
            }
            catch
            {
                Debug.LogWarning("Voice JSON parsing failed, original content: " + recognizedText);
            }
        }

        lastRecognizedCommand = command;

        if (showDebugLogs)
            Debug.Log($"🎤 Recognized command: '{command}'");

        ProcessVoiceCommand(command);
    }
    
    private void ProcessVoiceCommand(string command)
    {
        // 游戏开始指令（优先级最高）
        if (ContainsAnyKeyword(command, startGameCommands))
        {
            if (gameStateManager != null && !gameStateManager.gameStarted)
            {
                gameStateManager.StartGame($"Voice command: '{command}'");
                if (showDebugLogs) Debug.Log("🚀 Game started via voice command");
                return;
            }
        }
        
        // 如果游戏未开始，只处理开始指令
        if (gameStateManager != null && !gameStateManager.gameStarted)
        {
            if (showDebugLogs) Debug.Log("⏸️ Game not started, ignoring other voice commands");
            return;
        }
        
        // 摄像头画面控制
        if (ContainsAnyKeyword(command, showCameraCommands))
        {
            if (cameraViewObject != null) cameraViewObject.SetActive(true);
            if (showDebugLogs) Debug.Log("📷 Showing camera view");
            return;
        }
        if (ContainsAnyKeyword(command, hideCameraCommands))
        {
            if (cameraViewObject != null) cameraViewObject.SetActive(false);
            if (showDebugLogs) Debug.Log("📷 Hiding camera view");
            return;
        }
        
        // 添加货物
        if (ContainsAnyKeyword(command, addCargoCommands))
        {
            if (cargoSpawner != null)
            {
                cargoSpawner.AddCargo();
                if (showDebugLogs) Debug.Log("🧳 Cargo added via voice command");
            }
            else
            {
                Debug.LogWarning("CargoSpawner component not found, cannot add cargo");
            }
            return;
        }
        

        
        // 风向控制
        if (ContainsAnyKeyword(command, windLeftCommands))
        {
            AdjustWindDirection(-1, "Wind direction left");
            return;
        }
        
        if (ContainsAnyKeyword(command, windRightCommands))
        {
            AdjustWindDirection(1, "Wind direction right");
            return;
        }
        
        if (ContainsAnyKeyword(command, windRandomCommands))
        {
            AdjustWindDirection(UnityEngine.Random.value > 0.5f ? 1 : -1, "Random wind direction");
            return;
        }
        
        // 天空切换
        if (ContainsAnyKeyword(command, morningSkyCommands))
        {
            if (skyboxMorning != null) RenderSettings.skybox = skyboxMorning;
            if (showDebugLogs) Debug.Log("🌅 Switching to morning sky");
            return;
        }
        if (ContainsAnyKeyword(command, noonSkyCommands))
        {
            if (skyboxNoon != null) RenderSettings.skybox = skyboxNoon;
            if (showDebugLogs) Debug.Log("🌞 Switching to noon sky");
            return;
        }
        if (ContainsAnyKeyword(command, eveningSkyCommands))
        {
            if (skyboxEvening != null) RenderSettings.skybox = skyboxEvening;
            if (showDebugLogs) Debug.Log("🌇 Switching to evening sky");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"❓ Unrecognized voice command: '{command}'");
    }
    

    
    private void AdjustWindDirection(int direction, string action)
    {
        if (setWindDirectionMethod != null && boatController != null)
        {
            setWindDirectionMethod.Invoke(boatController, new object[] { direction });
            
            if (showDebugLogs)
            {
                string directionStr = direction > 0 ? "Right Wind" : "Left Wind";
                Debug.Log($"🌬️ {action}: {directionStr}");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ Wind direction control not available - SetWindDirection method not found");
        }
    }
    
    private bool ContainsAnyKeyword(string text, List<string> keywords)
    {
        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword.ToLower()))
                return true;
        }
        return false;
    }
    
    private void OnVoiceStatusUpdated(string status)
    {
        if (showDebugLogs)
            Debug.Log($"🎙️ Voice status: {status}");
    }
    
    // === Public Interface ===
    /// <summary>
    /// Toggle voice control on/off
    /// </summary>
    public void ToggleVoiceControl()
    {
        enableVoiceControl = !enableVoiceControl;
    }
    
    // === Clean up resources ===
    void OnDestroy()
    {
        if (voiceRecognizer != null)
        {
            voiceRecognizer.OnTranscriptionResult -= OnSpeechRecognized;
            voiceRecognizer.OnStatusUpdated -= OnVoiceStatusUpdated;
        }
    }
} 