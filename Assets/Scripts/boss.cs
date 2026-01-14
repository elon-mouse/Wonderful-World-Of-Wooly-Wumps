using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFight : MonoBehaviour
{
    [Header("Boss Settings")]
    public int bossHealth = 3;
    public float vulnerableTime = 30f;
    public float timeBetweenPhases = 60f;

    [Header("Teleport Points")]
    public Transform[] teleportPlatforms;

    [Header("Obstacle Phases")]
    public GameObject miniCourseTrigger;
    public GameObject rollingSawSpawner;
    public GameObject jumpAttackTrigger;

    [Header("References")]
    public Animator animator;
    public Collider2D bossHitbox;

    private int currentPhase = 0;
    private bool canBeHit = false;
    private bool wasHit = false;

    void Start()
    {
        StartCoroutine(BossLoop());
    }

    IEnumerator BossLoop()
    {
        while (bossHealth > 0)
        {
            yield return new WaitForSeconds(timeBetweenPhases);

            TeleportToPlatform();
            yield return StartCoroutine(DoObstaclePhase());

            yield return StartCoroutine(VulnerablePhase());

            if (!wasHit)
            {
                // Repeat same obstacle
                continue;
            }

            wasHit = false;
            currentPhase = (currentPhase + 1) % 3;
        }

        Die();
    }

    void TeleportToPlatform()
    {
        int index = Random.Range(0, teleportPlatforms.Length);
        transform.position = teleportPlatforms[index].position;
        animator.SetTrigger("Teleport");
    }

    IEnumerator DoObstaclePhase()
    {
        DisableAllObstacles();

        if (currentPhase == 0)
        {
            miniCourseTrigger.SetActive(true);
        }
        else if (currentPhase == 1)
        {
            rollingSawSpawner.SetActive(true);
        }
        else if (currentPhase == 2)
        {
            jumpAttackTrigger.SetActive(true);
        }

        yield return new WaitForSeconds(15f); // obstacle duration

        DisableAllObstacles();
        animator.SetTrigger("JumpDown");
    }

    IEnumerator VulnerablePhase()
    {
        canBeHit = true;
        bossHitbox.enabled = true;
        animator.SetBool("Vulnerable", true);

        float timer = 0f;
        while (timer < vulnerableTime)
        {
            if (wasHit)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        canBeHit = false;
        bossHitbox.enabled = false;
        animator.SetBool("Vulnerable", false);
    }

    void DisableAllObstacles()
    {
        miniCourseTrigger.SetActive(false);
        rollingSawSpawner.SetActive(false);
        jumpAttackTrigger.SetActive(false);
    }

    public void TakeHit()
    {
        if (!canBeHit) return;

        wasHit = true;
        bossHealth--;
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
