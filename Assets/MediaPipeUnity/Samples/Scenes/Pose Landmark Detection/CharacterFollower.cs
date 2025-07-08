using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 角色跟随系统 - 让虚拟角色跟随玩家的姿势移动
    /// </summary>
    public class CharacterFollower : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private Transform characterTransform; // 要控制的角色Transform
        [SerializeField] private float moveSensitivity = 5f; // 移动灵敏度
        [SerializeField] private float smoothSpeed = 2f; // 平滑移动速度
        [SerializeField] private bool enableRotation = true; // 是否启用旋转跟随
        [SerializeField] private float rotationSpeed = 3f; // 旋转速度
        
        [Header("姿势检测参考点")]
        [SerializeField] private bool useShoulders = true; // 使用肩膀作为参考点
        [SerializeField] private bool useHips = true; // 使用臀部作为参考点
        [SerializeField] private bool useHands = false; // 使用手部作为参考点
        
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
        
        // MediaPipe 姿势关键点索引定义
        private const int NOSE = 0;
        private const int LEFT_SHOULDER = 11;
        private const int RIGHT_SHOULDER = 12;
        private const int LEFT_HIP = 23;
        private const int RIGHT_HIP = 24;
        private const int LEFT_WRIST = 15;
        private const int RIGHT_WRIST = 16;
        
        void Start()
        {
            // 获取姿势检测器组件
            poseLandmarkerRunner = FindObjectOfType<PoseLandmarkerRunner>();
            if (poseLandmarkerRunner == null)
            {
                Debug.LogError("CharacterFollower: 未找到PoseLandmarkerRunner组件！");
                return;
            }
            
            // 如果没有指定角色Transform，使用当前GameObject
            if (characterTransform == null)
            {
                characterTransform = transform;
            }
            
            // 初始化目标位置和旋转
            targetPosition = characterTransform.position;
            targetRotation = characterTransform.rotation;
            
            Debug.Log("CharacterFollower: 角色跟随系统已启动");
        }
        
        void Update()
        {
            // 获取最新的姿势检测结果
            if (poseLandmarkerRunner != null && poseLandmarkerRunner.LatestResult.poseLandmarks != null)
            {
                ProcessPoseData(poseLandmarkerRunner.LatestResult);
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
        /// 处理姿势数据并更新角色位置
        /// </summary>
        /// <param name="result">姿势检测结果</param>
        private void ProcessPoseData(PoseLandmarkerResult result)
        {
            try
            {
                if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
                    return;
                    
                // 获取第一个检测到的人的姿势数据
                var landmarks = result.poseLandmarks[0];
                
                // 使用更安全的方式检查landmarks - 完全避免类型转换
                bool isValidLandmarks = false;
                try
                {
                    // 使用ReferenceEquals避免直接比较MediaPipe类型
                    if (!ReferenceEquals(landmarks, null))
                    {
                        // 尝试反射访问Count属性来验证数据有效性
                        var landmarksType = landmarks.GetType();
                        var countProperty = landmarksType.GetProperty("Count");
                        if (countProperty != null)
                        {
                            var count = (int)countProperty.GetValue(landmarks);
                            isValidLandmarks = count > 0;
                        }
                    }
                }
                catch
                {
                    isValidLandmarks = false;
                }
                
                if (!isValidLandmarks)
                {
                    return;
                }
                
                // 计算身体中心点
                Vector3 bodyCenter = CalculateBodyCenter(landmarks);
                
                if (!isInitialized)
                {
                    lastCenterPosition = bodyCenter;
                    isInitialized = true;
                    return;
                }
                
                // 计算移动差异
                Vector3 movement = bodyCenter - lastCenterPosition;
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
                
                // 计算旋转（基于肩膀角度）
                if (enableRotation)
                {
                    CalculateBodyRotation(landmarks);
                }
                
                lastCenterPosition = bodyCenter;
                
                // 调试信息
                if (showDebugInfo)
                {
                    Debug.Log($"Body Center: {bodyCenter}, Movement: {movement}, Target: {targetPosition}");
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CharacterFollower: 姿势数据处理错误: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 计算身体中心点 - 使用纯反射避免类型转换
        /// </summary>
        /// <param name="landmarks">姿势关键点</param>
        /// <returns>身体中心点坐标</returns>
        private Vector3 CalculateBodyCenter(object landmarks)
        {
            Vector3 center = Vector3.zero;
            int pointCount = 0;
            
            try
            {
                // 使用反射获取landmarks信息，完全避免类型转换
                var landmarksType = landmarks.GetType();
                var countProperty = landmarksType.GetProperty("Count");
                var indexProperty = landmarksType.GetProperty("Item"); // 索引器
                
                if (countProperty == null || indexProperty == null)
                {
                    return lastCenterPosition; // 无法访问数据，返回上一帧位置
                }
                
                int count = (int)countProperty.GetValue(landmarks);
                if (count == 0)
                {
                    return lastCenterPosition;
                }
                
                // 获取landmarks的辅助方法
                System.Func<int, Vector3> GetLandmarkPosition = (int index) =>
                {
                    if (index >= count) return Vector3.zero;
                    
                    try
                    {
                        var landmark = indexProperty.GetValue(landmarks, new object[] { index });
                        if (ReferenceEquals(landmark, null)) return Vector3.zero;
                        
                        var landmarkType = landmark.GetType();
                        var xProp = landmarkType.GetProperty("x") ?? landmarkType.GetProperty("X");
                        var yProp = landmarkType.GetProperty("y") ?? landmarkType.GetProperty("Y");
                        var zProp = landmarkType.GetProperty("z") ?? landmarkType.GetProperty("Z");
                        
                        if (xProp != null && yProp != null && zProp != null)
                        {
                            float x = System.Convert.ToSingle(xProp.GetValue(landmark));
                            float y = System.Convert.ToSingle(yProp.GetValue(landmark));
                            float z = System.Convert.ToSingle(zProp.GetValue(landmark));
                            return new Vector3(x, y, z);
                        }
                    }
                    catch
                    {
                        // 忽略访问错误
                    }
                    
                    return Vector3.zero;
                };
                
                // 使用选定的参考点计算中心
                if (useShoulders && count > RIGHT_SHOULDER)
                {
                    center += GetLandmarkPosition(LEFT_SHOULDER);
                    center += GetLandmarkPosition(RIGHT_SHOULDER);
                    pointCount += 2;
                }
                
                if (useHips && count > RIGHT_HIP)
                {
                    center += GetLandmarkPosition(LEFT_HIP);
                    center += GetLandmarkPosition(RIGHT_HIP);
                    pointCount += 2;
                }
                
                if (useHands && count > RIGHT_WRIST)
                {
                    center += GetLandmarkPosition(LEFT_WRIST);
                    center += GetLandmarkPosition(RIGHT_WRIST);
                    pointCount += 2;
                }
                
                // 如果没有选择任何参考点，使用肩膀作为默认
                if (pointCount == 0 && count > RIGHT_SHOULDER)
                {
                    center += GetLandmarkPosition(LEFT_SHOULDER);
                    center += GetLandmarkPosition(RIGHT_SHOULDER);
                    pointCount = 2;
                }
                
                if (pointCount > 0)
                {
                    center /= pointCount;
                }
                else
                {
                    center = lastCenterPosition;
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CalculateBodyCenter错误: {e.Message}");
                }
                center = lastCenterPosition; // 使用上一帧的位置作为备用
            }
            
            return center;
        }
        
        /// <summary>
        /// 根据肩膀角度计算身体旋转 - 使用纯反射
        /// </summary>
        /// <param name="landmarks">姿势关键点</param>
        private void CalculateBodyRotation(object landmarks)
        {
            try
            {
                // 使用反射获取landmarks信息
                var landmarksType = landmarks.GetType();
                var countProperty = landmarksType.GetProperty("Count");
                var indexProperty = landmarksType.GetProperty("Item");
                
                if (countProperty == null || indexProperty == null)
                    return;
                
                int count = (int)countProperty.GetValue(landmarks);
                if (count <= RIGHT_SHOULDER)
                    return;
                
                // 获取landmark位置的辅助方法
                System.Func<int, Vector3> GetLandmarkPosition = (int index) =>
                {
                    if (index >= count) return Vector3.zero;
                    
                    try
                    {
                        var landmark = indexProperty.GetValue(landmarks, new object[] { index });
                        if (ReferenceEquals(landmark, null)) return Vector3.zero;
                        
                        var landmarkType = landmark.GetType();
                        var xProp = landmarkType.GetProperty("x") ?? landmarkType.GetProperty("X");
                        var yProp = landmarkType.GetProperty("y") ?? landmarkType.GetProperty("Y");
                        var zProp = landmarkType.GetProperty("z") ?? landmarkType.GetProperty("Z");
                        
                        if (xProp != null && yProp != null && zProp != null)
                        {
                            float x = System.Convert.ToSingle(xProp.GetValue(landmark));
                            float y = System.Convert.ToSingle(yProp.GetValue(landmark));
                            float z = System.Convert.ToSingle(zProp.GetValue(landmark));
                            return new Vector3(x, y, z);
                        }
                    }
                    catch
                    {
                        // 忽略访问错误
                    }
                    
                    return Vector3.zero;
                };
                    
                // 计算肩膀之间的向量
                Vector3 leftShoulder = GetLandmarkPosition(LEFT_SHOULDER);
                Vector3 rightShoulder = GetLandmarkPosition(RIGHT_SHOULDER);
                
                Vector3 shoulderDirection = rightShoulder - leftShoulder;
                
                // 计算旋转角度
                if (shoulderDirection.magnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(shoulderDirection.y, shoulderDirection.x) * Mathf.Rad2Deg;
                    targetRotation = Quaternion.Euler(0, 0, angle);
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CalculateBodyRotation错误: {e.Message}");
                }
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
            Debug.Log("CharacterFollower: 角色位置已重置");
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