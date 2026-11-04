using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WesternAdventure.Core;

[InitializeOnLoad]
public static class BossCombatChecks
{
    static BossController boss;
    static Animator animator;
    static PlayerHealth health;
    static Transform player;
    static int scenario, phase, initialHealth;
    static double deadline;
    static float started;
    static string report;
    static bool sawAttack, changed;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static BossCombatChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Tick; SessionState.SetBool("BossChecks", false); }
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BossChecks", false))
            { phase = 0; scenario = 0; report = ""; deadline = EditorApplication.timeSinceStartup + 60; EditorApplication.update += Tick; }
        };
    }
    [MenuItem("WesternAdventure/Polish/Run Boss Timing Checks %&h")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/WesternMap.unity");
        SessionState.SetBool("BossChecks", true);
        EditorApplication.isPlaying = true;
    }
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception(message); report += "PASS: " + message + "\n"; }
    static void MovePlayer(Vector3 p)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false; player.position = p; Physics.SyncTransforms();
    }
    static void Prepare()
    {
        boss.enabled = false;
        animator.speed = scenario == 1 ? .5f : 1f;
        animator.Play("Idle", 0, 0); animator.Update(0);
        MovePlayer(boss.transform.position + boss.transform.forward * 2.5f + Vector3.up * .5f);
        initialHealth = health.CurrentHealth;
        sawAttack = false; changed = false; started = Time.time;
        boss.enabled = true; phase = 1;
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.isPaused) return;
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout scenario=" + scenario + " phase=" + phase);
            if (phase == 0)
            {
                var flow = UnityEngine.Object.FindFirstObjectByType<AdventureFlow>();
                if (flow == null) return;
                typeof(AdventureFlow).GetMethod("Resume", Private).Invoke(flow, null);
                flow.enabled = false;
                foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None)) e.enabled = false;
                boss = UnityEngine.Object.FindFirstObjectByType<BossController>();
                animator = boss.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                player = flow.player.transform; health = player.GetComponent<PlayerHealth>();
                player.GetComponent<PlayerMovement>().enabled = false;
                player.GetComponent<PlayerCombat>().enabled = false;
                typeof(PlayerHealth).GetField("regenerationPerSecond", Private).SetValue(health, 0f);
                typeof(BossController).GetField("combatDuration", Private).SetValue(boss, 100f);
                Prepare(); return;
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Attack")) sawAttack = true;
            if (scenario < 2)
            {
                if (health.CurrentHealth < initialHealth && !changed)
                {
                    Check(state.IsName("Attack") && state.normalizedTime >= .50f && state.normalizedTime < .65f,
                        "Damage matches impact frame at animator speed " + animator.speed + ", normalized=" + state.normalizedTime);
                    Check(initialHealth - health.CurrentHealth == 20, "Exactly one 20 HP impact");
                    int hp = health.CurrentHealth; boss.OnAttackImpact();
                    Check(health.CurrentHealth == hp, "Duplicate impact cannot deal damage twice");
                    changed = true;
                }
                if (sawAttack && !state.IsName("Attack") && !animator.IsInTransition(0))
                {
                    Check(changed && health.CurrentHealth == initialHealth - 20, "Full swing completes with one hit");
                    scenario++; Prepare();
                }
            }
            else
            {
                if (sawAttack && state.normalizedTime > .2f && !changed)
                {
                    if (scenario == 2) MovePlayer(player.position + boss.transform.forward * 10f);
                    if (scenario == 3) { animator.ResetTrigger("Attack"); animator.CrossFadeInFixedTime("Idle", .05f); }
                    if (scenario == 4) boss.enabled = false;
                    changed = true;
                }
                CheckNoDamage();
                if (changed && Time.time - started > 2.8f)
                {
                    Check(health.CurrentHealth == initialHealth, scenario == 2 ? "Escaped swing misses" : scenario == 3 ? "Interrupted swing does not damage" : "Disabled boss does not damage");
                    scenario++;
                    if (scenario == 5) Finish(); else Prepare();
                }
                // Prevent the interrupted swing from starting another attack during this case.
                if (scenario == 3 && changed && !state.IsName("Attack")) boss.enabled = false;
            }
        }
        catch (Exception e) { report += "FAIL: " + e + "\n"; Finish(); }
    }
    static void CheckNoDamage()
    { if (health.CurrentHealth != initialHealth) throw new Exception("Unexpected damage in scenario " + scenario); }
    static void Finish()
    {
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/BossCombatChecks.txt", report);
        Debug.Log(report); EditorApplication.update -= Tick;
        SessionState.SetBool("BossChecks", false); EditorApplication.isPlaying = false;
    }
}
