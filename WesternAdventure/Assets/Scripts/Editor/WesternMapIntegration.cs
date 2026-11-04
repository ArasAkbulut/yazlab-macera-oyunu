using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WesternAdventure.Companion;
using WesternAdventure.NPC;
using WesternAdventure.UI;

namespace WesternAdventure.EditorTools
{
    /// <summary>
    /// WesternMap.unity içindeki gerçek Lever/TunnelGate mekanizmasına companion AI'ı
    /// bağlar. Arkadaşın yaptığı objelere (TunnelGate, LeverBase, LeverHandle, Player)
    /// dokunmadan, sadece yeni bileşen/obje ekler. ML-Agents'a özgü bileşenlerin iç
    /// alanları (Behavior Name, Space Size, Continuous Actions, Decision Period,
    /// Detectable Tags) bu menü çalıştıktan sonra Inspector'dan elle doğrulanmalı
    /// (bkz. konsol çıktısı).
    /// </summary>
    public static class WesternMapIntegration
    {
        private const string ScenePath = "Assets/Scenes/WesternMap.unity";

        [MenuItem("WesternAdventure/WesternMap'e Companion Entegre Et")]
        public static void IntegrateCompanion()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject tunnelGate = GameObject.Find("TunnelGate");
            GameObject leverHandle = GameObject.Find("LeverHandle");
            GameObject player = GameObject.Find("Player");

            if (tunnelGate == null || leverHandle == null || player == null)
            {
                Debug.LogError("TunnelGate, LeverHandle veya Player bulunamadı — sahne yapısı değişmiş olabilir.");
                return;
            }

            // 1) Kapı açma script'i (arkadaşın objesine sadece component ekleniyor, değiştirilmiyor)
            if (tunnelGate.GetComponent<TunnelGateController>() == null)
            {
                tunnelGate.AddComponent<TunnelGateController>();
            }

            // 2) Lever'a ulaşma algılama bölgesi. ÖNEMLİ: LeverHandle'ın kendi scale'i çok küçük
            //    (0.1, 0.6, 0.1) olduğundan, bunu child yaparsak collider'ın gerçek dünya
            //    yarıçapı da küçülüyordu (bug). Bu yüzden bağımsız, scale'i 1 olan kök seviye
            //    bir obje olarak, LeverHandle'ın dünya konumuna yerleştiriyoruz.
            GameObject triggerGO = GameObject.Find("LeverTriggerZone");
            if (triggerGO == null) triggerGO = new GameObject("LeverTriggerZone");
            triggerGO.transform.SetParent(null, true);
            triggerGO.transform.position = leverHandle.transform.position;
            triggerGO.transform.localScale = Vector3.one;

            var triggerCollider = triggerGO.GetComponent<SphereCollider>();
            if (triggerCollider == null) triggerCollider = triggerGO.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 8f;

            if (triggerGO.GetComponent<TargetZone>() == null)
            {
                triggerGO.AddComponent<TargetZone>();
            }

            // 3) Companion (köpek) ajanı, oyuncunun yanında
            GameObject companionGO = GameObject.Find("CompanionDog");
            if (companionGO == null)
            {
                companionGO = new GameObject("CompanionDog");
                companionGO.transform.position = player.transform.position + new Vector3(2f, 0f, 0f);
                companionGO.AddComponent<CharacterController>();
                companionGO.AddComponent<CompanionAgent>();
                companionGO.AddComponent<BehaviorParameters>();
                companionGO.AddComponent<DecisionRequester>();
                companionGO.AddComponent<RayPerceptionSensorComponent3D>();
            }

            var companionAgent = companionGO.GetComponent<CompanionAgent>();
            SetPrivateField(companionAgent, "obstacleTarget", triggerGO.transform);
            // Gerçek oyun sahnesinde köpek, kapıyı bir kez açtıktan sonra başlangıç noktasına
            // ışınlanıp tekrar yürümeye başlamamalı — olduğu yerde kalmalı.
            SetPrivateField(companionAgent, "returnToStartOnEpisodeBegin", false);
            // Haritanın bitmemiş/karmaşık kısımlarında serbest model navigasyonu güvenilir
            // değildi; köpek oyuncuyu takip etsin, lever'e yeterince yaklaşınca da otomatik açsın.
            SetPrivateField(companionAgent, "followTarget", player.transform);

