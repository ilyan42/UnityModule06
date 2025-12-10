using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class GostIA_Controller : MonoBehaviour
{
    // Detection
    [Header("Detection")]
    public float radiusDetection = 10f;
    [Range(0f, 180f)] public float fieldOfView = 120f; // FOV for vision
    public LayerMask layerDetection; // Player layer
    public LayerMask obstacleMask;   // Obstacles blocking line of sight

    // Combat / end
    [Header("Kill / Game Over")]
    public float killDistance = 1.5f; // distance at which we consider the player caught

    // Movement
    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float waypointReachThreshold = 0.3f;
    public float waitAtWaypointSeconds = 1f;

    // Patrol points
    [Header("Patrol")]
    public Transform[] waypoints;

    // Loss handling
    [Header("Loss Handling")]
    public float lossTimeout = 3f; // seconds to keep chasing after losing sight before returning to patrol

    private Transform player;
    private GostIA_Movement gostMovement; // Optional existing movement script; we'll manage movement here
    private bool playerDetected;
    private NavMeshAgent agent;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float lastSeenTime = -999f;

    private enum GhostState { Patrol, Chase }
    private GhostState state = GhostState.Patrol;

    private Animator animator;

    void Start()
    {
        gostMovement = GetComponent<GostIA_Movement>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = waypointReachThreshold;
            agent.autoBraking = true;
        }
        SetState(GhostState.Patrol);
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        DetectPlayer();

        // State transitions
        if (playerDetected)
        {
            lastSeenTime = Time.time;
            SetState(GhostState.Chase);
        }
        else
        {
            // If we haven't seen the player for lossTimeout, return to patrol
            if (Time.time - lastSeenTime > lossTimeout)
                SetState(GhostState.Patrol);
        }

        // Actions per state
        switch (state)
        {
            case GhostState.Patrol:
                PatrolUpdate();
                break;
            case GhostState.Chase:
                ChaseUpdate();
                break;
        }

        // Optional: keep external movement disabled since we handle movement here
        if (gostMovement != null) gostMovement.enabled = false;
    }

    void DetectPlayer()
    {
        // Distance check
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > radiusDetection)
        {
            playerDetected = false;
            return;
        }

        // FOV check
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f)
        {
            playerDetected = false;
            return;
        }

        // Line of sight check (raycast against obstacles)
        int combinedMask = obstacleMask.value | layerDetection.value;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, toPlayer, out RaycastHit hit, radiusDetection, combinedMask))
        {
            playerDetected = hit.transform.CompareTag("Player");
        }
        else
        {
            playerDetected = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radiusDetection);
        // Draw FOV cone
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * radiusDetection);
        Gizmos.DrawLine(transform.position, transform.position + right * radiusDetection);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered detection zone.");
            playerDetected = true;
        }
    }

    // --- Patrol ---
    void PatrolUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (agent != null)
        {
            agent.speed = patrolSpeed;

            // Ensure we have a destination
            if (!agent.hasPath || agent.remainingDistance <= waypointReachThreshold)
            {
                if (waitTimer <= 0f)
                {
                    // Start waiting at the waypoint
                    waitTimer = waitAtWaypointSeconds;

                }
                else
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        // Go to next waypoint
                        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                        agent.stoppingDistance = waypointReachThreshold;
                        agent.SetDestination(waypoints[currentWaypointIndex].position);
                    }
                    animator.SetBool("IsWalking", false);
                }
            }
        }
        else
        {
            // Fallback simple movement if no agent
            Transform target = waypoints[currentWaypointIndex];
            MoveTowards(target.position, patrolSpeed);
            animator.SetBool("IsWalking", true);
            Debug.Log("IsWalking: " + animator.GetBool("IsWalking"));

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= waypointReachThreshold)
            {
                if (waitTimer <= 0f)
                {
                    waitTimer = waitAtWaypointSeconds;
                }
                else
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                    }
                }
            }
        }
    }

    // --- Chase ---
    void ChaseUpdate()
    {
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0f;
            agent.SetDestination(player.position);
        }
        else
        {
            MoveTowards(player.position, chaseSpeed);

            // Face player
            Vector3 dir = (player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
            }
        }

        // Game over check
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= killDistance)
        {
            Debug.Log("Game Over");
            // Hook for future: call a GameManager or trigger UI
        }
    }

    void MoveTowards(Vector3 targetPos, float speed)
    {
        Vector3 pos = transform.position;
        Vector3 to = (targetPos - pos);
        to.y = 0f; // keep on plane
        Vector3 step = to.normalized * speed * Time.deltaTime;

        // Prevent overshoot
        if (step.sqrMagnitude > to.sqrMagnitude)
            step = to;

        transform.position = pos + step;

    }

    void SetState(GhostState newState)
    {
        if (state == newState) return;
        state = newState;
        waitTimer = 0f;

        if (agent != null)
        {
            if (state == GhostState.Patrol && waypoints != null && waypoints.Length > 0)
            {
                agent.speed = patrolSpeed;
                agent.stoppingDistance = waypointReachThreshold;
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
            else if (state == GhostState.Chase)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0f;
                agent.SetDestination(player.position);
            }
        }
    }


}
