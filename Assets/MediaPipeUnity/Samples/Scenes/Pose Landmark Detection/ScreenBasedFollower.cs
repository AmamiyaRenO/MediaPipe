using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

/// <summary>
/// 基于屏幕坐标的简单跟随器 - 灵感来自GitHub项目
/// 通过观察屏幕上的关键点可视化来推断人物位置
/// </summary>
public class ScreenBasedFollower : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private bool enableMovement = true;
    
    [Header("自动创建")]
    [SerializeField] private bool autoCreateCube = true;
    [SerializeField] private Color cubeColor = Color.red;
    
    private GameObject followCube;
    private PoseLandmarkerRunner poseRunner;
    private Camera mainCamera;
    private Vector3 lastDetectedPosition;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    
    void Start()
    {
        Debug.Log("🚀 启动基于屏幕的姿态跟随器");
        
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 查找姿态检测器
        poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
        if (poseRunner == null)
        {
            Debug.LogError("❌ 未找到PoseLandmarkerRunner");
            return;
        }
        
        if (autoCreateCube)
        {
            CreateFollowCube();
        }
        
        // 初始化位置
        targetPosition = new Vector3(0, 0, 5); // 在摄像机前方
        
        Debug.Log("✅ 屏幕跟随器初始化完成");
    }
    
    void CreateFollowCube()
    {
        followCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        followCube.name = "ScreenBasedFollowCube";
        followCube.transform.localScale = Vector3.one * 1.5f;
        
        // 设置材质
        Renderer renderer = followCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = cubeColor;
            material.SetFloat("_Metallic", 0.5f);
            material.SetFloat("_Smoothness", 0.8f);
            renderer.material = material;
        }
        
        Debug.Log("✅ 创建了屏幕跟随立方体");
    }
    
    void Update()
    {
        if (!enableMovement || followCube == null || poseRunner == null) return;
        
        // 尝试获取姿态数据
        TryGetPosePosition();
        
        // 平滑移动到目标位置
        followCube.transform.position = Vector3.Lerp(
            followCube.transform.position, 
            targetPosition, 
            Time.deltaTime * smoothSpeed
        );
        
        // 按键控制
        HandleInput();
    }
    
    void TryGetPosePosition()
    {
        try
        {
            var result = poseRunner.LatestResult;
            
            // 尝试多种方式获取位置数据
            if (!ReferenceEquals(result, null))
            {
                // 方法1：尝试使用反射获取第一个检测到的人的鼻子位置
                Vector3? nosePos = GetNosePositionFromResult(result);
                if (nosePos.HasValue)
                {
                    UpdateTargetPosition(nosePos.Value);
                    return;
                }
                
                // 方法2：如果方法1失败，使用简单的屏幕中心偏移模拟
                SimulateMovementFromInput();
            }
        }
        catch (System.Exception e)
        {
            if (Time.frameCount % 300 == 0) // 每5秒记录一次错误
            {
                Debug.LogWarning($"屏幕跟随器错误: {e.Message}");
            }
        }
    }
    
    Vector3? GetNosePositionFromResult(object result)
    {
        try
        {
            // 使用反射安全访问
            var resultType = result.GetType();
            var poseLandmarksProperty = resultType.GetProperty("poseLandmarks");
            
            if (poseLandmarksProperty != null)
            {
                var poseLandmarks = poseLandmarksProperty.GetValue(result);
                if (!ReferenceEquals(poseLandmarks, null))
                {
                    var countProperty = poseLandmarks.GetType().GetProperty("Count");
                    if (countProperty != null)
                    {
                        int count = (int)countProperty.GetValue(poseLandmarks);
                        if (count > 0)
                        {
                            var itemProperty = poseLandmarks.GetType().GetProperty("Item");
                            if (itemProperty != null)
                            {
                                var firstPose = itemProperty.GetValue(poseLandmarks, new object[] { 0 });
                                if (!ReferenceEquals(firstPose, null))
                                {
                                    return ExtractNosePosition(firstPose);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // 静默失败，尝试其他方法
        }
        
        return null;
    }
    
    Vector3? ExtractNosePosition(object landmarks)
    {
        try
        {
            var landmarksType = landmarks.GetType();
            var itemProperty = landmarksType.GetProperty("Item");
            
            if (itemProperty != null)
            {
                // 获取鼻子关键点（索引0）
                var noseLandmark = itemProperty.GetValue(landmarks, new object[] { 0 });
                if (!ReferenceEquals(noseLandmark, null))
                {
                    var landmarkType = noseLandmark.GetType();
                    var xProp = landmarkType.GetProperty("x") ?? landmarkType.GetProperty("X");
                    var yProp = landmarkType.GetProperty("y") ?? landmarkType.GetProperty("Y");
                    
                    if (xProp != null && yProp != null)
                    {
                        float x = System.Convert.ToSingle(xProp.GetValue(noseLandmark));
                        float y = System.Convert.ToSingle(yProp.GetValue(noseLandmark));
                        
                        // 转换为世界坐标
                        Vector3 worldPos = new Vector3(
                            (x - 0.5f) * sensitivity * 8f,  // 左右移动
                            (0.5f - y) * sensitivity * 6f,  // 上下移动
                            5f  // 固定深度
                        );
                        
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"🎯 鼻子位置: ({x:F3}, {y:F3}) -> 世界坐标: {worldPos}");
                        }
                        
                        return worldPos;
                    }
                }
            }
        }
        catch
        {
            // 静默失败
        }
        
        return null;
    }
    
    void UpdateTargetPosition(Vector3 detectedPosition)
    {
        if (!isInitialized)
        {
            lastDetectedPosition = detectedPosition;
            isInitialized = true;
        }
        
        targetPosition = detectedPosition;
        lastDetectedPosition = detectedPosition;
    }
    
    void SimulateMovementFromInput()
    {
        // 如果无法获取真实姿态数据，使用鼠标模拟（用于测试）
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 5f;
            
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
            targetPosition = worldPos;
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"🖱️ 鼠标模拟: {worldPos}");
            }
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 重置位置
            targetPosition = new Vector3(0, 0, 5);
            Debug.Log("🔄 重置位置");
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            // 切换移动功能
            enableMovement = !enableMovement;
            Debug.Log($"🔄 移动功能: {(enableMovement ? "启用" : "禁用")}");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 300, 350, 100));
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUILayout.Label("🖥️ 屏幕姿态跟随器", style);
        GUILayout.Label($"状态: {(enableMovement ? "运行中" : "暂停")}", style);
        GUILayout.Label("R-重置 T-切换 鼠标-测试", style);
        
        GUILayout.EndArea();
    }
} 