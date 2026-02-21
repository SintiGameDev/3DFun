using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using TMPro;

public class PillenImClub : MonoBehaviour
{
    // Singleton-Instanz für den globalen, referenzfreien Zugriff durch die Items
    public static PillenImClub Instance { get; private set; }

    public GameObject UIGameObject;
    public TextMeshProUGUI scoreUi;

    [Header("Referenzen")]
    [Tooltip("Das Prefab des Collectibles, das gespawnt werden soll.")]
    public GameObject collectiblePrefab;

    [Tooltip("Array der leeren GameObjects, die als Spawnpunkte dienen.")]
    public Transform[] spawnPoints;

    [Header("Spielregeln")]
    public int targetScore = 5;
    public string endSceneName = "WinScene";

    private int _currentScore = 0;
    private int _lastSpawnIndex = -1;

    private void Awake()
    {
        // Singleton-Pattern Initialisierung
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        scoreUi = UIGameObject.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("CollectibleManager: Keine Spawnpunkte zugewiesen!");
            return;
        }
        if (collectiblePrefab == null)
        {
            Debug.LogError("CollectibleManager: Kein Collectible Prefab zugewiesen!");
            return;
        }

        SpawnNextCollectible();
    }

    private void Update()
    {
        //scoreUi = UIGameObject.GetComponent<TextMeshPro>();
        //// Aktualisiert die Score-Anzeige
        //scoreUi.text = $"Pillen: {_currentScore} / {targetScore}";



    // Zuweisung (unter der Annahme, dass UIGameObject das GameObject "Score" referenziert)
    scoreUi = UIGameObject.GetComponent<TextMeshProUGUI>();

// Aktualisiert die Score-Anzeige
if (scoreUi != null)
{
    scoreUi.text = $"Pillen: {_currentScore} / {targetScore}";
}
else
{
    Debug.LogError("TextMeshProUGUI Komponente auf dem UIGameObject nicht gefunden.");
}
    }


    /// <summary>
    /// Wird vom Collectible aufgerufen, sobald der Spieler es berührt.
    /// </summary>
    public void OnCollectibleGathered()
    {
        _currentScore++;

        if (_currentScore >= targetScore)
        {
            LoadEndSequence();
        }
        else
        {
            SpawnNextCollectible();
        }
    }

    private void SpawnNextCollectible()
    {
        int nextIndex = 0;

        // Garantiert, dass das neue Item nicht am exakt selben Punkt spawnt wie das vorherige
        if (spawnPoints.Length > 1)
        {
            do
            {
                nextIndex = Random.Range(0, spawnPoints.Length);
            }
            while (nextIndex == _lastSpawnIndex);
        }

        _lastSpawnIndex = nextIndex;
        Transform spawnTransform = spawnPoints[nextIndex];

        // Instanziiert das Prefab an der Position und Rotation des gewählten Spawnpunktes
        Instantiate(collectiblePrefab, spawnTransform.position, spawnTransform.rotation);
    }

    private void LoadEndSequence()
    {
        SceneManager.LoadScene(endSceneName);
    }
}
