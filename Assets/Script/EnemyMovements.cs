using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Runtime.CompilerServices;


[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovements : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    //AI Settings
    public float followRange = 8f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;
    private bool isAttacking = false;
    private float attackDuration = 2f;
    public float movementSpeed;
    public float chaseSpeed;

    //waypoints
    public Transform[] waypoints;
    private int currentWaypoint = 0;
    private float waitTimer = 0f;
    public float waitTime = 2f;

    // Update is called once per frame
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();


        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypoint].position);
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        attackTimer += Time.deltaTime;

        if (distance <= attackRange)
        {
            AttackPlayer();
        }
        else if (distance <= followRange)
        {
            FollowPlayer();
        }
        else
        {
            PatrolWaypoints();
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);

    }

    private void PatrolWaypoints()
    {
        if (waypoints.Length == 0) return;

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = movementSpeed;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsAttacking", true);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint].position);
                waitTimer = 0f;
            }
        }
    }

    private void FollowPlayer()
    {
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = chaseSpeed;

        animator.SetBool("IsChasing", true);
        animator.SetBool("IsAttacking", false);

        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        //stop movements
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;

        //face player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        //trigger attack if cooldown ready
        if (attackTimer >= attackCooldown)
        {
            isAttacking = true;
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");

            Debug.Log("Enemy attacks the player!");
            attackTimer = 0f;

            //start cooldown
            StartCoroutine(ResetAttackAfterDelay(attackDuration));
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
        animator.SetBool("IsAttacking", false);
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    private IEnumerator ResetAttackAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isAttacking) EndAttack();
    }

    // 🔹 GIZMOS SECTION 🔹
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, followRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }


}
