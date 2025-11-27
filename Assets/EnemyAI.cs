using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum PatrolMode { RandomNavMesh, Waypoints }

    [Header("General")]
    public Transform player;
    public PatrolMode patrolMode = PatrolMode.RandomNavMesh;
    public bool startPatrollingOnStart = true;

    [Header("NavMeshAgent")]
    public NavMeshAgent agent;
    public float stoppingDistanceAttack = 1.5f; // fallback if Animator root motion not used

    [Header("Patrol - Random")]
    public float randomPatrolRadius = 10f;
    public float randomPatrolSampleAttempts = 10;

    [Header("Patrol - Waypoints")]
    public List<Transform> waypoints = new List<Transform>();
    public bool loopWaypoints = true;
    public bool drawWaypointLines = true;

    [Header("Patrol - shared")]
    public float waitAtPointSeconds = 2f;
    public bool waitOnStart = false;

    [Header("Chase")]
    public float chaseRange = 8f;
    public bool chaseDebug = true;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public bool attackDebug = true;

    [Header("Animator")]
    public Animator animator;
    public string paramIsWalking = "isWalking";
    public string paramIsRunning = "isRunning";
    public string paramAttackTrigger = "attackTrigger";
    public string paramIsIdle = "isIdle";

    // internal state
    private enum State { Idle, Patrol, Chase, Attack }
    private State state = State.Idle;

    private int waypointIndex = 0;
    private bool isWaiting = false;
    private bool attackOnCooldown = false;
    private Coroutine patrolCoroutine;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (player == null)
        {
            // try to find player tag
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
    }

    void Start()
    {
        if (startPatrollingOnStart)
            StartPatrol();
        else
            state = State.Idle;
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Priority transitions
        if (distToPlayer <= attackRange)
        {
            if (state != State.Attack)
            {
                TransitionToState(State.Attack);
            }
        }
        else if (distToPlayer <= chaseRange)
        {
            if (state != State.Chase && state != State.Attack)
            {
                if (chaseDebug) Debug.Log("Chasing Player");
                TransitionToState(State.Chase);
            }
        }
        else
        {
            // Player out of chase/attack range
            if (state == State.Chase || state == State.Attack)
            {
                // return to patrol
                if (chaseDebug) Debug.Log("Player left chase range. Returning to patrol.");
                TransitionToState(State.Patrol);
            }
        }

        // Behavior implementations
        switch (state)
        {
            case State.Chase:
                DoChase();
                break;
            case State.Attack:
                DoAttack();
                break;
            case State.Patrol:
                // patrol logic handled in coroutine
                break;
            case State.Idle:
                SetAnimatorIdle(true);
                break;
        }
    }

    #region State management
    void TransitionToState(State newState)
    {
        if (state == newState) return;

        // exit actions
        if (patrolCoroutine != null && (newState != State.Patrol))
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }

        // enter actions
        state = newState;

        switch (state)
        {
            case State.Patrol:
                StartPatrol();
                break;
            case State.Chase:
                agent.isStopped = false;
                agent.speed = Mathf.Max(agent.speed, 3.5f);
                SetAnimatorRunning(true);
                break;
            case State.Attack:
                agent.isStopped = true;
                SetAnimatorRunning(false);
                SetAnimatorWalking(false);
                SetAnimatorIdle(false);
                break;
            case State.Idle:
                agent.isStopped = true;
                SetAnimatorIdle(true);
                break;
        }
    }
    #endregion

    #region Patrol
    public void StartPatrol()
    {
        if (patrolCoroutine != null) StopCoroutine(patrolCoroutine);
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    IEnumerator PatrolRoutine()
    {
        state = State.Patrol;
        agent.isStopped = false;
        agent.speed = 2f;
        SetAnimatorRunning(false);

        if (waitOnStart)
        {
            isWaiting = true;
            SetAnimatorWalking(false);
            SetAnimatorIdle(true);
            yield return new WaitForSeconds(waitAtPointSeconds);
            isWaiting = false;
        }

        while (state == State.Patrol)
        {
            Vector3 targetPos = transform.position;

            if (patrolMode == PatrolMode.Waypoints && waypoints.Count > 0)
            {
                Transform wp = waypoints[waypointIndex];
                targetPos = wp.position;
                agent.SetDestination(targetPos);
                SetAnimatorWalking(true);

                // wait until close to waypoint
                while (Vector3.Distance(transform.position, targetPos) > agent.stoppingDistance + 0.1f)
                {
                    // interruption: if player enters chase/attack range, break out
                    if (Vector3.Distance(transform.position, player.position) <= chaseRange)
                        yield break;
                    yield return null;
                }

                // reached
                SetAnimatorWalking(false);
                SetAnimatorIdle(true);
                isWaiting = true;
                yield return new WaitForSeconds(waitAtPointSeconds);
                isWaiting = false;

                // increment waypoint index
                waypointIndex++;
                if (waypointIndex >= waypoints.Count)
                {
                    if (loopWaypoints) waypointIndex = 0;
                    else waypointIndex = waypoints.Count - 1; // stay at last
                }
            }
            else // RandomNavMesh patrol
            {
                Vector3 randomPoint = NavMeshUtilities.SampleRandomNavMeshPoint(transform.position, randomPatrolRadius, (int)randomPatrolSampleAttempts);
                if (randomPoint != Vector3.zero)
                {
                    targetPos = randomPoint;
                    agent.SetDestination(targetPos);
                    SetAnimatorWalking(true);

                    // go until near
                    while (Vector3.Distance(transform.position, targetPos) > agent.stoppingDistance + 0.1f)
                    {
                        if (Vector3.Distance(transform.position, player.position) <= chaseRange)
                            yield break;
                        yield return null;
                    }

                    SetAnimatorWalking(false);
                    SetAnimatorIdle(true);
                    isWaiting = true;
                    yield return new WaitForSeconds(waitAtPointSeconds);
                    isWaiting = false;
                }
                else
                {
                    // couldn't find random point; wait a bit
                    yield return new WaitForSeconds(0.5f);
                }
            }

            yield return null;
        }
    }
    #endregion

    #region Chase / Attack
    void DoChase()
    {
        if (player == null) return;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        SetAnimatorRunning(true);
        SetAnimatorWalking(false);
    }

    void DoAttack()
    {
        if (player == null) return;

        // face player
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }

        if (!attackOnCooldown)
        {
            // perform the attack
            if (attackDebug) Debug.Log("Attacked Player");
            if (animator != null) animator.SetTrigger(paramAttackTrigger);

            // TODO: apply damage here (hit detection, event, etc.)

            // set cooldown
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    IEnumerator AttackCooldownRoutine()
    {
        attackOnCooldown = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(attackCooldown);
        attackOnCooldown = false;

        // after cooldown, if player still in attack range continue attacking, else resume chase
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            // continue attacking next frame
        }
        else if (dist <= chaseRange)
        {
            TransitionToState(State.Chase);
        }
        else
        {
            TransitionToState(State.Patrol);
        }
    }
    #endregion

    #region Animator helpers
    void SetAnimatorWalking(bool v)
    {
        if (animator == null) return;
        animator.SetBool(paramIsWalking, v);
        animator.SetBool(paramIsIdle, !v);
    }

    void SetAnimatorRunning(bool v)
    {
        if (animator == null) return;
        animator.SetBool(paramIsRunning, v);
        if (v)
        {
            animator.SetBool(paramIsWalking, false);
            animator.SetBool(paramIsIdle, false);
        }
    }

    void SetAnimatorIdle(bool v)
    {
        if (animator == null) return;
        animator.SetBool(paramIsIdle, v);
        if (v)
        {
            animator.SetBool(paramIsWalking, false);
            animator.SetBool(paramIsRunning, false);
        }
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        // chase range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // waypoints
        if (patrolMode == PatrolMode.Waypoints && waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.25f);
                if (drawWaypointLines)
                {
                    Transform next = (i + 1 < waypoints.Count) ? waypoints[i + 1] : (loopWaypoints ? waypoints[0] : null);
                    if (next != null)
                        Gizmos.DrawLine(waypoints[i].position, next.position);
                }
            }
        }

        // Random patrol radius
        if (patrolMode == PatrolMode.RandomNavMesh)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, randomPatrolRadius);
        }
    }
    #endregion
}
