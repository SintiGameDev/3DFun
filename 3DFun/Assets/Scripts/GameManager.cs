using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameManager – verwaltet Player, Ziel-Objekt und den Fade-Out beim Level-Neustart.
/// 
/// Setup:
///  1. Leeres GameObject erstellen, dieses Script anhängen.
///  2. Im Inspector die bereits in der Szene platzierten GameObjects für
///     Player und Goal zuweisen (keine Prefabs – direkt die Scene-Objekte).
///  3. Ein Canvas mit einem vollflächigen schwarzen Image erstellen (FadePanel).
///     Das Image dem FadePanel-Feld zuweisen.
///  4. Das Goal-Objekt braucht einen Collider mit „Is Trigger" = true.
///     GoalTrigger.cs wird automatisch ergänzt, falls nicht vorhanden.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Scene-Objekte (keine Prefabs!)")]
    [Tooltip("Das Player-GameObject, das bereits in der Szene platziert ist")]
    public GameObject playerObject;

    [Tooltip("Das Ziel-GameObject, das bereits in der Szene platziert ist")]
    public GameObject goalObject;

    [Header("Fade-Einstellungen")]
    [Tooltip("Schwarzes UI-Image, das für den Fade verwendet wird")]
    public Image fadePanel;

    [Tooltip("Dauer des Fade-Outs in Sekunden")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.5f;

    // ──────────────────────────────────────────────
    // Singleton
    public static GameManager Instance { get; private set; }

    private bool _isTransitioning = false;

    // ──────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Kein DontDestroyOnLoad – der Manager lebt nur in dieser Szene,
        // da Player und Goal ebenfalls zur Szene gehören.
    }

    void Start()
    {
        // Fade-Panel zu Beginn unsichtbar
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.raycastTarget = false;
        }

        // GoalTrigger sicherstellen
        if (goalObject != null)
        {
            GoalTrigger trigger = goalObject.GetComponent<GoalTrigger>();
            if (trigger == null)
                goalObject.AddComponent<GoalTrigger>();
        }
        else
        {
            Debug.LogWarning("[GameManager] Kein Goal-Objekt zugewiesen!");
        }

        if (playerObject == null)
            Debug.LogWarning("[GameManager] Kein Player-Objekt zugewiesen!");
    }

    // ──────────────────────────────────────────────
    /// <summary>Wird vom GoalTrigger aufgerufen, sobald der Spieler kollidiert.</summary>
    public void OnGoalReached()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        StartCoroutine(FadeAndReload());
    }

    // ──────────────────────────────────────────────
    IEnumerator FadeAndReload()
    {
        // ── Fade-Out ──
        if (fadePanel != null)
        {
            fadePanel.raycastTarget = true;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                Color c = fadePanel.color;
                c.a = alpha;
                fadePanel.color = c;
                yield return null;
            }

            // Sicherheitshalber auf voll-schwarz setzen
            Color final = fadePanel.color;
            final.a = 1f;
            fadePanel.color = final;
        }
        else
        {
            // Kein Fade-Panel → kurze Pause als Fallback
            yield return new WaitForSeconds(fadeDuration);
        }

        // ── Szene neu laden ──
        _isTransitioning = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}