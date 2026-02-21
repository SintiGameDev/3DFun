//using System; // Für die Action-Callbacks
//using UnityEngine;
//using UnityEngine.AI;
//using UnityEngine.SceneManagement;

//[RequireComponent(typeof(Animator))]
//[RequireComponent(typeof(NavMeshAgent))]
//[RequireComponent(typeof(PhysicalStatsLogic))] // Zwingend für das Asset
//public class EnemyControllerNavMesh : MonoBehaviour
//{
//    [Header("Dependencies")]
//    [Tooltip("Referenz zum NavLinkManager in der Szene.")]
//    public NavLinkManager navLinkManager;
//    public Transform player;

//    [Header("Targeting & Radius")]
//    public float detectionRadius = 15f;
//    public float catchRadius = 1.5f;

//    [Header("Patrol Setup")]
//    public Transform[] patrolPoints;
//    public float waypointTolerance = 1.0f;

//    [Header("Movement Speeds")]
//    public float runSpeed = 5f;
//    public float walkSpeed = 2f;

//    [Header("Pathfinding Optimization")]
//    [Tooltip("Wie oft pro Sekunde darf ein neuer Pfad berechnet werden? Verhindert Performance-Einbrüche.")]
//    public float pathRequestCooldown = 0.5f;

//    [Header("Scene Management")]
//    public string endSceneName = "EndScene";

//    // Components
//    private Animator _animator;
//    private NavMeshAgent _agent;
//    private PhysicalStatsLogic _physicalStats;

//    // State & Timers
//    private bool _isGameEnded = false;
//    private int _currentWaypointIndex = 0;
//    private float _lastPathRequestTime = 0f;

//    private enum EnemyState { Idle, Patrolling, Chasing }
//    private EnemyState _currentState = EnemyState.Idle;

//    // Animator Hashes
//    private readonly int _idleStateHash = Animator.StringToHash("Idle");
//    private readonly int _walkStateHash = Animator.StringToHash("Walk");
//    private readonly int _runStateHash = Animator.StringToHash("Run");

//    void Start()
//    {
//        _animator = GetComponent<Animator>();
//        _agent = GetComponent<NavMeshAgent>();
//        _physicalStats = GetComponent<PhysicalStatsLogic>();

//        if (player == null)
//        {
//            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
//            if (playerObj != null) player = playerObj.transform;
//        }

//        if (navLinkManager == null)
//        {
//            navLinkManager = FindObjectOfType<NavLinkManager>();
//            if (navLinkManager == null) Debug.LogError("Kein NavLinkManager in der Szene gefunden!");
//        }

//        if (patrolPoints != null && patrolPoints.Length > 0)
//        {
//            ChangeState(EnemyState.Patrolling);
//            RequestNewPath(patrolPoints[_currentWaypointIndex].position);
//        }
//        else
//        {
//            ChangeState(EnemyState.Idle);
//        }
//    }

//    void Update()
//    {
//        if (player == null || _isGameEnded || navLinkManager == null) return;

//        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

//        // 1. Priorität: Berührung
//        if (distanceToPlayer <= catchRadius)
//        {
//            TriggerEndSequence();
//            return;
//        }

//        // 2. Priorität: Verfolgung
//        if (distanceToPlayer <= detectionRadius)
//        {
//            ChasePlayer();
//        }
//        // 3. Priorität: Patrouille
//        else if (patrolPoints != null && patrolPoints.Length > 0)
//        {
//            Patrol();
//        }
//        else
//        {
//            ChangeState(EnemyState.Idle);
//            _agent.ResetPath();
//        }
//    }

//    private void Patrol()
//    {
//        ChangeState(EnemyState.Patrolling);
//        _agent.speed = walkSpeed;

//        // Prüfen, ob Agent am Ziel angekommen ist
//        if (!_agent.pathPending && _agent.remainingDistance <= waypointTolerance)
//        {
//            _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolPoints.Length;
//            RequestNewPath(patrolPoints[_currentWaypointIndex].position);
//        }
//    }

//    private void ChasePlayer()
//    {
//        ChangeState(EnemyState.Chasing);
//        _agent.speed = runSpeed;

//        // Pfad zum Spieler periodisch aktualisieren (Throttling für Performance)
//        if (Time.time - _lastPathRequestTime >= pathRequestCooldown)
//        {
//            RequestNewPath(player.position);
//        }
//    }

//    private void RequestNewPath(Vector3 targetPosition)
//    {
//        _lastPathRequestTime = Time.time;

//        // Nutzt die API des Assets, um den Pfad anhand der PhysicalStats zu validieren
//        navLinkManager.RequestPath(_physicalStats, targetPosition, (bool success) =>
//        {
//            if (success)
//            {
//                // Wenn das Asset den Pfad validiert hat, weisen wir den Agenten an, ihn zu laufen
//                _agent.SetDestination(targetPosition);
//            }
//            else
//            {
//                Debug.LogWarning("Kein valider Pfad für die physikalischen Parameter des Gegners gefunden.");
//            }
//        });
//    }

//    private void ChangeState(EnemyState newState)
//    {
//        if (_currentState == newState) return;
//        _currentState = newState;

//        switch (_currentState)
//        {
//            case EnemyState.Idle: _animator.Play(_idleStateHash); break;
//            case EnemyState.Patrolling: _animator.Play(_walkStateHash); break;
//            case EnemyState.Chasing: _animator.Play(_runStateHash); break;
//        }
//    }

//    private void TriggerEndSequence()
//    {
//        _isGameEnded = true;
//        _agent.isStopped = true;
//        SceneManager.LoadScene(endSceneName);
//    }

//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, detectionRadius);
//        Gizmos.color = Color.red;
//        Gizmos.DrawWireSphere(transform.position, catchRadius);
//    }
//}