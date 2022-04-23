using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.AI;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem ShootingSystem;
    [SerializeField]
    private Transform RootTransform;
    [SerializeField]
    private float AttackDelay = 0.25f;
    [SerializeField]
    private int Damage = 10;
    [SerializeField]
    private PoolableObject BulletPrefab;
    [SerializeField]
    private LayerMask LayerMask;
    [SerializeField]
    private Vector3 BulletSpread = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField]
    private float BulletSpeed = 0.25f;
    [SerializeField]
    private float BulletForce = 100;
    [SerializeField]
    private NavMeshAgent Agent;

    private HashSet<Attackable> Attackables = new HashSet<Attackable>();
    private Attackable CurrentAttackable;
    private Coroutine AttackCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        Attackable attackable = other.GetComponentInParent<Attackable>();
        if (attackable != null && attackable.Life > 0)
        {
            if (attackable.OnDie == null)
            {
                attackable.OnDie += HandleDeath;
            }
            
            Attackables.Add(attackable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Attackable attackable = other.GetComponentInParent<Attackable>();
        if (attackable != null)
        {
            attackable.OnDie = null;
            Attackables.Remove(attackable);
        }
    }

    private void HandleDeath(Attackable Attackable)
    {
        Attackables.Remove(Attackable);
    }

    private void Update()
    {
        if (Attackables.Count > 0)
        {
            Agent.updateRotation = false;
            Attackable closestAttackable = Attackables
                .OrderBy(attackable => Vector3.Distance(transform.position, attackable.transform.position))
                .First();

            Quaternion lookRotation = Quaternion.LookRotation(
                (closestAttackable.transform.position - RootTransform.position).normalized, 
                Vector3.up
            );
            RootTransform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);

            if (CurrentAttackable != closestAttackable)
            {
                CurrentAttackable = closestAttackable;

                if (AttackCoroutine != null)
                {
                    StopCoroutine(AttackCoroutine);
                }

                AttackCoroutine = StartCoroutine(Attack(closestAttackable));
            }
        }
        else
        {
            Agent.updateRotation = true;
            if (AttackCoroutine != null)
            {
                StopCoroutine(AttackCoroutine);
            }
        }
    }

    private IEnumerator Attack(Attackable Attackable)
    {
        while (Attackable != null && Attackable.Life > 0)
        {
            ShootingSystem.Play();
            WaitForSeconds Wait = new WaitForSeconds(AttackDelay);

            TrailRenderer trail = ObjectPool.CreateInstance(BulletPrefab, 10)
                .GetObject()
                .GetComponent<TrailRenderer>();

            trail.transform.position = ShootingSystem.transform.position;
            trail.Clear();
            Vector3 direction = (Attackable.transform.position - ShootingSystem.transform.position).normalized + new Vector3(
                Random.Range(-BulletSpread.x, BulletSpread.x),
                Random.Range(-BulletSpread.y, BulletSpread.y),
                Random.Range(-BulletSpread.z, BulletSpread.z)
            );
            direction.y = 0;
            direction.Normalize();

            if (Physics.Raycast(ShootingSystem.transform.position,
                direction,
                out RaycastHit hit,
                float.MaxValue,
                LayerMask))
            {
                StartCoroutine(MoveTrail(
                    Attackable, 
                    trail, 
                    hit.point, 
                    hit.collider.GetComponent<Rigidbody>(), 
                    true
                ));
            }
            else
            {
                StartCoroutine(MoveTrail(Attackable, trail, direction * 100, null, false));
            }

            yield return Wait;
        }
    }

    private IEnumerator MoveTrail(Attackable Attackable, TrailRenderer Trail, Vector3 HitPoint, Rigidbody HitBody, bool MadeImpact)
    {
        Vector3 startPosition = Trail.transform.position;
        Vector3 direction = (HitPoint - Trail.transform.position).normalized;

        float distance = Vector3.Distance(Trail.transform.position, HitPoint);
        float startingDistance = distance;

        while (distance > 0)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, HitPoint, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * BulletSpeed;

            yield return null;
        }

        Trail.transform.position = HitPoint;

        if (MadeImpact)
        {
            Attackable.TakeDamage(Damage);
            if (HitBody != null)
            {
                HitBody.AddForce(direction * BulletForce, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(Trail.time);

        Trail.gameObject.SetActive(false);
    }
}
