using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using WesternAdventure.Core;

namespace WesternAdventure.EditorTools
{
    [InitializeOnLoad]
    public static class AdventureSmokeTest
    {
        private static double deadline;
        private static int stage;
        private static AdventureFlow flow;
        private static float originalGateY;
        private static string results = "";
        static AdventureSmokeTest()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Tick; SessionState.SetBool("AdventureSmoke", false); }
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AdventureSmoke", false))
                { stage = 0; results = ""; deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Tick; }
            };
        }
        [MenuItem("WesternAdventure/Polish/Run Gameplay Checks %&t")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) return;
            EditorSceneManager.OpenScene("Assets/Scenes/WesternMap.unity");
            SessionState.SetBool("AdventureSmoke", true);
            EditorApplication.isPlaying = true;
        }
        [MenuItem("WesternAdventure/Polish/Inspect Guard Check %&g")]
        public static void InspectGuard() { SessionState.SetBool("InspectGuard",true); Run(); }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            results += "PASS: " + message + "\n";
        }
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        private static void Teleport(CharacterController cc, Vector3 position)
        { cc.enabled = false; cc.transform.position = position; cc.enabled = true; Physics.SyncTransforms(); }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.isPaused) return;
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out at stage " + stage + "; dog=" + flow.companion.transform.position);
                if (stage == 0)
                {
                    flow = UnityEngine.Object.FindFirstObjectByType<AdventureFlow>();
                    if (flow == null) return;
                    foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None)) enemy.enabled=false;
                    Check(Time.timeScale == 0 && !flow.player.GetComponent<PlayerCombat>().enabled, "Title pauses combat and time");
                    var path = new NavMeshPath();
                    NavMesh.SamplePosition(flow.companion.transform.position, out var from, 3, NavMesh.AllAreas);
                    NavMesh.SamplePosition(flow.companion.entrance.position, out var to, 3, NavMesh.AllAreas);
                    Check(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Dog spawn has complete path to entrance");
                    Invoke(flow, "ShowIntro");
                    for(int i=0;i<5;i++) Invoke(flow,"NextIntro");
                    Check(Time.timeScale == 1 && flow.player.GetComponent<PlayerCombat>().enabled, "Intro finishes into gameplay");
                    Invoke(flow, "Pause"); Check(Time.timeScale == 0, "Pause stops world"); Invoke(flow, "Resume");
                    var playerAnimator=flow.player.GetComponentInChildren<Animator>();
                    playerAnimator.SetBool("IsRunning",true);
                    flow.player.GetComponent<PlayerMovement>().enabled=false;
                    Check(!playerAnimator.GetBool("IsRunning"), "Disabling movement clears running animation");
                    var pc=flow.player.GetComponent<CharacterController>();
                    Teleport(pc,flow.companion.entrance.position + Vector3.up * 1.02f);
                    for(int i=0;i<100;i++) pc.Move(new Vector3(0,-.02f,.04f));
                    Check(pc.transform.position.z < flow.companion.entrance.position.z + 1.1f, "Player cannot pass beneath lintel");
                    Teleport(pc,flow.companion.entrance.position + new Vector3(2,1.02f,-1));
                    Check(!flow.returnGuard.gameObject.activeInHierarchy, "Guard hidden before boss defeat");
                    flow.companion.CommandClearObstacle(flow.companion.leverTarget);
                    Check(!flow.companion.IsBusy, "Lever command blocked while four rivals live");
                    for(int i=0;i<3;i++) flow.approachEnemies[i].TakeDamage(10000);
                    flow.companion.CommandClearObstacle(flow.companion.leverTarget);
                    Check(!flow.companion.IsBusy && !flow.CanSendCompanion, "Three defeated rivals do not unlock lever");
                    flow.approachEnemies[3].TakeDamage(10000);
                    Check(flow.CanSendCompanion, "Fourth defeated rival unlocks lever");
                    originalGateY=GameObject.Find("TunnelGate").transform.position.y;
                    flow.companion.CommandClearObstacle(flow.companion.leverTarget);
                    stage=1;
                }
                else if(stage==1 && flow.companion.LeverOpened)
                { Check(true,"Dog physically reached and operated lever"); stage=2; }
                else if(stage==2 && !flow.companion.IsBusy)
                {
                    Check(GameObject.Find("TunnelGate").transform.position.y > originalGateY,"Lever raised gate");
                    Check(flow.companion.transform.position.z < flow.companion.entrance.position.z + .4f,"Dog returned through passage");
                    Teleport(flow.player.GetComponent<CharacterController>(),flow.chest.position + new Vector3(0,1.1f,-2));
                    flow.boss.TakeDamage(10000); Check(flow.boss.IsDead,"Boss defeat remains functional");
                    flow.StartCoroutine((System.Collections.IEnumerator)flow.GetType().GetMethod("OpenTreasure",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,null)); stage=3;
                }
                else if(stage==3 && flow.TreasureCollected)
                {
                    Check(Time.timeScale==1, "Boss and chest do not end the game");
                    Check(flow.returnGuard.gameObject.activeInHierarchy, "Guard appears after boss defeat");
                    Check(Mathf.Abs(flow.returnGuard.transform.position.x-flow.returnGate.position.x)<3 && Mathf.Abs(flow.returnGuard.transform.position.z-flow.returnGate.position.z)<1, "Guard positioned inside tunnel");
                    Check(!flow.returnGuard.IsConversationOpen && !flow.CanMeetGuard, "Guard waits for return through gate");
                    Check(Quaternion.Angle(flow.chestLid.localRotation,Quaternion.identity)>90, "Treasure lid opens");
                    var pc=flow.player.GetComponent<CharacterController>();
                    Teleport(pc,new Vector3(flow.returnGate.position.x+4,1.1f,flow.returnGate.position.z));
                    for(int i=0;i<200;i++) pc.Move(new Vector3(-.05f,-.02f,0));
                    Check(pc.transform.position.x < flow.returnGate.position.x-3, "Player physically crosses opened gate on return");
                    stage=4;
                }
                else if(stage==4 && flow.returnGuard.IsConversationOpen)
                {
                    Check(flow.ReturnedThroughGate,"Guard conversation starts after return");
                    Check(!flow.player.GetComponent<PlayerCombat>().enabled, "Guard conversation disables punches");
                    Check(!flow.player.GetComponentInChildren<Animator>().GetBool("IsRunning"), "Player is not running during guard dialogue");
                    // Exercise the actual encounter's success event, independently of nondeterministic LLM text.
                    if (SessionState.GetBool("InspectGuard",false))
                    { SessionState.SetBool("InspectGuard",false); EditorApplication.isPaused=true; stage=6; }
                    else { GameEvents.RaiseNpcUnlockedDoor(); stage=5; }
                }
                else if(stage==6) { GameEvents.RaiseNpcUnlockedDoor(); stage=5; deadline=EditorApplication.timeSinceStartup+30; }
                else if(stage==5 && Time.timeScale==0)
                {
                    Check(flow.returnGuard.Passed && !flow.returnGuard.IsConversationOpen, "Victory waits for guard resolution and final line");
                    Finish();
                }
            }
            catch(Exception e) { results += "FAIL: " + e + "\n"; Finish(); }
        }
        private static void Finish()
        {
            File.WriteAllText("AdventureSmokeReport.txt",results);
            Debug.Log(results);
            SessionState.SetBool("AdventureSmoke",false); EditorApplication.update-=Tick;
            EditorApplication.isPlaying=false;
        }
    }
}
