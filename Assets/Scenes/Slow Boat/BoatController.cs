using UnityEngine;
using System.Reflection;
using System.Collections;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections.Generic;

public class BoatController : MonoBehaviour
{
    [Header("依赖组件")]
    public PoseLandmarkerRunner poseRunner;
    public Transform boatModel; // 实际旋转的子物体（如 BoatRoot）

    [Header("玩家控制")]
    public float tiltMultiplier = 50f;
    public float maxTiltAngle = 75f;

    [Header("风浪扰动")]
    public float waveStrength = 50f;
    public float waveFrequency = 0.3f;
    public float directionChangeInterval = 30f;

    [Header("惯性参数")]
    public float inertia = 0.6f;
    public float damping = 0.6f;

    [Header("浮动参数")]
    public float floatOffset = -0.1f;           // 手动下沉或抬高
    public float floatAmplitude = 0.7f;         // 控制波动影响程度

    [Header("调试参数")]
    public bool showDebugLog = true;

    private float currentTilt = 0f;
    private float angularVelocity = 0f;
    private float currentWaveStrength = 0f;
    private float baseWaveOffset = 0f;
    private int currentDirection = 1;
    private float lastDirectionChangeTime = 0f;

    private FieldInfo landmarkField;
    public SimpleWave seaWave;

    private GUIStyle blueStyle, redStyle;

    private float baseY; // 船体原始 Y 坐标

    void Start()
    {
        if (poseRunner == null)
            poseRunner = FindObjectOfType<PoseLandmarkerRunner>();

        if (poseRunner == null)
        {
            Debug.LogError("❌ 没有找到 PoseLandmarkerRunner");
            enabled = false;
            return;
        }

        if (boatModel == null)
        {
            Debug.LogError("❌ 请在 BoatController 中指定 boatModel（用于旋转的子物体）");
            enabled = false;
            return;
        }

        lastDirectionChangeTime = Time.time;
        RefreshWaveDirection();

        baseY = (seaWave != null) ? seaWave.GetWaveHeightAtPosition(transform.position) : transform.position.y;

        StartCoroutine(WaitForResultThenInitField());
    }

    IEnumerator WaitForResultThenInitField()
    {
        while (true)
        {
            var result = poseRunner.LatestResult;
            if (!result.Equals(default(PoseLandmarkerResult)) &&
                result.poseLandmarks != null &&
                result.poseLandmarks.Count > 0)
                break;

            yield return null;
        }

        landmarkField = poseRunner.LatestResult.poseLandmarks[0]
            .GetType()
            .GetField("landmarks", BindingFlags.Instance | BindingFlags.Public);

        if (landmarkField == null)
            Debug.LogError("❌ 无法找到 landmarks 字段");
        else
            Debug.Log("✅ 成功绑定 landmarks 字段");
    }

    void RefreshWaveDirection()
    {
        currentDirection = Random.value > 0.5f ? 1 : -1;
        currentWaveStrength = Random.Range(waveStrength * 0.7f, waveStrength);
        baseWaveOffset = -currentWaveStrength * currentDirection;

        if (seaWave != null)
        {
            seaWave.waveAngle = currentDirection > 0 ? 0f : 180f;
        }
    }

