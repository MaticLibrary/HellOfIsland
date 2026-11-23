using UnityEngine;

public class ThrowTrigger : MonoBehaviour
{
    [SerializeField] private BalloonMinigame minigame;

    private CoconutThrower activeThrower;

    private void OnTriggerEnter(Collider other)
    {
        CoconutThrower thrower = other.GetComponentInChildren<CoconutThrower>();
        if (thrower == null || activeThrower == thrower)
            return;

        BalloonMinigame game = minigame;
        if (game == null && transform.parent != null)
            game = transform.parent.GetComponentInChildren<BalloonMinigame>(true);

        if (game == null)
        {
            Debug.LogError("Assign the balloon minigame to the throwing mat.", this);
            return;
        }

        minigame = game;
        if (!game.BeginGame())
            return;

        activeThrower = thrower;
        thrower.CanThrow = true;
    }

    private void OnTriggerExit(Collider other)
    {
        CoconutThrower thrower = other.GetComponentInChildren<CoconutThrower>();
        if (thrower == null || activeThrower != thrower)
            return;

        activeThrower = null;
        thrower.CanThrow = false;
        minigame.EndGame();
    }
}
