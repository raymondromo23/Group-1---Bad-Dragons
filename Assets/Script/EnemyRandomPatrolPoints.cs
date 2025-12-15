using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyRandomPatrolPoints : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Follow Settings")]
    public float followRange = 10f;
    public float attackRange = 3f;

    [Header("Patrol Settings")]
    public float patrolRadius = 8f;
    public float patrolWaitTime = 3f;
    public float chaseSpeed = 6f;
    public float movementSpeed = 4f;

    [Header("Attack Settings")]
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;
    private bool isAttacking = false;
    private float attackDuration = 2f;

    private Vector3 patrolTarget;
    private float waitTimer = 0f;


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;

        }

        SetNewPatrolPoint();
    }

    private void Update()
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
            Patrol();
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);

    }

    void Patrol()
    {

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = movementSpeed;

        animator.SetBool("IsChasing", false);
        animator.SetBool("IsAttacking", true);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;


            if (waitTimer >= patrolWaitTime)
            {
                SetNewPatrolPoint();
                waitTimer = 0f;
            }
        }
    }
    void FollowPlayer()
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

    void SetNewPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, followRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}
