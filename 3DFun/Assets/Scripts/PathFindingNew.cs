using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PathFindingNew : MonoBehaviour
{
    [Header("Komponenten")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Statuswerte")]
    public float health = 100f;

    [Header("Patrouille (Wegpunkte)")]
    [Tooltip("Ziehen Sie hier Ihre leeren GameObjects in der gew¸nschten Reihenfolge hinein. F¸r Ping-Pong-Patrouille werden 2-3 Wegpunkte empfohlen.")]
    public Transform[] waypoints;
    [Tooltip("Distanz, ab wann ein Wegpunkt als erreicht gilt.")]
    public float waypointTolerance = 1f;
    private int currentWaypointIndex = 0;
    private int patrolDirection = 1; // 1 = vorw‰rts, -1 = r¸ckw‰rts

    [Header("Angriff")]
    public float timeBetweenAttacks = 2f;
    public GameObject projectile;
    private bool alreadyAttacked;

    [Header("Sicht- & Angriffsradien")]
    public float sightRange = 15f;
    public float attackRange = 2f; // Kennzeichnet den Kontakt-/Trigger-Abstand (optional)
    public bool playerInSightRange, playerInAttackRange;

    [Header("Endszene")]
    [Tooltip("Name der Szene, die geladen wird, wenn der Spieler den Gegner ber¸hrt (leer => kein Laden)")]
    public string endSceneName = "EndScene";

    private void Awake()
    {
        // Sucht den Spieler automatisch, falls er "PlayerObj" heiﬂt
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("PlayerObj");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Spieler-Objekt 'PlayerObj' nicht gefunden. Player-Transform kann im Inspector zugewiesen werden.");
            }
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent nicht gefunden.");
        }
    }

    private void Update()
    {
        // Sicherheitscheck
        if (agent == null) return;

        // Pr¸fen der Radien (nur wenn ein Spieler gesetzt ist)
        if (player != null)
        {
            playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        }
        else
        {
            playerInSightRange = false;
            playerInAttackRange = false;
        }

        // Zustandslogik:
        // - Player in Sicht => verfolgen
        // - sonst Patrouille fortsetzen (ping-pong zwischen Waypoints)
        if (playerInSightRange && player != null)
        {
            ChasePlayer();
        }
        else
        {
            Patroling();
        }
    }

    private void Patroling()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Clamp index
        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // SetDestination nur wenn Ziel sich wirklich unterscheidet (vermeidet Spam)
        if ((agent.destination - targetWaypoint.position).sqrMagnitude > 0.1f)
        {
            agent.SetDestination(targetWaypoint.position);
        }

        // Wenn Wegpunkt erreicht, weiter (ping-pong)
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            // Wenn am Ende oder Anfang, Richtung umdrehen
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                patrolDirection = -1;
            }
            else if (currentWaypointIndex == 0)
            {
                patrolDirection = 1;
            }

            currentWaypointIndex += patrolDirection;

            // Safety clamp
            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);

            // Optional: neues Ziel sofort setzen
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void ChasePlayer()
    {
        if (player == null) return;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // nicht genutzt f¸r Endszene-Trigger in dieser Implementierung
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Bei Kontakt mit dem Spieler die Endszene laden (falls gesetzt)
        if (other.transform == player || other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(endSceneName))
            {
                // Optional: kleiner Schutz gegen mehrfaches Laden
                if (!Application.isPlaying) return;
                try
                {
                    SceneManager.LoadScene(endSceneName);
                }
                catch
                {
                    Debug.LogWarning($"Szene '{endSceneName}' konnte nicht geladen werden. Pr¸fen Sie den Szenen-Namen oder f¸gen Sie die Szene den Build-Einstellungen hinzu.");
                }
            }
            else
            {
                Debug.Log("[PathFindingNew] Kein Endszene-Name angegeben (endSceneName ist leer).");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Zeichnet Linien zwischen den Wegpunkten zur besseren ‹bersicht im Editor
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null && waypoints[(i + 1) % waypoints.Length] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
                }
            }
        }
    }
}