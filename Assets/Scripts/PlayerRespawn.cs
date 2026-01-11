using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public Transform respawnPoint;
    public float respawnDelay = 0.1f;

    Rigidbody2D rb;
    Collider2D col;
    HealthSystem health;
    bool respawning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        health = GetComponent<HealthSystem>();

        if (respawnPoint == null)
        {
            var rp = GameObject.FindGameObjectWithTag("Respawn");
            if (rp != null) respawnPoint = rp.transform;
        }

        Debug.Log($"[PlayerRespawn] Awake on {name} respawnPoint={(respawnPoint ? respawnPoint.name : "NULL")}");
    }

    public void ForceRespawn()
    {
        if (respawning) return;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        respawning = true;

        if (col) col.enabled = false;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep();
        }

        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        health.ResetHealth();

        yield return null;

        if (rb) rb.WakeUp();
        if (col) col.enabled = true;

        respawning = false;
    }
}
