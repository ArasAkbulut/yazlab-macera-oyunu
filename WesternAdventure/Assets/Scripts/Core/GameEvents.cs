using System;

namespace WesternAdventure.Core
{
    /// <summary>
    /// Companion AI ve LLM NPC bileşenlerinin, birbirinden habersiz biçimde
    /// oyun durumunu (kapı açma, diyalog bitişi vb.) bildirebildiği ortak olay merkezi.
    /// </summary>
    public static class GameEvents
    {
        public static event Action NpcUnlockedDoor;
        public static event Action DialogueEnded;
        public static event Action CompanionClearedObstacle;

        public static void RaiseNpcUnlockedDoor() => NpcUnlockedDoor?.Invoke();
        public static void RaiseDialogueEnded() => DialogueEnded?.Invoke();
        public static void RaiseCompanionClearedObstacle() => CompanionClearedObstacle?.Invoke();
    }
}
