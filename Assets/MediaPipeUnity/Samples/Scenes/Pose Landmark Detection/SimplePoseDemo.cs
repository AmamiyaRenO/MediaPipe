using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 简化版姿势跟随演示 - 避免UI组件问题
    /// </summary>
    public class SimplePoseDemo : MonoBehaviour
    {
        [Header("演示设置")]
        [SerializeField] private bool autoCreateCharacter = true;
        [SerializeField] private Vector3 characterStartPosition = Vector3.zero;
        [SerializeField] private UnityEngine.Color characterColor = UnityEngine.Color.green;
        
        private SimpleCharacterFollower follower;
        private PoseLandmarkerRunner poseRunner;
        private GameObject demoCharacter;
        
        void Start()
        {
            Debug.Log("=== 启动简化版姿势跟随演示 ===");
            
            // 查找姿势检测器
            poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseRunner == null)
            {
                Debug.LogError("❌ 未找到PoseLandmarkerRunner！请确保MediaPipe姿势检测系统已设置。");
                return;
            }
            
            // 自动创建演示角色
            if (autoCreateCharacter)
            {
                CreateDemoCharacter();
            }
            
            Debug.Log("✅ 简化版演示系统已启动，请在摄像头前移动来测试效果！");
        }
        
        void CreateDemoCharacter()
        {
            // 创建简单的立方体作为演示角色
            demoCharacter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            demoCharacter.name = "SimplePoseFollowCharacter";
            demoCharacter.transform.position = characterStartPosition;
            demoCharacter.transform.localScale = Vector3.one;
            
            // 设置颜色
            Renderer renderer = demoCharacter.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = characterColor;
                material.SetFloat("_Metallic", 0.2f);
                material.SetFloat("_Smoothness", 0.8f);
                renderer.material = material;
            }
            
            // 添加跟随组件
            follower = demoCharacter.AddComponent<SimpleCharacterFollower>();
            
            // 强制启用调试信息
            var showDebugField = typeof(SimpleCharacterFollower).GetField("showDebugInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (showDebugField != null)
            {
                showDebugField.SetValue(follower, true);
                Debug.Log("✅ 已启用SimpleCharacterFollower调试信息");
            }
            
            Debug.Log("✅ 创建了演示角色：" + demoCharacter.name);
        }
        
        void Update()
        {
            // 简单的键盘控制
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCharacter();
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleDebugInfo();
            }
        }
        
        public void ResetCharacter()
        {
            if (follower != null)
            {
                follower.ResetPosition();
                Debug.Log("🔄 角色位置已重置");
            }
        }
        
        public void ToggleDebugInfo()
        {
            if (follower != null)
            {
                // 通过反射切换调试信息（避免直接访问私有字段）
                var field = typeof(SimpleCharacterFollower).GetField("showDebugInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    bool currentValue = (bool)field.GetValue(follower);
                    field.SetValue(follower, !currentValue);
                    Debug.Log($"🔍 调试信息已{(!currentValue ? "启用" : "禁用")}");
                }
            }
        }
        
        void OnGUI()
        {
            // 简单的状态显示
            GUILayout.BeginArea(new UnityEngine.Rect(10, 10, 300, 100));
            
            var titleStyle = new GUIStyle();
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = UnityEngine.Color.white;
            GUILayout.Label("=== 简化版姿势跟随演示 ===", titleStyle);
            
            string status = "状态: ";
            if (poseRunner != null)
            {
                status += "✅ 姿势检测器运行中";
            }
            else
            {
                status += "❌ 姿势检测器未找到";
            }
            
            if (follower != null)
            {
                status += " | ✅ 角色跟随器已启动";
            }
            
            var statusStyle = new GUIStyle();
            statusStyle.normal.textColor = UnityEngine.Color.white;
            GUILayout.Label(status, statusStyle);
            
            var helpStyle = new GUIStyle();
            helpStyle.normal.textColor = UnityEngine.Color.white;
            GUILayout.Label("按 R 键重置角色位置", helpStyle);
            GUILayout.Label("按 空格键 切换调试信息", helpStyle);
            
            GUILayout.EndArea();
        }
        
        [ContextMenu("重置角色")]
        public void ResetCharacterFromMenu()
        {
            ResetCharacter();
        }
        
        [ContextMenu("切换调试")]
        public void ToggleDebugFromMenu()
        {
            ToggleDebugInfo();
        }
    }
} 