using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextHints : MonoBehaviour
{
    [SerializeField] private float displayTime = 4f;

    private TextMeshProUGUI hintText;
    private float timer;

    private void Awake()
    {
        hintText = GetComponent<TextMeshProUGUI>();
        hintText.enabled = false;
    }

    private void Update()
    {
        if (!hintText.enabled)
            return;

        timer += Time.deltaTime;
        if (timer >= displayTime)
        {
            hintText.enabled = false;
            timer = 0f;
        }
    }

    public void ShowHint(string message)
    {
        hintText.text = message;
        hintText.enabled = true;
        timer = 0f;
    }
}
