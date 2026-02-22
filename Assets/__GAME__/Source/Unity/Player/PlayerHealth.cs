using System;
using UnityEngine;

/// Здоровье персонажа

public class PlayerHealth : MonoBehaviour
{
    #region Settings

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float invulOnHitTime = 0.5f;

    #endregion

    #region State

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private float _invulTimer;

    #endregion

    #region Events

    ///Вызывается при получении урона (после снятия HP)
    public event Action<int> OnDamaged;

    /// Вызывается при смерти
    public event Action OnDied;

    #endregion

    #region PublicAPI

    ///Инициализация. Вызывается из PlayerController.Awake()
    public void Initialize()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
        _invulTimer = 0f;
    }
    
    /// Установить неуязвимость на указанное время (например, при подкате)
    /// Берётся максимум из текущего остатка и нового значения
    public void SetInvulnerable(float time)
    {
        _invulTimer = Mathf.Max(_invulTimer, time);
    }

    public void TakeDamage(int dmg)
    {
        if (_invulTimer > 0f || IsDead) return;

        CurrentHealth -= dmg;
        _invulTimer = invulOnHitTime;

        OnDamaged?.Invoke(dmg);

        if (CurrentHealth <= 0)
            Die();
    }

    #endregion

    #region Lifecycle

    private void Update()
    {
        if (!IsDead)
            _invulTimer = Mathf.Max(0f, _invulTimer - Time.deltaTime);
    }

    #endregion

    #region Internal

    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke();
    }

    #endregion
}