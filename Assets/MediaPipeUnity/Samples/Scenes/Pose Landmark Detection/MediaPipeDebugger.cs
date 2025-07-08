using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Reflection;

/// <summary>
/// MediaPipe数据结构深度调试器
/// 用于找出真正的姿态数据访问路径
/// </summary>
public class MediaPipeDebugger : MonoBehaviour
{
    private PoseLandmarkerRunner poseRunner;
    private int debugFrameCounter = 0;
    
    void Start()
    {
        Debug.Log("🔍 启动MediaPipe数据结构调试器");
        
        poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
        if (poseRunner == null)
        {
            Debug.LogError("❌ 未找到PoseLandmarkerRunner");
            return;
        }
        
        Debug.Log("✅ 找到PoseLandmarkerRunner，开始深度分析数据结构");
    }
    
    void Update()
    {
        if (poseRunner == null) return;
        
        debugFrameCounter++;
        
        // 每60帧（大约1秒）进行一次详细分析
        if (debugFrameCounter % 60 == 0)
        {
            AnalyzeLatestResult();
        }
        
        // 按D键进行即时分析
        if (Input.GetKeyDown(KeyCode.D))
        {
            AnalyzeLatestResult();
        }
        
        // 按A键尝试从注解控制器获取数据
        if (Input.GetKeyDown(KeyCode.A))
        {
            AnalyzeAnnotationController();
        }
    }
    
    void AnalyzeLatestResult()
    {
        try
        {
            var result = poseRunner.LatestResult;
            
            Debug.Log("=== MediaPipe结果分析 ===");
            Debug.Log($"🔍 result是否为null: {ReferenceEquals(result, null)}");
            
            if (!ReferenceEquals(result, null))
            {
                AnalyzeResultStructure(result);
            }
            else
            {
                Debug.Log("❌ result为null，无法分析");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"分析过程中出错: {e.Message}");
        }
    }
    
