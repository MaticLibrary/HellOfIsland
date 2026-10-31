using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private AudioClip collectSound;

    private int charge;

    public int Charge => charge;

    public void CellPickup()
    {
        charge++;

        if (collectSound == null)
        {
            Debug.LogError("Assign a collection sound to Inventory.", this);
            return;
        }

        AudioSource.PlayClipAtPoint(collectSound, transform.position);
    }
}
