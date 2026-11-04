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

        private const string GuardTextureFolder = "Assets/Characters/TunnelGuardTextures";

        /// <summary>
        /// Mixamo Peasant Man modelinin dokuları FBX'e gömülü geliyor ve URP'de malzeme olarak
        /// bağlanmadığı için muhafız bembeyaz görünüyordu. Dokuları dışarı çıkarır, URP/Lit
        /// malzemesi oluşturur ve sahnedeki muhafızın tüm renderer'larına atar.
        /// </summary>
        [MenuItem("WesternAdventure/Muhafız Görselini Düzelt (Doku + URP Malzeme)")]
        public static void FixGuardMaterials()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var importer = AssetImporter.GetAtPath(GuardModelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Model bulunamadı: {GuardModelPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(GuardTextureFolder))
            {
                AssetDatabase.CreateFolder("Assets/Characters", "TunnelGuardTextures");
            }
            importer.ExtractTextures(GuardTextureFolder);
            AssetDatabase.Refresh();

            Texture2D diffuse = FindTexture("Peasant_Man_diffuse");
            Texture2D normal = FindTexture("Peasant_Man_normal");
            if (diffuse == null)
            {
                Debug.LogError("Peasant_Man_diffuse dokusu FBX'ten çıkarılamadı/bulunamadı.");
                return;
            }

            if (normal != null)
            {
                var normalImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }
            }

            string materialPath = GuardTextureFolder + "/TunnelGuard_URP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", diffuse);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", diffuse);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject guardGO = GameObject.Find("TunnelGuard");
            if (guardGO == null)
            {
                Debug.LogError("Sahnede TunnelGuard bulunamadı.");
                return;
            }

            int count = 0;
            foreach (Renderer r in guardGO.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Muhafız malzemesi düzeltildi: {count} renderer'a {materialPath} atandı.");
        }

        private static Texture2D FindTexture(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:Texture2D"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
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
            // Boy ve zemin hizası için: "Köpek Görselini Ayarla (Boy + Zemin)" menüsü.

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

        /// <summary>
        /// Muhafızı hazinenin (TreasurePlaceholder) başına taşır ve GuardEncounter ile bağlar:
        /// boss yenilince oyuncu yaklaştığında diyalog otomatik açılır.
        /// "WesternMap'e NPC Muhafız Entegre Et" daha önce çalıştırılmış olmalı.
        /// </summary>
        [MenuItem("WesternAdventure/Muhafızı Hazinenin Başına Yerleştir")]
        public static void PlaceGuardAtTreasure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Deaktif objeler GameObject.Find ile bulunamadığı için kök objeler üzerinden aranıyor.
            GameObject FindRoot(string name)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == name) return root;
                }
                return null;
            }

            GameObject guardGO = FindRoot("TunnelGuard");
            GameObject canvasGO = FindRoot("TunnelGuardDialogueCanvas");
            GameObject treasure = FindRoot("TreasurePlaceholder");
            GameObject player = FindRoot("Player");
            if (guardGO == null || canvasGO == null || treasure == null || player == null)
            {
                Debug.LogError("TunnelGuard, TunnelGuardDialogueCanvas, TreasurePlaceholder veya Player " +
                               "bulunamadı. Önce 'WesternMap'e NPC Muhafız Entegre Et' menüsünü çalıştır.");
                return;
            }

            BossHealth bossHealth = Object.FindFirstObjectByType<BossHealth>(FindObjectsInactive.Include);
            if (bossHealth == null)
            {
                Debug.LogWarning("Sahnede BossHealth bulunamadı — muhafız boss beklemeden konuşmaya hazır olacak.");
            }

            // Konum: hazinenin oyuncunun geldiği tarafında (boss -> hazine yönünün tersi), yüzü
            // oyuncuya dönük. Yükseklik: eski konumdaki zemin-pivot farkı korunarak yeni zemine oturtulur.
            Vector3 approachDir = bossHealth != null
                ? bossHealth.transform.position - treasure.transform.position
                : Vector3.left;
            approachDir.y = 0f;
            approachDir = approachDir.sqrMagnitude > 0.01f ? approachDir.normalized : Vector3.left;

            float pivotOffset = GroundOffset(guardGO.transform.position, guardGO);
            Vector3 target = treasure.transform.position + approachDir * 2.5f;
            target.y = GroundHeight(target, guardGO) + pivotOffset;
            guardGO.transform.position = target;
            guardGO.transform.rotation = Quaternion.LookRotation(approachDir);

            // Muhafız modeli oyuncuya göre çok küçük kalıyordu: görselin boyunu, oyuncunun
            // görünen boyunun ~1.2 katına ölçekle (tek seferlik ölçüm, menü tekrar çalışsa da sabit).
            Transform guardVisual = guardGO.transform.Find("TunnelGuardModel");
            float playerHeight = VisibleHeight(player.transform);
            if (guardVisual != null && playerHeight > 0.1f)
            {
                guardVisual.localScale = Vector3.one;
                float guardHeight = VisibleHeight(guardVisual);
                if (guardHeight > 0.1f)
                {
                    float factor = Mathf.Clamp(playerHeight * 1.2f / guardHeight, 0.5f, 5f);
                    guardVisual.localScale = Vector3.one * factor;
                    Debug.Log($"Muhafız görseli ölçeklendi: oyuncu {playerHeight:F2} m, muhafız {guardHeight:F2} m -> x{factor:F2}");
                }
            }

            // Beyaz yazılar çöl zemininde okunmuyordu: panelin arkasına yarı saydam koyu bir zemin.
            Transform background = canvasGO.transform.Find("DialogueBackground");
            if (background == null)
            {
                var bgGO = new GameObject("DialogueBackground", typeof(Image));
                bgGO.transform.SetParent(canvasGO.transform, false);
                background = bgGO.transform;
            }
            background.SetAsFirstSibling();
            var bgImage = background.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.65f);
            bgImage.raycastTarget = false;

            // Yerleşim ekranın ORTASINA göre sabit piksel idi; küçük Game penceresinde yazı
            // kutusu (-300 px) ekranın dışında kalıyordu. Artık ekran boyutuyla ölçekleniyor ve
            // her şey ekranın altına sabitli bir altyazı paneli gibi duruyor.
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgRect = (RectTransform)background;
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, 10f);
            bgRect.sizeDelta = new Vector2(-60f, 250f);

            Text reply = FindChildComponent<Text>(canvasGO, "ReplyText");
            if (reply != null)
            {
                reply.fontSize = 22;
                reply.supportRichText = true;
                reply.verticalOverflow = VerticalWrapMode.Overflow;
                SetBottomRect(reply.transform, new Vector2(0f, 175f), new Vector2(1050f, 120f));
            }
            Text thinking = FindChildComponent<Text>(canvasGO, "ThinkingIndicator");
            if (thinking != null) SetBottomRect(thinking.transform, new Vector2(0f, 100f), new Vector2(400f, 30f));
            Text error = FindChildComponent<Text>(canvasGO, "ErrorText");
            if (error != null) SetBottomRect(error.transform, new Vector2(0f, 100f), new Vector2(1000f, 30f));
            InputField inputField = canvasGO.GetComponentInChildren<InputField>(true);
            if (inputField != null)
            {
                SetBottomRect(inputField.transform, new Vector2(-80f, 50f), new Vector2(760f, 44f));
                if (inputField.placeholder is Text placeholder) placeholder.text = "Cevabını buraya yaz...";
            }
            Button sendButton = canvasGO.GetComponentInChildren<Button>(true);
            if (sendButton != null) SetBottomRect(sendButton.transform, new Vector2(390f, 50f), new Vector2(150f, 44f));

            var encounter = guardGO.GetComponent<GuardEncounter>();
            if (encounter == null) encounter = guardGO.AddComponent<GuardEncounter>();

            var disable = new System.Collections.Generic.List<Behaviour>();
            var movement = player.GetComponent<PlayerMovement>();
            var combat = player.GetComponent<PlayerCombat>();
            var cam = Object.FindFirstObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
            if (movement != null) disable.Add(movement);
            if (combat != null) disable.Add(combat);
            if (cam != null) disable.Add(cam);

            var so = new SerializedObject(encounter);
            so.FindProperty("dialogueController").objectReferenceValue = guardGO.GetComponent<NpcDialogueController>();
            so.FindProperty("dialogueUI").objectReferenceValue = guardGO.GetComponent<DialogueUI>();
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvasGO;
            so.FindProperty("playerInput").objectReferenceValue = canvasGO.GetComponentInChildren<InputField>(true);
            so.FindProperty("player").objectReferenceValue = player.transform;
            so.FindProperty("boss").objectReferenceValue = bossHealth != null ? bossHealth.gameObject : null;
            so.FindProperty("talkDistance").floatValue = 10f;
            SerializedProperty list = so.FindProperty("disableWhileTalking");
            list.arraySize = disable.Count;
            for (int i = 0; i < disable.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = disable[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"Muhafız hazinenin başına yerleştirildi ({target}). Boss: " +
                      $"{(bossHealth != null ? bossHealth.name : "yok")}. Kilitlenen bileşenler: {disable.Count}. " +
                      "Test için Ollama'nın çalıştığından emin ol (ollama serve / llama3.2:3b).");
        }

        /// <summary>
        /// Köpek görselini oyuncunun bacak boyuna (görünen boyun ~%50'si) ölçekler ve
        /// ayaklarını CharacterController kapsülünün tabanına indirir. Önceden görsel kökün
        /// (kapsül merkezinin) hizasına konduğu için köpek havada süzülüyordu.
        /// </summary>
        [MenuItem("WesternAdventure/Köpek Görselini Ayarla (Boy + Zemin)")]
        public static void FitDogVisual()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject dog = GameObject.Find("CompanionDog");
            GameObject player = GameObject.Find("Player");
            Transform visual = dog != null ? dog.transform.Find("CompanionDogModel") : null;
            if (visual == null || player == null)
            {
                Debug.LogError("CompanionDog/CompanionDogModel veya Player bulunamadı.");
                return;
            }

            const float legRatio = 0.5f;
            float playerHeight = VisibleHeight(player.transform);

            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            float dogHeight = VisibleHeight(visual);
            if (playerHeight > 0.1f && dogHeight > 0.05f)
            {
                float factor = Mathf.Clamp(playerHeight * legRatio / dogHeight, 0.3f, 6f);
                visual.localScale = Vector3.one * factor;
            }

            // Ayakları kapsülün tabanına hizala.
            var controller = dog.GetComponent<CharacterController>();
            float lossyY = dog.transform.lossyScale.y;
            float capsuleBottom = controller != null
                ? dog.transform.position.y + (controller.center.y - controller.height * 0.5f) * lossyY
                : dog.transform.position.y;
            float visualBottom = VisibleBottom(visual);
            visual.localPosition = new Vector3(0f, (capsuleBottom - visualBottom) / lossyY, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Köpek ayarlandı: oyuncu {playerHeight:F2} m, köpek {VisibleHeight(visual):F2} m, " +
                      $"ölçek x{visual.localScale.x:F2}, yükseklik ofseti {visual.localPosition.y:F2}");
        }

        private static float VisibleBottom(Transform root)
        {
            float bottom = float.PositiveInfinity;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled) bottom = Mathf.Min(bottom, r.bounds.min.y);
            }
            return float.IsPositiveInfinity(bottom) ? root.position.y : bottom;
        }

        private static T FindChildComponent<T>(GameObject parent, string childName) where T : Component
        {
            foreach (T component in parent.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == childName) return component;
            }
            return null;
        }

        private static void SetBottomRect(Transform target, Vector2 anchoredPos, Vector2 size)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static float VisibleHeight(Transform root)
        {
            bool found = false;
            Bounds bounds = default;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue; // ör. oyuncunun gizli kapsül mesh'i
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found ? bounds.size.y : 0f;
        }

        private static float GroundHeight(Vector3 point, GameObject ignore)
        {
            var hits = Physics.RaycastAll(point + Vector3.up * 50f, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(ignore.transform)) continue;
                if (hit.point.y > best) best = hit.point.y;
            }
            return float.IsNegativeInfinity(best) ? point.y : best;
        }

        private static float GroundOffset(Vector3 pivot, GameObject owner)
        {
            float ground = GroundHeight(pivot, owner);
            float offset = pivot.y - ground;
            // Mantıksız bir sonuçta (zemin bulunamadı / çok yüksek) güvenli bir varsayılan kullan.
            return offset >= 0f && offset < 3f ? offset : 0f;
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
