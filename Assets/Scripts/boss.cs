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
    public BossJumpAttack jumpAttack; // IMPORTANT CHANGE

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
        bossHitbox.enabled = false;
        DisableAllObstacles();
        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (bossHealth > 0)
        {
            yield return new WaitForSeconds(timeBetweenStages);

            TeleportBoss();
            TeleportPlayer();

            yield return RunObstacle();

            yield return VulnerablePhase();

            if (!wasHit)
                continue; // repeat stage

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
    }

    void TeleportPlayer()
    {
        if (playerTeleportPoints.Length == 0 || player == null) return;

        player.position = playerTeleportPoints[stageIndex].position;
    }

    IEnumerator RunObstacle()
    {
        DisableAllObstacles();

        if (stageIndex == 0 && miniCourseObstacle != null)
        {
            miniCourseObstacle.SetActive(true);
        }
        else if (stageIndex == 1 && sawObstacle != null)
        {
            sawObstacle.SetActive(true);
        }
        else if (stageIndex == 2 && jumpAttack != null)
        {
            jumpAttack.JumpAtPlayer();
        }

        yield return new WaitForSeconds(obstacleDuration);

        DisableAllObstacles();
        animator.SetTrigger("JumpDown");
    }

    IEnumerator VulnerablePhase()
    {
        canBeHit = true;
        bossHitbox.enabled = true;
        animator.SetBool("Vulnerable", true);

        float timer = 0f;

        while (timer < vulnerableTime && !wasHit)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        canBeHit = false;
        bossHitbox.enabled = false;
        animator.SetBool("Vulnerable", false);
    }

    void DisableAllObstacles()
    {
        if (miniCourseObstacle) miniCourseObstacle.SetActive(false);
        if (sawObstacle) sawObstacle.SetActive(false);
    }

    public void TakeHit()
    {
        if (!canBeHit || hitLocked) return;

        hitLocked = true;
        wasHit = true;
        bossHealth--;

        bossHitbox.enabled = false; // prevent double hits
        animator.SetTrigger("Hit");
    }

    void Die()
    {
        animator.SetTrigger("Die");
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(5);
    }
}
