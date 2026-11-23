using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerCollisions : MonoBehaviour
{
    private const int CellsRequired = 5;

    [SerializeField] private Transform playerCamera;
    [SerializeField] private float reach = 3f;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorLockedSound;
    [SerializeField] private Light doorLight;
    [SerializeField] private TextHints textHints;

    private Animator targetedDoorAnimator;
    private AudioSource targetedDoorAudio;
    private Inventory inventory;
    private Animator doorAnimator;
    private Renderer doorStatusLens;
    private Material doorStatusMaterial;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        if (inventory == null)
            Debug.LogError("The player needs an Inventory component to open powered doors.", this);

        if (textHints == null)
            TryCreateHintText();

        TryFindDoorLight();
    }

    private void Update()
    {
        targetedDoorAnimator = null;
        targetedDoorAudio = null;

        if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null)
        {
            UpdateDoorLight();
            return;
        }

        if (!Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || !hit.collider.CompareTag("playerDoor"))
        {
            UpdateDoorLight();
            return;
        }

        targetedDoorAnimator = hit.collider.GetComponentInParent<Animator>();
        targetedDoorAudio = hit.collider.GetComponent<AudioSource>();

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            UpdateDoorLight();
            return;
        }

        if (targetedDoorAnimator == null)
        {
            Debug.LogError("The targeted door needs a parent Animator.", hit.collider);
            return;
        }

        bool open = !targetedDoorAnimator.GetBool("DoorOpen");
        if (open && (inventory == null || inventory.Charge < CellsRequired))
        {
            int charge = inventory == null ? 0 : inventory.Charge;

            if (charge == 0)
                ShowDoorBlockedHint("DOOR LOCKED - NEED " + CellsRequired + " POWER CELLS");
            else
                ShowDoorBlockedHint("DOOR LOCKED - NEED " + (CellsRequired - charge) + " MORE POWER CELL(S)");

            if (doorLockedSound != null)
                AudioSource.PlayClipAtPoint(doorLockedSound, hit.point);

            UpdateDoorLight();
            return;
        }

        targetedDoorAnimator.SetBool("DoorOpen", open);

        if (!open)
        {
            UpdateDoorLight();
            return;
        }

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
        UpdateDoorLight();

        if (inventory != null && inventory.Charge >= CellsRequired)
            inventory.HudOff();
    }

    private void TryCreateHintText()
    {
        GameObject existing = GameObject.Find("TextHintGUI");
        if (existing != null)
        {
            textHints = existing.GetComponent<TextHints>();
            return;
        }

        GameObject canvasObject = new GameObject("TextHintGUI");
        canvasObject.transform.SetParent(transform.root, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();

        TextMeshProUGUI tmp = canvasObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tmp.rectTransform.sizeDelta = new Vector2(1400f, 200f);
        tmp.rectTransform.anchoredPosition = new Vector2(0f, -200f);
        textHints = canvasObject.AddComponent<TextHints>();
    }

    private void TryFindDoorLight()
    {
        GameObject target = GameObject.FindGameObjectWithTag("playerDoor");
        if (target == null)
            return;

        doorAnimator = target.GetComponentInParent<Animator>();
        Transform mount = doorAnimator != null ? doorAnimator.transform : target.transform.parent;
        if (mount == null)
            mount = target.transform;

        GameObject torchObject = GameObject.Find("Door Status Torch");
        if (torchObject == null)
        {
            torchObject = new GameObject("Door Status Torch");
            torchObject.transform.SetParent(mount, false);
            CreateTorchFixture(torchObject.transform);
        }
        else
        {
            torchObject.transform.SetParent(mount, false);
        }

        torchObject.transform.localPosition = new Vector3(0f, 2.78f, 2.02f);
        doorLight = torchObject.GetComponent<Light>();
        if (doorLight == null)
            doorLight = torchObject.AddComponent<Light>();

        Transform lens = torchObject.transform.Find("Status Lens");
        if (lens == null)
        {
            lens = torchObject.transform.Find("Torch Flame");
            if (lens != null)
                lens.name = "Status Lens";
        }
        if (lens != null)
            doorStatusLens = lens.GetComponent<Renderer>();
        else
            CreateTorchFixture(torchObject.transform);

        if (doorStatusLens != null)
            doorStatusMaterial = doorStatusLens.material;

        doorLight.type = LightType.Point;
        doorLight.range = 4f;
        doorLight.intensity = 5f;
        doorLight.shadows = LightShadows.None;
        UpdateDoorLight();
    }

    private void CreateTorchFixture(Transform torchTransform)
    {
        GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backing.name = "Torch Mount";
        backing.transform.SetParent(torchTransform, false);
        backing.transform.localPosition = new Vector3(0f, -0.08f, -0.08f);
        backing.transform.localScale = new Vector3(0.28f, 0.4f, 0.12f);
        Destroy(backing.GetComponent<Collider>());

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "Status Lens";
        lens.transform.SetParent(torchTransform, false);
        lens.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        lens.transform.localScale = new Vector3(0.22f, 0.22f, 0.16f);
        Destroy(lens.GetComponent<Collider>());

        doorStatusLens = lens.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("Could not find a supported shader for the door status torch.", this);
            return;
        }

        doorStatusMaterial = new Material(shader);
        doorStatusMaterial.EnableKeyword("_EMISSION");
        doorStatusLens.material = doorStatusMaterial;
    }

    private void ShowDoorBlockedHint(string message)
    {
        if (textHints != null)
            textHints.ShowHint(message);
    }

    private void UpdateDoorLight()
    {
        if (doorLight == null)
            return;

        Color statusColor = inventory != null
            ? inventory.ChargeIndicatorColor
            : Inventory.GetChargeIndicatorColor(0);
        doorLight.color = statusColor;
        doorLight.enabled = true;

        if (doorStatusMaterial == null && doorStatusLens != null)
            doorStatusMaterial = doorStatusLens.material;
        if (doorStatusMaterial == null)
            return;

        if (doorStatusMaterial.HasProperty("_BaseColor"))
            doorStatusMaterial.SetColor("_BaseColor", statusColor);
        if (doorStatusMaterial.HasProperty("_Color"))
            doorStatusMaterial.SetColor("_Color", statusColor);
        if (doorStatusMaterial.HasProperty("_EmissionColor"))
            doorStatusMaterial.SetColor("_EmissionColor", statusColor * 3f);
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
            action + " (" + (inventory == null ? 0 : inventory.Charge) + "/" + CellsRequired + ")");
    }
}
