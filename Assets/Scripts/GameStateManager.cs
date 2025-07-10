using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏状态管理器 - 控制游戏开始画面和语音启动
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("=== Game State Control ===")]
    [Tooltip("Is the game started")]
    public bool gameStarted = false;
    
    [Header("=== Title UI Settings ===")]
    [Tooltip("Game title")]
    public string gameTitle = "Slow Boat Motion Control Game";
    
    [Tooltip("Subtitle")]
    public string gameSubtitle = "Say 'start' or 'begin' to play";
    
    [Tooltip("Hint text")]
    public string hintText = "Voice Control | Motion Control";
    
    [Header("=== Component Dependencies ===")]
    [Tooltip("Boat controller")]
    public BoatController boatController;
    
    [Tooltip("Voice controller")]
    public BoatVoiceController voiceController;
    
    [Tooltip("Pose detector")]
    public MonoBehaviour poseRunner; // PoseLandmarkerRunner
    
    [Header("=== Start Commands ===")]
    public List<string> startGameCommands = new List<string>
    {
        "start", "begin", "开始", "go", "play"
    };
    
    [Header("=== UI Style Settings ===")]
    [Tooltip("Title font size")]
    public int titleFontSize = 48;
    
    [Tooltip("Subtitle font size")]
    public int subtitleFontSize = 24;
    
    [Tooltip("Hint font size")]
    public int hintFontSize = 18;
    
    // === 私有变量 ===
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle hintStyle;
    private bool stylesInitialized = false;
    
    // 动画效果
    private float blinkTimer = 0f;
    private bool showBlink = true;
    
    void Start()
    {
        // 自动查找组件
        InitializeComponents();
        
        // 游戏开始时暂停所有控制
        SetGameComponentsActive(false);
        
        Debug.Log("🎮 Game state manager initialized - waiting for voice 'start' command");
    }
    
    void Update()
    {
        // 闪烁动画
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= 1f)
        {
            blinkTimer = 0f;
            showBlink = !showBlink;
        }
        
        // 检查语音指令
        CheckForStartCommand();
    }
    
    private void InitializeComponents()
    {
        // 自动查找船体控制器
        if (boatController == null)
        {
            boatController = FindObjectOfType<BoatController>();
        }
        
        // 自动查找语音控制器
        if (voiceController == null)
        {
            voiceController = FindObjectOfType<BoatVoiceController>();
        }
        
        // 自动查找姿态检测器
        if (poseRunner == null)
        {
            // 尝试通过类型名查找
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "PoseLandmarkerRunner")
                {
                    poseRunner = mb;
                    break;
                }
            }
        }
        
        // 订阅语音识别事件
        if (voiceController != null && voiceController.voiceRecognizer != null)
        {
            voiceController.voiceRecognizer.OnTranscriptionResult += OnSpeechRecognized;
        }
        else
        {
            // 如果语音控制器还未初始化，稍后再试
            Invoke(nameof(TrySubscribeToVoiceEvents), 2f);
        }
    }
    
    private void TrySubscribeToVoiceEvents()
    {
        if (voiceController != null && voiceController.voiceRecognizer != null)
        {
            voiceController.voiceRecognizer.OnTranscriptionResult += OnSpeechRecognized;
            Debug.Log("✅ Voice recognition events subscribed");
        }
        else
        {
            Debug.LogWarning("⚠️ Voice recognition component not found, please call StartGame() manually");
        }
    }
    
    private void CheckForStartCommand()
    {
        // 也可以通过键盘空格键开始游戏（用于测试）
        if (Input.GetKeyDown(KeyCode.Space) && !gameStarted)
        {
            StartGame("Keyboard Spacebar");
        }
    }
    
    private void OnSpeechRecognized(string recognizedText)
    {
        if (gameStarted) return; // 如果游戏已开始，忽略开始指令
        
        string command = recognizedText.ToLower().Trim();
        
        // 尝试解析JSON格式的语音结果
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
                Debug.LogWarning("Failed to parse voice JSON: " + recognizedText);
            }
        }
        
        // 检查是否为开始指令
        foreach (string startCmd in startGameCommands)
        {
            if (command.Contains(startCmd.ToLower()))
            {
                StartGame($"Voice command: '{command}'");
                return;
            }
        }
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame(string trigger = "Manual call")
    {
        if (gameStarted) return;
        
        gameStarted = true;
        Debug.Log($"🚀 Game started! Trigger: {trigger}");
        
        // 启用所有游戏组件
        SetGameComponentsActive(true);
        
        // 可以添加开始游戏的音效或动画
        Debug.Log("✅ Boat control system activated");
    }
    
    private void SetGameComponentsActive(bool active)
    {
        // 控制船体控制器（但不直接禁用，由BoatController内部检查gameStarted状态）
        if (boatController != null)
        {
            // BoatController会通过检查gameStarted状态来决定是否执行控制逻辑
            Debug.Log($"🎮 Boat controller status: {(active ? "Active" : "Standby")}");
        }
        
        // 语音控制器始终保持激活以监听开始指令
        if (voiceController != null)
        {
            voiceController.enableVoiceControl = true; // 保持激活以监听start指令
            Debug.Log($"🎤 Voice controller: {(active ? "Full functionality activated" : "Only listening for start command")}");
        }
        
        // 姿态检测器可以始终运行，由BoatController决定是否使用检测结果
        if (poseRunner != null)
        {
            Debug.Log($"👤 Pose detector: {(active ? "Results effective" : "Only detecting, not controlling")}");
        }
    }
    
    void OnGUI()
    {
        if (gameStarted) return; // 游戏开始后不显示标题画面
        
        // 初始化样式
        if (!stylesInitialized)
        {
            InitializeGUIStyles();
        }
        
        // 获取屏幕尺寸
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        
        // 绘制半透明背景
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;
        
        // 计算垂直居中位置
        int centerY = screenHeight / 2;
        
        // 绘制主标题
        Rect titleRect = new Rect(0, centerY - 120, screenWidth, 60);
        GUI.Label(titleRect, gameTitle, titleStyle);
        
        // 绘制副标题（闪烁效果）
        if (showBlink)
        {
            Rect subtitleRect = new Rect(0, centerY - 40, screenWidth, 40);
            GUI.Label(subtitleRect, gameSubtitle, subtitleStyle);
        }
        
        // 绘制提示文本
        Rect hintRect = new Rect(0, centerY + 20, screenWidth, 30);
        GUI.Label(hintRect, hintText, hintStyle);
        
        // 绘制支持的语音指令
        string commandsText = "Supported voice commands: " + string.Join(" | ", startGameCommands);
        Rect commandsRect = new Rect(0, screenHeight - 80, screenWidth, 25);
        GUI.Label(commandsRect, commandsText, hintStyle);
        
        // 绘制键盘提示
        string keyboardHint = "Or press SPACEBAR to start";
        Rect keyboardRect = new Rect(0, screenHeight - 50, screenWidth, 25);
        GUI.Label(keyboardRect, keyboardHint, hintStyle);
    }
    
    private void InitializeGUIStyles()
    {
        // 主标题样式
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = titleFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        
        // 副标题样式
        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = subtitleFontSize,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
        
        // 提示文本样式
        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = hintFontSize,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
        };
        
        stylesInitialized = true;
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (voiceController != null && voiceController.voiceRecognizer != null)
        {
            voiceController.voiceRecognizer.OnTranscriptionResult -= OnSpeechRecognized;
        }
    }
} 