using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class SimpleWave : MonoBehaviour
{
    [Header("Wave Dynamics")]
    public float waveHeight = 0.5f;
    public float waveFrequency = 1f;
    [Tooltip("海浪变化速度")]
    public float waveSpeed = 0.3f;
    [Tooltip("海浪方向角度（0 = 向右，90 = 向前）")]
    [Range(0, 360)]
    public float waveAngle = 0f;
    [Tooltip("限制最大波峰高度（防止穿模）")]
    public float maxWaveHeight = 0.5f;

    [Header("Texture Scroll")]
    public float textureScrollSpeed = 0.1f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Renderer rend;

    private Vector2 waveDir = Vector2.right;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        rend = GetComponent<Renderer>();
        UpdateWaveDirection();
    }

    void Update()
    {
        UpdateWaveDirection();

        Vector3[] vertices = new Vector3[baseVertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = baseVertices[i];
            Vector2 pos = new Vector2(v.x, v.z);
            // 主浪（有方向感）
            float mainPhase = Vector2.Dot(waveDir, pos);
            float mainWave = Mathf.Sin(mainPhase * waveFrequency + Time.time * waveSpeed);
            // 可选：再加一组主浪，方向稍有偏移
            float mainPhase2 = Vector2.Dot(new Vector2(
                Mathf.Cos((waveAngle + 20f) * Mathf.Deg2Rad),
                Mathf.Sin((waveAngle + 20f) * Mathf.Deg2Rad)), pos);
            float mainWave2 = Mathf.Sin(mainPhase2 * (waveFrequency * 0.8f) + Time.time * waveSpeed * 1.1f);
            // 杂波（无方向感，幅度小）
            float sum = 0f;
            int waveCount = 3;
            for (int w = 0; w < waveCount; w++)
            {
                float angle = w * (360f / waveCount);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                float phase = Vector2.Dot(dir, pos);
                float freq = waveFrequency * (0.5f + 0.5f * Mathf.PerlinNoise(w * 10.1f, 0));
                float speed = waveSpeed * (0.5f + 0.5f * Mathf.PerlinNoise(0, w * 20.2f));
                float offset = w * 100f;
                sum += Mathf.Sin(phase * freq + Time.time * speed + offset);
            }
            float noise1 = Mathf.PerlinNoise(v.x * 0.15f, Time.time * 0.2f);
            float noise2 = Mathf.PerlinNoise(v.z * 0.12f, Time.time * 0.18f + 100f);
            float noise = (noise1 + noise2) * 0.5f;
            // 合成
            float finalWaveY = (mainWave + 0.5f * mainWave2) * waveHeight * (0.7f + noise * 0.3f);
            finalWaveY += 0.2f * sum / waveCount * waveHeight; // 杂波幅度较小
            finalWaveY += (noise - 0.5f) * waveHeight * 0.3f;
            finalWaveY = Mathf.Clamp(finalWaveY, -maxWaveHeight, maxWaveHeight);
            v.y = Mathf.Lerp(v.y, v.y + finalWaveY, 0.5f);
            vertices[i] = v;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        if (rend != null && rend.material != null && rend.material.mainTexture != null)
        {
            Vector2 offset = rend.material.mainTextureOffset;
            offset.x += waveDir.x * textureScrollSpeed * Time.deltaTime;
            offset.y += waveDir.y * textureScrollSpeed * Time.deltaTime;
            rend.material.mainTextureOffset = offset;
        }
    }

    void UpdateWaveDirection()
    {
        float rad = waveAngle * Mathf.Deg2Rad;
        waveDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    public float GetWaveHeightAtPosition(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        Vector2 pos = new Vector2(local.x, local.z);
        // 主浪
        float mainPhase = Vector2.Dot(waveDir, pos);
        float mainWave = Mathf.Sin(mainPhase * waveFrequency + Time.time * waveSpeed);
        float mainPhase2 = Vector2.Dot(new Vector2(
            Mathf.Cos((waveAngle + 20f) * Mathf.Deg2Rad),
            Mathf.Sin((waveAngle + 20f) * Mathf.Deg2Rad)), pos);
        float mainWave2 = Mathf.Sin(mainPhase2 * (waveFrequency * 0.8f) + Time.time * waveSpeed * 1.1f);
        // 杂波
        float sum = 0f;
        int waveCount = 3;
        for (int w = 0; w < waveCount; w++)
        {
            float angle = w * (360f / waveCount);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float phase = Vector2.Dot(dir, pos);
            float freq = waveFrequency * (0.5f + 0.5f * Mathf.PerlinNoise(w * 10.1f, 0));
            float speed = waveSpeed * (0.5f + 0.5f * Mathf.PerlinNoise(0, w * 20.2f));
            float offset = w * 100f;
            sum += Mathf.Sin(phase * freq + Time.time * speed + offset);
        }
        float noise1 = Mathf.PerlinNoise(local.x * 0.15f, Time.time * 0.2f);
        float noise2 = Mathf.PerlinNoise(local.z * 0.12f, Time.time * 0.18f + 100f);
        float noise = (noise1 + noise2) * 0.5f;
        float finalY = (mainWave + 0.5f * mainWave2) * waveHeight * (0.7f + noise * 0.3f);
        finalY += 0.2f * sum / waveCount * waveHeight;
        finalY += (noise - 0.5f) * waveHeight * 0.3f;
        return transform.position.y + Mathf.Clamp(finalY, -maxWaveHeight, maxWaveHeight);
    }
}
