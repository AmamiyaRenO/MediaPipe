using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 测试姿势跟随系统的基本功能
    /// </summary>
    public class TestPoseFollowSystem : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private bool autoCreateTestCharacter = true;
        [SerializeField] private GameObject testCharacterPrefab;
        
        private SimpleCharacterFollower simpleFollower;
        private CharacterFollower fullFollower;
        private PoseLandmarkerRunner poseRunner;
        
        void Start()
        {
            Debug.Log("=== 开始测试姿势跟随系统 ===");
            
            // 查找必要的组件
            poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseRunner == null)
            {
                Debug.LogError("❌ 未找到PoseLandmarkerRunner组件！请确保MediaPipe姿势检测系统已设置。");
                return;
            }
            else
            {
                Debug.Log("✅ 找到PoseLandmarkerRunner组件");
            }
            
            // 创建测试角色
            if (autoCreateTestCharacter)
            {
                CreateTestCharacters();
            }
            
            // 开始测试
            InvokeRepeating(nameof(TestSystemStatus), 2f, 5f);
        }
        
        void CreateTestCharacters()
        {
            Debug.Log("🎭 创建测试角色...");
            
            // 创建SimpleCharacterFollower测试角色
            GameObject simpleChar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            simpleChar.name = "SimpleFollowerTest";
            simpleChar.transform.position = new Vector3(-2, 0, 0);
            simpleChar.GetComponent<Renderer>().material.color = UnityEngine.Color.green;
            
            simpleFollower = simpleChar.AddComponent<SimpleCharacterFollower>();
            Debug.Log("✅ 创建了SimpleCharacterFollower测试角色");
            
            // 创建CharacterFollower测试角色
            GameObject fullChar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fullChar.name = "FullFollowerTest";
            fullChar.transform.position = new Vector3(2, 0, 0);
            fullChar.GetComponent<Renderer>().material.color = UnityEngine.Color.blue;
            
            fullFollower = fullChar.AddComponent<CharacterFollower>();
            Debug.Log("✅ 创建了CharacterFollower测试角色");
        }
        
        void TestSystemStatus()
        {
            Debug.Log("🔍 === 系统状态检查 ===");
            
            // 检查姿势检测器状态
            if (poseRunner != null)
            {
                try
                {
                    var result = poseRunner.LatestResult;
                    if (result.poseLandmarks != null && result.poseLandmarks.Count > 0)
                    {
                        Debug.Log($"✅ 姿势检测正常 - 检测到 {result.poseLandmarks.Count} 个人");
                    }
                    else
                    {
                        Debug.Log("⚠️ 姿势检测器运行中，但未检测到人体");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ 姿势检测器访问错误: {e.Message}");
                }
            }
            
            // 检查SimpleCharacterFollower状态
            if (simpleFollower != null)
            {
                Vector3 pos = simpleFollower.transform.position;
                Debug.Log($"📍 SimpleFollower位置: {pos}");
            }
            
            // 检查CharacterFollower状态
            if (fullFollower != null)
            {
                Vector3 pos = fullFollower.transform.position;
                Debug.Log($"📍 FullFollower位置: {pos}");
            }
            
            Debug.Log("=== 状态检查完成 ===");
        }
        
        [ContextMenu("重置所有角色位置")]
        public void ResetAllCharacters()
        {
            if (simpleFollower != null)
            {
                simpleFollower.ResetPosition();
                Debug.Log("🔄 重置了SimpleCharacterFollower位置");
            }
            
            if (fullFollower != null)
            {
                fullFollower.ResetPosition();
                Debug.Log("🔄 重置了CharacterFollower位置");
            }
        }
        
        [ContextMenu("测试错误处理")]
        public void TestErrorHandling()
        {
            Debug.Log("🧪 测试错误处理机制...");
            
            // 模拟一些边界情况
            try
            {
                if (simpleFollower != null)
                {
                    simpleFollower.SetMoveSensitivity(0f);
                    simpleFollower.SetMoveSensitivity(100f);
                    simpleFollower.ToggleRotation(true);
                    simpleFollower.ToggleRotation(false);
                    simpleFollower.ToggleSmoothMovement(true);
                    simpleFollower.ToggleSmoothMovement(false);
                    
                    Debug.Log("✅ SimpleCharacterFollower错误处理测试通过");
                }
                
                if (fullFollower != null)
                {
                    fullFollower.SetMoveSensitivity(0f);
                    fullFollower.SetMoveSensitivity(100f);
                    fullFollower.ToggleRotation(true);
                    fullFollower.ToggleRotation(false);
                    fullFollower.ToggleSmoothMovement(true);
                    fullFollower.ToggleSmoothMovement(false);
                    
                    Debug.Log("✅ CharacterFollower错误处理测试通过");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 错误处理测试失败: {e.Message}");
            }
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new UnityEngine.Rect(10, UnityEngine.Screen.height - 150, 300, 140));
            
            var titleStyle = new GUIStyle();
            titleStyle.fontSize = 16;
            titleStyle.normal.textColor = UnityEngine.Color.white;
            GUILayout.Label("姿势跟随系统测试", titleStyle);
            
            if (GUILayout.Button("重置所有角色"))
            {
                ResetAllCharacters();
            }
            
            if (GUILayout.Button("测试错误处理"))
            {
                TestErrorHandling();
            }
            
            if (GUILayout.Button("显示详细状态"))
            {
                TestSystemStatus();
            }
            
            // 显示系统状态
            string status = "系统状态: ";
            if (poseRunner != null)
            {
                status += "✅ 姿势检测器";
            }
            else
            {
                status += "❌ 姿势检测器";
            }
            
            if (simpleFollower != null)
            {
                status += " | ✅ Simple跟随器";
            }
            
            if (fullFollower != null)
            {
                status += " | ✅ Full跟随器";
            }
            
            var statusStyle = new GUIStyle();
            statusStyle.normal.textColor = UnityEngine.Color.white;
            GUILayout.Label(status, statusStyle);
            
            GUILayout.EndArea();
        }
    }
} 