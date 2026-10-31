using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float pitch;
    
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchHeight = 1.15f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingCameraLocalPosition;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        standingCameraLocalPosition = playerCamera.localPosition;
        playerActionMap = inputActions.FindActionMap("Player", true);
        moveAction = playerActionMap.FindAction("Move", true);
        lookAction = playerActionMap.FindAction("Look", true);
        jumpAction = playerActionMap.FindAction("Jump", true);
        sprintAction = playerActionMap.FindAction("Sprint", true);
    }

    private void OnEnable()
    {
        if (playerActionMap != null)
            playerActionMap.Enable();
    }

    private void OnDisable()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
        playerCamera.localEulerAngles = new Vector3(pitch, 0f, 0f);

        bool wantsCrouch = Keyboard.current != null && Keyboard.current.cKey.isPressed;
        if (!wantsCrouch && controller.height < standingHeight && !CanStandUp())
            wantsCrouch = true;

        float targetHeight = wantsCrouch ? Mathf.Max(crouchHeight, controller.radius * 2f) : standingHeight;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = standingCenter - Vector3.up * (standingHeight - controller.height) * 0.5f;
        Vector3 cameraPosition = standingCameraLocalPosition;
        cameraPosition.y -= (standingHeight - controller.height) * 0.5f;
        playerCamera.localPosition = cameraPosition;

        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        bool crouching = wantsCrouch || controller.height < standingHeight - 0.05f;
        float speed = crouching ? crouchSpeed : sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (!crouching && controller.isGrounded && jumpAction.WasPressedThisFrame())
            verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);

        verticalSpeed += gravity * Time.deltaTime;
        velocity.y = verticalSpeed;
        controller.Move(velocity * Time.deltaTime);
    }


    private bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 feet = transform.TransformPoint(standingCenter - Vector3.up * (standingHeight * 0.5f) + Vector3.up * controller.radius);
        Vector3 head = transform.TransformPoint(standingCenter + Vector3.up * (standingHeight * 0.5f - controller.radius));
        Collider[] overlaps = Physics.OverlapCapsule(feet, head, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap.transform != transform && !overlap.transform.IsChildOf(transform))
                return false;
        }

        return true;
    }
}
