using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NeedleBullet : MonoBehaviour
{
    private Vector3 moveDir;
    private float moveSpeed;
    private float lifeTime;
    private int damage;

    private float timer;
    private bool initialized;

    public void Initialize(Vector3 direction, float speed, float life, int dmg)
    {
        moveDir = direction.normalized;
        moveSpeed = speed;
        lifeTime = life;
        damage = dmg;
        timer = 0f;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        BattlePlayerController player = other.GetComponent<BattlePlayerController>();
        if (player == null)
            player = other.GetComponentInParent<BattlePlayerController>();

        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}