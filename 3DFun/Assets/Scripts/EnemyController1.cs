using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyController – bewegt den Enemy auf ein Ziel zu und weicht dabei
/// Hindernissen im Sichtradius eigenständig aus.
///
/// Setup:
///  1. Script auf das "Enemy" GameObject legen.
///  2. Im Inspector das Ziel-Transform zuweisen (z.B. den Player).
///  3. Einen CharacterController oder Rigidbody auf dem Enemy aktivieren
///     (Script unterstützt beide – CharacterController wird bevorzugt).
///  4. Layer der Hindernisse im Inspector unter „Obstacle Layers" eintragen.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    // ── Ziel ──────────────────────────────────────────────────────────────
    [Header("Ziel")]
    [Tooltip("Transform des Ziels, auf das der Enemy zulaufen soll")]
    public Transform target;

    // ── Bewegung ──────────────────────────────────────────────────────────
    [Header("Bewegung")]
    [Tooltip("Laufgeschwindigkeit in Units/Sekunde")]
    public float moveSpeed = 3.5f;

    [Tooltip("Wie schnell der Enemy seine Richtung dreht (Grad/Sekunde)")]
    public float rotationSpeed = 120f;

    [Tooltip("Schwerkraft (nach unten)")]
    public float gravity = -9.81f;

    // ── Obstacle Avoidance ────────────────────────────────────────────────
    [Header("Obstacle Avoidance")]
    [Tooltip("Radius, in dem Hindernisse erkannt werden")]
    public float detectionRadius = 3f;

    [Tooltip("Anzahl der Raycasts, die fächerförmig vor dem Enemy gecastet werden")]
    [Range(3, 21)]
    public int rayCount = 9;

    [Tooltip("Gesamtwinkel des Fächers in Grad (z.B. 180 = Halbkreis)")]
    [Range(30f, 180f)]
    public float fieldOfView = 120f;

    [Tooltip("Layer-Maske der Objekte, die als Hindernis gelten")]
    public LayerMask obstacleLayers;

    [Tooltip("Stärke, mit der Hindernisse die Richtung beeinflussen")]
    public float avoidanceStrength = 2.5f;

    // ── Animation ─────────────────────────────────────────────────────────
    [Header("Animation")]
    [Tooltip("Name des Bool-Parameters im Animator Controller")]
    public string walkingBoolName = "isWalking";

    // ── Debug ─────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Raycasts im Scene-View anzeigen")]
    public bool showGizmos = true;

    // ── Private ───────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Animator _animator;
    private Vector3 _velocity;          // vertikale Geschwindigkeit (Gravitation)
    private Vector3 _desiredDirection;  // für Gizmos

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning("[EnemyController] Kein Animator gefunden – Animation wird übersprungen.");
    }

    void Update()
    {
        if (target == null) return;

        // ── 1. Basisrichtung zum Ziel ──────────────────────────────────
        Vector3 toTarget = (target.position - transform.position);
        toTarget.y = 0f;
        Vector3 baseDir = toTarget.normalized;

        // ── 2. Ausweich-Vektor berechnen ───────────────────────────────
        Vector3 avoidDir = ComputeAvoidanceDirection();

        // ── 3. Richtungen kombinieren ──────────────────────────────────
        Vector3 moveDir = (baseDir + avoidDir * avoidanceStrength).normalized;
        _desiredDirection = moveDir; // für Gizmos

        // ── 4. Rotation ────────────────────────────────────────────────
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // ── 5. Gravitation ─────────────────────────────────────────────
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;

        // ── 6. Bewegen ─────────────────────────────────────────────────
        Vector3 horizontalMotion = moveDir * moveSpeed * Time.deltaTime;
        Vector3 verticalMotion = new Vector3(0f, _velocity.y, 0f) * Time.deltaTime;
        _cc.Move(horizontalMotion + verticalMotion);

        // ── 7. Animation ───────────────────────────────────────────────
        if (_animator != null)
        {
            bool isMoving = horizontalMotion.sqrMagnitude > 0.0001f;
            _animator.SetBool(walkingBoolName, isMoving);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sendet Raycasts fächerförmig nach vorne und berechnet einen
    /// Ausweichvektor: je näher ein Hindernis, desto stärker die Abstoßung.
    /// </summary>
    Vector3 ComputeAvoidanceDirection()
    {
        Vector3 avoidance = Vector3.zero;
        float halfFOV = fieldOfView * 0.5f;
        float angleStep = rayCount > 1 ? fieldOfView / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFOV + angleStep * i;
            Vector3 rayDir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            if (Physics.Raycast(transform.position, rayDir, out RaycastHit hit,
                                detectionRadius, obstacleLayers))
            {
                // Abstoßung: weg vom Hindernis, gewichtet nach Nähe
                float weight = 1f - (hit.distance / detectionRadius); // 0..1
                Vector3 repulse = (transform.position - hit.point).normalized;
                repulse.y = 0f;
                avoidance += repulse * weight;
            }
        }

        return avoidance;
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Sichtradius
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Raycasts
        float halfFOV = fieldOfView * 0.5f;
        float angleStep = rayCount > 1 ? fieldOfView / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFOV + angleStep * i;
            Vector3 rayDir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            bool hit = Physics.Raycast(transform.position, rayDir, out RaycastHit info,
                                       detectionRadius, obstacleLayers);

            Gizmos.color = hit ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position,
                           rayDir * (hit ? info.distance : detectionRadius));
        }

        // Gewünschte Bewegungsrichtung
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _desiredDirection * 2f);
        }
    }
}