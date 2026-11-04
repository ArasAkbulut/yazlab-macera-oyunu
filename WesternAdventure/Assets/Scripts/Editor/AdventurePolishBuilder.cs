using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WesternAdventure.NPC;
using WesternAdventure.Companion;
using WesternAdventure.Core;

namespace WesternAdventure.EditorTools
{
    public static class AdventurePolishBuilder
    {
        private const string Folder = "Assets/WesternCraft";
        private const string ScenePath = "Assets/Scenes/WesternMap.unity";
        private static Material wood, woodLight, iron, brass, stone;
        private static int environment;

        [MenuItem("WesternAdventure/Polish/Build Adventure %&b")]
        public static void Build()
        {
            try
            {
                if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.OpenScene(ScenePath);
                environment = LayerMask.NameToLayer("Environment");
                Directory.CreateDirectory(Folder + "/Materials"); Directory.CreateDirectory(Folder + "/Prefabs");
                AssetDatabase.Refresh();
                wood = Mat("Weathered Cedar", new Color(.28f, .13f, .055f));
                woodLight = Mat("Sunlit Cedar", new Color(.48f, .27f, .11f));
                iron = Mat("Forged Iron", new Color(.10f, .13f, .14f), .65f);
                brass = Mat("Aged Brass", new Color(.74f, .49f, .15f), .7f);
                stone = Mat("Sandstone", new Color(.53f, .35f, .20f));

                var player = Need("Player"); var dog = Need("CompanionDog");
                var lever = Need("LeverBase"); var handle = Need("LeverHandle");
                var target = Need("LeverTriggerZone"); var gate = Need("TunnelGate");
                var oldRoot = GameObject.Find("WesternCraft Set"); if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
                var root = new GameObject("WesternCraft Set");

                // Keep the existing lever area and give it a grounded, enclosed mine winch room.
                var floor = Need("TEMP_LeverTestFloor"); floor.layer = environment;
                // The old lever was beyond the canyon boundary. Keep the winch on the approach side.
                floor.transform.position = new Vector3(-245, .1f, 60);
                floor.transform.localScale = new Vector3(6.8f, .2f, 7.6f);
                Physics.SyncTransforms();
                float floorY = floor.GetComponent<Collider>().bounds.max.y;
                Vector3 roomCenter = new Vector3(-245, floorY, 60);
                var room = Room(); var roomInstance = SavePrefab(room, "DogOnlyWinchRoom", root.transform);
                roomInstance.transform.position = roomCenter;
                var entrance = roomInstance.transform.Find("CommandPoint");
                var inside = roomInstance.transform.Find("LeverPoint");
                lever.transform.position = inside.position + Vector3.up * .15f;
                handle.transform.position = inside.position + Vector3.up * .52f;
                lever.GetComponent<Renderer>().sharedMaterial = iron;
                handle.GetComponent<Renderer>().sharedMaterial = brass;
                target.transform.position = inside.position + Vector3.up * .4f;
                var trigger = target.GetComponent<SphereCollider>(); trigger.radius = .4f; trigger.enabled = false;
                // Gameplay guide operates the lever once after physically arriving. Training trigger stays available.
                target.GetComponent<TargetZone>().enabled = false;

                var ml = dog.GetComponent<CompanionAgent>(); if (ml != null) ml.enabled = false;
                foreach (var component in dog.GetComponents<MonoBehaviour>())
                    if (component.GetType().Name == "DecisionRequester") component.enabled = false;
                var cc = dog.GetComponent<CharacterController>(); cc.height = 1.2f; cc.radius = .32f;
                cc.center = new Vector3(0, .62f, 0); cc.stepOffset = .3f; cc.skinWidth = .025f;
                var visual = dog.transform.Find("CompanionDogModel");
                if (visual != null)
                {
                    var renderers = visual.GetComponentsInChildren<Renderer>();
                    Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    visual.localScale *= 1.2f / bounds.size.y;
                    Physics.SyncTransforms(); bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    visual.position += Vector3.up * (dog.transform.position.y - bounds.min.y);
                }
                var guide = dog.GetComponent<CompanionGuide>(); if (guide == null) guide = dog.AddComponent<CompanionGuide>();
                guide.player = player.transform; guide.entrance = entrance; guide.leverTarget = inside; guide.leverHandle = handle.transform;

                // Restore the beginning of the route (the old player position was beside the boss for testing).
                player.transform.position = new Vector3(-299.0f, 1.09f, 25);
                player.transform.rotation = Quaternion.Euler(0, 65, 0);
                dog.transform.position = new Vector3(-296.8f, .04f, 25);

                // Keep the gate collider and controller. Only replace its visible slab.
                var gateRenderer = gate.GetComponent<Renderer>(); gateRenderer.enabled = false;
                var gateModifier = gate.GetComponent<NavMeshModifier>(); if (gateModifier == null) gateModifier = gate.AddComponent<NavMeshModifier>();
                gateModifier.ignoreFromBuild = true;
                var gateObstacle = gate.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (gateObstacle == null) gateObstacle = gate.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                gateObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box; gateObstacle.size = Vector3.one; gateObstacle.carving = true;
                var previous = gate.transform.Find("CraftedGate"); if(previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var gateVisual = Gate(); var gatePrefab = SavePrefab(gateVisual, "IronboundMineGate", gate.transform);
                gatePrefab.name = "CraftedGate";
                Vector3 gateSize = gate.transform.lossyScale;
                gatePrefab.transform.localScale = new Vector3(1 / gateSize.x, 1 / gateSize.y, 1 / gateSize.z);
                gatePrefab.transform.localPosition = Vector3.zero;

                var chestAnchor = Need("TreasurePlaceholder");
                chestAnchor.GetComponent<Renderer>().enabled = false;
                chestAnchor.GetComponent<Collider>().enabled = false;
                var chestObject = Chest(); var chestInstance = SavePrefab(chestObject, "ProspectorsTreasureChest", root.transform);
                chestInstance.transform.position = new Vector3(chestAnchor.transform.position.x, .04f, chestAnchor.transform.position.z);
                var flow = root.AddComponent<AdventureFlow>();
                guide.adventure = flow;
                flow.approachEnemies = Enumerable.Range(1,4).Select(i => Need("Enemy_0" + i).GetComponent<EnemyHealth>()).ToArray();
                flow.player = player.transform; flow.companion = guide; flow.chest = chestInstance.transform;
                flow.chestLid = chestInstance.transform.Find("LidPivot"); flow.boss = UnityEngine.Object.FindFirstObjectByType<BossHealth>();
                flow.viewCamera = Camera.main;
                var guard = Need("TunnelGuard").GetComponent<GuardEncounter>();
                flow.returnGuard = guard; flow.returnGate = gate.transform; guard.adventure = flow;
                var guardPosition = new Vector3(-236, 0, 73);
                if (UnityEngine.AI.NavMesh.SamplePosition(guardPosition, out var ground, 5, UnityEngine.AI.NavMesh.AllAreas))
                    guardPosition = ground.position;
                guard.transform.position = guardPosition;
                guard.transform.rotation = Quaternion.Euler(0, 90, 0);
                var guardSettings = new SerializedObject(guard);
                guardSettings.FindProperty("talkDistance").floatValue = 5f;
                guardSettings.FindProperty("openingQuestion").stringValue = "Dur, yolcu! Golemi yenip hazineyi almışsın. Bu hazineyle ne yapacaksın? Geçmene izin vermem için beni ikna et.";
                var dialogueCanvas = (GameObject)guardSettings.FindProperty("dialogueCanvas").objectReferenceValue;
                guardSettings.ApplyModifiedPropertiesWithoutUndo();
                StyleGuardDialogue(dialogueCanvas);
                var menuView = new GameObject("MenuView").transform; menuView.SetParent(root.transform);
                menuView.position = player.transform.position + new Vector3(-7, 5, -8);
                menuView.LookAt(player.transform.position + new Vector3(8, 1, 4)); flow.menuView = menuView;

                // Match navigation geometry to actual static collision, including imported props on Default.
                foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    if(collider.isTrigger || collider is CharacterController ||
                       collider.GetComponentInParent<PlayerHealth>() != null || collider.GetComponentInParent<EnemyHealth>() != null ||
                       collider.GetComponentInParent<CompanionGuide>() != null ||
                       collider.GetComponentInParent<WesternAdventure.NPC.NpcDialogueController>() != null) continue;
                    collider.gameObject.layer = environment;
                }
                foreach(var surface in UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
                {
                    surface.BuildNavMesh();
                    string dataPath = Folder + "/" + surface.name + "-Navigation.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(dataPath);
                    if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
                    else
                    {
                        EditorUtility.CopySerialized(surface.navMeshData, existing);
                        surface.RemoveData(); surface.navMeshData = existing; surface.AddData();
                        EditorUtility.SetDirty(existing);
                    }
                    EditorUtility.SetDirty(surface);
                }
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                File.WriteAllText("AdventurePolishReport.txt", ValidateScene());
                Debug.Log("ADVENTURE_POLISH_BUILD_OK: " + ValidateScene());
                Selection.activeGameObject = roomInstance;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            catch(Exception e) { File.WriteAllText("AdventurePolishReport.txt", e.ToString()); Debug.LogException(e); }
        }

        private static void StyleGuardDialogue(GameObject canvasObject)
        {
            if (canvasObject == null) throw new Exception("Guard dialogue canvas missing");
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.sortingOrder = 150;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            foreach (var text in canvasObject.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 28; text.resizeTextForBestFit = false;
                text.fontStyle = FontStyle.Bold;
                if (text.name == "ReplyText") { text.fontSize = 30; text.color = Color.white; Place(text.rectTransform, 0, 245, 1060, 200); }
                else if (text.name == "ThinkingIndicator" || text.name == "ErrorText") Place(text.rectTransform, 0, 115, 1060, 50);
                EditorUtility.SetDirty(text);
            }
            foreach (var image in canvasObject.GetComponentsInChildren<Image>(true))
                if (image.name.Contains("Background"))
                { image.color = new Color(.025f,.03f,.035f,.97f); Place(image.rectTransform,0,190,1200,370); }
            var input = canvasObject.GetComponentInChildren<InputField>(true);
            if (input != null)
            {
                Place((RectTransform)input.transform,-90,60,860,64);
                input.textComponent.fontSize = 28; input.textComponent.color = new Color(.08f,.08f,.08f);
                if (input.placeholder is Text placeholder) { placeholder.fontSize = 28; placeholder.color = new Color(.3f,.3f,.3f); }
            }
            var button = canvasObject.GetComponentInChildren<Button>(true);
            if (button != null) Place((RectTransform)button.transform,455,60,180,64);
        }
        private static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,0);
            rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h);
        }

