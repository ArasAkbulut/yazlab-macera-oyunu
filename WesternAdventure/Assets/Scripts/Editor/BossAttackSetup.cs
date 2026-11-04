using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BossAttackSetup
{
    [MenuItem("WesternAdventure/Polish/Configure Boss Impact %&u")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) return;
        const string folder = "Assets/WesternCraft/Animations";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/WesternCraft", "Animations");
        const string path = folder + "/BossAttack.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            var source = AssetDatabase.LoadAllAssetsAtPath("Assets/Kevin Iglesias/Characters/Humanoid Giant/Animations/Combat/Giant@UnarmedAttack01.fbx")
                .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            clip = Object.Instantiate(source);
            clip.name = "BossAttack";
            AssetDatabase.CreateAsset(clip, path);
        }
        // The left fist reaches its furthest forward position at frame 30.
        AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent { time = 30f / clip.frameRate, functionName = "OnAttackImpact" } });
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/BossGolemController.controller");
        var machine = controller.layers[0].stateMachine;
        var attack = machine.states.First(s => s.state.name == "Attack").state;
        attack.motion = clip;
        foreach (var transition in machine.anyStateTransitions.Where(t => t.destinationState == attack))
        { transition.duration = .08f; transition.hasFixedDuration = true; transition.canTransitionToSelf = false; }
        foreach (var transition in attack.transitions)
        { transition.hasExitTime = true; transition.exitTime = 1f; transition.duration = .1f; transition.hasFixedDuration = true; }
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log("Boss impact configured: frame 30, clip length=" + clip.length);
    }
}
