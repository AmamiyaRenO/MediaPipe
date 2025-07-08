using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 极简版姿势跟随演示 - 零配置，最高兼容性
    /// </summary>
    public class UltraSimpleDemo : MonoBehaviour
    {
        [Header("自动设置")]
        [SerializeField] private bool autoCreateCharacter = true;
        [SerializeField] private UnityEngine.Color characterColor = UnityEngine.Color.cyan;
        
        private UltraSimpleFollower follower;
        private GameObject demoCharacter;
        
        void Start()
        {
            Debug.Log("🚀 === 启动极简版姿势跟随演示 ===");
            
            if (autoCreateCharacter)
            {
                CreateCharacter();
            }
            
            Debug.Log("✅ 极简演示系统已启动！");
            Debug.Log("🎮 控制说明：");
            Debug.Log("   R 键 - 重置角色位置");
            Debug.Log("   D 键 - 切换演示模式");
        }
        
        void CreateCharacter()
        {
            // 创建简单的演示角色
            demoCharacter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            demoCharacter.name = "UltraSimplePoseCharacter";
            demoCharacter.transform.position = new Vector3(0, 1, 0); // 稍微抬高一点
            demoCharacter.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f); // 稍微大一点，更容易看到
            
            // 设置材质
            Renderer renderer = demoCharacter.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = characterColor;
                material.SetFloat("_Metallic", 0.3f);
                material.SetFloat("_Smoothness", 0.7f);
                renderer.material = material;
            }
            
            // 添加跟随组件
            follower = demoCharacter.AddComponent<UltraSimpleFollower>();
            
            Debug.Log($"✅ 创建了极简演示角色 - 位置: {demoCharacter.transform.position}, 颜色: {characterColor}");
        }
        
        void Update()
        {
            // 简单的键盘控制
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCharacter();
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                ToggleDemoMode();
            }
        }
        
        public void ResetCharacter()
        {
            if (follower != null)
            {
                follower.ResetPosition();
            }
        }
        
        public void ToggleDemoMode()
        {
            if (follower != null)
            {
                follower.ToggleDemoMode();
            }
        }
        
        void OnGUI()
        {
            // 极简的状态显示
            GUILayout.BeginArea(new UnityEngine.Rect(10, 10, 350, 80));
            
            var style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = UnityEngine.Color.white;
            
            GUILayout.Label("🚀 极简版姿势跟随演示", style);
            GUILayout.Label("按 R 重置 | 按 D 切换演示模式", style);
            
            string status = "状态: ";
            if (follower != null)
            {
                status += "✅ 角色运行中";
            }
            else
            {
                status += "❌ 等待初始化";
            }
            
            GUILayout.Label(status, style);
            
            GUILayout.EndArea();
        }
        
        [ContextMenu("重置角色")]
        public void ResetFromMenu()
        {
            ResetCharacter();
        }
        
        [ContextMenu("切换演示")]
        public void ToggleFromMenu()
        {
            ToggleDemoMode();
        }
    }
} 