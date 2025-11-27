using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Camera playerCamera; // assign in Inspector
    private NavMeshAgent agent;

    private RaycastHit[] hits = new RaycastHit[1];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Right-click to move
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.RaycastNonAlloc(ray, hits) > 0)
            {
                agent.SetDestination(hits[0].point);
            }
        }
    }
}
