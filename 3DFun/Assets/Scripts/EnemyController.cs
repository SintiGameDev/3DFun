using UnityEngine;
using UnityEngine.SceneManagement; // Wichtig für das Laden der Endsequenz

[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    [Header("Targeting & Radius")]
    [Tooltip("Ziehen Sie das Spieler-GameObject hier hinein.")]
    public Transform player;
    [Tooltip("Ab dieser Distanz beginnt der Gegner zu laufen.")]
    public float detectionRadius = 10f;
    [Tooltip("Distanz, die als 'Berührung' gewertet wird, um die Sequenz zu starten.")]
    public float catchRadius = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;

    [Header("Scene Management")]
    [Tooltip("Der exakte Name der Szene, die geladen werden soll.")]
    public string endSceneName = "EndScene";

    private Animator _animator;
    private bool _isChasing = false;
    private bool _isGameEnded = false;

    // Animator Hashes (Performanter als String-Aufrufe im Update-Loop)
    private readonly int _idleStateHash = Animator.StringToHash("Idle");
    private readonly int _runStateHash = Animator.StringToHash("Run");

    void Start()
    {
        _animator = GetComponent<Animator>();

        // Fallback: Sucht den Spieler automatisch, falls er im Inspector nicht zugewiesen wurde
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("EnemyController: Kein Spieler zugewiesen und kein Objekt mit Tag 'Player' gefunden!");
            }
        }

        // Setzt die Startanimation
        _animator.Play(_idleStateHash);
    }

    void Update()
    {
        // Sicherheitsabbruch, wenn kein Spieler existiert oder das Spiel bereits beendet ist
        if (player == null || _isGameEnded) return;

        // Distanz zum Spieler berechnen
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= catchRadius)
        {
            // Spieler erreicht
            TriggerEndSequence();
        }
        else if (distanceToPlayer <= detectionRadius)
        {
            // Spieler im Sichtfeld -> Verfolgung aufnehmen
            ChasePlayer();
        }
        else if (_isChasing)
        {
            // Optional: Wenn der Spieler den Radius wieder verlässt, bricht der Gegner ab
            StopChasing();
        }
    }

    private void ChasePlayer()
    {
        if (!_isChasing)
        {
            _isChasing = true;
            _animator.Play(_runStateHash); // Animation wechseln
        }

        // 1. Rotation zum Spieler (auf der Y-Achse isoliert, damit der Gegner nicht kippt)
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 2. Bewegung nach vorne
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void StopChasing()
    {
        _isChasing = false;
        _animator.Play(_idleStateHash);
    }

    private void TriggerEndSequence()
    {
        _isGameEnded = true; // Verhindert mehrfaches Triggern in nachfolgenden Frames

        // Hier wird die Szene geladen
        SceneManager.LoadScene(endSceneName);
    }

    // Dieses Editor-Feature zeichnet die Radien im Scene-View zur leichteren visuellen Justierung
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}