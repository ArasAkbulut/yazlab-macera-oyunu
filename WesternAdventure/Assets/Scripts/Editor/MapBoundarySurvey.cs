using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class MapBoundarySurvey
{
 [MenuItem("WesternAdventure/Polish/Survey Map Boundaries %&j")]
 public static void Survey()
 {
  var text=new StringBuilder();
  foreach(var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) text.AppendLine("TERRAIN "+t.name+" pos="+t.transform.position+" size="+t.terrainData.size);
  foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
   if(!c.isTrigger && !(c is CharacterController)) text.AppendLine(c.name+" | "+c.GetType().Name+" | bounds="+c.bounds.ToString("F2")+" | enabled="+c.enabled);
  Directory.CreateDirectory("Logs");File.WriteAllText("Logs/MapBoundarySurvey.txt",text.ToString());
  var obj=new GameObject("Survey camera"); var cam=obj.AddComponent<Camera>();
  cam.transform.position=new Vector3(-242,180,50); cam.transform.rotation=Quaternion.Euler(90,0,0);
  cam.orthographic=true;cam.orthographicSize=90;cam.farClipPlane=400;cam.backgroundColor=Color.gray;cam.clearFlags=CameraClearFlags.SolidColor;
  var rt=new RenderTexture(1600,1200,24);cam.targetTexture=rt;cam.Render();
  var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1200),0,0);image.Apply();
  File.WriteAllBytes("Logs/MapBoundarySurvey.png",image.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(obj);
  Debug.Log("Boundary survey ready");
 }
}
