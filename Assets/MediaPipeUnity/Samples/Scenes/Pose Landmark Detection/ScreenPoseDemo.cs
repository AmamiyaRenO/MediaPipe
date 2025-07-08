using UnityEngine;

/// <summary>
/// 屏幕姿态演示管理器 - 参考GitHub项目的思路
/// 直接在PoseLandmarker场景中创建跟随器
/// </summary>
public class ScreenPoseDemo : MonoBehaviour
{
    [Header("演示设置")]
    [SerializeField] private bool enableOnStart = true;
    
    private ScreenBasedFollower follower;
    private GameObject demoObject;
    
    void Start()
    {
        Debug.Log("🎬 启动屏幕姿态演示");
        
        if (enableOnStart)
        {
            CreateDemo();
        }
    }
    
    void CreateDemo()
    {
        // 创建演示对象
        demoObject = new GameObject("ScreenPoseDemo");
        demoObject.transform.position = Vector3.zero;
        
        // 添加跟随器组件
        follower = demoObject.AddComponent<ScreenBasedFollower>();
        
        Debug.Log("✅ 屏幕姿态演示创建完成！移动身体来控制红色立方体");
    }
    
    void Update()
    {
        // F键创建演示
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (demoObject == null)
            {
                CreateDemo();
            }
            else
            {
                Debug.Log("⚠️ 演示已存在");
            }
        }
        
        // X键删除演示
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (demoObject != null)
            {
                DestroyImmediate(demoObject);
                demoObject = null;
                follower = null;
                Debug.Log("🗑️ 已删除屏幕姿态演示");
            }
        }
    }
    
    void OnGUI()
    {
        UnityEngine.GUILayout.BeginArea(new UnityEngine.Rect(10, 100, 400, 150));
        
        UnityEngine.GUIStyle titleStyle = new UnityEngine.GUIStyle();
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = UnityEngine.FontStyle.Bold;
        titleStyle.normal.textColor = UnityEngine.Color.white;
        
        UnityEngine.GUIStyle textStyle = new UnityEngine.GUIStyle();
        textStyle.fontSize = 14;
        textStyle.normal.textColor = UnityEngine.Color.white;
        
        UnityEngine.GUILayout.Label("🎭 屏幕姿态跟随演示", titleStyle);
        UnityEngine.GUILayout.Space(10);
        
        if (demoObject == null)
        {
            UnityEngine.GUILayout.Label("按 F 键创建演示", textStyle);
        }
        else
        {
            UnityEngine.GUILayout.Label("状态: ✅ 演示运行中", textStyle);
            UnityEngine.GUILayout.Label("移动身体控制红色立方体", textStyle);
            UnityEngine.GUILayout.Label("R-重置 T-切换 X-删除", textStyle);
        }
        
        UnityEngine.GUILayout.EndArea();
    }
} 