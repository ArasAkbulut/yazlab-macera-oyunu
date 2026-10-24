using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WesternAdventure.Companion;

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
            tempFloor.transform.position = new Vector3(leverHandle.transform.position.x, 0f, leverHandle.transform.position.z - 5f);
            tempFloor.transform.localScale = new Vector3(20f, 1f, 25f);

            Directory_CreateDirectoryForScenes();
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(
                "WesternMap entegrasyonu tamamlandı.\n" +
                "ŞİMDİ ELLE YAPILMASI GEREKENLER (CompanionDog objesini seç, Inspector'da):\n" +
                "1) Behavior Parameters -> Behavior Name: 'Companion', Vector Observation Space Size: 9, " +
                "Continuous Actions: 2, Behavior Type: Default\n" +
                "2) Decision Requester -> Decision Period: 5\n" +
                "3) CompanionAgent -> Max Step: 1000\n" +
                "4) Ray Perception Sensor 3D -> Detectable Tags listesine 'target' ekle\n" +
                "5) İstersen Companion.onnx modelini Behavior Parameters > Model alanına sürükleyip " +
                "Behavior Type: Inference Only yaparsan eğitilmiş modelle çalışır.\n" +
                "Ayrıca CompanionDog'u Player'ın yanına görsel olarak yerleştirmeyi/köpek modeliyle " +
                "değiştirmeyi unutma (şu an CharacterController'lı boş bir obje).");
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
    }
}
