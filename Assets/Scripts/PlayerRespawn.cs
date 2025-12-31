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
        Debug.Log("[PlayerRespawn] ForceRespawn called");

        if (!respawning)
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

        Debug.Log($"[PlayerRespawn] Waiting {respawnDelay}s");
        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
        {
            Debug.Log($"[PlayerRespawn] Teleporting to {respawnPoint.position}");
            transform.position = respawnPoint.position;
        }
        else
        {
            Debug.LogError("[PlayerRespawn] respawnPoint is NULL");
        }

        // Reset health
        health.ResetHealth();


        yield return null;

        if (rb) rb.WakeUp();
        if (col) col.enabled = true;

        respawning = false;
        Debug.Log("[PlayerRespawn] Respawn complete");
    }
}