            // 4) GEÇİCİ test zemini: bu alandaki terrain/collider arkadaş tarafından henüz
            //    bitmediği için (kendi notlarında da belirtmiş), test sırasında köpeğin
            //    boşluğa düşmemesi için görünmez bir platform. Gerçek zemin tamamlanınca silinmeli.
            GameObject tempFloor = GameObject.Find("TEMP_LeverTestFloor");
            if (tempFloor == null)
            {
                tempFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tempFloor.name = "TEMP_LeverTestFloor";
                Object.DestroyImmediate(tempFloor.GetComponent<MeshRenderer>());
            }
            // Yükseklik 2 olduğu için merkez y=-1 -> üst yüzey y=0 (zemin seviyesi). Merkez y=0
            // olunca üst yüzey y=1'de kalıp kenarında 1 birimlik görünmez duvar oluşturuyordu
            // (oyuncu ve köpek x≈-267.5 hizasında takılıyordu).
            tempFloor.transform.position = new Vector3(leverHandle.transform.position.x, -1f, leverHandle.transform.position.z - 5f);
            // Köpek, gerçek engellerden kaçınırken yana doğru sapabiliyor; platform dar olursa
            // bu sapma onu bitmemiş zeminin dışına, boşluğa düşürüyordu. Bolca pay bırakıldı.
            tempFloor.transform.localScale = new Vector3(70f, 2f, 60f);

            Directory_CreateDirectoryForScenes();
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(
                "WesternMap entegrasyonu tamamlandı.\n" +
                "ŞİMDİ ELLE YAPILMASI GEREKENLER (CompanionDog objesini seç, Inspector'da):\n" +
                "1) Behavior Parameters -> Behavior Name: 'Companion', Vector Observation Space Size: 6, " +
                "Continuous Actions: 2, Behavior Type: Default\n" +
                "2) Decision Requester -> Decision Period: 5\n" +
                "3) CompanionAgent -> Max Step: 1000\n" +
                "4) Ray Perception Sensor 3D -> Detectable Tags listesine 'target' ekle\n" +
                "5) İstersen Companion.onnx modelini Behavior Parameters > Model alanına sürükleyip " +
                "Behavior Type: Inference Only yaparsan eğitilmiş modelle çalışır.\n" +
                "Ayrıca CompanionDog'u Player'ın yanına görsel olarak yerleştirmeyi/köpek modeliyle " +
                "değiştirmeyi unutma (şu an CharacterController'lı boş bir obje).");
        }

        private const string GuardModelPath = "Assets/Characters/TunnelGuard.fbx";

        /// <summary>
        /// Mixamo'dan indirilen görsel modeli (TunnelGuard.fbx) TunnelGuard objesinin
        /// altına ekler, böylece NPC artık ekranda görünür olur.
        /// </summary>
        [MenuItem("WesternAdventure/TunnelGuard Görselini Bağla")]
        public static void AttachGuardVisual()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject guardGO = GameObject.Find("TunnelGuard");
            if (guardGO == null)
            {
                Debug.LogError("TunnelGuard bulunamadı — önce 'WesternMap'e NPC Muhafız Entegre Et' çalıştırılmalı.");
                return;
            }

