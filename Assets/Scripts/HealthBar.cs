using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Bar Images")]
    public Image healthFill;   // Green (top layer)
    public Image damageFill;   // Red (behind green)

    [Header("Damage Animation")]
    public float damageSpeed = 1.5f;

    private float targetFill = 1f;

    void Start()
    {
        // Safety init so bars are always valid at start
        if (healthFill != null)
            healthFill.fillAmount = 1f;

        if (damageFill != null)
            damageFill.fillAmount = 1f;

        targetFill = 1f;
    }

    void Update()
    {
        // Absolute safety guard
        if (healthFill == null || damageFill == null)
            return;

        // Smoothly animate red bar down to green bar
        if (damageFill.fillAmount > targetFill)
        {
            damageFill.fillAmount = Mathf.MoveTowards(
                damageFill.fillAmount,
                targetFill,
                damageSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Updates the health bar instantly (green) and sets target for damage bar (red)
    /// </summary>
    public void SetHealth(float current, float max)
    {
        if (healthFill == null || damageFill == null)
            return;

        float fill = Mathf.Clamp01(current / max);

        targetFill = fill;
        healthFill.fillAmount = fill;
    }

    /// <summary>
    /// Fully resets both bars to 100%
    /// </summary>
    public void ResetBar()
    {
        if (healthFill == null || damageFill == null)
            return;

        targetFill = 1f;
        healthFill.fillAmount = 1f;
        damageFill.fillAmount = 1f;
    }
}
