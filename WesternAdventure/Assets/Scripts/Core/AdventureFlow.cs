using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using WesternAdventure.Companion;
using WesternAdventure.NPC;

namespace WesternAdventure.Core
{
    [DefaultExecutionOrder(-500)]
    public class AdventureFlow : MonoBehaviour
    {
        public Transform player;
        public CompanionGuide companion;
        public Transform chest;
        public Transform chestLid;
        public BossHealth boss;
        public Camera viewCamera;
        public Transform menuView;
        public GuardEncounter returnGuard;
        public Transform returnGate;
        public EnemyHealth[] approachEnemies;
        public int DefeatedApproachEnemies
        {
            get
            {
                int count = 0;
                if (approachEnemies != null)
                    foreach (var enemy in approachEnemies) if (enemy != null && enemy.IsDead) count++;
                return count;
            }
        }
        public bool CanSendCompanion => approachEnemies != null && approachEnemies.Length == 4 && DefeatedApproachEnemies == 4;
        public bool TreasureCollected { get; private set; }
        public bool ReturnedThroughGate { get; private set; }
        public bool CanMeetGuard => TreasureCollected && ReturnedThroughGate && screen == Screen.Playing;
        private enum Screen { Title, Intro, Playing, Pause, Won, Lost }
        private Screen screen;
        private GameObject overlay;
        private RectTransform panel;
        private Text hint, objective;
        private Font font;
        private int introIndex;
        private bool opened;
        private float previousPlayerX;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private ThirdPersonCamera cameraController;
        private PlayerHealth health;
        private readonly string[] speakers = { "KOVBOY", "KOVBOY", "DOSTUM", "KOVBOY", "KOVBOY" };
        private readonly string[] lines = {
            "Harita bizi bu kanyona getirdi. Hazineye giden yolu birlikte bulacağız.",
            "Sanırım hazineyi bizden önce almaya gelenler olmuş. Önce şu dört kişiyi yenmemiz gerekecek. Ortalık güvenli olmadan seni kola gönderemem, dostum.",
            "Hav! Hav!\n(Köpek eski madenin girişine doğru bakar.)",
            "Şu alçak geçitten yalnızca sen sığarsın. Yanına geldiğimizde E ile sana işaret edeceğim. İçerideki kolu indir; ben dışarıda beklerim.",
            "Kapının ardında tehlike var. Golem yorulduğunda yaklaşır, yumruk atarım. Hazırsan yola çıkalım, dostum." };

        private void Awake()
        {
            previousPlayerX = player.position.x;
            movement = player.GetComponent<PlayerMovement>();
            combat = player.GetComponent<PlayerCombat>();
            health = player.GetComponent<PlayerHealth>();
            cameraController = viewCamera.GetComponent<ThirdPersonCamera>();
            if (cameraController != null) cameraController.externalCursorControl = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (returnGuard != null) returnGuard.gameObject.SetActive(false);
            BuildCanvas();
            ShowTitle();
        }
        private void Start() { ApplyState(); }
        private void LateUpdate()
        {
            if (boss != null && boss.IsDead && returnGuard != null && !returnGuard.gameObject.activeSelf)
                returnGuard.gameObject.SetActive(true);
        }
        private void OnDestroy() { Time.timeScale = 1; }

