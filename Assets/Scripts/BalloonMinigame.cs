using UnityEngine;

public class BalloonMinigame : MonoBehaviour
{
    [SerializeField] private int targetHits = 10;
    [SerializeField] private TextHints textHints;
    [SerializeField] private BalloonTarget[] balloons;

    private int hits;
    private int currentTarget = -1;
    private bool completed;
    private bool gameStarted;

    private void Awake()
    {
        if (balloons == null || balloons.Length == 0)
        {
            Debug.LogError("Balloon minigame needs balloon targets assigned.", this);
            return;
        }

        for (int i = 0; i < balloons.Length; i++)
        {
            if (balloons[i] == null)
            {
                Debug.LogError("Balloon minigame has an unassigned balloon target.", this);
                continue;
            }

            balloons[i].gameObject.SetActive(false);
            balloons[i].SetTarget(false);
        }
    }

    public bool BeginGame()
    {
        if (completed || balloons == null || balloons.Length == 0)
            return false;

        ResolveTextHints();
        gameStarted = true;
        if (currentTarget < 0)
            ChooseNextTarget();
        else
        {
            balloons[currentTarget].gameObject.SetActive(true);
            balloons[currentTarget].SetTarget(true);
        }

        ShowProgressHint();
        return true;
    }

    public void EndGame()
    {
        gameStarted = false;
        if (currentTarget >= 0)
        {
            balloons[currentTarget].SetTarget(false);
            balloons[currentTarget].gameObject.SetActive(false);
        }

        ResolveTextHints();
        textHints?.HideHint();
    }

    public void RegisterHit(int balloonIndex)
    {
        if (!gameStarted || completed || balloonIndex != currentTarget)
            return;

        hits++;
        BalloonTarget hitBalloon = balloons[balloonIndex];
        hitBalloon.SetTarget(false);

        if (hits >= targetHits)
        {
            CompleteMinigame();
            return;
        }

        hitBalloon.gameObject.SetActive(false);
        ChooseNextTarget();
        ShowProgressHint();
    }

    private void ChooseNextTarget()
    {
        if (balloons == null || balloons.Length == 0)
            return;

        int next = Random.Range(0, balloons.Length);
        if (balloons.Length > 1 && next == currentTarget)
            next = (next + Random.Range(1, balloons.Length)) % balloons.Length;

        currentTarget = next;
        balloons[currentTarget].gameObject.SetActive(true);
        balloons[currentTarget].SetTarget(true);
    }

    private void CompleteMinigame()
    {
        completed = true;

        for (int i = 0; i < balloons.Length; i++)
        {
            if (balloons[i] == null)
                continue;

            balloons[i].SetTarget(false);
            balloons[i].gameObject.SetActive(false);
        }

        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Balloon minigame could not find the player Inventory.");
            return;
        }

        inventory.CellPickup();
        ResolveTextHints();
        textHints?.ShowHint("CEL OSIAGNIETY: +1 BATERIA (10/10)");

        RewardDisplay reward = transform.parent != null
            ? transform.parent.GetComponentInChildren<RewardDisplay>(true)
            : null;
        if (reward != null)
            reward.Show();
    }

    private void ShowProgressHint()
    {
        ResolveTextHints();
        textHints?.ShowPersistentHint("BALONIKI: " + hits + "/" + targetHits);
    }

    private void ResolveTextHints()
    {
        if (textHints == null)
            textHints = FindFirstObjectByType<TextHints>();
    }
}
