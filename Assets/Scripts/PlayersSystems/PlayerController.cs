using UnityEngine;
using UnityEngine.AI;

namespace PlayersSystems
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private NavMeshAgent navMeshAgent;
        [SerializeField]
        private bool isMoving;
        public bool IsMoving => isMoving;

        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public void GoToDestination(Vector3 destination)
        {
            navMeshAgent.SetDestination(destination);
        }
    }
}