        private void BuildCanvas()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var root = new GameObject("Adventure UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            objective = Label(root.transform, "Görev", new Vector2(1050, 64), new Vector2(0, 300), 30, new Color(1, .85f, .55f));
            hint = Label(root.transform, "", new Vector2(1050, 84), new Vector2(0, -280), 30, Color.white);
            objective.rectTransform.anchorMin = objective.rectTransform.anchorMax = new Vector2(.5f, 1);
            objective.rectTransform.anchoredPosition = new Vector2(0, -60);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(.5f, 0);
            hint.rectTransform.anchoredPosition = new Vector2(0, 75);
            overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(root.transform, false);
            var rect = (RectTransform)overlay.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(.035f, .045f, .045f, .90f);
            panel = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); panel.SetParent(overlay.transform, false); panel.sizeDelta = new Vector2(1200, 680);
        }
        private Text Label(Transform parent, string text, Vector2 size, Vector2 position, int fontSize, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.rectTransform.sizeDelta = size; label.rectTransform.anchoredPosition = position;
            var shadow = go.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.9f); shadow.effectDistance = new Vector2(1,-2);
            return label;
        }
        private void Button(string text, float y, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(panel, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(360, 62); rect.anchoredPosition = new Vector2(0, y);
            go.GetComponent<Image>().color = new Color(.64f, .37f, .14f);
            var button = go.GetComponent<Button>(); var colors = button.colors; colors.highlightedColor = new Color(1, .84f, .58f); button.colors = colors;
            button.onClick.AddListener(action);
            Label(go.transform, text, new Vector2(340, 58), Vector2.zero, 30, Color.white);
        }
        private void ClearPanel()
        {
            foreach (Transform child in panel) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.SetActive(true);
            hint.text = "";
        }
        private void Heading(string small, string title, string body)
        {
            Label(panel, small, new Vector2(1100, 40), new Vector2(0, 280), 26, new Color(.89f, .64f, .32f));
            Label(panel, title, new Vector2(1150, 160), new Vector2(0, 175), 62, new Color(1, .92f, .75f));
            Label(panel, body, new Vector2(1060, 175), new Vector2(0, 30), 32, new Color(.85f, .84f, .78f));
        }
        public void ShowTitle()
        {
            screen = Screen.Title; ClearPanel();
            Heading("BİR KANYON • İKİ DOST • BİR SIR", "BATININ İZİNDE", "Yolu yalnız yürümeyeceksin.\nDostuna güven, geçidi aç ve kayıp hazineyi bul.");
            Button("MACERAYA BAŞLA", -110, () => { introIndex = 0; ShowIntro(); });
            Button("ÇIKIŞ", -190, () => Application.Quit());
            Label(panel, "WASD  Hareket     SHIFT  Koş     SPACE  Zıpla     SOL TIK  Yumruk\nE  Etkileşim     ESC  Duraklat", new Vector2(1150, 90), new Vector2(0, -300), 25, new Color(.65f, .65f, .59f));
            ApplyState();
        }
        private void ShowIntro()
        {
            screen = Screen.Intro; ClearPanel();
            Heading($"YOLUN BAŞINDA   /   {introIndex + 1} – {lines.Length}", speakers[introIndex], lines[introIndex]);
            Button(introIndex == lines.Length - 1 ? "YOLA ÇIK" : "DEVAM ET", -150, NextIntro);
            Button("KONUŞMAYI ATLA", -230, Resume);
            ApplyState();
        }
        private void NextIntro() { if (++introIndex >= lines.Length) Resume(); else ShowIntro(); }
        private void Resume() { screen = Screen.Playing; overlay.SetActive(false); ApplyState(); }
        private void ApplyState()
        {
            bool playing = screen == Screen.Playing;
            Time.timeScale = playing ? 1 : 0;
            movement.enabled = combat.enabled = playing;
            if (cameraController != null) cameraController.enabled = playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !playing;
            if (screen == Screen.Title && menuView != null) viewCamera.transform.SetPositionAndRotation(menuView.position, menuView.rotation);
            objective.enabled = playing;
        }
        private void Pause()
        {
            screen = Screen.Pause; ClearPanel(); Heading("KISA BİR MOLA", "YOL SENİ BEKLER", "Hazır olduğunda dostunla birlikte devam et.");
            Button("DEVAM ET", -110, Resume); Button("BAŞTAN BAŞLA", -190, Restart); Button("ÇIKIŞ", -270, () => Application.Quit()); ApplyState();
        }
        private void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        private void Finish(bool won)
        {
            screen = won ? Screen.Won : Screen.Lost; ClearPanel();
            Heading(won ? "MACERA TAMAMLANDI" : "YOLCULUK BİTMEDİ", won ? "BİRLİKTE BAŞARDINIZ" : "BİR ŞANS DAHA", won ? "Hazineyi aldınız, geçitten geri döndünüz ve muhafızı ikna ettiniz.\nDostunla birlikte artık eve dönebilirsin." : "Dostun seni bekliyor. Yeniden deneyebilirsin.");
            Button("YENİDEN OYNA", -140, Restart); Button("ÇIKIŞ", -220, () => Application.Quit()); ApplyState();
        }
        private void Update()
        {
            var keys = Keyboard.current;
            if (screen == Screen.Intro && keys != null && keys.enterKey.wasPressedThisFrame) { NextIntro(); return; }
            if (screen == Screen.Pause && keys != null && keys.escapeKey.wasPressedThisFrame) { Resume(); return; }
            if (screen != Screen.Playing) return;
            if (returnGuard != null && returnGuard.IsConversationOpen) { hint.text = ""; return; }
            if (health.IsDead) { Finish(false); return; }
            if (returnGuard != null && returnGuard.Passed) { Finish(true); return; }
            if (TreasureCollected && returnGate != null && previousPlayerX >= returnGate.position.x - 3f &&
                player.position.x < returnGate.position.x - 3f && Mathf.Abs(player.position.z - returnGate.position.z) < 5f)
                ReturnedThroughGate = true;
            previousPlayerX = player.position.x;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) { Pause(); return; }
            bool bossDefeated = boss != null && boss.IsDead;
            objective.text = !CanSendCompanion ? $"01  /  Rakipleri yen • {DefeatedApproachEnemies}/4" : TreasureCollected ? "04  /  Geçitten geri dön • Muhafızla konuş" : !companion.LeverOpened ? "01  /  Dostunla eski maden koluna ulaş" : !bossDefeated ? "02  /  Golemi yen • Dinlenirken saldır" : "03  /  Hazine sandığını aç";
            hint.text = "";
            if (!companion.LeverOpened && Vector3.Distance(player.position, companion.entrance.position) < 4)
            {
                if (!CanSendCompanion)
                {
                    hint.text = $"Önce dört düşmanı yenmelisin. Kalan: {4 - DefeatedApproachEnemies}";
                    return;
                }
                hint.text = companion.IsBusy ? "Dostun kola gidiyor…" : Vector3.Distance(companion.transform.position, companion.entrance.position) >= 8 ? "Dostunun yanına gelmesini bekle…" : "[E]  Dostunu alçak geçitten kola gönder";
                if (keys != null && keys.eKey.wasPressedThisFrame && Vector3.Distance(companion.transform.position, companion.entrance.position) < 8)
                    companion.CommandClearObstacle(companion.leverTarget);
            }
            if (chest != null && Vector3.Distance(player.position, chest.position) < 3)
            {
                hint.text = TreasureCollected ? "Hazine sende. Açtığın geçitten geri dön." : bossDefeated ? "[E]  Hazineyi aç" : "Hazineyi golem koruyor.";
                if (bossDefeated && !opened && keys != null && keys.eKey.wasPressedThisFrame)
                { opened = true; StartCoroutine(OpenTreasure()); }
            }
        }
        private System.Collections.IEnumerator OpenTreasure()
        {
            float t = 0;
            while (t < 1) { t += Time.deltaTime; if (chestLid != null) chestLid.localRotation = Quaternion.Euler(-105 * Mathf.SmoothStep(0, 1, t), 0, 0); yield return null; }
            opened = true;
            TreasureCollected = true;
        }
    }
}
