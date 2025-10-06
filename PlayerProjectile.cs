using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 2f;

    public int damage = 1;

    private Vector2 direction;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        rb.linearVelocity = direction * speed;
    }

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (collision.CompareTag("Enemy"))
        //{
        //    EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        //    if (enemy != null)
        //    {
        //        enemy.TakeDamage(damage);
        //    }
        //    Destroy(gameObject);
        //}
        //else if (collision.CompareTag("Obstacle"))
        //{
        //    Destroy(gameObject);
        //}

        Destroy(gameObject);
    }
}
