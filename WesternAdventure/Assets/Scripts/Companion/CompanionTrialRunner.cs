using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using WesternAdventure.Core;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Rapor için tekrarlanabilir deneme testi: CompanionTest sahnesinde eğitilmiş modeli
    /// (Inference Only) N kez çalıştırır, her episode'un sonucunu (hedefe ulaştı mı / MaxStep
    /// zaman aşımı), süresini ve adım sayısını kaydeder. Sonunda başarı oranı ve ortalama süreyi
    /// Console'a ve repo kökündeki training/companion_trial_results.txt dosyasına yazar.
    /// Menü: WesternAdventure > Companion 5 Deneme Testi.
    /// Not: Eğitim (mlagents-learn) için Behavior Type = Default olmalı ve Academy trainer'a
    /// sadece domain yüklenirken bağlanmayı dener; Play'de bağlanmıyorsa script'leri yeniden derletin.
    /// </summary>
    public class CompanionTrialRunner : MonoBehaviour
    {
        public const string SessionFlag = "WesternAdventure.CompanionTrials";

        [SerializeField] private CompanionAgent agent;
        [SerializeField] private int trialCount = 5;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_EDITOR
            if (!UnityEditor.SessionState.GetBool(SessionFlag, false)) return;
            UnityEditor.SessionState.EraseBool(SessionFlag);
            new GameObject("CompanionTrialRunner (geçici)").AddComponent<CompanionTrialRunner>();
#endif
        }

        private struct Trial
        {
            public bool Success;
            public float Seconds;
            public int Steps;
        }

        private readonly List<Trial> _trials = new List<Trial>();
        private int _lastCompletedEpisodes;
        private int _lastStepCount;
        private float _episodeStart;
        private bool _successThisEpisode;
        private bool _finished;

        private void Awake()
        {
            if (agent == null) agent = FindFirstObjectByType<CompanionAgent>();
        }

        private void OnEnable() => GameEvents.CompanionClearedObstacle += HandleSuccess;
        private void OnDisable() => GameEvents.CompanionClearedObstacle -= HandleSuccess;

        private void Start()
        {
            _episodeStart = Time.time;
            _lastCompletedEpisodes = agent != null ? agent.CompletedEpisodes : 0;
            Debug.Log($"[Deneme] {trialCount} denemelik companion testi başladı.");
        }

        private void HandleSuccess() => _successThisEpisode = true;

        private void FixedUpdate()
        {
            // EndEpisode sonrası StepCount sıfırlanıyor; son geçerli değeri saklıyoruz.
            if (agent != null && agent.StepCount > 0) _lastStepCount = agent.StepCount;
        }

        private void Update()
        {
            if (_finished || agent == null) return;
            if (agent.CompletedEpisodes == _lastCompletedEpisodes) return;

            _lastCompletedEpisodes = agent.CompletedEpisodes;
            var trial = new Trial
            {
                Success = _successThisEpisode,
                Seconds = Time.time - _episodeStart,
                Steps = _lastStepCount
            };
            _trials.Add(trial);
            Debug.Log($"[Deneme] {_trials.Count}/{trialCount}: {(trial.Success ? "BAŞARILI" : "ZAMAN AŞIMI")}, " +
                      $"{trial.Seconds:F1} sn, {trial.Steps} adım");

            _successThisEpisode = false;
            _lastStepCount = 0;
            _episodeStart = Time.time;

            if (_trials.Count >= trialCount) Finish();
        }

        private void Finish()
        {
            _finished = true;
            int successes = 0;
            float successSeconds = 0f;
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            sb.AppendLine($"Companion deneme testi — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("Sahne: CompanionTest.unity, model: Companion.onnx (Inference Only), MaxStep: " + agent.MaxStep);
            sb.AppendLine("| # | Sonuç | Süre (sn) | Adım |");
            sb.AppendLine("|---|---|---|---|");
            for (int i = 0; i < _trials.Count; i++)
            {
                Trial t = _trials[i];
                if (t.Success) { successes++; successSeconds += t.Seconds; }
                sb.AppendLine(string.Format(inv, "| {0} | {1} | {2:F1} | {3} |",
                    i + 1, t.Success ? "Başarılı" : "Zaman aşımı", t.Seconds, t.Steps));
            }
            float rate = 100f * successes / _trials.Count;
            string avg = successes > 0 ? (successSeconds / successes).ToString("F1", inv) : "-";
            sb.AppendLine(string.Format(inv, "Başarı oranı: {0}/{1} (%{2:F0}), başarılı denemelerde ortalama süre: {3} sn",
                successes, _trials.Count, rate, avg));

            string report = sb.ToString();
            Debug.Log("[Deneme] Sonuçlar:\n" + report);

            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "training",
                    "companion_trial_results.txt"));
                File.WriteAllText(path, report, Encoding.UTF8);
                Debug.Log($"[Deneme] Sonuçlar dosyaya yazıldı: {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Deneme] Dosyaya yazılamadı: {ex.Message}");
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
