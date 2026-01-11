using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image healthFill;   // Green (top)
    public Image damageFill;   // Red (middle)

    public float damageSpeed = 1.5f;

    private float targetFill = 1f;

    void Update()
    {
        if (damageFill.fillAmount > targetFill)
        {
            damageFill.fillAmount = Mathf.MoveTowards(
                damageFill.fillAmount,
                targetFill,
                damageSpeed * Time.deltaTime
            );
        }
    }

    public void SetHealth(float current, float max)
    {
        targetFill = current / max;
        healthFill.fillAmount = targetFill;
    }

    public void ResetBar()
    {
        targetFill = 1f;
        healthFill.fillAmount = 1f;
        damageFill.fillAmount = 1f;
    }
}
