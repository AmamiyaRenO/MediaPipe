using UnityEngine;

/// <summary>
/// 强制演示脚本 - 确保立方体一定会移动
/// </summary>
public class ForceDemo : MonoBehaviour
{
    [Header("强制移动设置")]
    public bool alwaysMove = true;
    public float moveSpeed = 2f;
    public float moveRange = 3f;
    public Color cubeColor = Color.red;
    
    private GameObject demoCube;
    private float timer = 0f;
    
    void Start()
    {
        Debug.Log("🔥 ForceDemo 启动 - 强制移动演示!");
        
        // 创建演示立方体
        demoCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        demoCube.name = "ForceDemoCube";
        demoCube.transform.position = new Vector3(-3, 0, 0);
        demoCube.transform.localScale = Vector3.one * 1.2f;
        
        // 设置颜色
        Renderer renderer = demoCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = cubeColor;
        }
        
        Debug.Log("✅ 强制演示立方体已创建，应该会开始移动!");
    }
    
    void Update()
    {
        if (demoCube != null && alwaysMove)
        {
            timer += Time.deltaTime * moveSpeed;
            
            // 强制圆周运动
            Vector3 newPos = new Vector3(
                Mathf.Sin(timer) * moveRange - 3f,
                Mathf.Cos(timer) * moveRange,
                0
            );
            
            demoCube.transform.position = newPos;
            
            // 每秒打印一次日志确认脚本在运行
            if (Mathf.Floor(timer) != Mathf.Floor(timer - Time.deltaTime))
            {
                Debug.Log($"🔥 ForceDemo 运行中: {timer:F1}秒, 位置: {newPos}");
            }
        }
        
        // 按键控制
        if (Input.GetKeyDown(KeyCode.F))
        {
            alwaysMove = !alwaysMove;
            Debug.Log($"🔥 强制移动: {(alwaysMove ? "启用" : "禁用")}");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 300, 80));
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.red;
        
        GUILayout.Label("🔥 强制演示模式", style);
        GUILayout.Label($"移动: {(alwaysMove ? "开启" : "关闭")}", style);
        GUILayout.Label("按F键切换", style);
        
        GUILayout.EndArea();
    }
} 