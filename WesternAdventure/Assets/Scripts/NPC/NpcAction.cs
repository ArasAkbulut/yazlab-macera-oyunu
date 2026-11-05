using System;

namespace WesternAdventure.NPC
{
    public enum NpcActionType
    {
        None,
        OpenDoor,
        EndConversation,
        AttackPlayer
    }

    public enum NpcDisposition
    {
        Friendly,
        Neutral,
        Hostile
    }

    /// <summary>
    /// Yerel LLM'den beklenen yapılandırılmış (structured/JSON) çıktının karşılığı.
    /// Alan adları, modele sistem promptunda bildirilen JSON şemasıyla birebir eşleşmelidir
    /// (JsonUtility büyük/küçük harfe duyarlıdır).
    /// </summary>
    [Serializable]
    public class NpcAction
    {
        public string reply;
        public string disposition;
        public string action;

        // Muhafızın her turda doldurduğu değerlendirme alanları. Karar (action) bu alanlardan
        // kod tarafında türetilir: küçük yerel modelde (3B) doğrudan "action" seçtirmek, cevabı
        // olumlu olsa bile saldırı seçmesi gibi tutarsızlıklara yol açıyordu (bkz. README Bölüm 6).
        public bool hakaret;       // oyuncu hakaret/saygısızlık etti mi?
        public bool yardim_amaci;  // hazineyi başkalarına yardım için mi istiyor?
        public bool guven_ornegi;  // geçmişte birine yardım ettiği somut bir örnek verdi mi?
        public bool celiski;       // kendi söyledikleriyle çelişti mi?
        public bool alakasiz;      // cevaplar saçma/konuyla ilgisiz mi?

        /// <summary>Son turda oyuncunun ikna edip etmediği (tüm koşullar sağlanmalı).</summary>
        public bool IsConvincing => yardim_amaci && guven_ornegi && !celiski && !alakasiz && !hakaret;

        public NpcDisposition GetDisposition()
        {
            return Enum.TryParse(Normalize(disposition), true, out NpcDisposition parsed)
                ? parsed
                : NpcDisposition.Neutral;
        }

        public NpcActionType GetActionType()
        {
            return Enum.TryParse(Normalize(action), true, out NpcActionType parsed)
                ? parsed
                : NpcActionType.None;
        }

        // Model çıktısı "open_door" gibi snake_case gelir, C# enum isimleri "OpenDoor" gibi PascalCase'dir.
        private static string Normalize(string value) => value?.Replace("_", string.Empty);
    }
}
