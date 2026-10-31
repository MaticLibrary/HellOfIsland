using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCollisions : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float reach = 3f;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private int cellsRequired = 4;

    private Animator targetedDoorAnimator;
    private AudioSource targetedDoorAudio;
    private Inventory inventory;
    private string doorFeedback;
    private float doorFeedbackUntil;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        if (inventory == null)
            Debug.LogError("The player needs an Inventory component to open powered doors.", this);
    }

    private void Update()
    {
        targetedDoorAnimator = null;
        targetedDoorAudio = null;

        if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null)
            return;

        if (!Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || !hit.collider.CompareTag("playerDoor"))
            return;

        targetedDoorAnimator = hit.collider.GetComponentInParent<Animator>();
        targetedDoorAudio = hit.collider.GetComponent<AudioSource>();

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (targetedDoorAnimator == null)
        {
            Debug.LogError("The targeted door needs a parent Animator.", hit.collider);
            return;
        }

        bool open = !targetedDoorAnimator.GetBool("DoorOpen");
        if (open && (inventory == null || inventory.Charge < cellsRequired))
        {
            int charge = inventory == null ? 0 : inventory.Charge;
            doorFeedback = "NEED " + cellsRequired + " POWER CELLS (" + charge + "/" + cellsRequired + ")";
            doorFeedbackUntil = Time.time + 2.5f;
            return;
        }

        targetedDoorAnimator.SetBool("DoorOpen", open);

        if (!open)
            return;

        if (doorOpenSound == null)
        {
            Debug.LogError("Assign a door opening sound to PlayerCollisions.", this);
            return;
        }

        if (targetedDoorAudio == null)
        {
            Debug.LogError("The targeted door needs an AudioSource.", hit.collider);
            return;
        }

        targetedDoorAudio.PlayOneShot(doorOpenSound);
    }

    private void OnGUI()
    {
        if (targetedDoorAnimator == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Rect prompt = new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.68f, 220f, 42f);
        GUI.Box(prompt, GUIContent.none);
        GUI.Box(new Rect(prompt.x + 6f, prompt.y + 5f, 32f, 32f), "E");

        string action = targetedDoorAnimator.GetBool("DoorOpen") ? "CLOSE DOOR" : "OPEN DOOR";
        GUI.Label(new Rect(prompt.x + 44f, prompt.y + 5f, 168f, 32f),
            action + " (" + (inventory == null ? 0 : inventory.Charge) + "/" + cellsRequired + ")");

        if (Time.time < doorFeedbackUntil)
        {
            Rect feedback = new Rect(Screen.width * 0.5f - 140f, prompt.y + 46f, 280f, 36f);
            GUI.Box(feedback, doorFeedback);
        }
    }
}
