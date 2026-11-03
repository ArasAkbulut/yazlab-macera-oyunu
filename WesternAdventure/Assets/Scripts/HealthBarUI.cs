using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("Düþman barý")]
    [SerializeField] private float enemyBarHeight = 1.5f;

    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;

    private Canvas barCanvas;
    private RectTransform fillRect;
    private Image fillImage;
    private Camera viewCamera;

    private bool isPlayer;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (playerHealth == null && enemyHealth == null)
        {
            Debug.LogWarning(
                "HealthBarUI, PlayerHealth veya EnemyHealth bulunan ana nesneye eklenmeli.",
                this
            );

            enabled = false;
            return;
        }

        isPlayer = playerHealth != null;
        viewCamera = Camera.main;

        CreateBar();
        RefreshBar();
    }

    private void CreateBar()
    {
        GameObject canvasObject = new GameObject(
            name + "_HealthCanvas",
            typeof(RectTransform),
            typeof(Canvas)
        );

        canvasObject.layer = LayerMask.NameToLayer("UI");
        barCanvas = canvasObject.GetComponent<Canvas>();

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();

        if (isPlayer)
        {
            // Ekranýn sol üstünde sabit oyuncu barý.
            barCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            barCanvas.sortingOrder = 10;

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        else
        {
            // Düþmanýn baþýnýn üzerinde duran bar.
            barCanvas.renderMode = RenderMode.WorldSpace;

            canvasRect.SetParent(transform, false);
            canvasRect.localPosition =
                new Vector3(0f, enemyBarHeight, 0f);

            canvasRect.sizeDelta = new Vector2(120f, 16f);
            canvasRect.localScale = Vector3.one * 0.01f;
        }

        Image background = CreateImage(
            "Background",
            canvasRect,
            new Color(0.08f, 0.06f, 0.05f, 0.95f)
        );

        RectTransform backgroundRect = background.rectTransform;

        if (isPlayer)
        {
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = new Vector2(30f, -30f);
            backgroundRect.sizeDelta = new Vector2(300f, 28f);
        }
        else
        {
            Stretch(backgroundRect);
        }

        GameObject innerObject = new GameObject(
            "Inner",
            typeof(RectTransform)
        );

        innerObject.layer = canvasObject.layer;

        RectTransform innerRect =
            innerObject.GetComponent<RectTransform>();

        innerRect.SetParent(backgroundRect, false);
        Stretch(innerRect);

        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        Color barColor = isPlayer
            ? new Color(0.2f, 0.85f, 0.3f)
            : new Color(0.9f, 0.15f, 0.1f);

        fillImage = CreateImage("Fill", innerRect, barColor);
        fillRect = fillImage.rectTransform;
        Stretch(fillRect);
    }

    private Image CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        imageObject.layer = LayerMask.NameToLayer("UI");
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (barCanvas == null)
        {
            return;
        }

        RefreshBar();

        if (!isPlayer && barCanvas.enabled)
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera != null)
            {
                // Düþman dönse de bar kameraya dönük kalsýn.
                barCanvas.transform.rotation =
                    viewCamera.transform.rotation;
            }
        }
    }

    private void RefreshBar()
    {
        int current = isPlayer
            ? playerHealth.CurrentHealth
            : enemyHealth.CurrentHealth;

        int maximum = isPlayer
            ? playerHealth.MaxHealth
            : enemyHealth.MaxHealth;

        float ratio = maximum > 0
            ? Mathf.Clamp01((float)current / maximum)
            : 0f;

        // Barý soldan saða doldur.
        fillRect.anchorMax = new Vector2(ratio, 1f);
        fillImage.enabled = ratio > 0f;

        // Düþman ölünce barý gizle.
        // Oyuncu ölünce boþ bar ekranda kalsýn.
        barCanvas.enabled = isPlayer || !enemyHealth.IsDead;
    }

    private void OnDisable()
    {
        if (barCanvas != null)
        {
            barCanvas.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (barCanvas != null)
        {
            Destroy(barCanvas.gameObject);
        }
    }
}