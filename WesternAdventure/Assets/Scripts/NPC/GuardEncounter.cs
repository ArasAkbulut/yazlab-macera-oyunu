using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WesternAdventure.Core;
using WesternAdventure.UI;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// Hazinenin başındaki muhafızla karşılaşmayı yönetir:
    ///   1) Boss yaşarken muhafız bekler (konuşma açılmaz).
    ///   2) Boss yenildikten sonra oyuncu talkDistance kadar yaklaşınca diyalog paneli açılır,
    ///      muhafız "Hazineye neden layıksın?" diye sorar, oyuncunun kontrolleri kilitlenir.
    ///   3) LLM kararı: open_door -> muhafız kenara çekilir (hazine oyuncunun);
    ///      attack_player -> oyuncu ölür. Her iki durumda panel birkaç saniye sonra kapanır.
    /// Escape ile konuşmadan çıkılabilir; tekrar açmak için uzaklaşıp yeniden yaklaşmak gerekir.
    /// </summary>
    public class GuardEncounter : MonoBehaviour
    {
        private enum State { WaitingForBoss, Ready, Talking, Done }

        [Header("Bağlantılar")]
        [SerializeField] private NpcDialogueController dialogueController;
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private GameObject dialogueCanvas;
        [SerializeField] private InputField playerInput;
        [SerializeField] private Transform player;
        [Tooltip("Yenilince (obje deaktif/yok olunca) muhafız konuşmaya hazır olur. Boşsa baştan hazırdır.")]
        [SerializeField] private GameObject boss;
        [Tooltip("Konuşma sırasında kapatılacak oyuncu/kamera bileşenleri (hareket, saldırı, kamera).")]
        [SerializeField] private Behaviour[] disableWhileTalking;

        [Header("Ayarlar")]
        [SerializeField] private float talkDistance = 10f;
        [SerializeField] private string openingQuestion =
            "Dur! Golem'i yendin ama hazine öylece senin olmaz. Hazineye neden layıksın?";
        [SerializeField] private float closeDelay = 4f;
        [Tooltip("open_door kararında muhafızın çekileceği yerel ofset.")]
        [SerializeField] private Vector3 stepAsideOffset = new Vector3(0f, 0f, 3f);

        [Header("Durum (sadece izleme)")]
        [SerializeField] private State state = State.WaitingForBoss;

        public AdventureFlow adventure;
        public bool IsConversationOpen { get; private set; }
        public bool Passed { get; private set; }
        private bool _mustLeaveFirst;
        private PlayerHealth _playerHealth;

        private void Awake()
        {
            if (player != null) _playerHealth = player.GetComponent<PlayerHealth>();
            if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (Time.timeScale == 0 || (adventure != null && !adventure.CanMeetGuard)) return;
            switch (state)
            {
                case State.WaitingForBoss:
                    if (boss == null || !boss.activeInHierarchy)
                    {
                        state = State.Ready;
                        Debug.Log("[Muhafız] Boss yenildi, muhafız konuşmaya hazır.", this);
                    }
                    break;

                case State.Ready:
                    if (player == null) return;
                    float distance = HorizontalDistance(player.position, transform.position);
                    if (_mustLeaveFirst)
                    {
                        if (distance > talkDistance * 1.5f) _mustLeaveFirst = false;
                    }
                    else if (distance <= talkDistance && !IsPlayerDead())
                    {
                        StartConversation();
                    }
                    break;

                case State.Talking:
                    FacePlayer();
                    Keyboard keyboard = Keyboard.current;
                    if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    {
                        // Oyuncu konuşmadan vazgeçti; tekrar açmak için uzaklaşıp dönmeli.
                        CloseConversation();
                        _mustLeaveFirst = true;
                        state = State.Ready;
                    }
                    break;
            }
        }

        private void StartConversation()
        {
            IsConversationOpen = true;
            state = State.Talking;
            Subscribe();
            Debug.Log("[Muhafız] Konuşma başladı.", this);

            // Açılış sorusu geçmişe de ekleniyor ki LLM bunu sorduğunu bilsin, tekrar sormasın.
            dialogueController.ResetConversation(openingQuestion);
            if (dialogueCanvas != null) dialogueCanvas.SetActive(true);
            if (dialogueUI != null)
            {
                dialogueUI.ShowLine(
                    $"Muhafız: \"{openingQuestion}\"\n" +
                    $"<size=24>({dialogueController.MaxTurns} cevap hakkın var. Kutuya tıkla, yaz, Enter'a bas. Çıkmak için Esc.)</size>");
            }

            SetPlayerControls(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Kutuyu otomatik odaklamıyoruz: oyuncu o an W/A/S/D'ye basarak yürüyor olabilir ve
            // bu tuşlar kutuya yazılıp muhafıza "wwww" diye gidiyordu. Oyuncu kutuya tıklayarak yazar.
            if (playerInput != null) playerInput.text = string.Empty;
        }

        private void HandleGuardLetsPass()
        {
            if (state != State.Talking) return;
            state = State.Done;
            Unsubscribe();
            Debug.Log("[Muhafız] Oyuncu ikna etti — hazine oyuncunun.", this);
            StartCoroutine(FinishAfterDelay(stepAside: true));
        }

        private void HandleGuardAttacks()
        {
            if (state != State.Talking) return;
            state = State.Done;
            Unsubscribe();
            Debug.Log("[Muhafız] Oyuncu ikna edemedi — muhafız saldırdı.", this);
            if (_playerHealth != null) _playerHealth.TakeDamage(_playerHealth.MaxHealth);
            StartCoroutine(FinishAfterDelay(stepAside: false));
        }

        private IEnumerator FinishAfterDelay(bool stepAside)
        {
            // Oyuncu muhafızın son repliğini okuyabilsin diye panel bir süre açık kalır.
            if (playerInput != null) playerInput.interactable = false;
            yield return new WaitForSecondsRealtime(closeDelay);

            if (stepAside)
            {
                transform.position += transform.rotation * stepAsideOffset;
            }
            CloseConversation();
            Passed = stepAside;
        }

        private void CloseConversation()
        {
            IsConversationOpen = false;
            dialogueController.ResetConversation();
            Unsubscribe();
            if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
            if (playerInput != null) playerInput.interactable = true;
            // Kamera tekrar açılınca Start'ta değil, sol tıkta imleci kilitliyor; o yüzden
            // burada da kilitliyoruz ki oyuncu doğrudan oyuna dönebilsin.
            SetPlayerControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetPlayerControls(bool enabled)
        {
            if (disableWhileTalking == null) return;
            foreach (Behaviour behaviour in disableWhileTalking)
            {
                if (behaviour != null) behaviour.enabled = enabled;
            }
        }

        private void FacePlayer()
        {
            if (player == null) return;
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(toPlayer), 5f * Time.deltaTime);
            }
        }

        private bool IsPlayerDead() => _playerHealth != null && _playerHealth.IsDead;

        private void Subscribe()
        {
            Unsubscribe();
            GameEvents.NpcUnlockedDoor += HandleGuardLetsPass;
            GameEvents.NpcAttackedPlayer += HandleGuardAttacks;
        }

        private void Unsubscribe()
        {
            GameEvents.NpcUnlockedDoor -= HandleGuardLetsPass;
            GameEvents.NpcAttackedPlayer -= HandleGuardAttacks;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
