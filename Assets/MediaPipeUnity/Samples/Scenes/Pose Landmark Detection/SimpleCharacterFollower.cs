using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 简化版角色跟随系统 - 避免复杂的API问题
    /// </summary>
    public class SimpleCharacterFollower : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private Transform characterTransform; // 要控制的角色Transform
        [SerializeField] private float moveSensitivity = 5f; // 移动灵敏度
        [SerializeField] private float smoothSpeed = 2f; // 平滑移动速度
        [SerializeField] private bool enableRotation = true; // 是否启用旋转跟随
        [SerializeField] private float rotationSpeed = 3f; // 旋转速度
        
        [Header("移动约束")]
        [SerializeField] private Vector2 moveBounds = new Vector2(10f, 10f); // 移动边界
        [SerializeField] private bool constrainMovement = true; // 是否约束移动范围
        
        [Header("调试显示")]
        [SerializeField] private bool showDebugInfo = true; // 显示调试信息
        [SerializeField] private bool enableSmoothMovement = true; // 启用平滑移动
        
        private Vector3 lastCenterPosition; // 上一帧的中心位置
        private Vector3 targetPosition; // 目标位置
        private Quaternion targetRotation; // 目标旋转
        private bool isInitialized = false; // 是否已初始化
        
        private PoseLandmarkerRunner poseLandmarkerRunner; // 姿势检测器引用
        
        void Start()
        {
            // 获取姿势检测器组件
            poseLandmarkerRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseLandmarkerRunner == null)
            {
                Debug.LogError("SimpleCharacterFollower: 未找到PoseLandmarkerRunner组件！");
                return;
            }
            
            Debug.Log("✅ 找到PoseLandmarkerRunner，开始监控注解控制器数据");
            
            // 如果没有指定角色Transform，使用当前GameObject
            if (characterTransform == null)
            {
                characterTransform = transform;
            }
            
            // 初始化目标位置和旋转
            targetPosition = characterTransform.position;
            targetRotation = characterTransform.rotation;
            
            Debug.Log("SimpleCharacterFollower: 简化版角色跟随系统已启动");
        }
        
        void Update()
        {
            // 添加调试信息
            if (Time.frameCount % 120 == 0) // 每2秒打印一次
            {
                Debug.Log($"🔍 SimpleCharacterFollower调试: poseLandmarkerRunner={(poseLandmarkerRunner != null ? "存在" : "null")}, isInitialized={isInitialized}");
            }
            
            // 尝试获取姿势检测结果
            if (poseLandmarkerRunner != null)
            {
                try
                {
                    var result = poseLandmarkerRunner.LatestResult;
                    
                    // 添加更详细的调试
                    if (Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"🔍 姿态结果调试: result={(!ReferenceEquals(result, null) ? "有数据" : "null")}, poseLandmarks={(!ReferenceEquals(result, null) && !ReferenceEquals(result.poseLandmarks, null) ? "有数据" : "null")}");
                        if (!ReferenceEquals(result, null))
                        {
                            // 检查result的其他属性
                            Debug.Log($"🔍 检查result类型: {result.GetType().Name}");
                            var properties = result.GetType().GetProperties();
                            foreach (var prop in properties)
                            {
                                try
                                {
                                    var value = prop.GetValue(result);
                                    Debug.Log($"🔍 属性 {prop.Name}: {(!ReferenceEquals(value, null) ? "有数据" : "null")}");
                                }
                                catch
                                {
                                    Debug.Log($"🔍 属性 {prop.Name}: 无法访问");
                                }
                            }
                            
                            if (!ReferenceEquals(result.poseLandmarks, null))
                            {
                                Debug.Log($"🔍 姿态数量: {result.poseLandmarks.Count}");
                            }
                        }
                    }
                    
                    bool hasValidPoseData = false;
                    
                    // 使用反射方式访问MediaPipe数据（仿照UltraSimpleFollower的成功方法）
                    if (!ReferenceEquals(result, null))
                    {
                        try
                        {
                            // 使用反射安全地访问MediaPipe数据
                            var poseLandmarksProperty = result.GetType().GetProperty("poseLandmarks");
                            if (!ReferenceEquals(poseLandmarksProperty, null))
                            {
                                var poseLandmarks = poseLandmarksProperty.GetValue(result);
                                if (!ReferenceEquals(poseLandmarks, null))
                                {
                                    var countProperty = poseLandmarks.GetType().GetProperty("Count");
                                    if (!ReferenceEquals(countProperty, null))
                                    {
                                        var count = (int)countProperty.GetValue(poseLandmarks);
                                        if (count > 0)
                                        {
                                            // 获取第一个关键点列表
                                            var itemProperty = poseLandmarks.GetType().GetProperty("Item");
                                            if (!ReferenceEquals(itemProperty, null))
                                            {
                                                var firstLandmarkList = itemProperty.GetValue(poseLandmarks, new object[] { 0 });
                                                if (!ReferenceEquals(firstLandmarkList, null))
                                                {
                                                    // 直接使用这个数据
                                                    hasValidPoseData = true;
                                                    
                                                    // 直接计算鼻子位置
                                                    Vector3 nosePosition = GetNosePositionFromLandmarks(firstLandmarkList);
                                                    
                                                    if (!isInitialized)
                                                    {
                                                        lastCenterPosition = nosePosition;
                                                        isInitialized = true;
                                                        targetPosition = characterTransform.position;
                                                        return;
                                                    }
                                                    
                                                    // 计算移动差异
                                                    Vector3 movement = nosePosition - lastCenterPosition;
                                                    movement *= moveSensitivity;
                                                    
                                                    // 更新目标位置
                                                    Vector3 newPosition = targetPosition + new Vector3(movement.x, movement.y, 0);
                                                    
                                                    // 应用移动约束
                                                    if (constrainMovement)
                                                    {
                                                        newPosition.x = Mathf.Clamp(newPosition.x, -moveBounds.x, moveBounds.x);
                                                        newPosition.y = Mathf.Clamp(newPosition.y, -moveBounds.y, moveBounds.y);
                                                    }
                                                    
                                                    targetPosition = newPosition;
                                                    lastCenterPosition = nosePosition;
                                                    
                                                    if (Time.frameCount % 60 == 0)
                                                    {
                                                        Debug.Log($"🎯 姿态跟随: 鼻子={nosePosition}, 移动={movement}, 目标={targetPosition}");
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
                            if (Time.frameCount % 120 == 0)
                            {
                                Debug.LogWarning($"反射访问失败: {e.Message}");
                            }
                        }
                    }
                    
                    // 只有在没有有效姿态数据时才使用备用移动
                    if (!hasValidPoseData)
                    {
                        if (Time.frameCount % 120 == 0)
                        {
                            Debug.Log("⚠️ 没有有效姿态数据");
                        }
                        // 移除备用移动，不做任何操作
                    }
                }
                catch (System.Exception e)
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"SimpleCharacterFollower: 姿势数据处理出错: {e.Message}");
                    }
                    
                    // 使用备用的简单移动逻辑
                    ProcessFallbackMovement();
                }
            }
            else
            {
                // 没有姿态检测器时使用备用移动
                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log("⚠️ 没有姿态检测器，使用备用移动");
                }
                ProcessFallbackMovement();
            }
            
            // 平滑移动角色到目标位置
            if (isInitialized && characterTransform != null)
            {
                if (enableSmoothMovement)
                {
                    characterTransform.position = Vector3.Lerp(characterTransform.position, targetPosition, Time.deltaTime * smoothSpeed);
                    
                    if (enableRotation)
                    {
                        characterTransform.rotation = Quaternion.Lerp(characterTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                    }
                }
                else
                {
                    characterTransform.position = targetPosition;
                    if (enableRotation)
                    {
                        characterTransform.rotation = targetRotation;
                    }
                }
            }
        }
        
        /// <summary>
        /// 从landmarks中获取鼻子位置
        /// </summary>
        private Vector3 GetNosePositionFromLandmarks(object landmarks)
        {
            try
            {
                // 直接尝试获取鼻子关键点（通常是索引0）
                var landmarksType = landmarks.GetType();
                var indexProperty = landmarksType.GetProperty("Item"); // 索引器
                
                if (!ReferenceEquals(indexProperty, null))
                {
                    // 获取鼻子关键点（索引0）
                    var noseLandmark = indexProperty.GetValue(landmarks, new object[] { 0 });
                    if (!ReferenceEquals(noseLandmark, null))
                    {
                        // 使用反射获取x, y坐标
                        var landmarkType = noseLandmark.GetType();
                        var xProp = landmarkType.GetProperty("x") ?? landmarkType.GetProperty("X");
                        var yProp = landmarkType.GetProperty("y") ?? landmarkType.GetProperty("Y");
                        
                        if (!ReferenceEquals(xProp, null) && !ReferenceEquals(yProp, null))
                        {
                            float x = System.Convert.ToSingle(xProp.GetValue(noseLandmark));
                            float y = System.Convert.ToSingle(yProp.GetValue(noseLandmark));
                            
                            // MediaPipe坐标转换：0-1范围转换为Unity世界坐标
                            Vector3 nosePosition = new Vector3(
                                (x - 0.5f) * 10f,  // X轴：-5到5的范围
                                (0.5f - y) * 6f,   // Y轴：-3到3的范围，注意Y轴翻转
                                0
                            );
                            
                            return nosePosition;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"获取鼻子位置失败: {e.Message}");
                }
            }
            
            // 如果获取失败，返回上一帧的位置
            return lastCenterPosition;
        }
        
        /// <summary>
        /// 备用移动逻辑（当姿势检测失败时）
        /// </summary>
        private void ProcessFallbackMovement()
        {
            // 简单的演示移动：让角色做一个缓慢的圆周运动
            if (!isInitialized)
            {
                isInitialized = true;
                targetPosition = characterTransform.position; // 初始化目标位置
                return;
            }
            
            float time = Time.time * 1f; // 增加速度
            Vector3 circleMovement = new Vector3(Mathf.Sin(time), Mathf.Cos(time), 0) * 2f; // 增加范围
            targetPosition = Vector3.zero + circleMovement;
            
            // 每2秒打印一次调试信息
            if (Time.frameCount % 120 == 0 && showDebugInfo)
            {
                Debug.Log($"🔄 备用移动: time={time:F2}, targetPosition={targetPosition}, 角色位置={characterTransform.position}");
            }
        }
        
        /// <summary>
        /// 重置角色位置到原点
        /// </summary>
        public void ResetPosition()
        {
            targetPosition = Vector3.zero;
            targetRotation = Quaternion.identity;
            isInitialized = false;
            Debug.Log("SimpleCharacterFollower: 角色位置已重置");
        }
        
        /// <summary>
        /// 设置移动灵敏度
        /// </summary>
        /// <param name="sensitivity">灵敏度值</param>
        public void SetMoveSensitivity(float sensitivity)
        {
            moveSensitivity = sensitivity;
        }
        
        /// <summary>
        /// 切换旋转跟随
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void ToggleRotation(bool enable)
        {
            enableRotation = enable;
        }
        
        /// <summary>
        /// 切换平滑移动
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void ToggleSmoothMovement(bool enable)
        {
            enableSmoothMovement = enable;
        }
        
        void OnDrawGizmosSelected()
        {
            // 绘制移动边界
            if (constrainMovement)
            {
                Gizmos.color = UnityEngine.Color.yellow;
                Vector3 center = transform.position;
                Vector3 size = new Vector3(moveBounds.x * 2, moveBounds.y * 2, 1);
                Gizmos.DrawWireCube(center, size);
            }
            
            // 绘制目标位置
            if (isInitialized)
            {
                Gizmos.color = UnityEngine.Color.green;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
                
                // 绘制从当前位置到目标位置的线
                Gizmos.color = UnityEngine.Color.red;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
} 