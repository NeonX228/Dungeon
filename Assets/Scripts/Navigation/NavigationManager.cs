using Dungeon;
using UnityEngine;
using static Navigation.Classes;
namespace Navigation
{
    public class NavigationManager : MonoBehaviour
    {
        public static NavigationManager Instance { get; private set; }
        public NavNode[,] map;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            map = new NavNode[DungeonGenerator.Instance.dungeonSize.x, DungeonGenerator.Instance.dungeonSize.y];
        }
    }
}
