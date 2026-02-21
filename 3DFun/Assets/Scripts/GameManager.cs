using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameManager – verwaltet Player, Ziel-Objekt und den Fade-Out beim Level-Neustart.
/// 
/// Setup:
///  1. Leeres GameObject erstellen, dieses Script anhängen.
///  2. Im Inspector Player Prefab und Goal Prefab zuweisen.
///  3. Ein Canvas mit einem vollflächigen schwarzen Image erstellen (FadePanel).
///     Das Image dem FadePanel-Feld zuweisen.
///  4. Goal Prefab braucht einen Collider mit „Is Trigger" = true
///     sowie das Script GoalTrigger.cs.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab des Spieler-Characters")]
    public GameObject playerPrefab;

    [Tooltip("Prefab des Ziel-Objekts")]
    public GameObject goalPrefab;

    [Header("Spawn-Positionen")]
    [Tooltip("Spawn-Position des Spielers (leer = Weltorigin)")]
    public Transform playerSpawnPoint;

    [Tooltip("Spawn-Position des Ziels (leer = Weltorigin)")]
    public Transform goalSpawnPoint;

    [Header("Fade-Einstellungen")]
    [Tooltip("Schwarzes UI-Image, das für den Fade verwendet wird")]
    public Image fadePanel;

    [Tooltip("Dauer des Fade-Outs in Sekunden")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.5f;

    // ──────────────────────────────────────────────
    // Singleton
    public static GameManager Instance { get; private set; }

    private GameObject _currentPlayer;
    private GameObject _currentGoal;
    private bool _isTransitioning = false;

    // ──────────────────────────────────────────────
    void Awake()
    {
        // Singleton-Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        SpawnObjects();
    }

    // ──────────────────────────────────────────────
    /// <summary>Instanziiert Player und Goal in die Szene.</summary>
    void SpawnObjects()
    {
        Vector3 playerPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        Quaternion playerRot = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;

        Vector3 goalPos = goalSpawnPoint != null ? goalSpawnPoint.position : new Vector3(5f, 0f, 5f);
        Quaternion goalRot = goalSpawnPoint != null ? goalSpawnPoint.rotation : Quaternion.identity;

        if (playerPrefab != null)
            _currentPlayer = Instantiate(playerPrefab, playerPos, playerRot);
        else
            Debug.LogWarning("[GameManager] Kein Player Prefab zugewiesen!");

        if (goalPrefab != null)
        {
            _currentGoal = Instantiate(goalPrefab, goalPos, goalRot);

            // GoalTrigger-Component automatisch hinzufügen, falls nicht vorhanden
            GoalTrigger trigger = _currentGoal.GetComponent<GoalTrigger>();
            if (trigger == null)
                trigger = _currentGoal.AddComponent<GoalTrigger>();
        }
        else
        {
            Debug.LogWarning("[GameManager] Kein Goal Prefab zugewiesen!");
        }
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