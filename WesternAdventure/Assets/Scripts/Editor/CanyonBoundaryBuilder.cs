using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class CanyonBoundaryBuilder
{
    public const string RootName = "Canyon Boundary";
    const string Folder = "Assets/WesternCraft/Boundaries";
    [MenuItem("WesternAdventure/Polish/Seal Canyon Boundaries %&#k")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/WesternMap.unity")
            throw new InvalidOperationException("Open WesternMap before rebuilding canyon boundaries.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/WesternCraft", "Boundaries");
        var old = GameObject.Find(RootName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Kanyon açıklıklarını kapat");
        var gate = GameObject.Find("TunnelGate").GetComponent<BoxCollider>().bounds;
        float x = gate.center.x, low = gate.min.z + .12f, high = gate.max.z - .12f;
        // Continuous loops lie within the existing outer mountain belt. The divider
        // joins both outer edges, so the door is the only route between the arenas.
        Ridge(root.transform,"South rim",new Vector2(-323,1.8f),new Vector2(-126.6f,1.8f),3.2f,0,0);
        Ridge(root.transform,"North rim",new Vector2(-323,98.2f),new Vector2(-126.6f,98.2f),3.2f,0,1);
        Ridge(root.transform,"West rim",new Vector2(-323,1.8f),new Vector2(-323,98.2f),3.2f,0,2);
        Ridge(root.transform,"East rim",new Vector2(-126.6f,1.8f),new Vector2(-126.6f,98.2f),3.2f,0,3);
        Ridge(root.transform,"Gate south ridge",new Vector2(x,1.8f),new Vector2(x,low),3.2f,0,4);
        Ridge(root.transform,"Gate north ridge",new Vector2(x,high),new Vector2(x,98.2f),3.2f,0,5);
        // Low underside meets the closed gate, with enough clearance when it opens.
        Ridge(root.transform,"Gate stone arch",new Vector2(x,low),new Vector2(x,high),3.2f,6.4f,6);
        Physics.SyncTransforms();
        foreach(var surface in UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
        {
            var previous = surface.navMeshData;
            string path = AssetDatabase.GetAssetPath(previous);
            surface.BuildNavMesh();
            if (previous != null && !string.IsNullOrEmpty(path))
            {
                EditorUtility.CopySerialized(surface.navMeshData, previous);
                surface.RemoveData(); surface.navMeshData=previous; surface.AddData(); EditorUtility.SetDirty(previous);
            }
            EditorUtility.SetDirty(surface);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CANYON_BOUNDARIES_READY: sealed outer rim and gate divider; navigation rebuilt.");
    }
    static Material Material(string name, Color color)
    {
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.05f);return m;
    }
    static void Ridge(Transform root,string name,Vector2 a,Vector2 b,float width,float bottom,int seed)
    {
        var obj=new GameObject(name);obj.transform.SetParent(root,false);obj.layer=LayerMask.NameToLayer("Environment");obj.isStatic=true;
        Vector2 delta=b-a;float length=delta.magnitude;Vector2 side=new Vector2(-delta.y,delta.x).normalized;
        // Tall collision stays within the rock footprint and also blocks climbing
        // over neighbouring imported peaks (some are taller than the terrain).
        var blocker=new GameObject("Rock boundary collision");blocker.transform.SetParent(obj.transform,false);blocker.layer=obj.layer;
        float collisionBottom=bottom==0?-5:bottom;
        blocker.transform.position=new Vector3((a.x+b.x)/2,(collisionBottom+80)/2,(a.y+b.y)/2);
        blocker.transform.rotation=Quaternion.LookRotation(new Vector3(delta.x,0,delta.y));
        blocker.AddComponent<BoxCollider>().size=new Vector3(width,80-collisionBottom,length+.12f);
        var modifier=obj.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
        var vertices=new List<Vector3>();var triangles=new List<int>[] {new List<int>(),new List<int>(),new List<int>()};
        int count=Mathf.Max(2,Mathf.CeilToInt(length/4));
        for(int i=0;i<=count;i++)
        {
            Vector2 point=Vector2.Lerp(a,b,(float)i/count);
            float noise=Mathf.PerlinNoise(i*.83f+seed*4.7f,seed+2.3f);
            float peak=10.5f+noise*4.5f;
            float[] heights={bottom==0?-3:bottom,Mathf.Max(bottom+1,4+noise*4),peak};
            for(int band=0;band<3;band++)
            {
                float radius=width*.5f+(band==0?1.5f+noise:band==1?.4f+noise*1.2f:-.1f);
                for(int face=0;face<2;face++)
                {var p=point+side*radius*(face==0?-1:1);vertices.Add(new Vector3(p.x,heights[band],p.y));}
            }
        }
        // Duplicate triangle vertices for flat shaded, angular sandstone facets.
        for(int i=0;i<count;i++)
        {
            int n=i*6,m=(i+1)*6;
            for(int band=0;band<2;band++)
            {
                Quad(triangles[band],n+band*2,m+band*2,m+band*2+2,n+band*2+2);
                Quad(triangles[band],n+band*2+1,n+band*2+3,m+band*2+3,m+band*2+1);
            }
            Quad(triangles[2],n+4,m+4,m+5,n+5);
            Quad(triangles[0],n,n+1,m+1,m);
        }
        Quad(triangles[0],0,2,3,1);Quad(triangles[1],2,4,5,3);
        int end=count*6;Quad(triangles[0],end,end+1,end+3,end+2);Quad(triangles[1],end+2,end+3,end+5,end+4);
        var flat=new List<Vector3>();var groups=new List<int>[] {new List<int>(),new List<int>(),new List<int>()};
        for(int s=0;s<3;s++) foreach(int v in triangles[s]){groups[s].Add(flat.Count);flat.Add(vertices[v]);}
        var mesh=new Mesh {name=name};mesh.SetVertices(flat);mesh.subMeshCount=3;
        for(int s=0;s<3;s++) mesh.SetTriangles(groups[s],s);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/"+name+".asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(stored==null){AssetDatabase.CreateAsset(mesh,path);stored=mesh;}else{stored.Clear(); stored.SetVertices(flat); stored.subMeshCount=3; for(int s=0;s<3;s++) stored.SetTriangles(groups[s],s); stored.RecalculateNormals(); stored.RecalculateBounds(); stored.UploadMeshData(false); EditorUtility.SetDirty(stored); UnityEngine.Object.DestroyImmediate(mesh);}
        obj.AddComponent<MeshFilter>().sharedMesh=stored;
        obj.AddComponent<MeshRenderer>().sharedMaterials=new[]{Material("Canyon ochre",new Color(.58f,.29f,.13f)),Material("Canyon pale sandstone",new Color(.66f,.51f,.29f)),Material("Canyon sunlit cap",new Color(.50f,.29f,.14f))};
    }
    static void Quad(List<int> t,int a,int b,int c,int d){t.Add(a);t.Add(c);t.Add(b);t.Add(a);t.Add(d);t.Add(c);}
}
