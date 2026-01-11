using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float speed = 2f;
    public float distanceBetween = 5f;

    private float distance;

    void Update()
    {
        distance = Vector2.Distance(transform.position, player.position);

        if (distance < distanceBetween)
        {
            // Move ONLY on X axis
            float directionX = Mathf.Sign(player.position.x - transform.position.x);

            transform.position = new Vector2(
                transform.position.x + directionX * speed * Time.deltaTime,
                transform.position.y
            );

            // Optional: flip sprite
            if (directionX != 0)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * directionX;
                transform.localScale = scale;
            }
        }
    }
}
