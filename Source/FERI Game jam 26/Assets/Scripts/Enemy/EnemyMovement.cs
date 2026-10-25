using Unity.VisualScripting;
using UnityEngine;


public class EnemyMovement : MonoBehaviour, IKnockbackable
{
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private int direction = 1;

    [Header("points")]
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;

    [Header("stats")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float knockbackPower = 6f;
    [SerializeField] private float knockbackUpPower = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackCounter = 0f;

    public void Knockback(int dir)
    {
        if (dir == 0) return;

        //velocity is replaced so every hit feels the same
        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * knockbackPower, knockbackUpPower);

        //stun, patrol is paused for a bit
        knockbackCounter = knockbackDuration;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        //tick stun, patrol is paused while knocked back
        if (knockbackCounter > 0f)
        {
            knockbackCounter -= Time.deltaTime;
            return;
        }

        //turn around at either point
        if (direction == 1 && transform.position.x >= right.position.x)
        {
            direction = -1;
        }
        else if (direction == -1 && transform.position.x <= left.position.x)
        {
            direction = 1;
        }

        //walk, keeps vertical speed so gravity still works
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        //face the walking direction (flip this if your sprite faces left by default)
        sprite.flipX = direction == -1;
    }
}
