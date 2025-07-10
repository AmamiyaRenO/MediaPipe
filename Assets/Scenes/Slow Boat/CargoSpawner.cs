using UnityEngine;
using System.Collections.Generic;

public class CargoSpawner : MonoBehaviour
{
    [Header("货物设置")]
    public GameObject cargoPrefab;          // 拖入你的 Cargo Prefab
    public Transform cargoParent;           // 可选：用于组织层级（一般是 BoatModel 或专门的 CargoRoot 空物体）
    public int cargoCount = 3;              // 要生成几个
    public Vector3 startOffset = new Vector3(0, 0.5f, 0);  // 第一个货物相对位置
    public Vector3 spacing = new Vector3(0.5f, 0, 0);       // 每个货物之间的间距
    public Transform spawnPoint; // 拖入唯一的生成点

    void Start()
    {
        if (cargoPrefab == null)
        {
            Debug.LogError("❌ 没有设置 cargoPrefab！");
            return;
        }

        for (int i = 0; i < cargoCount; i++)
        {
            Vector3 localPos = startOffset + i * spacing;
            Vector3 worldPos = transform.TransformPoint(localPos); // 转换到世界坐标

            GameObject cargo = Instantiate(cargoPrefab, worldPos, Quaternion.identity);
            if (cargoParent != null)
            {
                cargo.transform.SetParent(cargoParent);
            }
        }

        Debug.Log($"📦 成功生成 {cargoCount} 个货物");
    }

    public void AddCargo()
    {
        GameObject cargo = Instantiate(cargoPrefab, spawnPoint.position, Quaternion.identity);
        if (cargoParent != null)
        {
            cargo.transform.SetParent(cargoParent);
        }
        Debug.Log("�� 通过语音添加了一个货物");
    }
}
