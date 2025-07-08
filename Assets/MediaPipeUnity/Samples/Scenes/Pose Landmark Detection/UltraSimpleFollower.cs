using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 极简版角色跟随器 - 完全避免MediaPipe API兼容性问题
    /// </summary>
    public class UltraSimpleFollower : MonoBehaviour
    {
        [Header("基础设置")]
        [SerializeField] private float moveSensitivity = 3f;
        [SerializeField] private float smoothSpeed = 2f;
        [SerializeField] private bool enableDemo = true;
        
        [Header("演示移动")]
        [SerializeField] private float demoSpeed = 2f;
        [SerializeField] private float demoRange = 3f;
        
        private Vector3 targetPosition;
        private PoseLandmarkerRunner poseRunner;
        private bool isRunning = false;
        
        void Start()
        {
            targetPosition = transform.position;
            
            // 强制启用演示模式进行测试
            enableDemo = true;
            
            // 查找姿势检测器
            poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseRunner != null)
            {
                Debug.Log("✅ UltraSimpleFollower: 找到姿势检测器");
                isRunning = true;
            }
            else
            {
                Debug.LogWarning("⚠️ UltraSimpleFollower: 未找到姿势检测器，将使用演示模式");
                isRunning = true; // 强制启用运行
            }
            
            Debug.Log($"🚀 UltraSimpleFollower: 启动完成 - 演示模式: {enableDemo}, 运行状态: {isRunning}");
        }
        
        void Update()
        {
            if (!isRunning) 
            {
                if (Time.frameCount % 120 == 0) // 每2秒提醒一次
                {
                    Debug.LogWarning("⚠️ UltraSimpleFollower: 未运行状态");
                }
                return;
            }
            
            if (poseRunner != null)
            {
                // 尝试使用姿势数据
                TryProcessPoseData();
            }
            else if (enableDemo)
            {
                // 演示模式：简单的圆周运动
                ProcessDemoMovement();
                
                // 每2秒打印一次演示模式状态
                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log("🎮 UltraSimpleFollower: 演示模式运行中");
                }
            }
            
            // 平滑移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
        
        void TryProcessPoseData()
        {
            try
            {
                // 尝试获取姿势数据，但用最安全的方式
                var result = poseRunner.LatestResult;
                
                // 添加调试信息
                if (Time.frameCount % 120 == 0) // 每2秒打印一次
                {
                    Debug.Log($"🔍 调试: result = {(ReferenceEquals(result, null) ? "null" : "有数据")}");
                    if (!ReferenceEquals(result, null))
                    {
                        Debug.Log($"🔍 调试: result.poseLandmarks = {(ReferenceEquals(result.poseLandmarks, null) ? "null" : "有数据")}");
                    }
                }
                
                // 检查是否有姿势数据 - 改进版本
                bool hasPoseData = false;
                Vector3 nosePosition = Vector3.zero;
                
                try
                {
                    // 使用反射安全地访问MediaPipe数据
                    if (!ReferenceEquals(result, null) && !ReferenceEquals(result.poseLandmarks, null))
                    {
                        var poseLandmarksProperty = result.poseLandmarks.GetType().GetProperty("Count");
                        if (!ReferenceEquals(poseLandmarksProperty, null))
                        {
                            var count = (int)poseLandmarksProperty.GetValue(result.poseLandmarks);
                            if (count > 0)
                            {
                                // 获取第一个关键点列表
                                var itemProperty = result.poseLandmarks.GetType().GetProperty("Item");
                                if (!ReferenceEquals(itemProperty, null))
                                {
                                    var firstLandmarkList = itemProperty.GetValue(result.poseLandmarks, new object[] { 0 });
                                    if (!ReferenceEquals(firstLandmarkList, null))
                                    {
                                        var landmarkCountProperty = firstLandmarkList.GetType().GetProperty("Count");
                                        if (!ReferenceEquals(landmarkCountProperty, null))
                                        {
                                            var landmarkCount = (int)landmarkCountProperty.GetValue(firstLandmarkList);
                                            
                                            // 鼻子是索引0，确保有足够的关键点
                                            if (landmarkCount > 0)
                                            {
                                                var landmarkItemProperty = firstLandmarkList.GetType().GetProperty("Item");
                                                if (!ReferenceEquals(landmarkItemProperty, null))
                                                {
                                                    var noseLandmark = landmarkItemProperty.GetValue(firstLandmarkList, new object[] { 0 });
                                                    if (!ReferenceEquals(noseLandmark, null))
                                                    {
                                                        // 获取x, y坐标
                                                        var xProperty = noseLandmark.GetType().GetProperty("X");
                                                        var yProperty = noseLandmark.GetType().GetProperty("Y");
                                                        
                                                        if (!ReferenceEquals(xProperty, null) && !ReferenceEquals(yProperty, null))
                                                        {
                                                            float x = (float)xProperty.GetValue(noseLandmark);
                                                            float y = (float)yProperty.GetValue(noseLandmark);
                                                            
                                                            // MediaPipe坐标是0-1范围，需要转换到Unity坐标
                                                            nosePosition = new Vector3(
                                                                (x - 0.5f) * moveSensitivity * 4f,  // 左右移动
                                                                (0.5f - y) * moveSensitivity * 3f,  // 上下移动（Y轴翻转）
                                                                0
                                                            );
                                                            
                                                            hasPoseData = true;
                                                            // 减少日志输出，只在需要时打印
                                                            if (Time.frameCount % 60 == 0) // 每60帧（约1秒）打印一次
                                                            {
                                                                Debug.Log($"🎯 检测到姿态: 鼻子位置 ({x:F2}, {y:F2}) -> Unity ({nosePosition.x:F2}, {nosePosition.y:F2})");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"姿态数据解析失败: {e.Message}");
                    hasPoseData = false;
                }
                
                if (hasPoseData)
                {
                    // 使用真实的姿态数据
                    targetPosition = Vector3.Lerp(targetPosition, nosePosition, Time.deltaTime * 3f);
                    
                    // 限制移动范围
                    targetPosition.x = Mathf.Clamp(targetPosition.x, -5f, 5f);
                    targetPosition.y = Mathf.Clamp(targetPosition.y, -3f, 3f);
                }
                else
                {
                    // 没有姿势数据时使用演示模式
                    if (enableDemo)
                    {
                        ProcessDemoMovement();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"UltraSimpleFollower: 姿势处理错误，切换到演示模式: {e.Message}");
                // 出错时切换到演示模式
                if (enableDemo)
                {
                    ProcessDemoMovement();
                }
            }
        }
        
        void ProcessDemoMovement()
        {
            // 简单的圆周运动演示
            float time = Time.time * demoSpeed;
            Vector3 circleMovement = new Vector3(
                Mathf.Sin(time) * demoRange,
                Mathf.Cos(time * 0.7f) * demoRange * 0.5f + 1f, // 加个偏移，确保在摄像头视野内
                0
            );
            
            Vector3 newTargetPosition = Vector3.zero + circleMovement;
            targetPosition = newTargetPosition;
            
            // 每2秒打印一次位置调试信息
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"🎮 演示移动: time={time:F2}, circleMovement={circleMovement}, targetPosition={targetPosition}, currentPos={transform.position}");
            }
        }
        
        /// <summary>
        /// 重置位置
        /// </summary>
        [ContextMenu("重置位置")]
        public void ResetPosition()
        {
            targetPosition = Vector3.zero;
            transform.position = Vector3.zero;
            Debug.Log("🔄 UltraSimpleFollower: 位置已重置");
        }
        
        /// <summary>
        /// 切换演示模式
        /// </summary>
        [ContextMenu("切换演示模式")]
        public void ToggleDemoMode()
        {
            enableDemo = !enableDemo;
            Debug.Log($"🔄 UltraSimpleFollower: 演示模式 {(enableDemo ? "启用" : "禁用")}");
        }
        
        /// <summary>
        /// 设置移动参数
        /// </summary>
        public void SetMoveParams(float sensitivity, float speed)
        {
            moveSensitivity = sensitivity;
            smoothSpeed = speed;
        }
        
        void OnDrawGizmosSelected()
        {
            // 绘制移动范围
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(10, 6, 1));
            
            // 绘制目标位置
            if (Application.isPlaying)
            {
                Gizmos.color = UnityEngine.Color.green;
                Gizmos.DrawWireSphere(targetPosition, 0.3f);
                
                // 绘制连接线
                Gizmos.color = UnityEngine.Color.red;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
} 