        private static GameObject Need(string name) => GameObject.Find(name) ?? throw new InvalidOperationException("Missing scene object: " + name);
        private static Material Mat(string name, Color color, float metallic = 0)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", .25f); return material;
        }
        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.layer = environment;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static void Sign(Transform parent, string text, Vector3 position, float size)
        {
            var obj = new GameObject("Sign " + text); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.identity;
            var label = obj.AddComponent<TextMesh>(); label.text = text; label.fontSize = 64; label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = new Color(1, .85f, .54f);
        }
        private static GameObject Room()
        {
            var obj = new GameObject("DogOnlyWinchRoom"); var t = obj.transform;
            Box(t,"Stone foundation",new Vector3(0,-.12f,0),new Vector3(6.8f,.24f,7.6f),stone,true);
            Box(t,"Left wall",new Vector3(-3.1f,1.9f,0),new Vector3(.4f,3.8f,7),wood,true);
            Box(t,"Right wall",new Vector3(3.1f,1.9f,0),new Vector3(.4f,3.8f,7),wood,true);
            Box(t,"Back wall",new Vector3(0,1.9f,3.3f),new Vector3(6.2f,3.8f,.4f),wood,true);
            Box(t,"Roof",new Vector3(0,3.85f,0),new Vector3(6.8f,.3f,7.6f),woodLight,true);
            // A 1.4m wide, 1.5m high opening: dog 1.2m, player 2m.
            Box(t,"Front left",new Vector3(-1.95f,1.9f,-3.3f),new Vector3(2.5f,3.8f,.4f),wood,true);
            Box(t,"Front right",new Vector3(1.95f,1.9f,-3.3f),new Vector3(2.5f,3.8f,.4f),wood,true);
            Box(t,"Low passage lintel",new Vector3(0,2.65f,-3.3f),new Vector3(1.4f,2.3f,.6f),wood,true);
            for(int side=-1; side<=1; side+=2)
            {
                Box(t,"Entrance trim",new Vector3(side*.8f,.75f,-3.65f),new Vector3(.16f,1.5f,.18f),iron);
                for(int i=0;i<7;i++) Box(t,"Wall rib",new Vector3(side*3.32f,1.9f,-3+i),new Vector3(.12f,3.8f,.13f),woodLight);
            }
            Box(t,"Entry header",new Vector3(0,1.58f,-3.65f),new Vector3(1.75f,.16f,.18f),iron);
            for(int i=0;i<13;i++) Box(t,"Front plank",new Vector3(-3+i*.5f,2.5f,-3.53f),new Vector3(.035f,2.4f,.025f),iron);
            Box(t,"Sign board",new Vector3(0,2.55f,-3.62f),new Vector3(3.6f,.8f,.15f),iron);
            Sign(t,"MADEN VİNCİ",new Vector3(0,2.58f,-3.72f),.12f);
            var point = new GameObject("CommandPoint").transform; point.SetParent(t,false); point.localPosition = new Vector3(0,.05f,-4.4f);
            point = new GameObject("LeverPoint").transform; point.SetParent(t,false); point.localPosition = new Vector3(0,.05f,1);
            return obj;
        }
        private static GameObject Gate()
        {
            var obj = new GameObject("IronboundMineGate"); var t=obj.transform;
            // Gate lies in the YZ plane, matching the original collider.
            for(int i=0;i<12;i++)
                Box(t,"Cedar plank",new Vector3(0,0,-3.76f+i*.685f),new Vector3(.48f,9.5f,.65f),i%3==0?woodLight:wood);
            foreach(float y in new[]{-3.4f,0,3.4f})
            {
                Box(t,"Iron crossbar",new Vector3(-.28f,y,0),new Vector3(.16f,.25f,8.2f),iron);
                Box(t,"Iron crossbar",new Vector3(.28f,y,0),new Vector3(.16f,.25f,8.2f),iron);
                for(int i=0;i<12;i++) Box(t,"Bolt",new Vector3(-.39f,y,-3.76f+i*.685f),new Vector3(.09f,.10f,.10f),brass);
            }
            Box(t,"Center seal",new Vector3(-.4f,.5f,0),new Vector3(.12f,.9f,.65f),brass);
            return obj;
        }
        private static GameObject Chest()
        {
            var obj=new GameObject("ProspectorsTreasureChest"); var t=obj.transform;
            Box(t,"Body",new Vector3(0,.43f,0),new Vector3(2,.86f,1.25f),wood,true);
            for(int i=0;i<8;i++) Box(t,"Front plank",new Vector3(-.875f+i*.25f,.44f,-.64f),new Vector3(.23f,.78f,.06f),i%2==0?woodLight:wood);
            foreach(float x in new[]{-.76f,.76f})
            {
                Box(t,"Iron binding",new Vector3(x,.44f,-.69f),new Vector3(.12f,.87f,.08f),iron);
                Box(t,"Foot",new Vector3(x,.06f,0),new Vector3(.24f,.12f,1.42f),iron);
                for(int y=0;y<4;y++) Box(t,"Rivet",new Vector3(x,.14f+y*.2f,-.75f),Vector3.one*.065f,brass);
            }
            Box(t,"Lock",new Vector3(0,.65f,-.73f),new Vector3(.26f,.32f,.1f),brass);
            var pivot=new GameObject("LidPivot").transform; pivot.SetParent(t,false); pivot.localPosition=new Vector3(0,.86f,.625f);
            for(int i=0;i<7;i++)
            {
                float angle=-75+i*25; float r=angle*Mathf.Deg2Rad;
                var slat=Box(pivot,"Arched lid plank",new Vector3(0,Mathf.Cos(r)*.38f,Mathf.Sin(r)*.64f-.625f),new Vector3(2.04f,.11f,.28f),i%2==0?woodLight:wood);
                slat.transform.localRotation=Quaternion.Euler(angle,0,0);
                foreach(float x in new[]{-.76f,.76f})
                {
                    var band=Box(pivot,"Lid iron band",new Vector3(x,Mathf.Cos(r)*.42f,Mathf.Sin(r)*.68f-.625f),new Vector3(.13f,.08f,.3f),iron);
                    band.transform.localRotation=Quaternion.Euler(angle,0,0);
                }
            }
            for(int i=0;i<14;i++) Box(t,"Gold ingot",new Vector3(-.65f+(i%5)*.3f,.85f+(i/5)*.08f,-.35f+(i%3)*.3f),new Vector3(.24f,.09f,.16f),brass);
            return obj;
        }
        private static GameObject SavePrefab(GameObject source,string name,Transform parent)
        {
            var asset=PrefabUtility.SaveAsPrefabAsset(source,Folder+"/Prefabs/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(source);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent); return instance;
        }
        public static string ValidateScene()
        {
            var flow=UnityEngine.Object.FindFirstObjectByType<AdventureFlow>();
            if(flow==null || flow.companion==null || flow.chestLid==null || flow.boss==null) throw new Exception("Gameplay references missing");
            var dog=flow.companion.GetComponent<CharacterController>(); var player=flow.player.GetComponent<CharacterController>();
            if(dog.height>=1.5f || player.height<=1.5f) throw new Exception("Passage dimensions invalid");
            if(flow.companion.GetComponent<CompanionAgent>().enabled) throw new Exception("Two dog movement drivers enabled");
            if(Need("LeverTriggerZone").GetComponent<SphereCollider>().radius>.5f) throw new Exception("Lever trigger too large");
            if(!UnityEngine.AI.NavMesh.SamplePosition(flow.player.position,out _,3,UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("Player not near NavMesh");
            return "PASS: references, dog/player clearance, single dog driver, lever range, player NavMesh. Original prefabs; no paid assets.";
        }
        [MenuItem("WesternAdventure/Polish/Diagnose Routes %&d")]
        public static void Diagnose()
        {
            var flow=UnityEngine.Object.FindFirstObjectByType<AdventureFlow>();
            var text=new System.Text.StringBuilder();
            UnityEngine.AI.NavMesh.SamplePosition(flow.companion.transform.position,out var start,5,UnityEngine.AI.NavMesh.AllAreas);
            text.AppendLine("Start="+start.position+" dog="+flow.companion.transform.position);
            UnityEngine.AI.NavMesh.SamplePosition(flow.companion.entrance.position,out var entry,5,UnityEngine.AI.NavMesh.AllAreas);
            text.AppendLine("Entry="+entry.position+" desired="+flow.companion.entrance.position);
            foreach(var c in Physics.OverlapSphere(new Vector3(-275.08f,.7f,39.52f),1))
                text.AppendLine("Obstacle at old stall: "+c.name+" layer="+c.gameObject.layer+" bounds="+c.bounds);
            for(int x=-300;x<=-210;x+=5) for(int z=25;z<=75;z+=5)
            {
                var p=new Vector3(x,0,z);
                if(!UnityEngine.AI.NavMesh.SamplePosition(p,out var end,2,UnityEngine.AI.NavMesh.AllAreas))continue;
                var path=new UnityEngine.AI.NavMeshPath();
                UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,UnityEngine.AI.NavMesh.AllAreas,path);
                if(path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)text.AppendLine("Reachable "+end.position);
            }
            File.WriteAllText("AdventureRoutes.txt",text.ToString());
        }
    }
}
