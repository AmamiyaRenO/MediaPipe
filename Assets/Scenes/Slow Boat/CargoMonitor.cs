using UnityEngine;

public class CargoMonitor : MonoBehaviour
{
    public Transform cargo;        // 拖入你创建的 Cube
    public float fallThresholdY = -1f;
    private bool isLost = false;

    void Update()
    {
        if (!isLost && cargo.position.y < fallThresholdY)
        {
            isLost = true;
            Debug.LogWarning("📦 货物掉入海中！游戏失败！");
            // 可以在此添加 UI 提示或游戏状态切换
        }
    }
}
