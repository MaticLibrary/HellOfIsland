using UnityEngine;

public class PowerCell : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f;

    private bool collected;

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.TryGetComponent(out Inventory inventory))
            return;

        collected = true;
        inventory.CellPickup();
        Destroy(gameObject);
    }
}
