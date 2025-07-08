using UnityEngine;

/// <summary>
/// 场景清理器 - 清理可能冲突的GameObject和脚本
/// </summary>
public class SceneCleaner : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🧹 开始清理场景...");
        CleanupPreviousObjects();
    }
    
    void CleanupPreviousObjects()
    {
        // 查找并删除之前创建的GameObject
        string[] objectNames = {
            "ScreenBasedFollowCube",
            "ScreenPoseDemo", 
            "test",
            "PoseFollowDemo",
            "SimpleFollowCube",
            "CharacterFollowCube"
        };
        
        foreach (string objName in objectNames)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                Debug.Log($"🗑️ 删除冲突对象: {objName}");
                DestroyImmediate(obj);
            }
        }
        
        // 查找并禁用可能冲突的脚本
        var conflictingScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in conflictingScripts)
        {
            string scriptName = script.GetType().Name;
            if (scriptName.Contains("CharacterFollower") || 
                scriptName.Contains("ScreenBasedFollower") ||
                scriptName.Contains("SimplePoseDemo") ||
                scriptName.Contains("ScreenPoseDemo") ||
                scriptName.Contains("UltraSimple"))
            {
                if (script.gameObject != gameObject) // 不要禁用自己
                {
                    Debug.Log($"⏸️ 禁用冲突脚本: {scriptName} 在 {script.gameObject.name}");
                    script.enabled = false;
                }
            }
        }
        
        Debug.Log("✅ 场景清理完成");
        
        // 清理完成后删除自己
        Destroy(this);
    }
    
    void Update()
    {
        // 按C键手动清理
        if (Input.GetKeyDown(KeyCode.C))
        {
            CleanupPreviousObjects();
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 50, 300, 80));
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUILayout.Label("🧹 场景清理器", style);
        GUILayout.Label("按 C 键手动清理冲突对象", style);
        
        GUILayout.EndArea();
    }
} 