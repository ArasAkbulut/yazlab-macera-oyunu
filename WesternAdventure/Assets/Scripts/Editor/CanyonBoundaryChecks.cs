using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using WesternAdventure.Core;
using WesternAdventure.Companion;

[InitializeOnLoad]
public static class CanyonBoundaryChecks
{
 static int stage, probes; static double deadline; static float waitUntil;
 static string report; static CharacterController cc; static Transform gate; static Vector3 spawn;
 static CanyonBoundaryChecks()
 {
  EditorApplication.playModeStateChanged+=s=>{
   if(s==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;SessionState.SetBool("BoundaryChecks",false);}
   if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BoundaryChecks",false))
   {stage=0;probes=0;report="";deadline=EditorApplication.timeSinceStartup+45;EditorApplication.update+=Tick;}
  };
 }
 [MenuItem("WesternAdventure/Polish/Check Canyon Boundaries %&#v")]
 public static void Run()
 {
  if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  EditorSceneManager.OpenScene("Assets/Scenes/WesternMap.unity");SessionState.SetBool("BoundaryChecks",true);EditorApplication.isPlaying=true;
 }
 static void Check(bool value,string text){if(!value)throw new Exception(text);report+="PASS: "+text+"\n";}
 static void Place(Vector3 p){cc.enabled=false;cc.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
 static void Cross(Vector3 start,Vector3 direction,float distance)
 {Place(start);for(int i=0;i<80;i++)cc.Move(direction*(distance/80));probes++;}
 static bool Path(Vector3 a,Vector3 b)
 {
  if(!NavMesh.SamplePosition(a,out var from,3,NavMesh.AllAreas)||!NavMesh.SamplePosition(b,out var to,3,NavMesh.AllAreas))throw new Exception("Probe endpoints not on NavMesh");
  var path=new NavMeshPath();return NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
 }
 static void Tick()
 {
  try {
   if(EditorApplication.isPaused)return;
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Boundary checks timeout stage "+stage);
   if(stage==0)
   {
    var flow=UnityEngine.Object.FindFirstObjectByType<AdventureFlow>();if(flow==null)return;
    typeof(AdventureFlow).GetMethod("Resume",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(flow,null);flow.enabled=false;
    foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None))enemy.enabled=false;
    UnityEngine.Object.FindFirstObjectByType<BossController>().enabled=false;
    flow.companion.enabled=false;
    cc=flow.player.GetComponent<CharacterController>();spawn=cc.transform.position;
    cc.GetComponent<PlayerMovement>().enabled=false;cc.GetComponent<PlayerCombat>().enabled=false;
    gate=GameObject.Find("TunnelGate").transform;waitUntil=Time.time+.7f;stage=1;return;
   }
   if(Time.time<waitUntil)return;
   if(stage==1)
   {
    float gx=gate.position.x;
    foreach(float y in new[]{1.1f,3.5f,17f,36f})
    {
     for(float z=5;z<96;z+=2)
     {
      if(z>68.5f&&z<77.6f)continue;
      Cross(new Vector3(gx-4,y,z),Vector3.right,8);
      if(cc.transform.position.x>=gx)throw new Exception("Divider crossed z="+z+" y="+y);
      Cross(new Vector3(gx+4,y,z),Vector3.left,8);
      if(cc.transform.position.x<=gx)throw new Exception("Reverse divider crossed");
     }
     for(float x=-319;x<-130;x+=3)
     {
      Cross(new Vector3(x,y,6),Vector3.back,10);if(cc.transform.position.z<1.8f)throw new Exception("South exit");
      Cross(new Vector3(x,y,94),Vector3.forward,10);if(cc.transform.position.z>98.2f)throw new Exception("North exit");
     }
     for(float z=6;z<95;z+=3)
     {
      Cross(new Vector3(-319,y,z),Vector3.left,10);if(cc.transform.position.x<-323)throw new Exception("West exit");
      Cross(new Vector3(-131,y,z),Vector3.right,10);if(cc.transform.position.x>-126.6f)throw new Exception("East exit");
     }
     Cross(new Vector3(gx-4,y,73),Vector3.right,8);
     if(cc.transform.position.x>=gx)throw new Exception("Closed gate or arch bypass at height "+y);
    }
    Check(true,probes+" CharacterController probes block perimeter, divider and gate at ground/jump/peak heights");
    Check(!Path(new Vector3(gx-5,0,73),new Vector3(gx+5,0,73)),"Closed gate has no NavMesh detour between arenas");
    Place(spawn);
    var controller=gate.GetComponent<TunnelGateController>();
    typeof(TunnelGateController).GetMethod("HandleCompanionClearedObstacle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
    waitUntil=Time.time+6;stage=2;return;
   }
   if(stage==2)
   {
    float gx=gate.position.x;
    Check(Path(new Vector3(gx-5,0,73),new Vector3(gx+5,0,73)),"Opened gate restores NavMesh route");
    Cross(new Vector3(gx-4,1.1f,71.5f),Vector3.right,8);Check(cc.transform.position.x>gx+2,"Player passes opened gate toward boss");
    Cross(new Vector3(gx+4,1.1f,71.5f),Vector3.left,8);Check(cc.transform.position.x<gx-2,"Player returns through opened gate");
    Finish();
   }
  }catch(Exception e){report+="FAIL: "+e+"\n";Finish();}
 }
 static void Finish(){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/CanyonBoundaryChecks.txt",report);Debug.Log(report);EditorApplication.update-=Tick;SessionState.SetBool("BoundaryChecks",false);EditorApplication.isPlaying=false;}
}
