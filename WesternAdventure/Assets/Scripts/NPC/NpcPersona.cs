using System.Collections.Generic;
using UnityEngine;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// NPC'nin kişiliğini ve amacını tasarımcının (programcı olmadan) Inspector'dan
    /// düzenleyebilmesi için ScriptableObject olarak tutulur. Rapor gereksinimi
    /// gereği bu tanım README'de de paylaşılmalıdır.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNpcPersona", menuName = "WesternAdventure/NPC Persona")]
    public class NpcPersona : ScriptableObject
    {
        [SerializeField] private string npcName = "Tünel Muhafızı";

        [TextArea(3, 6)]
        [SerializeField] private string backstoryAndGoal =
            "Sen, Golem'in tünelindeki çıkışı bekleyen sert, güvenmeyen bir muhafızsın. Oyuncu Golem'i yendi, hazineyi aldı ve tünelden geri dönüyor; sen önünü kestin. Hazineyi gerçekten hak edip etmediğine, oyuncunun sana anlatacaklarına bakarak sen karar vereceksin. İkna olursan kenara çekilip hazineyle geçmesine izin verirsin (open_door); olmazsan onu alt edersin (attack_player).";

        [SerializeField] private int maxConversationTurns = 3;

        public int MaxConversationTurns => maxConversationTurns;

        [Tooltip("Son tur dışındaki her turda muhafızın soracağı sorgu sorusu (sırayla). Küçük yerel " +
                 "modeller serbest 'takip sorusu üret' talimatında konudan sapıp kendinden bahsetmeye " +
                 "başlıyordu; sabit bir soru iskeleti sorgulamayı tutarlı tutuyor.")]
        [SerializeField] private string[] interrogationQuestions =
        {
            "Sana neden güveneyim? Daha önce kimin için ne yaptın?",
            "Sözünü tutacağını nereden bileyim?"
        };

        [Header("Karar repliği (kod tarafı, kararla her zaman uyumlu)")]
        [SerializeField] private string passLine = "Sözlerin tutarlı, niyetin temiz. Geç bakalım yolcu, hazine senin.";
        [SerializeField] private string failLine = "Seni dinledim ama ikna olmadım. Bu hazineyle buradan geçemezsin!";
        [SerializeField] private string insultLine = "Bana bu şekilde konuşmanın bedelini ödeyeceksin!";

        public string PassLine => passLine;
        public string FailLine => failLine;
        public string InsultLine => insultLine;

        /// <summary>turn. cevaptan sonra muhafızın soracağı sorgu sorusu (son tur için yok).</summary>
        public string GetQuestionAfterTurn(int turn)
        {
            if (interrogationQuestions == null || interrogationQuestions.Length == 0) return "Sana neden güveneyim?";
            return interrogationQuestions[Mathf.Clamp(turn - 1, 0, interrogationQuestions.Length - 1)];
        }

        /// <summary>
        /// Muhafız her turda konuşmanın tamamını (soru/cevap dökümü) okuyup değerlendirme alanlarını
        /// doldurur ve son cevaba kısa bir tepki yazar. Karar ve sıradaki soru kod tarafındadır.
        /// </summary>
        public string BuildSystemPrompt(int currentTurn)
        {
            return
                $"Sen bir western macera oyunundaki NPC'sin. İsmin: {npcName}.\n" +
                $"Karakterin ve amacın: {backstoryAndGoal}\n" +
                $"Oyuncuyu sorguluyorsun. Şu an oyuncunun {currentTurn}. cevabındayız (en fazla {maxConversationTurns}).\n\n" +
                "Kurallar:\n" +
                "- SADECE aşağıdaki JSON şemasıyla cevap ver, başka hiçbir metin, açıklama veya markdown ekleme.\n" +
                "- Şema: {\"hakaret\": true|false, \"yardim_amaci\": true|false, \"guven_ornegi\": true|false, " +
                "\"celiski\": true|false, \"alakasiz\": true|false, \"reply\": string, " +
                "\"disposition\": \"friendly\"|\"neutral\"|\"hostile\"}\n" +
                "- hakaret: Oyuncu sana hakaret etti ya da kaba/saygısız konuştu mu?\n" +
                "- yardim_amaci: Oyuncu hazineyi başka insanlara (köy, aile, hastalar vb.) yardım için kullanacağını söyledi mi?\n" +
                "- guven_ornegi: Oyuncu geçmişte başka birine yardım ettiği SOMUT bir örnek anlattı mı? " +
                "(kimseye yardım etmedim derse false)\n" +
                "- celiski: Oyuncu kendi söylediklerini değiştirdi ya da birbiriyle çelişen şeyler söyledi mi? " +
                "(örn. önce 'annem için' deyip sonra 'annem yok' demek)\n" +
                "- alakasiz: Oyuncunun cevapları saçma, şaka ya da hazineyle hiç ilgisiz mi?\n" +
                "- reply: Oyuncunun SON cevabına TEK kısa cümlelik, sert bir muhafıza yakışan bir tepki. " +
                "Soru sorma, karar bildirme.\n" +
                "- reply SADECE Türkçe kelimelerden oluşmalı; İngilizce kelime kullanma.";
        }

        /// <summary>LLM'e gönderilecek kullanıcı mesajı: konuşmanın soru/cevap dökümü.</summary>
        public static string BuildTranscript(IReadOnlyList<string> questions, IReadOnlyList<string> answers)
        {
            var sb = new System.Text.StringBuilder("Konuşma:\n");
            for (int i = 0; i < answers.Count; i++)
            {
                if (i < questions.Count) sb.Append("Muhafız: ").Append(questions[i]).Append('\n');
                sb.Append("Oyuncu: ").Append(answers[i]).Append('\n');
            }
            return sb.ToString();
        }
    }
}
