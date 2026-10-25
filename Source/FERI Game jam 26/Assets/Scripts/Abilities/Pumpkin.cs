using UnityEngine;

public class Pumpkin : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 300f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;
    private bool used;

    void Awake()
    {
        rb =GetComponent<Rigidbody2D>();
        used = false;
    }

    public void Launch(int dir)
    {
        rb.linearVelocityX = dir * speed;
        

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) { return; }

        if (other.GetComponentInParent<PlayerMovement>() != null) return;
        if (other.GetComponentInParent<Pumpkin>() != null) return;

        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(damage);

        Debug.Log($"Pumpkin hit {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)}, trigger: {other.isTrigger})", other);

        used = true;
        Destroy(gameObject);
    }

}