    void Update()
    {
        if (poseRunner == null || landmarkField == null) return;

        float timeSinceLastChange = Time.time - lastDirectionChangeTime;
        if (timeSinceLastChange > directionChangeInterval)
        {
            lastDirectionChangeTime = Time.time;
            RefreshWaveDirection();
        }

        float t = Time.time * waveFrequency;
        float noise = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 0.1f * waveStrength;
        float waveOffset = baseWaveOffset + noise;

        float playerOffset = GetLeftRightBias() * tiltMultiplier;
        float targetAngle = Mathf.Clamp(waveOffset + playerOffset, -maxTiltAngle, maxTiltAngle);

        float torque = (targetAngle - currentTilt) * 1.2f;
        angularVelocity += torque * Time.deltaTime / inertia;
        angularVelocity *= Mathf.Exp(-damping * Time.deltaTime);

        currentTilt += angularVelocity * Time.deltaTime;
        currentTilt = Mathf.Clamp(currentTilt, -maxTiltAngle, maxTiltAngle);

        // 改进浮动实现（非对称）
        if (seaWave != null)
        {
            float waveY = seaWave.GetWaveHeightAtPosition(transform.position);
            float dy = waveY - baseY;
            float bias = dy > 0 ? 0.4f : 1.0f; // 波峰抑制，上浮减弱，波谷不变
            float finalY = baseY + dy * floatAmplitude * bias + floatOffset;
            transform.position = new Vector3(transform.position.x, finalY, transform.position.z);
        }

        boatModel.localRotation = Quaternion.Euler(0f, 0f, currentTilt);

        if (seaWave != null && Time.frameCount % 5 == 0)
        {
            seaWave.waveHeight = currentWaveStrength * 0.1f;
            seaWave.waveFrequency = waveFrequency;
        }
    }

    public float GetCurrentTilt() => currentTilt;

    float GetLeftRightBias()
    {
        var result = poseRunner.LatestResult;
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            return 0f;

        var first = result.poseLandmarks[0];
        var raw = landmarkField.GetValue(first) as IList;
        if (raw == null || raw.Count < 13) return 0f;

        var left = raw[11];
        var right = raw[12];

        float yL = (float)left.GetType().GetField("y").GetValue(left);
        float yR = (float)right.GetType().GetField("y").GetValue(right);

        return Mathf.Clamp((yR - yL) * 5f, -1f, 1f);
    }

    void OnGUI()
    {
        if (blueStyle == null)
        {
            blueStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.blue } };
            redStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
        }

        int w = 400, h = 120, x0 = 40, y0 = 40;
        int sampleCount = 60;
        float timeWindow = 3f;

        GUI.Box(new Rect(x0 - 10, y0 - 10, w + 20, h + 20), "未来风浪预告");
        DrawLine(new Vector2(x0, y0 + h / 2), new Vector2(x0 + w, y0 + h / 2), Color.gray, 1f);

        Vector2 last = Vector2.zero;
        for (int i = 0; i < sampleCount; i++)
        {
            float dt = (i / (float)(sampleCount - 1)) * timeWindow;
            float time = (Time.time + dt) * waveFrequency;
            float n = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 0.1f * waveStrength;
            float s = Mathf.Abs(baseWaveOffset + n);

            Vector2 pt = new Vector2(
                x0 + i * (w / (sampleCount - 1)),
                y0 + h / 2 - s * (h / 2) / waveStrength
            );
            if (i > 0) DrawLine(last, pt, Color.cyan, 2f);
            last = pt;
        }

        GUI.Label(new Rect(x0 - 30, y0 + h / 2 - 10, 60, 20), "<- 现在");

        string dir = currentDirection > 0 ? "→" : "←";
        GUIStyle arrowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = currentDirection > 0 ? Color.red : Color.blue }
        };
        GUI.Label(new Rect(x0 + w / 2 - 20, y0 + h + 10, 60, 40), dir, arrowStyle);

        GUI.Label(new Rect(x0, y0 + h + 40, 300, 20), "箭头指示当前风浪方向，曲线为强度波动");
    }

    void DrawLine(Vector2 p1, Vector2 p2, Color color, float width)
    {
        Color savedColor = GUI.color;
        Matrix4x4 savedMatrix = GUI.matrix;

        GUI.color = color;
        float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;
        float length = Vector2.Distance(p1, p2);

        GUIUtility.RotateAroundPivot(angle, p1);
        GUI.DrawTexture(new Rect(p1.x, p1.y - width / 2, length, width), Texture2D.whiteTexture);
        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }
}
