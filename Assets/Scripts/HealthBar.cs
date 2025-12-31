using UnityEngine;
using UnityEngine.UI;

public class HealthBarFollow : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Settings")]
    public Slider healthBarPrefab;  // Assign your Slider prefab in the inspector
    public Vector3 offset = new Vector3(0, 1, 0); // Position above the object

    private Slider healthBarInstance;
    private UnityEngine.Camera cam; // Fully qualified to avoid conflicts

    void Start()
    {
        currentHealth = maxHealth;

        // Cache the main camera
        cam = UnityEngine.Camera.main;
        if (cam == null)
        {
            Debug.LogError("No camera tagged MainCamera found in the scene!");
        }

        // Instantiate health bar UI
        if (healthBarPrefab != null)
        {
            // Make sure there is a Canvas in the scene
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("No Canvas found in the scene! Create one first.");
                return;
            }

            healthBarInstance = Instantiate(healthBarPrefab, canvasObj.transform);
            healthBarInstance.maxValue = maxHealth;
            healthBarInstance.value = currentHealth;
        }
        else
        {
            Debug.LogError("Assign a HealthBar prefab in the inspector!");
        }
    }

    void Update()
    {
        // Make health bar follow the object
        if (healthBarInstance != null && cam != null)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + offset);
            healthBarInstance.transform.position = screenPos;
        }
    }

    // Call this to reduce health
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }

    // Call this to heal
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarInstance != null)
            healthBarInstance.value = currentHealth;
    }
}
