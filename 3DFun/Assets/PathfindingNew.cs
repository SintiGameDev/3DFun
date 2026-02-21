using UnityEngine;
using UnityEngine.AI;

public class PathFindingNew : MonoBehaviour
{
    [Header("Komponenten")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Statuswerte")]
    public float health = 100f;

    [Header("Patrouille (Wegpunkte)")]
    [Tooltip("Ziehen Sie hier Ihre leeren GameObjects in der gewünschten Reihenfolge hinein.")]
    public Transform[] waypoints;
    [Tooltip("Distanz, ab wann ein Wegpunkt als erreicht gilt.")]
    public float waypointTolerance = 1f;
    private int currentWaypointIndex = 0;

    [Header("Angriff")]
    public float timeBetweenAttacks = 2f;
    public GameObject projectile;
    private bool alreadyAttacked;

    [Header("Sicht- & Angriffsradien")]
    public float sightRange = 15f;
    public float attackRange = 5f;
    public bool playerInSightRange, playerInAttackRange;

    private void Awake()
    {
        // Sucht den Spieler automatisch, falls er "PlayerObj" heißt
        GameObject playerObj = GameObject.Find("PlayerObj");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Spieler-Objekt 'PlayerObj' nicht gefunden!");
        }

        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Sicherheitscheck
        if (player == null) return;

        // Prüfen der Radien
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // Zustandslogik
        if (!playerInSightRange && !playerInAttackRange) Patroling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInAttackRange && playerInSightRange) AttackPlayer();
    }

    //private void Patroling()
    //{
    //    // Abbruch, wenn keine Wegpunkte im Editor zugewiesen wurden
    //    if (waypoints == null || waypoints.Length == 0) return;

    //    Transform targetWaypoint = waypoints[currentWaypointIndex];

    //    // Agent zum aktuellen Wegpunkt schicken
    //    agent.SetDestination(targetWaypoint.position);

    //    // Distanz zum aktuellen Wegpunkt messen (ignoriert die Y-Achse für mehr Präzision bei Höhenunterschieden)
    //    Vector3 distanceToWaypoint = transform.position - targetWaypoint.position;
    //    distanceToWaypoint.y = 0;

    //    // Wenn der Wegpunkt erreicht ist, zum nächsten schalten
    //    if (distanceToWaypoint.magnitude < waypointTolerance)
    //    {
    //        // Modulo (%) sorgt dafür, dass der Index nach dem letzten Punkt wieder auf 0 springt
    //        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    //    }
    //}
    private void Patroling()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 1. Verhindert den Spam von SetDestination. 
        // Wir übergeben das Ziel nur, wenn der Agent aktuell ein anderes (oder gar kein) Ziel hat.
        // sqrMagnitude wird genutzt, da es keine performancelastige Wurzel zieht (im Gegensatz zu Vector3.Distance).
        if ((agent.destination - targetWaypoint.position).sqrMagnitude > 0.1f)
        {
            agent.SetDestination(targetWaypoint.position);
        }

        // 2. Native Distanzprüfung des Agenten nutzen statt fehleranfälliger Vektor-Mathematik
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;

            // Optional: Ziel direkt für den nächsten Frame zuweisen, um Verzögerungen zu minimieren
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // Gegner stoppt für den Angriff
        agent.SetDestination(transform.position);

        // Gegner dreht sich zum Spieler
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
        {
            // --- Angriffslogik ---
            if (projectile != null)
            {
                Rigidbody rb = Instantiate(projectile, transform.position + transform.forward, Quaternion.identity).GetComponent<Rigidbody>();
                rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
                rb.AddForce(transform.up * 8f, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("Kein Projektil im Editor zugewiesen!");
            }
            // --- Ende Angriffslogik ---

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Zeichnet Linien zwischen den Wegpunkten zur besseren Übersicht im Editor
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