            Transform existingVisual = guardGO.transform.Find("TunnelGuardModel");
            if (existingVisual != null)
            {
                Object.DestroyImmediate(existingVisual.gameObject);
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuardModelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"Model bulunamadı: {GuardModelPath}");
                return;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, scene);
            visual.name = "TunnelGuardModel";
            visual.transform.SetParent(guardGO.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("TunnelGuard görseli bağlandı.");
        }

        private const string CompanionModelPath =
            "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/germanshepherd.prefab";

        /// <summary>
        /// Hazır German Shepherd modelini (Bublisher - 3D Stylized Animated Dogs Kit, ücretsiz)
        /// CompanionDog objesinin altına ekler — arkadaşın kullandığı kovboy/golem modelleriyle
        /// aynı PBR/stilize kalite seviyesinde olduğu için elle modellenen low-poly köpeğin yerini alır.
        /// </summary>
        [MenuItem("WesternAdventure/CompanionDog Görselini Bağla")]
        public static void AttachCompanionVisual()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject companionGO = GameObject.Find("CompanionDog");
            if (companionGO == null)
            {
                Debug.LogError("CompanionDog bulunamadı — önce 'WesternMap'e Companion Entegre Et' çalıştırılmalı.");
                return;
            }

            Transform existingVisual = companionGO.transform.Find("CompanionDogModel");
            if (existingVisual != null)
            {
                Object.DestroyImmediate(existingVisual.gameObject);
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CompanionModelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"Model bulunamadı: {CompanionModelPath}");
                return;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, scene);
            visual.name = "CompanionDogModel";
            visual.transform.SetParent(companionGO.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("CompanionDog görseli bağlandı (German Shepherd).");
        }

        private const string GuardPersonaPath = "Assets/Data/NpcPersonas/TunnelGuardPersona.asset";

        /// <summary>
        /// Muhafız NPC'sini WesternMap'e yerleştirir. ÖNEMLİ: Arkadaşın planına göre bu NPC'nin
        /// asıl yeri "ikinci alanda patron dövüşü sonrası dönüş yolu" — bu alan henüz
        /// haritada yok. Bu yüzden şimdilik TunnelGate'in hemen ötesine, geçici bir konuma
        /// yerleştiriyoruz; o alan hazır olunca sadece TunnelGuard'ı taşımak yeterli olacak.
        /// </summary>
        [MenuItem("WesternAdventure/WesternMap'e NPC Muhafız Entegre Et")]
        public static void IntegrateNpc()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject tunnelGate = GameObject.Find("TunnelGate");
            if (tunnelGate == null)
            {
                Debug.LogError("TunnelGate bulunamadı — sahne yapısı değişmiş olabilir.");
                return;
            }

            NpcPersona persona = GetOrCreateGuardPersona();

            // 1) EventSystem (yoksa)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            // 2) Muhafız NPC objesi — GEÇİCİ konum: TunnelGate'in biraz ötesi.
            GameObject guardGO = GameObject.Find("TunnelGuard");
            bool isNew = guardGO == null;
            if (isNew)
            {
                guardGO = new GameObject("TunnelGuard", typeof(NpcDialogueController));
            }
            guardGO.transform.position = tunnelGate.transform.position + new Vector3(0f, 0f, 15f);

            var dialogueController = guardGO.GetComponent<NpcDialogueController>();
            SetPrivateField(dialogueController, "persona", persona);

            // 3) Diyalog UI (geçici Canvas — arkadaşın asıl oyun UI'ı hazır olunca bununla
            //    değiştirilmeli; şimdilik AI mantığını test edilebilir kılmak için var)
            GameObject canvasGO = GameObject.Find("TunnelGuardDialogueCanvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("TunnelGuardDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

                Text replyText = CreateLabel(canvasGO.transform, "ReplyText", new Vector2(0, 150), new Vector2(700, 120), "Muhafız: \"Hazineye neden layıksın?\"");
                Text thinkingText = CreateLabel(canvasGO.transform, "ThinkingIndicator", new Vector2(0, 70), new Vector2(300, 40), "Düşünüyor...");
                thinkingText.gameObject.SetActive(false);
                Text errorText = CreateLabel(canvasGO.transform, "ErrorText", new Vector2(0, 30), new Vector2(700, 40), string.Empty);
                errorText.color = Color.red;

                var resources = new DefaultControls.Resources();
                GameObject inputGO = DefaultControls.CreateInputField(resources);
                inputGO.transform.SetParent(canvasGO.transform, false);
                inputGO.name = "PlayerInputField";
                SetRect(inputGO.GetComponent<RectTransform>(), new Vector2(-110, -300), new Vector2(420, 40));

                GameObject buttonGO = DefaultControls.CreateButton(resources);
                buttonGO.transform.SetParent(canvasGO.transform, false);
                buttonGO.name = "SendButton";
                SetRect(buttonGO.GetComponent<RectTransform>(), new Vector2(260, -300), new Vector2(140, 40));
                buttonGO.GetComponentInChildren<Text>().text = "Gönder";

                var dialogueUI = guardGO.GetComponent<DialogueUI>();
                if (dialogueUI == null) dialogueUI = guardGO.AddComponent<DialogueUI>();
                SetPrivateField(dialogueUI, "dialogueController", dialogueController);
                SetPrivateField(dialogueUI, "replyText", replyText);
                SetPrivateField(dialogueUI, "thinkingIndicator", thinkingText.gameObject);
                SetPrivateField(dialogueUI, "errorText", errorText);

                var harness = guardGO.GetComponent<DialogueTestHarness>();
                if (harness == null) harness = guardGO.AddComponent<DialogueTestHarness>();
                SetPrivateField(harness, "dialogueController", dialogueController);
                SetPrivateField(harness, "playerInput", inputGO.GetComponent<InputField>());
                SetPrivateField(harness, "sendButton", buttonGO.GetComponent<Button>());

                canvasGO.SetActive(false); // oyuncu muhafızın yanına gelene kadar gizli kalsın
            }

            Directory_CreateDirectoryForScenes();
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(
                "WesternMap'e NPC muhafız entegrasyonu tamamlandı.\n" +
                "ÖNEMLİ: TunnelGuard şu an TunnelGate'in 15 birim ötesinde GEÇİCİ bir konumda — " +
                "arkadaşın 'ikinci alan + patron + dönüş yolu' kısmını bitirince buraya taşınmalı.\n" +
                "Diyalog Canvas'ı (TunnelGuardDialogueCanvas) şu an gizli; test etmek için Hierarchy'den " +
                "elle aktif hale getirip Play'e basabilirsin (gerçek oyunda bu, oyuncu muhafıza " +
                "yaklaşınca bir trigger ile açılmalı — henüz yazılmadı).\n" +
                "Cursor kilidi: ThirdPersonCamera Play'de fareyi kilitliyor, UI ile etkileşim için " +
                "Escape'e basıp imleci serbest bırakman gerekebilir.");
        }

        private static NpcPersona GetOrCreateGuardPersona()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NpcPersona>(GuardPersonaPath);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/Data/NpcPersonas");
            var persona = ScriptableObject.CreateInstance<NpcPersona>();
            AssetDatabase.CreateAsset(persona, GuardPersonaPath);
            AssetDatabase.SaveAssets();
            return persona;
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string text)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(go.GetComponent<RectTransform>(), anchoredPos, size);
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void Directory_CreateDirectoryForScenes()
        {
            // Scenes klasörü zaten var, burada sadece güvence için.
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
