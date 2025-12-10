using UnityEngine;

public class GostIA_Movement : MonoBehaviour
{
    private Animator animator;
    public float moveSpeed = 3f;
    private Transform player;
    private Vector3 initialPosition;
    private bool isMovingToPlayer = false;
    private bool isReturningToInitial = false;
    public float returnThreshold = 0.1f;
    public float chaseDistance = 0.5f;

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        initialPosition = transform.position;
    }

    void Update()
    {

        if (isMovingToPlayer)
        {
            MoveTowards(player.position);
            if (Vector3.Distance(transform.position, player.position) <= chaseDistance)
            {
                isMovingToPlayer = false;
                isReturningToInitial = true;
            }
        }
        else if (isReturningToInitial)
        {
            MoveTowards(initialPosition);
            if (Vector3.Distance(transform.position, initialPosition) <= returnThreshold)
            {
                isReturningToInitial = false;
                animator.SetBool("IsWalking", false);
            }
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        animator.SetBool("IsWalking", true);
        Debug.Log("IsWalking: " + animator.GetBool("IsWalking"));
    }
}