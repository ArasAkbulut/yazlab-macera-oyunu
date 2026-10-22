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
        [SerializeField] private string npcName = "Kapı Bekçisi";

        [TextArea(3, 6)]
        [SerializeField] private string backstoryAndGoal =
            "Sen eski bir kasaba bekçisisin. Arkandaki kapıyı sadece seni gerçekten ikna eden " +
            "ya da doğru parolayı söyleyen kişiye açarsın. Kısa, sert ve western tarzı konuşursun.";

        public string BuildSystemPrompt()
        {
            return
                $"Sen bir western macera oyunundaki NPC'sin. İsmin: {npcName}.\n" +
                $"Karakterin ve amacın: {backstoryAndGoal}\n\n" +
                "Kurallar:\n" +
                "- SADECE aşağıdaki JSON şemasıyla cevap ver, başka hiçbir metin, açıklama veya markdown ekleme.\n" +
                "- Şema: {\"reply\": string, \"disposition\": \"friendly\"|\"neutral\"|\"hostile\", \"action\": \"none\"|\"open_door\"|\"end_conversation\"}\n" +
                "- \"reply\" alanına oyuncuya söyleyeceğin, karakterine uygun, kısa (1-3 cümle) bir repliği YAZ.\n" +
                "- \"reply\" alanı SADECE Türkçe olmalı. Tek bir İngilizce kelime veya ifade bile kullanma " +
                "(\"open\", \"now\", \"welcome\" gibi kelimeler YASAK); tamamen Türkçe yaz.\n" +
                "- Oyuncu seni gerçekten ikna ederse veya doğru parolayı söylerse \"action\" alanını \"open_door\" yap.\n" +
                "- Konuşmayı bitirmek istiyorsan \"action\" alanını \"end_conversation\" yap, aksi halde \"none\" kullan.";
        }
    }
}
