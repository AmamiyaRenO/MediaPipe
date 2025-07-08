using UnityEngine;
using TMPro;

public enum GameState
{
    Waiting,
    Playing,
    Success,
    Failed
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("依赖组件")]
    public BoatController boat;
    public TextMeshProUGUI statusText;
    public GameObject uiSuccess;
    public GameObject uiFail;
    public GameObject uiStartPrompt;

    [Header("游戏参数")]
    public float successDuration = 5f;
    public float allowedTilt = 5f;

    private GameState currentState = GameState.Waiting;
    private float successTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowStartPrompt();
    }

    void Update()
    {
        if (currentState != GameState.Playing) return;

        float tilt = boat.GetCurrentTilt();

        if (Mathf.Abs(tilt) < allowedTilt)
        {
            successTimer += Time.deltaTime;
            statusText.text = $"🌊 平衡中：{successTimer:F1}s / {successDuration}s";

            if (successTimer >= successDuration)
            {
                GameSuccess();
            }
        }
        else
        {
            successTimer = 0f;
            statusText.text = $"⚠️ 倾斜过大，请调整姿势！";
        }
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        successTimer = 0f;

        statusText.text = "挑战开始：抵抗风浪保持平衡！";

        uiSuccess.SetActive(false);
        uiFail.SetActive(false);
        uiStartPrompt.SetActive(false);
    }

    public void GameSuccess()
    {
        currentState = GameState.Success;
        uiSuccess.SetActive(true);
        statusText.text = "✅ 成功抵抗风浪，平衡达成！";
    }

    public void GameFail()
    {
        currentState = GameState.Failed;
        uiFail.SetActive(true);
        statusText.text = "💥 船翻了！再来一次吧～";
    }

    public void RestartGame()
    {
        StartGame();
    }

    private void ShowStartPrompt()
    {
        currentState = GameState.Waiting;
        statusText.text = "点击开始挑战 🌊";
        uiStartPrompt.SetActive(true);
        uiSuccess.SetActive(false);
        uiFail.SetActive(false);
    }
}
