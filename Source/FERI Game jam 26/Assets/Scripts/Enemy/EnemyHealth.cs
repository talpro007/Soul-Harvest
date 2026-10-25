using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private IKnockbackable knockbackable;
    private Transform player;
    private float maxHealth;
    private bool dead;

    [Header("stats")]
    [SerializeField] private float health;
    [SerializeField] private float destroyDelay = 0f;   // seconds before he's removed (death animation / sound)

    public event System.Action Died;

    // 1 = full health, 0 = dead
    public float HealthPercent => health / maxHealth;

    public void TakeDamage(float damage)
    {
        if (dead) return;

        health -= damage;
        Debug.Log(gameObject.name + " took damage");
        if (health <= 0)
        {
            Die();
        }
        else
        {
            //push the enemy away from the player
            int dir = transform.position.x > player.position.x ? 1 : -1;
            if (knockbackable != null) knockbackable.Knockback(dir);
        }
    }

    private void Die()
    {
        dead = true;
        Debug.Log("Enemy died");
        Died?.Invoke();
        Destroy(gameObject, destroyDelay);
    }

    void Start()
    {
        maxHealth = health;
        knockbackable = GetComponent<IKnockbackable>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
}