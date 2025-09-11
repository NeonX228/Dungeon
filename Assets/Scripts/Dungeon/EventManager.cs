using System;

namespace Dungeon
{
    public static class EventManager
    {
        public static event Action OnGenerationComplete;

        public static void TriggerGenerationComplete()
        {
            OnGenerationComplete?.Invoke();
        }
    }
}