using UnityEngine;

public class CountMovement : MonoBehaviour, IKnockbackable
{
    private enum State { Patrol, Chase }

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Transform player;
    private PlayerMovement playerMovement;
    private State state = State.Patrol;
    private int direction = 1;
    private float lostSightCounter;
    private float knockbackCounter;

    [Header("points")]
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;

    [Header("vision")]
    [SerializeField] private float sightRange = 6f;
    [SerializeField] private float loseRange = 9f;
    [SerializeField] private float loseSightTime = 1.5f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("stats")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float knockbackPower = 6f;
    [SerializeField] private float knockbackUpPower = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    public void Knockback(int dir)
    {
        if (dir == 0) return;
        rb.linearVelocity = new Vector2(Mathf.Sign(dir) * knockbackPower, knockbackUpPower);
        knockbackCounter = knockbackDuration;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (knockbackCounter > 0f)
        {
            knockbackCounter -= Time.deltaTime;
            return;
        }

        switch (state)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase: Chase(); break;
        }
    }

    private bool CanSeePlayer(float range)
    {
        if (playerMovement.state == PlayerState.Death) return false;
        if (Vector2.Distance(transform.position, player.position) > range) return false;
        return !Physics2D.Linecast(transform.position, player.position, obstacleLayers);
    }

    private void Patrol()
    {
        if (CanSeePlayer(sightRange))
        {
            state = State.Chase;
            lostSightCounter = 0f;
            return;
        }

        if (direction == 1 && transform.position.x >= right.position.x) direction = -1;
        else if (direction == -1 && transform.position.x <= left.position.x) direction = 1;

        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
        sprite.flipX = direction == -1;
    }

    private void Chase()
    {
        if (CanSeePlayer(loseRange))
        {
            lostSightCounter = 0f;
        }
        else
        {
            lostSightCounter += Time.deltaTime;
            if (lostSightCounter >= loseSightTime)
            {
                state = State.Patrol;
                return;
            }
        }

        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.1f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        direction = dx > 0 ? 1 : -1;
        rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
        sprite.flipX = direction == -1;
    }

    // shows the ranges in the Scene view when he's selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}