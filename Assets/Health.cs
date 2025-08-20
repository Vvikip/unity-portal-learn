using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [Tooltip("Starting health. Leave negative to start at maxHealth.")]
    public float startHealth = -1f;
    [Tooltip("Health regenerated per second when not taking damage.")]
    public float regenPerSecond = 10f;

    [SerializeField]
    private float currentHealth;
    private bool tookDamageThisFrame;
    private bool isHandlingDeath;

    public float Current => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = (startHealth >= 0f) ? Mathf.Min(startHealth, maxHealth) : maxHealth;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        tookDamageThisFrame = true;
        if (IsDead)
        {
            OnDeath();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    void Update()
    {
        if (!IsDead)
        {
            if (!tookDamageThisFrame && currentHealth < maxHealth && regenPerSecond > 0f)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
            }
        }
        // Reset damage flag for next frame
        tookDamageThisFrame = false;
    }

    protected virtual void OnDeath()
    {
        if (isHandlingDeath) return;
        isHandlingDeath = true;
        StartCoroutine(HandleDeathSequence());
    }

    private System.Collections.IEnumerator HandleDeathSequence()
    {
        // Fade out
        yield return ScreenFader.Instance.FadeOut(1.0f);

        // Reload current scene
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);

        // Wait one frame to ensure scene is loaded
        yield return null;

        // Fade back in
        yield return ScreenFader.Instance.FadeIn(1.0f);
    }
}
