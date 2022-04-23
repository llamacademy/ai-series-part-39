using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField]
    private Attackable Attackable;
    [SerializeField]
    private NavMeshAgent Agent;
    [SerializeField]
    private Animator Animator;
    [SerializeField]
    private EnemyState State;
    [SerializeField]
    private Transform Target;
    [SerializeField]
    private float FieldOfView = 65;
    [SerializeField]
    private float LineOfSightDistance = 7f;
    [SerializeField]
    private float IdleSpeedModifier = 0.25f;

    private float InitialSpeed;
    private Vector3 TargetLocation;

    public static NavMeshTriangulation Triangulation;

    private void Awake()
    {
        InitialSpeed = Agent.speed;
        if (Triangulation.vertices == null || Triangulation.vertices.Length == 0)
        {
            Triangulation = NavMesh.CalculateTriangulation();
        }

        Attackable.OnTakeDamage += GetAggressive;
    }

    private void GetAggressive()
    {
        if (State == EnemyState.Idle || State == EnemyState.Initial)
        {
            State = EnemyState.Chasing;
            Agent.speed = InitialSpeed;
            Animator.SetBool("HasTarget", true);
        }
    }

    private void Update()
    {
        if (Agent.enabled)
        {
            switch (State)
            {
                case EnemyState.Initial:
                    State = EnemyState.Idle;
                    break;
                case EnemyState.Idle:
                    DoIdleMovement();
                    break;
                case EnemyState.Chasing:
                    DoTargetMovement();
                    break;
                case EnemyState.Attacking:
                    DoAttack();
                    break;
            }
        }
    }

    private void DoIdleMovement()
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        if (Vector3.Distance(transform.position, Target.position) < LineOfSightDistance
            && Vector3.Dot(transform.forward, direction) >= Mathf.Cos(FieldOfView))
        {
            GetAggressive();
        }
        else
        {
            Agent.speed = InitialSpeed * IdleSpeedModifier;
            Animator.SetBool("HasTarget", false);
            
            if (Vector3.Distance(transform.position, TargetLocation) <= Agent.stoppingDistance || TargetLocation == Vector3.zero)
            {
                Vector3 triangle1 = Triangulation.vertices[Random.Range(0, Triangulation.vertices.Length)];
                Vector3 triangle2 = Triangulation.vertices[Random.Range(0, Triangulation.vertices.Length)];

                TargetLocation = Vector3.Lerp(triangle1, triangle2, Random.value);
                Agent.SetDestination(TargetLocation);
            }
        }
    }

    private void DoTargetMovement()
    {
        Animator.SetBool("HasTarget", true);
        if (Vector3.Distance(Target.position, transform.position) > (Agent.stoppingDistance + Agent.radius) * 2)
        {
            Agent.SetDestination(Target.position);
        }
        else
        {
            State = EnemyState.Attacking;
        }
    }

    private void DoAttack()
    {
        if (Vector3.Distance(Target.position, transform.position) > (Agent.stoppingDistance + Agent.radius) * 2)
        {
            Animator.SetBool("IsAttacking", false);
            State = EnemyState.Chasing;
        }
        else
        {
            Quaternion lookRotation = Quaternion.LookRotation((Target.position - transform.position).normalized);
            transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
            Animator.SetBool("IsAttacking", true);
        }
    }
}
