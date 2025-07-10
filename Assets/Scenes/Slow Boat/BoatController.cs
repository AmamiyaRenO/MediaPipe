using UnityEngine;
using System.Reflection;
using System.Collections;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections.Generic;

public class BoatController : MonoBehaviour
{
    [Header("Dependencies")]
    public PoseLandmarkerRunner poseRunner;
    public Transform boatModel; // The child object to rotate (e.g., BoatRoot)
    public BoatVoiceController voiceController; // Voice controller
    public GameStateManager gameStateManager; // Game state manager

    [Header("Player Control")]
    public float tiltMultiplier = 50f;
    public float maxTiltAngle = 75f;

    [Header("Wave Disturbance")]
    public float waveStrength = 50f;
    public float waveFrequency = 0.3f;
    public float directionChangeInterval = 30f;

    [Header("Inertia Parameters")]
    public float inertia = 0.6f;
    public float damping = 0.6f;

    [Header("Floating Parameters")]
    public float floatOffset = -0.1f;           // Manually sink or raise
    public float floatAmplitude = 0.7f;         // Control the amplitude of floating

    [Header("Debug Parameters")]
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
            Debug.LogError("❌ PoseLandmarkerRunner not found");
            enabled = false;
            return;
        }

        if (boatModel == null)
        {
            Debug.LogError("❌ Please assign boatModel in BoatController (the child object to rotate)");
            enabled = false;
            return;
        }

        // Find voice controller (optional)
        if (voiceController == null)
        {
            voiceController = FindObjectOfType<BoatVoiceController>();
            if (voiceController != null)
                Debug.Log("✅ Voice controller found automatically");
        }
        
        // Find game state manager (optional)
        if (gameStateManager == null)
        {
            gameStateManager = FindObjectOfType<GameStateManager>();
            if (gameStateManager != null)
                Debug.Log("✅ Game state manager found automatically");
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
            Debug.LogError("❌ Cannot find landmarks field");
        else
            Debug.Log("✅ Successfully bound landmarks field");
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
        
        // 如果游戏未开始，不进行船体控制
        if (gameStateManager != null && !gameStateManager.gameStarted) return;

        float timeSinceLastChange = Time.time - lastDirectionChangeTime;
        if (timeSinceLastChange > directionChangeInterval)
        {
            lastDirectionChangeTime = Time.time;
            RefreshWaveDirection();
        }

        float t = Time.time * waveFrequency;
        float noise = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 0.1f * waveStrength;
        float waveOffset = baseWaveOffset + noise;

        float playerOffset = GetPlayerOffset() * tiltMultiplier;
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
    
    /// <summary>
    /// 设置风向 - 语音控制用
    /// </summary>
    /// <param name="direction">风向：1=右风，-1=左风</param>
    public void SetWindDirection(int direction)
    {
        currentDirection = direction;
        currentWaveStrength = Random.Range(waveStrength * 0.7f, waveStrength);
        baseWaveOffset = -currentWaveStrength * currentDirection;
        lastDirectionChangeTime = Time.time; // 重置计时器，避免立即再次改变

        if (seaWave != null)
        {
            seaWave.waveAngle = currentDirection > 0 ? 0f : 180f;
        }
        
        if (showDebugLog)
        {
            string dirStr = currentDirection > 0 ? "右风" : "左风";
            Debug.Log($"🌬️ 风向已设置为: {dirStr} (强度: {currentWaveStrength:F1})");
        }
    }
    
    /// <summary>
    /// 获取当前风向
    /// </summary>
    /// <returns>1=右风，-1=左风</returns>
    public int GetCurrentWindDirection() => currentDirection;

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

    float GetPlayerOffset()
    {
        return GetLeftRightBias(); // 使用姿态检测
    }

    void OnGUI()
    {
        // 如果游戏未开始，不显示控制UI
        if (gameStateManager != null && !gameStateManager.gameStarted) return;
        
        if (blueStyle == null)
        {
            blueStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.blue } };
            redStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
        }

        int w = 400, h = 120, x0 = 40, y0 = 40;
        int sampleCount = 60;
        float timeWindow = 3f;

        GUI.Box(new Rect(x0 - 10, y0 - 10, w + 20, h + 20), "Wave Forecast");
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

        GUI.Label(new Rect(x0 - 30, y0 + h / 2 - 10, 60, 20), "<- Now");

        string dir = currentDirection > 0 ? "→" : "←";
        GUIStyle arrowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = currentDirection > 0 ? Color.red : Color.blue }
        };
        GUI.Label(new Rect(x0 + w / 2 - 20, y0 + h + 10, 60, 40), dir, arrowStyle);

        GUI.Label(new Rect(x0, y0 + h + 40, 300, 20), $"Current Strength: {currentWaveStrength:F1}");

        float timeToChange = directionChangeInterval - (Time.time - lastDirectionChangeTime);
        if (timeToChange <= 5f)
        {
            GUIStyle warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
            GUI.Label(new Rect(x0, y0 + h + 65, 400, 30), "⚠️ Wind direction will change soon!", warnStyle);
        }
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
