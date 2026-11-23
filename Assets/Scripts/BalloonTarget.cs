using UnityEngine;

public class BalloonTarget : MonoBehaviour
{
    [SerializeField] private int balloonIndex;
    [SerializeField] private BalloonMinigame minigame;

    public void Configure(BalloonMinigame owner, int index)
    {
        minigame = owner;
        balloonIndex = index;
    }

    public void SetTarget(bool isTarget)
    {
        Collider balloonCollider = GetComponent<Collider>();
        if (balloonCollider != null)
            balloonCollider.enabled = isTarget;

        Renderer balloonRenderer = GetComponentInChildren<Renderer>();
        if (balloonRenderer == null)
            return;

        if (balloonRenderer.material.HasProperty("_EmissionColor"))
        {
            balloonRenderer.material.EnableKeyword("_EMISSION");
            balloonRenderer.material.SetColor("_EmissionColor", isTarget
                ? new Color(1f, 0.25f, 0.05f) * 2f
                : Color.black);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("coconut"))
            return;

        Destroy(collision.gameObject);
        minigame?.RegisterHit(balloonIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("coconut"))
            return;

        Destroy(other.gameObject);
        minigame?.RegisterHit(balloonIndex);
    }
}
