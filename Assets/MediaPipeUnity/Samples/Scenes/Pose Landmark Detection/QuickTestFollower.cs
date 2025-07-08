using UnityEngine;

/// <summary>
/// 超简单测试脚本 - 验证脚本运行和按键响应
/// </summary>
public class QuickTestFollower : MonoBehaviour
{
    [Header("测试设置")]
    public bool createTestCube = true;
    public Color cubeColor = Color.green;
    
    private GameObject testCube;
    private bool demoMode = true;
    private float time = 0f;
    
    void Start()
    {
        Debug.Log("🚀 QuickTestFollower 启动！");
        
        if (createTestCube)
        {
            CreateTestCube();
        }
        
        Debug.Log("✅ 测试脚本运行中，按D键切换模式，按R键重置");
    }
    
    void CreateTestCube()
    {
        // 创建测试立方体
        testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "QuickTestCube";
        testCube.transform.position = new Vector3(2, 1, 0);
        testCube.transform.localScale = Vector3.one;
        
        // 设置颜色
        Renderer renderer = testCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = cubeColor;
        }
        
        Debug.Log("✅ 创建了测试立方体");
    }
    
    void Update()
    {
        // 按键检测
        if (Input.GetKeyDown(KeyCode.D))
        {
            demoMode = !demoMode;
            Debug.Log($"🔄 切换模式: {(demoMode ? "演示模式" : "静止模式")}");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCube();
        }
        
        // 移动立方体
        if (testCube != null && demoMode)
        {
            time += Time.deltaTime;
            Vector3 newPos = new Vector3(
                Mathf.Sin(time) * 2f,
                Mathf.Cos(time) * 1f + 1f,
                0
            );
            testCube.transform.position = newPos;
        }
    }
    
    void ResetCube()
    {
        if (testCube != null)
        {
            testCube.transform.position = new Vector3(2, 1, 0);
            time = 0f;
            Debug.Log("🔄 重置立方体位置");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 100, 300, 100));
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUILayout.Label("🧪 快速测试", style);
        GUILayout.Label($"模式: {(demoMode ? "演示" : "静止")}", style);
        GUILayout.Label("D-切换 R-重置", style);
        
        GUILayout.EndArea();
    }
} 