using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFight : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Collider2D bossHitbox;

    [Header("Teleport Points (ORDER MATTERS)")]
    public Transform[] playerTeleportPoints;
    public Transform[] bossTeleportPoints;

    [Header("Obstacles")]
    public GameObject miniCourseObstacle;
    public GameObject sawObstacle;
    public BossJumpAttack jumpAttack;

    [Header("Boss Settings")]
    public int bossHealth = 3;
    public float obstacleDuration = 15f;
    public float vulnerableTime = 30f;
    public float timeBetweenStages = 60f;

    private int stageIndex = 0;
    private bool canBeHit = false;
    private bool wasHit = false;
    private bool hitLocked = false;

    void Start()
    {
        Debug.Log("[BossFight] Fight started");

        if (!player) Debug.LogError("[BossFight] Player NOT assigned!");
        if (!animator) Debug.LogError("[BossFight] Animator NOT assigned!");
        if (!bossHitbox) Debug.LogError("[BossFight] BossHitbox NOT assigned!");

        bossHitbox.enabled = false;
        DisableAllObstacles();

        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (bossHealth > 0)
        {
            Debug.Log($"[BossFight] Waiting {timeBetweenStages}s before stage {stageIndex}");
            yield return new WaitForSeconds(timeBetweenStages);

            Debug.Log($"[BossFight] Starting Stage {stageIndex}");

            TeleportBoss();
            TeleportPlayer();

            yield return RunObstacle();
            yield return VulnerablePhase();

            if (!wasHit)
            {
                Debug.Log("[BossFight] Player FAILED → repeating stage");
                continue;
            }

            Debug.Log("[BossFight] Player SUCCESS → next stage");

            wasHit = false;
            hitLocked = false;
            stageIndex = (stageIndex + 1) % playerTeleportPoints.Length;
        }

        Die();
    }

    void TeleportBoss()
    {
        if (bossTeleportPoints.Length == 0) return;

        transform.position = bossTeleportPoints[stageIndex].position;
        animator.SetTrigger("Teleport");

        Debug.Log($"[BossFight] Boss teleported to stage {stageIndex}");
    }

    void TeleportPlayer()
    {
        if (!player || playerTeleportPoints.Length == 0) return;

        player.position = playerTeleportPoints[stageIndex].position;

        Debug.Log($"[BossFight] Player teleported to stage {stageIndex}");
    }

    IEnumerator RunObstacle()
    {
        DisableAllObstacles();

        Debug.Log($"[BossFight] Running obstacle for stage {stageIndex}");

        if (stageIndex == 0 && miniCourseObstacle)
        {
            miniCourseObstacle.SetActive(true);
            Debug.Log("[BossFight] Mini-course obstacle enabled");
        }
        else if (stageIndex == 1 && sawObstacle)
        {
            sawObstacle.SetActive(true);
            Debug.Log("[BossFight] Saw obstacle enabled");
        }
        else if (stageIndex == 2 && jumpAttack)
        {
            jumpAttack.JumpAtPlayer();
            Debug.Log("[BossFight] Jump attack triggered");
        }

        yield return new WaitForSeconds(obstacleDuration);

        DisableAllObstacles();
        animator.SetTrigger("JumpDown");

        Debug.Log("[BossFight] Obstacle ended, boss jumped down");
    }

    IEnumerator VulnerablePhase()
    {
        canBeHit = true;
        bossHitbox.enabled = true;
        animator.SetBool("Vulnerable", true);

        Debug.Log("[BossFight] Boss is VULNERABLE");

        float timer = 0f;

        while (timer < vulnerableTime && !wasHit)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        canBeHit = false;
        bossHitbox.enabled = false;
        animator.SetBool("Vulnerable", false);

        Debug.Log("[BossFight] Vulnerable phase ended");
    }

    void DisableAllObstacles()
    {
        if (miniCourseObstacle) miniCourseObstacle.SetActive(false);
        if (sawObstacle) sawObstacle.SetActive(false);
    }

    public void TakeHit()
    {
        if (!canBeHit)
        {
            Debug.Log("[BossFight] Hit ignored (not vulnerable)");
            return;
        }

        if (hitLocked)
        {
            Debug.Log("[BossFight] Hit ignored (already hit)");
            return;
        }

        hitLocked = true;
        wasHit = true;
        bossHealth--;

        bossHitbox.enabled = false;
        animator.SetTrigger("Hit");

        Debug.Log($"[BossFight] Boss HIT! Remaining health: {bossHealth}");
    }

    void Die()
    {
        Debug.Log("[BossFight] Boss DEAD");
        animator.SetTrigger("Die");
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(5);
    }
}
