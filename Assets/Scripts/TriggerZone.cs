using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    private const int CellsRequired = 5;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorAudio;
    [SerializeField] private AudioClip doorOpenSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || !other.TryGetComponent(out Inventory inventory)
            || inventory.Charge < CellsRequired)
            return;

        if (doorAnimator == null)
        {
            Debug.LogError("Assign the outpost Animator to TriggerZone.", this);
            return;
        }

        if (doorAnimator.GetBool("DoorOpen"))
            return;

        doorAnimator.SetBool("DoorOpen", true);

        if (doorAudio == null || doorOpenSound == null)
        {
            Debug.LogError("Assign the door AudioSource and opening sound to TriggerZone.", this);
            return;
        }

        doorAudio.PlayOneShot(doorOpenSound);
    }
}
