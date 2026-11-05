using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WesternAdventure.Companion;

namespace WesternAdventure.EditorTools
{
    /// <summary>
    /// CompanionTest sahnesini açar, geçici bir CompanionTrialRunner ekler (sahneye kaydetmeden)
    /// ve Play Mode'a girer. Test bitince Play Mode kendiliğinden kapanır; sonuçlar Console'da
    /// ve training/companion_trial_results.txt dosyasında olur.
    /// </summary>
    public static class CompanionTrialMenu
    {
        private const string ScenePath = "Assets/Scenes/CompanionTest.unity";

        [MenuItem("WesternAdventure/Companion 5 Deneme Testi")]
        public static void RunTrials()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var agent = Object.FindFirstObjectByType<CompanionAgent>();
            if (agent == null)
            {
                Debug.LogError("CompanionTest sahnesinde CompanionAgent bulunamadı.");
                return;
            }

            // Sahneye kalıcı obje eklemek yerine bir oturum bayrağı: Play başlayınca
            // CompanionTrialRunner kendini oluşturur (bkz. CompanionTrialRunner.Bootstrap).
            SessionState.SetBool(CompanionTrialRunner.SessionFlag, true);
            EditorApplication.isPlaying = true;
        }
    }
}
