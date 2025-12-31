using UnityEngine;

public class Killbrick : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[Killbrick] Trigger by {collision.name} tag={collision.tag}");

        if (!collision.CompareTag("Player")) return;

        var respawn = collision.GetComponent<PlayerRespawn>();
        if (respawn == null)
        {
            Debug.LogError("[Killbrick] PlayerRespawn missing on Player!");
            return;
        }

        Debug.Log("[Killbrick] Forcing respawn");
        respawn.ForceRespawn();
    }
}
