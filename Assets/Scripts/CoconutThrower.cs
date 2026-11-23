using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class CoconutThrower : MonoBehaviour
{
    [SerializeField] private Rigidbody coconutPrefab;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private float throwSpeed = 30f;
    [SerializeField] private float throwCooldown = 0.25f;

    private InputAction attackAction;
    private AudioSource audioSource;
    private float nextThrowTime;

    public bool CanThrow { get; set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        attackAction = InputSystem.actions.FindAction("Player/Attack");
    }

    private void OnEnable()
    {
        attackAction?.Enable();
    }

    private void OnDisable()
    {
        attackAction?.Disable();
    }

    private void Update()
    {
        if (!CanThrow || coconutPrefab == null || attackAction == null
            || !attackAction.WasPressedThisFrame() || Time.time < nextThrowTime)
            return;

        nextThrowTime = Time.time + throwCooldown;

        Rigidbody coconut = Instantiate(coconutPrefab, transform.position, transform.rotation);
        coconut.name = "Coconut";
        coconut.gameObject.tag = "coconut";
        coconut.linearVelocity = transform.forward * throwSpeed;

        if (throwSound != null)
            audioSource.PlayOneShot(throwSound);
    }
}
