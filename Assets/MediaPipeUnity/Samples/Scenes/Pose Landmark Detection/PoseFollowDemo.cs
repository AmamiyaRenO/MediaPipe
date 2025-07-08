using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 姿势跟随演示脚本 - 提供UI控制和演示功能
    /// </summary>
    public class PoseFollowDemo : MonoBehaviour
    {
        [Header("演示角色")]
        [SerializeField] private GameObject demoCharacter; // 演示角色GameObject
        [SerializeField] private bool createDemoCharacter = true; // 是否自动创建演示角色
        
        [Header("UI控制")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle rotationToggle;
        [SerializeField] private Toggle smoothMovementToggle;
        [SerializeField] private Text infoText;
        
        private CharacterFollower characterFollower;
        private PoseLandmarkerRunner poseLandmarkerRunner;
        
        void Start()
        {
            InitializeDemo();
            SetupUI();
        }
        
        /// <summary>
        /// 初始化演示
        /// </summary>
        void InitializeDemo()
        {
            // 查找姿势检测器
            poseLandmarkerRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseLandmarkerRunner == null)
            {
                Debug.LogError("PoseFollowDemo: 未找到PoseLandmarkerRunner组件！");
                return;
            }
            
            // 创建或设置演示角色
            if (createDemoCharacter && demoCharacter == null)
            {
                CreateDemoCharacter();
            }
            
            // 添加或获取CharacterFollower组件
            if (demoCharacter != null)
            {
                characterFollower = demoCharacter.GetComponent<CharacterFollower>();
                if (characterFollower == null)
                {
                    characterFollower = demoCharacter.AddComponent<CharacterFollower>();
                }
            }
            
            Debug.Log("PoseFollowDemo: 演示系统已初始化");
        }
        
        /// <summary>
        /// 创建演示角色
        /// </summary>
        void CreateDemoCharacter()
        {
            // 创建一个简单的立方体作为演示角色
            demoCharacter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            demoCharacter.name = "DemoCharacter";
            demoCharacter.transform.position = Vector3.zero;
            demoCharacter.transform.localScale = Vector3.one * 0.5f;
            
            // 添加颜色材质
            Renderer renderer = demoCharacter.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = UnityEngine.Color.cyan;
                renderer.material = material;
            }
            
            Debug.Log("PoseFollowDemo: 演示角色已创建");
        }
        
        /// <summary>
        /// 设置UI控制
        /// </summary>
        void SetupUI()
        {
            // 重置按钮
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetCharacterPosition);
            }
            
            // 灵敏度滑块
            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = 5f; // 默认灵敏度
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }
            
            // 旋转开关
            if (rotationToggle != null)
            {
                rotationToggle.isOn = true; // 默认启用旋转
                rotationToggle.onValueChanged.AddListener(OnRotationToggled);
            }
            
            // 平滑移动开关
            if (smoothMovementToggle != null)
            {
                smoothMovementToggle.isOn = true; // 默认启用平滑移动
                smoothMovementToggle.onValueChanged.AddListener(OnSmoothMovementToggled);
            }
        }
        
        void Update()
        {
            UpdateInfoText();
        }
        
        /// <summary>
        /// 更新信息显示
        /// </summary>
        void UpdateInfoText()
        {
            if (infoText == null || characterFollower == null)
                return;
                
            string info = "姿势跟随演示\n";
            info += "━━━━━━━━━━━━━━━━\n";
            
            if (poseLandmarkerRunner != null && poseLandmarkerRunner.LatestResult.poseLandmarks != null)
            {
                var landmarks = poseLandmarkerRunner.LatestResult.poseLandmarks;
                info += $"检测到的姿势: {landmarks.Count}\n";
                
                if (landmarks.Count > 0)
                {
                    info += "✓ 姿势检测正常\n";
                    if (demoCharacter != null)
                    {
                        info += $"角色位置: {demoCharacter.transform.position:F2}\n";
                    }
                }
                else
                {
                    info += "⚠ 未检测到姿势\n";
                }
            }
            else
            {
                info += "⚠ 等待姿势检测...\n";
            }
            
            info += "━━━━━━━━━━━━━━━━\n";
            info += "控制说明:\n";
            info += "• 在摄像头前移动身体\n";
            info += "• 蓝色方块会跟随您的移动\n";
            info += "• 使用UI控制调整参数\n";
            
            infoText.text = info;
        }
        
        /// <summary>
        /// 重置角色位置
        /// </summary>
        void ResetCharacterPosition()
        {
            if (characterFollower != null)
            {
                characterFollower.ResetPosition();
                Debug.Log("PoseFollowDemo: 角色位置已重置");
            }
        }
        
        /// <summary>
        /// 灵敏度改变回调
        /// </summary>
        /// <param name="value">新的灵敏度值</param>
        void OnSensitivityChanged(float value)
        {
            if (characterFollower != null)
            {
                characterFollower.SetMoveSensitivity(value);
            }
        }
        
        /// <summary>
        /// 旋转开关回调
        /// </summary>
        /// <param name="enabled">是否启用旋转</param>
        void OnRotationToggled(bool enabled)
        {
            if (characterFollower != null)
            {
                characterFollower.ToggleRotation(enabled);
            }
        }
        
        /// <summary>
        /// 平滑移动开关回调
        /// </summary>
        /// <param name="enabled">是否启用平滑移动</param>
        void OnSmoothMovementToggled(bool enabled)
        {
            if (characterFollower != null)
            {
                characterFollower.ToggleSmoothMovement(enabled);
            }
        }
        
        /// <summary>
        /// 创建简单的UI控制面板
        /// </summary>
        [ContextMenu("创建UI控制面板")]
        void CreateUIPanel()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // 创建UI面板
            GameObject panel = new GameObject("PoseFollowControlPanel");
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0.3f, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new UnityEngine.Color(0, 0, 0, 0.8f);
            
            Debug.Log("PoseFollowDemo: UI控制面板已创建");
        }
    }
} 