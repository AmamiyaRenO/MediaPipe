using UnityEngine;

public class CargoDropDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            Debug.Log("💧 Cargo fell into the water!");
            Destroy(gameObject);
        }
    }
}
