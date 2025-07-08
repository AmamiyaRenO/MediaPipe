using UnityEngine;

public class CargoDropDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            Debug.Log("💧 货物落入水中！");
            Destroy(gameObject);
        }
    }

}
