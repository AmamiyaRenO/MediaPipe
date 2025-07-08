using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using System.Reflection;
using System.Collections;

public class PoseDataTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableTest = true;
    [SerializeField] private float movementScale = 10f;

    private PoseLandmarkerRunner poseRunner;
    private GameObject testCube;
    private Vector3 lastValidPosition;
    private bool hasValidData = false;

    void Start()
    {
        Debug.Log("🧪 启动姿态数据测试器");

        poseRunner = FindObjectOfType<PoseLandmarkerRunner>();
        if (poseRunner == null)
        {
            Debug.LogError("❌ 未找到PoseLandmarkerRunner");
            return;
        }

        CreateTestCube();
        Debug.Log("✅ 姿态数据测试器初始化完成");
    }

    void CreateTestCube()
    {
        testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "PoseDataTestCube";
        testCube.transform.position = new Vector3(0, 0, 5);
        testCube.transform.localScale = Vector3.one * 2f;

        var renderer = testCube.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.yellow;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.5f);
        renderer.material = material;

        Debug.Log("✅ 创建测试立方体 (黄色)");
    }

    void Update()
    {
        if (!enableTest || testCube == null || poseRunner == null) return;
        TestPoseDataExtraction();
    }

    void TestPoseDataExtraction()
    {
        try
        {
            var result = poseRunner.LatestResult;
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning("⚠️ 没有检测到任何姿态");
                }
                return;
            }

            var landmarksContainer = result.poseLandmarks[0];
            var fieldInfo = landmarksContainer.GetType().GetField("landmarks", BindingFlags.Instance | BindingFlags.Public);
            if (fieldInfo == null)
            {
                Debug.LogError("❌ 'landmarks' 字段不存在");
                return;
            }

            var rawLandmarks = fieldInfo.GetValue(landmarksContainer) as IList;
            if (rawLandmarks == null || rawLandmarks.Count == 0)
            {
                Debug.LogWarning("⚠️ landmarks 列表为空");
                return;
            }

            var nose = rawLandmarks[0];
            var noseType = nose.GetType();

            float x = (float)noseType.GetField("x").GetValue(nose);
            float y = (float)noseType.GetField("y").GetValue(nose);
            float z = (float)noseType.GetField("z").GetValue(nose);

            Vector3 normalizedPos = new Vector3(x, y, z);
            Vector3 worldPos = ConvertToWorldPosition(normalizedPos);
            testCube.transform.position = worldPos;

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"🎯 鼻子坐标: {normalizedPos} -> 世界坐标: {worldPos}");
            }

            lastValidPosition = worldPos;
            hasValidData = true;
        }
        catch (System.Exception e)
        {
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogError($"❌ 数据提取异常: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    Vector3 ConvertToWorldPosition(Vector3 normalizedPos)
    {
        float worldX = (normalizedPos.x - 0.5f) * movementScale;
        float worldY = (0.5f - normalizedPos.y) * movementScale * 0.6f;
        float worldZ = 5f; // 固定深度
        return new Vector3(worldX, worldY, worldZ);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 400, 120));

        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle textStyle = new GUIStyle
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        GUILayout.Label("🧪 姿态数据测试器", titleStyle);
        GUILayout.Label($"状态: {(hasValidData ? "✅ 获取数据成功" : "⏳ 等待数据")}", textStyle);

        if (hasValidData)
        {
            GUILayout.Label($"上次位置: {lastValidPosition}", textStyle);
            GUILayout.Label("黄色立方体应该跟随你的鼻子移动", textStyle);
        }

        GUILayout.EndArea();
    }
}
