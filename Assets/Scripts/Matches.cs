using UnityEngine;

public class Matches : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Inventory>() is Inventory inventory)
        {
            inventory.MatchPickup();
            Destroy(gameObject);
        }
    }
}
