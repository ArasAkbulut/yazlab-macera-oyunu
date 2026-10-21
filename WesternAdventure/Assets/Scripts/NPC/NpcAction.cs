using System;

namespace WesternAdventure.NPC
{
    public enum NpcActionType
    {
        None,
        OpenDoor,
        EndConversation
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
