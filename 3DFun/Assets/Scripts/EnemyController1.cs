using UnityEngine;

/// <summary>
/// EnemyController – bewegt den Enemy auf ein Ziel zu und weicht dabei
/// Hindernissen im Sichtradius eigenständig aus.
///
/// Setup:
///  1. Script auf das "Enemy" GameObject legen.
///  2. Im Inspector das Ziel-Transform zuweisen (z.B. den Player).
///  3. Layer der Hindernisse im Inspector unter „Obstacle Layers" eintragen.
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

    [Tooltip("Radius des SphereCasts – größere Werte erkennen Ecken früher")]
    [Range(0.05f, 1f)]
    public float sphereCastRadius = 0.3f;

    [Tooltip("Anzahl der SphereCasts, die fächerförmig vor dem Enemy gecastet werden")]
    [Range(3, 21)]
    public int rayCount = 11;

    [Tooltip("Gesamtwinkel des Fächers in Grad (z.B. 180 = Halbkreis)")]
    [Range(30f, 180f)]
    public float fieldOfView = 120f;

    [Tooltip("Layer-Maske der Objekte, die als Hindernis gelten")]
    public LayerMask obstacleLayers;

    [Tooltip("Stärke, mit der Hindernisse die Richtung beeinflussen")]
    public float avoidanceStrength = 2.5f;

    // ── Corner Escape ─────────────────────────────────────────────────────
    [Header("Corner Escape")]
    [Tooltip("Wie lange der Enemy kaum Fortschritt machen muss, bevor Escape greift (Sekunden)")]
    public float stuckTimeout = 0.8f;

    [Tooltip("Minimale Distanz pro Sekunde, unter der der Enemy als 'steckt' gilt")]
    public float stuckSpeedThreshold = 0.15f;

    [Tooltip("Wie lange der Escape-Impuls anhält (Sekunden)")]
    public float escapeDuration = 0.5f;

    // ── Animation ─────────────────────────────────────────────────────────
    [Header("Animation")]
    [Tooltip("Name des Bool-Parameters im Animator Controller")]
    public string walkingBoolName = "isWalking";

    // ── Footstep Audio ────────────────────────────────────────────────────
    [Header("Footstep Audio")]
    [Tooltip("AudioSource-Komponente (wird automatisch ergänzt falls leer)")]
    public AudioSource footstepAudioSource;

    [Tooltip("Library von Schrittgeräuschen – eines wird zufällig gewählt")]
    public AudioClip[] footstepClips;

    [Tooltip("Zeitintervall zwischen zwei Schritten in Sekunden")]
    [Range(0.1f, 2f)]
    public float footstepInterval = 0.4f;

    [Tooltip("Lautstärke der Schrittgeräusche")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.8f;

    [Tooltip("Zufällige Lautstärkenvariation (+/-) für natürlicheren Klang")]
    [Range(0f, 0.3f)]
    public float volumeVariation = 0.1f;

    [Tooltip("Zufällige Pitch-Variation (+/-) für natürlicheren Klang")]
    [Range(0f, 0.3f)]
    public float pitchVariation = 0.1f;

    // ── Voice Lines ───────────────────────────────────────────────────────
    [Header("Voice Lines")]
    [Tooltip("Library von gesprochenen Soundbites – einer wird zufällig gewählt")]
    public AudioClip[] voiceClips;

    [Tooltip("Minimale Wartezeit zwischen zwei Voice Lines in Sekunden")]
    [Range(1f, 30f)]
    public float voiceIntervalMin = 5f;

    [Tooltip("Maximale Wartezeit zwischen zwei Voice Lines in Sekunden")]
    [Range(1f, 60f)]
    public float voiceIntervalMax = 15f;

    [Tooltip("Lautstärke der Voice Lines")]
    [Range(0f, 1f)]
    public float voiceVolume = 1f;

    // ── Debug ─────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Raycasts im Scene-View anzeigen")]
    public bool showGizmos = true;

    // ── Private ───────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Animator _animator;
    private Vector3 _velocity;
    private Vector3 _desiredDirection;

    // Corner-Escape State
    private float _stuckTimer;
    private float _escapeTimer;
    private Vector3 _escapeDirection;
    private Vector3 _lastPosition;

    // Footstep State
    private float _footstepTimer;
    private int _lastFootstepIndex = -1;

    // Voice Line State
    private AudioSource _voiceAudioSource;
    private float _voiceTimer;
    private int _lastVoiceIndex = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning("[EnemyController] Kein Animator gefunden – Animation wird übersprungen.");

        // AudioSource automatisch ergänzen falls nicht manuell zugewiesen
        if (footstepAudioSource == null)
            footstepAudioSource = gameObject.AddComponent<AudioSource>();

        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.spatialBlend = 1f; // 3D-Sound

        // Separater AudioSource für Voice Lines
        _voiceAudioSource = gameObject.AddComponent<AudioSource>();
        _voiceAudioSource.playOnAwake = false;
        _voiceAudioSource.spatialBlend = 1f; // 3D-Sound
    }

    void Start()
    {
        _lastPosition = transform.position;
        _voiceTimer = Random.Range(voiceIntervalMin, voiceIntervalMax);
    }

    void Update()
    {
        if (target == null) return;

        // ── 1. Stuck-Erkennung ─────────────────────────────────────────
        float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
        float speedThisFrame = distanceMoved / Time.deltaTime;
        _lastPosition = transform.position;

        if (speedThisFrame < stuckSpeedThreshold)
            _stuckTimer += Time.deltaTime;
        else
            _stuckTimer = 0f;

        // Escape aktivieren wenn zu lange steckt
        if (_stuckTimer >= stuckTimeout && _escapeTimer <= 0f)
        {
            _stuckTimer = 0f;
            _escapeTimer = escapeDuration;

            // Escape-Richtung: seitlich weg vom Ziel (zufällig links oder rechts)
            Vector3 toTarget = (target.position - transform.position).normalized;
            toTarget.y = 0f;
            float side = Random.value > 0.5f ? 1f : -1f;
            _escapeDirection = Vector3.Cross(toTarget, Vector3.up) * side;
        }

        // ── 2. Bewegungsrichtung bestimmen ─────────────────────────────
        Vector3 moveDir;

        if (_escapeTimer > 0f)
        {
            // Escape-Modus: seitlich bewegen um Ecke zu lösen
            _escapeTimer -= Time.deltaTime;
            moveDir = _escapeDirection.normalized;
        }
        else
        {
            // Normal-Modus: Ziel + Avoidance
            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f;
            Vector3 baseDir = toTarget.normalized;

            Vector3 avoidDir = ComputeAvoidanceDirection();
            moveDir = (baseDir + avoidDir * avoidanceStrength).normalized;
        }

        _desiredDirection = moveDir;

        // ── 3. Rotation ────────────────────────────────────────────────
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // ── 4. Gravitation ─────────────────────────────────────────────
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;

        // ── 5. Bewegen ─────────────────────────────────────────────────
        Vector3 horizontalMotion = moveDir * moveSpeed * Time.deltaTime;
        Vector3 verticalMotion = new Vector3(0f, _velocity.y, 0f) * Time.deltaTime;
        _cc.Move(horizontalMotion + verticalMotion);

        // ── 6. Animation & Footsteps ───────────────────────────────────
        bool moving = horizontalMotion.sqrMagnitude > 0.0001f;

        if (_animator != null)
            _animator.SetBool(walkingBoolName, moving);

        // Footstep-Timer nur wenn bewegung aktiv
        if (moving)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                PlayFootstep();
                _footstepTimer = footstepInterval;
            }
        }
        else
        {
            // Timer zurücksetzen damit beim nächsten Loslauen sofort ein Schritt kommt
            _footstepTimer = 0f;
        }

        // ── 7. Voice Lines ─────────────────────────────────────────────
        _voiceTimer -= Time.deltaTime;
        if (_voiceTimer <= 0f)
        {
            PlayVoiceLine();
            _voiceTimer = Random.Range(voiceIntervalMin, voiceIntervalMax);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Spielt einen zufälligen Footstep-Clip aus der Library ab.</summary>
    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (footstepAudioSource == null) return;

        // Zufälligen Clip wählen – denselben wie zuletzt vermeiden
        int index;
        if (footstepClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, footstepClips.Length); }
            while (index == _lastFootstepIndex);
        }
        _lastFootstepIndex = index;

        // Lautstärke und Pitch leicht variieren für natürlicheren Klang
        footstepAudioSource.volume = Mathf.Clamp01(footstepVolume + Random.Range(-volumeVariation, volumeVariation));
        footstepAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        footstepAudioSource.PlayOneShot(footstepClips[index]);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Spielt einen zufälligen Voice-Clip ab – nur wenn kein anderer läuft.</summary>
    void PlayVoiceLine()
    {
        if (voiceClips == null || voiceClips.Length == 0) return;
        if (_voiceAudioSource == null) return;

        // Nicht unterbrechen wenn noch ein Clip läuft
        if (_voiceAudioSource.isPlaying) return;

        // Zufälligen Clip wählen – denselben wie zuletzt vermeiden
        int index;
        if (voiceClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, voiceClips.Length); }
            while (index == _lastVoiceIndex);
        }
        _lastVoiceIndex = index;

        _voiceAudioSource.volume = voiceVolume;
        _voiceAudioSource.pitch = 1f;
        _voiceAudioSource.PlayOneShot(voiceClips[index]);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Verwendet SphereCasts statt Raycasts – erkennt Ecken und breite
    /// Hindernisse zuverlässiger. Berechnet einen Abstoßungsvektor.
    /// </summary>
    Vector3 ComputeAvoidanceDirection()
    {
        Vector3 avoidance = Vector3.zero;
        float halfFOV = fieldOfView * 0.5f;
        float angleStep = rayCount > 1 ? fieldOfView / (rayCount - 1) : 0f;
        int centerIndex = rayCount / 2;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFOV + angleStep * i;
            Vector3 rayDir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            // SphereCast statt Raycast – erkennt Ecken großflächiger
            if (Physics.SphereCast(transform.position, sphereCastRadius, rayDir,
                                   out RaycastHit hit, detectionRadius, obstacleLayers))
            {
                float weight = 1f - (hit.distance / detectionRadius);

                // Mittlere Strahlen (direkt voraus) stärker gewichten
                float centerWeight = 1f - (Mathf.Abs(i - centerIndex) / (float)centerIndex) * 0.5f;
                weight *= centerWeight;

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

        // SphereCasts visualisieren
        float halfFOV = fieldOfView * 0.5f;
        float angleStep = rayCount > 1 ? fieldOfView / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFOV + angleStep * i;
            Vector3 rayDir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            bool hit = Physics.SphereCast(transform.position, sphereCastRadius, rayDir,
                                          out RaycastHit info, detectionRadius, obstacleLayers);

            Gizmos.color = hit ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position,
                           rayDir * (hit ? info.distance : detectionRadius));
        }

        // Bewegungsrichtung (Magenta = Escape-Modus, Cyan = Normal)
        if (Application.isPlaying)
        {
            Gizmos.color = _escapeTimer > 0f ? Color.magenta : Color.cyan;
            Gizmos.DrawRay(transform.position, _desiredDirection * 2f);
        }
    }
}