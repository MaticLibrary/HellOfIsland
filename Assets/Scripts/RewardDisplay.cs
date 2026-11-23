using UnityEngine;

public class RewardDisplay : MonoBehaviour
{
    [SerializeField] private float autoHideTime = 3f;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.localScale = Vector3.one;
        CancelInvoke();
        Invoke(nameof(Hide), autoHideTime);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
