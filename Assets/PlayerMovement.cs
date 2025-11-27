using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Camera mainCam;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            Ray r = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit, 500f, LayerMask.GetMask("Default")))

            {
                // Move the agent to clicked point
                agent.isStopped = false;
                agent.SetDestination(hit.point);


                if (Physics.Raycast(r, out hit))
                {
                    Debug.Log("HIT: " + hit.collider.name);
                }
                else
                {
                    Debug.Log("NO HIT");
                }

            }
        }
    }
}
