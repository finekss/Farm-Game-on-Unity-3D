using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Enemy_0 : MonoBehaviour
{
    #region CachedComponents
    private Rigidbody rb;
    private Collider col;
    #endregion

    #region MovementSettings
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float directionChangeTime = 2f;
    #endregion

    #region PatrolSettings
    [Header("Patrol")]
    [SerializeField] private float stopChance = 0.6f;
    [SerializeField] private float stopDurationMin = 0.2f;
    [SerializeField] private float stopDurationMax = 0.6f;
    [SerializeField] private float obstacleCheckDistance = 0.7f;
    [SerializeField] private LayerMask wallLayer;
    #endregion

    #region HealthSettings
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int stunTime = 1;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    #endregion

    #region AttackSettings
    [Header("Attack")]
    [SerializeField] private float aggroRange = 2f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 1;
    #endregion

    #region RuntimeState
    private Vector3 patrolDirection;
    private float moveTimer;
    private float stopTimer;
    private bool isStopped;
    private bool isCharging;
    private float chargeCooldownTimer;

    private enum State { Patrol, Aggro, Charging, Cooldown }
    private State currentState = State.Patrol;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        CurrentHealth = maxHealth;
        ChooseNewDirection();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        chargeCooldownTimer = Mathf.Max(0f, chargeCooldownTimer - Time.fixedDeltaTime);
        UpdateState();
        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Aggro: break; // TODO
            case State.Charging: break; // TODO
            case State.Cooldown: break; // TODO
        }
    }
    #endregion

    #region StateMachine
    private void UpdateState()
    {
        Character player = Character.player;
        if (player == null)
        {
            currentState = State.Patrol;
            return;
        }
        float distance = Vector3.Distance(transform.position, player.transform.position);
        currentState = distance <= aggroRange ? State.Aggro : State.Patrol;
        if (isCharging) currentState = State.Charging;
        if (chargeCooldownTimer > 0f) currentState = State.Cooldown;
    }
    #endregion

    #region Patrol
    private void Patrol()
    {
        if (isStopped)
        {
            stopTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector3.zero;
            if (stopTimer <= 0f)
            {
                isStopped = false;
                ChooseNewDirection();
            }
            return;
        }
        moveTimer -= Time.fixedDeltaTime;
        if (Physics.Raycast(col.bounds.center, patrolDirection, obstacleCheckDistance, wallLayer))
        {
            ChooseNewDirection();
            return;
        }
        rb.linearVelocity = patrolDirection * moveSpeed;
        if (moveTimer <= 0f)
        {
            if (Random.value < stopChance) StartStop();
            else ChooseNewDirection();
        }
    }

    private void ChooseNewDirection()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(rand.x, 0f, rand.y);
            if (!Physics.Raycast(transform.position, dir, obstacleCheckDistance, wallLayer))
            {
                patrolDirection = dir;
                moveTimer = directionChangeTime;
                return;
            }
        }
        patrolDirection = Vector3.zero;
    }

    private void StartStop()
    {
        isStopped = true;
        stopTimer = Random.Range(stopDurationMin, stopDurationMax);
        rb.linearVelocity = Vector3.zero;
    }
    #endregion

    #region Health
    public void ApplyDamage(int dmg) => StartCoroutine(TakeDamageRoutine(dmg));

    private IEnumerator TakeDamageRoutine(int dmg)
    {
        rb.linearVelocity = Vector3.zero;
        CurrentHealth -= dmg;
        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Destroy(gameObject);
            yield break;
        }
        yield return new WaitForSeconds(stunTime);
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
    #endregion
}
