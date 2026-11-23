using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [SerializeField] private AudioClip collectSound;
    [Header("Charge indicators")]
    [SerializeField] private Texture2D[] hudCharge;
    [SerializeField] private RawImage chargeHud;
    [SerializeField] private Texture2D[] meterCharge;
    [SerializeField] private Renderer meter;

    [Header("Matches")]
    [SerializeField] private RawImage matchHud;
    [SerializeField] private TextHints textHints;
    [SerializeField, TextArea] private string needMatchesHint = "Czym by tu rozpalić ogień...";
    [SerializeField] private float campfireRange = 2.2f;

    private int charge;
    private bool haveMatches;

    public int Charge => charge;
    public Color ChargeIndicatorColor => GetChargeIndicatorColor(charge);
    public RawImage MatchHud { get => matchHud; set => matchHud = value; }
    public TextHints HintDisplay { get => textHints; set => textHints = value; }

    public static Color GetChargeIndicatorColor(int chargeLevel)
    {
        switch (Mathf.Clamp(chargeLevel, 0, 5))
        {
            case 0: return new Color(1f, 0.12f, 0.12f);
            case 1: return new Color(1f, 0.38f, 0.05f);
            case 2: return new Color(1f, 0.82f, 0.08f);
            case 3: return new Color(0.55f, 0.9f, 0.12f);
            default: return new Color(0.12f, 1f, 0.28f);
        }
    }

    private void Awake()
    {
        EnsureHudSetup();
        RefreshHudTexture();

        if (textHints == null)
        {
            GameObject hintObject = GameObject.Find("TextHintGUI");
            if (hintObject != null)
                textHints = hintObject.GetComponent<TextHints>();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            return;

        TryLightNearbyCampfire();
    }

    public void CellPickup()
    {
        charge++;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        HudOn();
        RefreshHudTexture();

        if (meter != null && meterCharge != null && meterCharge.Length > 0)
        {
            int level = Mathf.Min(charge, meterCharge.Length - 1);
            if (meter.material != null)
                meter.material.mainTexture = meterCharge[level];
        }
    }

    public void MatchPickup()
    {
        haveMatches = true;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (matchHud != null)
            matchHud.enabled = true;
    }

    public void HudOn()
    {
        if (chargeHud != null)
            chargeHud.enabled = true;
    }

    public void HudOff()
    {
        if (chargeHud != null)
            chargeHud.enabled = false;
    }

    private void TryLightNearbyCampfire()
    {
        if (!haveMatches)
        {
            if (textHints != null)
                textHints.ShowHint(needMatchesHint);
            return;
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, campfireRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Campfire campfire = null;

        foreach (Collider collider in nearby)
        {
            campfire = collider.GetComponentInParent<Campfire>();
            if (campfire != null)
                break;
        }

        if (campfire == null || campfire.IsLit)
            return;

        campfire.LightFire();
        haveMatches = false;

        if (matchHud != null)
            matchHud.enabled = false;
    }

    private void EnsureHudSetup()
    {
        if (chargeHud != null)
            return;

        GameObject canvasObject = new GameObject("PowerHUDCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

        GameObject hudObject = new GameObject("PowerGUI");
        hudObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = hudObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(328f, 98f);
        rect.anchoredPosition = new Vector2(40f, 40f);

        RawImage image = hudObject.AddComponent<RawImage>();
        image.enabled = true;
        image.color = Color.white;
        chargeHud = image;
    }

    private void RefreshHudTexture()
    {
        if (chargeHud == null)
            return;

        if (hudCharge != null && hudCharge.Length > 0)
        {
            int level = Mathf.Min(charge, hudCharge.Length - 1);
            chargeHud.texture = hudCharge[level];
            return;
        }

        chargeHud.texture = CreateBatteryTexture(Mathf.Clamp(charge, 0, 5));
    }

    private Texture2D CreateBatteryTexture(int level)
    {
        int width = 328;
        int height = 98;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color dark = new Color(0.08f, 0.08f, 0.08f, 1f);
        Color body = new Color(0.18f, 0.18f, 0.18f, 1f);
        Color border = new Color(0.4f, 0.4f, 0.4f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            }
        }

        int bodyX = 12;
        int bodyY = 18;
        int bodyW = 260;
        int bodyH = 62;
        int terminalW = 18;

        for (int y = bodyY; y < bodyY + bodyH; y++)
        {
            for (int x = bodyX; x < bodyX + bodyW; x++)
            {
                if (x >= bodyX + 2 && x < bodyX + bodyW - 2 && y >= bodyY + 2 && y < bodyY + bodyH - 2)
                    texture.SetPixel(x, y, body);
                else
                    texture.SetPixel(x, y, border);
            }
        }

        for (int y = bodyY + 10; y < bodyY + bodyH - 10; y++)
        {
            for (int x = bodyX + 14; x < bodyX + bodyW - 18; x++)
            {
                texture.SetPixel(x, y, dark);
            }
        }

        int fillWidth = Mathf.RoundToInt(((bodyW - 28) * level) / 5f);
        Color fill = GetChargeIndicatorColor(level);
        for (int y = bodyY + 12; y < bodyY + bodyH - 12; y++)
        {
            for (int x = bodyX + 16; x < bodyX + 16 + fillWidth; x++)
            {
                texture.SetPixel(x, y, fill);
            }
        }

        for (int y = bodyY + 16; y < bodyY + bodyH - 16; y++)
        {
            for (int x = bodyX + bodyW; x < bodyX + bodyW + terminalW; x++)
                texture.SetPixel(x, y, border);
        }

        for (int y = bodyY + 24; y < bodyY + bodyH - 24; y++)
        {
            for (int x = bodyX + bodyW + 3; x < bodyX + bodyW + terminalW - 3; x++)
                texture.SetPixel(x, y, dark);
        }

        texture.Apply();
        return texture;
    }
}
