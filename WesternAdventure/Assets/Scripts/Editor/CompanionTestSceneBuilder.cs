using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WesternAdventure.Companion;

namespace WesternAdventure.EditorTools
{
    /// <summary>
    /// Companion AI'ı (ML-Agents) önce klavyeyle (Heuristic) test edip sonra eğitebileceğimiz
    /// minimal bir 3D sahne üretir — arkadaşın PlayerTest.unity'sindeki kurulumla aynı
    /// mantık (Ground + fiziksel engel + "Environment" layer). ML-Agents'a özgü bileşenler
    /// (BehaviorParameters, DecisionRequester, RayPerceptionSensorComponent3D) eklenir ama
    /// iç alanları bilinçli olarak burada set edilmez — sürümler arası iç alan adları
    /// kırılgan olabileceğinden, bu değerler menü çalıştıktan sonra Inspector'dan elle
    /// doğrulanıp girilmelidir (talimatlar konsola yazdırılır).
    /// </summary>
    public static class CompanionTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CompanionTest.unity";

        [MenuItem("WesternAdventure/Companion Test Sahnesi Oluştur")]
        public static void BuildCompanionTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer < 0)
            {
                Debug.LogWarning(
                    "'Environment' layer'ı bulunamadı (Edit > Project Settings > Tags and Layers'tan " +
                    "arkadaşının kullandığı sırayla ekleyebilirsin). Şimdilik Default layer kullanılıyor.");
                environmentLayer = 0;
            }

            var lightGO = new GameObject("Directional Light", typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraGO = new GameObject("Main Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0f, 22f, 0f);
            cameraGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.layer = environmentLayer;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestWall";
            wall.transform.position = new Vector3(0f, 1f, 0f);
            wall.transform.localScale = new Vector3(8f, 2f, 1f);
            wall.layer = environmentLayer;

            GameObject targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetGO.name = "Target";
            targetGO.transform.position = new Vector3(6f, 0.5f, 6f);
            targetGO.transform.localScale = Vector3.one * 1.2f;
            TintRenderer(targetGO, new Color(0.2f, 0.8f, 0.3f));
            Object.DestroyImmediate(targetGO.GetComponent<SphereCollider>());
            var targetCollider = targetGO.AddComponent<SphereCollider>();
            targetCollider.isTrigger = true;
            targetGO.AddComponent<TargetZone>();

            GameObject agentGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            agentGO.name = "CompanionAgent";
            agentGO.transform.position = new Vector3(-6f, 1f, -6f);
            TintRenderer(agentGO, new Color(0.25f, 0.55f, 0.95f));
            Object.DestroyImmediate(agentGO.GetComponent<CapsuleCollider>());
            agentGO.AddComponent<CharacterController>();

            var companionAgent = agentGO.AddComponent<CompanionAgent>();
            SetPrivateField(companionAgent, "obstacleTarget", targetGO.transform);

            agentGO.AddComponent<BehaviorParameters>();
            agentGO.AddComponent<DecisionRequester>();
            agentGO.AddComponent<RayPerceptionSensorComponent3D>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(
                "Companion test sahnesi (3D) oluşturuldu: " + ScenePath + "\n" +
                "ŞİMDİ ELLE YAPILMASI GEREKENLER (CompanionAgent objesini seç, Inspector'da):\n" +
                "1) Behavior Parameters -> Behavior Name: 'Companion', Vector Observation Space Size: 9, " +
                "Continuous Actions: 2, Behavior Type: Default\n" +
                "2) Decision Requester -> Decision Period: 5\n" +
                "3) Ray Perception Sensor 3D -> Detectable Tags listesine 'Target' ekle " +
                "(Target objesine önce Tags menüsünden 'Target' tag'ini tanımlayıp ata)\n" +
                "Sonra Play'e basıp WASD/ok tuşlarıyla maviyi (companion) yeşile (target) sürebilirsin " +
                "(duvarı dolaşman gerekecek).");
        }

        private static void TintRenderer(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            var material = new Material(renderer.sharedMaterial);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