    void AnalyzeResultStructure(object result)
    {
        var resultType = result.GetType();
        Debug.Log($"📋 Result类型: {resultType.FullName}");
        
        // 尝试所有可能的BindingFlags组合
        var allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        
        // 获取所有属性
        var properties = resultType.GetProperties(allFlags);
        Debug.Log($"📋 共有{properties.Length}个属性:");
        
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(result);
                Debug.Log($"  🔸 {prop.Name} ({prop.PropertyType.Name}): {GetValueDescription(value)}");
                
                // 如果是姿态相关的属性，深入分析
                if (prop.Name.ToLower().Contains("pose") || prop.Name.ToLower().Contains("landmark"))
                {
                    AnalyzePoseProperty(prop.Name, value);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"  ❌ {prop.Name}: 获取失败 - {e.Message}");
            }
        }
        
        // 如果属性获取失败，尝试获取字段
        if (properties.Length == 0)
        {
            Debug.Log("🔍 属性为空，尝试获取字段...");
            var fields = resultType.GetFields(allFlags);
            Debug.Log($"📋 共有{fields.Length}个字段:");
            
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(result);
                    Debug.Log($"  🔹 字段 {field.Name} ({field.FieldType.Name}): {GetValueDescription(value)}");
                    
                    if (field.Name.ToLower().Contains("pose") || field.Name.ToLower().Contains("landmark"))
                    {
                        AnalyzePoseProperty(field.Name, value);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  ❌ 字段 {field.Name}: {e.Message}");
                }
            }
        }
        
        // 尝试获取所有方法
        Debug.Log("🔍 尝试获取方法...");
        var methods = resultType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (method.GetParameters().Length == 0 && method.ReturnType != typeof(void))
            {
                if (method.Name.ToLower().Contains("pose") || method.Name.ToLower().Contains("landmark") || 
                    method.Name.StartsWith("get_") || method.Name.StartsWith("Get"))
                {
                    try
                    {
                        var value = method.Invoke(result, null);
                        Debug.Log($"  🔧 方法 {method.Name}(): {GetValueDescription(value)}");
                        
                        if (method.Name.ToLower().Contains("pose") || method.Name.ToLower().Contains("landmark"))
                        {
                            AnalyzePoseProperty(method.Name, value);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"  ❌ 方法 {method.Name}: {e.Message}");
                    }
                }
            }
        }
        
        // 尝试直接访问已知的属性名
        Debug.Log("🎯 尝试直接访问常见属性名...");
        string[] knownProperties = { 
            "poseLandmarks", "PoseLandmarks", "pose_landmarks",
            "landmarks", "Landmarks", "results", "Results"
        };
        
        foreach (var propName in knownProperties)
        {
            try
            {
                var prop = resultType.GetProperty(propName, allFlags);
                if (prop != null)
                {
                    var value = prop.GetValue(result);
                    Debug.Log($"  ⭐ 找到属性 {propName}: {GetValueDescription(value)}");
                    AnalyzePoseProperty(propName, value);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"  ❌ 直接访问 {propName}: {e.Message}");
            }
        }
    }
    
    void AnalyzePoseProperty(string propertyName, object value)
    {
        Debug.Log($"🎯 深入分析姿态属性: {propertyName}");
        
        if (ReferenceEquals(value, null))
        {
            Debug.Log($"  ❌ {propertyName} 为 null");
            return;
        }
        
        var valueType = value.GetType();
        Debug.Log($"  📋 类型: {valueType.FullName}");
        
        // 检查是否是集合类型
        if (valueType.IsGenericType || valueType.IsArray || HasCountProperty(value))
        {
            AnalyzeCollection(propertyName, value);
        }
        
        // 检查所有属性
        var properties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            try
            {
                var propValue = prop.GetValue(value);
                Debug.Log($"    🔹 {prop.Name}: {GetValueDescription(propValue)}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"    ❌ {prop.Name}: {e.Message}");
            }
        }
    }
    
    void AnalyzeCollection(string collectionName, object collection)
    {
        Debug.Log($"📦 分析集合: {collectionName}");
        
        try
        {
            // 尝试获取Count属性
            var countProperty = collection.GetType().GetProperty("Count");
            if (countProperty != null)
            {
                var count = countProperty.GetValue(collection);
                Debug.Log($"  📊 Count: {count}");
                
                if (count != null && (int)count > 0)
                {
                    // 尝试获取第一个元素
                    var itemProperty = collection.GetType().GetProperty("Item");
                    if (itemProperty != null)
                    {
                        try
                        {
                            var firstItem = itemProperty.GetValue(collection, new object[] { 0 });
                            Debug.Log($"  🎯 第一个元素: {GetValueDescription(firstItem)}");
                            
                            if (!ReferenceEquals(firstItem, null))
                            {
                                AnalyzeFirstLandmark(firstItem);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log($"  ❌ 获取第一个元素失败: {e.Message}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"  ❌ 分析集合失败: {e.Message}");
        }
    }
    
    void AnalyzeFirstLandmark(object landmark)
    {
        Debug.Log("🎯 分析第一个关键点:");
        
        var landmarkType = landmark.GetType();
        Debug.Log($"  📋 关键点类型: {landmarkType.FullName}");
        
        // 检查所有属性
        var properties = landmarkType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(landmark);
                Debug.Log($"    🔸 {prop.Name}: {GetValueDescription(value)}");
                
                // 如果这是坐标相关的属性，特别标注
                if (prop.Name.ToLower() == "x" || prop.Name.ToLower() == "y" || prop.Name.ToLower() == "z")
                {
                    Debug.Log($"    ⭐ 坐标值 {prop.Name}: {value} (类型: {prop.PropertyType.Name})");
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"    ❌ {prop.Name}: {e.Message}");
            }
        }
    }
    
    bool HasCountProperty(object obj)
    {
        if (ReferenceEquals(obj, null)) return false;
        return obj.GetType().GetProperty("Count") != null;
    }
    
    string GetValueDescription(object value)
    {
        if (ReferenceEquals(value, null))
            return "null";
        
        if (value is System.Collections.ICollection collection)
            return $"集合 (Count: {collection.Count})";
        
        return $"{value} ({value.GetType().Name})";
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 450, 400, 100));
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.yellow;
        
        GUILayout.Label("🔍 MediaPipe调试器", style);
        GUILayout.Label("按 D 键分析结果数据", style);
        GUILayout.Label("按 A 键分析注解控制器", style);
        GUILayout.Label("查看Console获取详细信息", style);
        
        GUILayout.EndArea();
    }
    
    void AnalyzeAnnotationController()
    {
        Debug.Log("=== 分析注解控制器 ===");
        
        try
        {
            var annotationController = poseRunner.PoseLandmarkerResultAnnotationController;
            if (annotationController == null)
            {
                Debug.Log("❌ 注解控制器为null");
                return;
            }
            
            Debug.Log($"✅ 找到注解控制器: {annotationController.GetType().FullName}");
            
            // 分析注解控制器的所有成员
            var controllerType = annotationController.GetType();
            var allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // 属性
            var properties = controllerType.GetProperties(allFlags);
            Debug.Log($"📋 注解控制器有{properties.Length}个属性:");
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(annotationController);
                    Debug.Log($"  🔸 {prop.Name}: {GetValueDescription(value)}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  ❌ {prop.Name}: {e.Message}");
                }
            }
            
            // 字段
            var fields = controllerType.GetFields(allFlags);
            Debug.Log($"📋 注解控制器有{fields.Length}个字段:");
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(annotationController);
                    Debug.Log($"  🔹 字段 {field.Name}: {GetValueDescription(value)}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  ❌ 字段 {field.Name}: {e.Message}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"分析注解控制器失败: {e.Message}");
        }
    }
} 