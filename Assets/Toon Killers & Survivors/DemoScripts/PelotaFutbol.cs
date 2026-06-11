using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PelotaFutbol : MonoBehaviour
{
    [Header("Configuración de Conducción")]
    [Tooltip("Distancia a la que el jugador puede empezar a conducir la pelota")]
    public float rangoConduccion = 1.3f;

    [Header("Ajustes de Rotación Visual")]
    [Tooltip("Multiplicador de la velocidad de rotación visual (1 = real, menor = gira más lento, mayor = gira más rápido)")]
    public float multiplicadorRotacion = 1f;

    [Header("Física Avanzada")]
    [Tooltip("Multiplicador de gravedad en el aire para caída pesada y realista")]
    public float multiplicadorGravedad = 2.2f;

    private TrailRenderer trail;
    
    public bool estaConducida { get; private set; } = false;
    public bool puedeSerRecogida { get; private set; } = true;
    
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Transform jugadorConductor;
    
    [Header("Configuración de Respawn")]
    [Tooltip("Posición a la que la pelota volverá al resetearse. Modificable desde el inspector.")]
    public Vector3 posicionRespawn = new Vector3(58.15f, 9.27f, 72.85f);
    
    private Vector3 ultimaPosicion;
    private float worldRadius = 0.165f;
    private float worldCenterOffsetY = 0.108f;
    
    private AudioSource audioSource;
    private AudioClip clipChuto;
    private AudioClip clipRebote;

    public float RadioMundo => worldRadius;
    public float OffsetCentroYMundo => worldCenterOffsetY;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        // Si posicionRespawn es cero, usar la posición actual de la escena como fallback
        if (posicionRespawn == Vector3.zero)
        {
            posicionRespawn = transform.position;
        }
    }

    void Start()
    {
        // Configurar física realista de balón de fútbol (peso oficial: ~430g)
        rb.mass = 0.43f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 0.1f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Configurar material físico para rebotes naturales y fluidos
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial pm = new PhysicsMaterial("FisicaPelotaReal");
            pm.bounciness = 0.72f; // Buen rebote (típico de balón inflado)
            pm.bounceCombine = PhysicsMaterialCombine.Maximum;
            pm.dynamicFriction = 0.4f; // Fricción realista para rodar en pasto/tierra
            pm.staticFriction = 0.4f;
            pm.frictionCombine = PhysicsMaterialCombine.Average;
            col.material = pm;
        }

        // Configurar rastro visual (TrailRenderer) de forma automática
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }
        
        trail.time = 0.6f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.autodestruct = false;
        trail.emitting = false;
        
        // Asignar un material unlit para que brille con el Bloom
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (unlitShader == null)
        {
            unlitShader = Shader.Find("Sprites/Default");
        }
        trail.material = new Material(unlitShader);
        
        // Gradiente neón (verde-limón neón a transparente)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.8f, 1.0f, 0.2f), 0.0f), new GradientColorKey(new Color(0.1f, 0.9f, 0.5f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;

        // Configurar AudioSource y generar audios procedurales
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // Sonido 3D
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 40f;
        audioSource.volume = 1f;

        GenerarAudiosProcedurales();
        ActualizarDimensionesColision();

        ultimaPosicion = transform.position;
    }

    public void ActualizarDimensionesColision()
    {
        if (sphereCollider != null)
        {
            worldRadius = sphereCollider.radius * transform.localScale.x;
            worldCenterOffsetY = sphereCollider.center.y * transform.localScale.y;
        }
        else
        {
            worldRadius = transform.localScale.x * 0.5f;
            worldCenterOffsetY = 0f;
        }
    }

    private void GenerarAudiosProcedurales()
    {
        int sampleRate = 44100;
        
        // 1. Kick/Chuto (Tacto del pie - thud sordo)
        float durationKick = 0.18f;
        int samplesKick = Mathf.RoundToInt(sampleRate * durationKick);
        float[] dataKick = new float[samplesKick];
        for (int i = 0; i < samplesKick; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(280f, 60f, t / durationKick);
            float phase = 2f * Mathf.PI * freq * t;
            float wave = Mathf.Sin(phase);
            float noise = (Random.value * 2f - 1f) * 0.15f;
            float env = Mathf.Exp(-18f * t);
            dataKick[i] = (wave + noise) * env * 0.6f;
        }
        clipChuto = AudioClip.Create("KickBall", samplesKick, 1, sampleRate, false);
        clipChuto.SetData(dataKick, 0);

        // 2. Rebote (Impacto con obstáculos/suelo)
        float durationBounce = 0.1f;
        int samplesBounce = Mathf.RoundToInt(sampleRate * durationBounce);
        float[] dataBounce = new float[samplesBounce];
        for (int i = 0; i < samplesBounce; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(450f, 180f, t / durationBounce);
            float phase = 2f * Mathf.PI * freq * t;
            float wave = Mathf.Sin(phase);
            float noise = (Random.value * 2f - 1f) * 0.05f;
            float env = Mathf.Exp(-35f * t);
            dataBounce[i] = (wave + noise) * env * 0.5f;
        }
        clipRebote = AudioClip.Create("BounceBall", samplesBounce, 1, sampleRate, false);
        clipRebote.SetData(dataBounce, 0);
    }

    public void PlaySoftDribbleSound(float volumen)
    {
        if (audioSource != null && clipRebote != null)
        {
            audioSource.pitch = Random.Range(0.85f, 0.95f);
            audioSource.PlayOneShot(clipRebote, volumen * 0.45f);
            audioSource.pitch = 1.0f; // Restaurar pitch
        }
    }

    public void IniciarConduccion(Transform jugador)
    {
        estaConducida = true;
        jugadorConductor = jugador;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Desactivar TODOS los colliders (principal e hijos) para que ningún mesh
        // de la calabaza empuje contra el terreno ni contra el CharacterController
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;
        
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    public void DetenerConduccion()
    {
        estaConducida = false;
        jugadorConductor = null;
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Reactivar todos los colliders al soltar la pelota
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = true;
    }

    public void Patear(Vector3 fuerza)
    {
        Patear(fuerza, Vector3.zero);
    }

    public void Patear(Vector3 fuerza, Vector3 spin)
    {
        DetenerConduccion();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Activar detección continua al patear
        
        // Cooldown dinámico: patadas más suaves = menor cooldown para retomar posesión rápida
        float ratioFuerza = Mathf.InverseLerp(7f, 22f, fuerza.magnitude);
        float cooldown = Mathf.Lerp(0.18f, 0.6f, ratioFuerza);
        StartCoroutine(CooldownRecogida(cooldown));
        
        if (trail != null)
        {
            trail.emitting = true;
            trail.Clear();
            trail.AddPosition(transform.position); // Forzar inicio inmediato del rastro
        }
        
        rb.AddForce(fuerza, ForceMode.Impulse);
        rb.angularVelocity = spin;

        if (audioSource != null && clipChuto != null)
        {
            audioSource.PlayOneShot(clipChuto, 1.0f);
        }
    }

    public void ResetearPelota(Vector3 posicion)
    {
        DetenerConduccion();
        transform.position = posicion;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        StartCoroutine(CooldownRecogida(0.5f));
    }

    public void ResetearPelotaOriginal()
    {
        ResetearPelota(posicionRespawn);
    }

    private IEnumerator CooldownRecogida(float tiempo)
    {
        puedeSerRecogida = false;
        yield return new WaitForSeconds(tiempo);
        puedeSerRecogida = true;
    }

    void Update()
    {
        // Rotar visualmente la pelota cuando está siendo conducida basándose en su propio desplazamiento real
        if (estaConducida)
        {
            Vector3 desplazamiento = transform.position - ultimaPosicion;
            desplazamiento.y = 0; // Descartar movimiento vertical
            
            float distanciaMover = desplazamiento.magnitude;
            if (distanciaMover > 0.001f)
            {
                // El eje de giro es perpendicular al vector de desplazamiento
                Vector3 ejeRotacion = Vector3.Cross(Vector3.up, desplazamiento.normalized);
                
                // Radio del balón para calcular rotación angular correcta (V = w * r => w = V / r)
                float radio = worldRadius;
                float angulo = (distanciaMover / radio) * Mathf.Rad2Deg * multiplicadorRotacion;
                
                // Rotar en el espacio mundial
                transform.Rotate(ejeRotacion, angulo, Space.World);
            }
        }

        ultimaPosicion = transform.position;
    }

    void FixedUpdate()
    {
        // Aplicar gravedad adicional y efecto Magnus en el aire
        if (!estaConducida && !rb.isKinematic)
        {
            ActualizarDimensionesColision();
            bool enSuelo = ChequearSuelo();
            if (!enSuelo)
            {
                // Gravedad adicional
                rb.AddForce(Physics.gravity * (multiplicadorGravedad - 1f), ForceMode.Acceleration);

                // Efecto Magnus: F = K * (w x v)
                float magnusCoeff = 0.12f;
                Vector3 magnusForce = magnusCoeff * Vector3.Cross(rb.angularVelocity, rb.linearVelocity);
                rb.AddForce(magnusForce, ForceMode.Acceleration);
            }
            else
            {
                // Resistencia al rodamiento en el césped para frenado natural
                rb.AddForce(-rb.linearVelocity * 0.25f, ForceMode.Acceleration);

                // Si está en el suelo y rueda muy lento, desactivar el rastro de trayectoria
                if (rb.linearVelocity.magnitude < 2.0f && trail != null)
                {
                    trail.emitting = false;
                }
            }
        }

        // Detectar si el jugador está cerca para iniciar conducción
        if (estaConducida || !puedeSerRecogida) return;
        
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.transform.position);
            if (distancia <= rangoConduccion)
            {
                ControladorTerceraPersona controlador = jugador.GetComponent<ControladorTerceraPersona>();
                if (controlador != null && controlador.EstaDisponibleParaConducir())
                {
                    controlador.IniciarDribling(this);
                }
            }
        }
    }

    private bool ChequearSuelo()
    {
        // El centro de la esfera en el espacio de mundo es:
        Vector3 centroMundo = transform.position + Vector3.up * worldCenterOffsetY;
        // Comenzamos el raycast un poco más abajo del centro de la esfera
        Vector3 origen = centroMundo + Vector3.down * (worldRadius * 0.8f);
        float distancia = worldRadius * 0.4f; // Distancia para sobrepasar el radio de la pelota + margen
        
        // Usar RaycastAll para poder ignorar la propia pelota
        RaycastHit[] hits = Physics.RaycastAll(origen, Vector3.down, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject != gameObject && !hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (estaConducida) return;
        
        // Ignorar colisiones de activación del jugador si están en cooldown
        if (collision.gameObject.CompareTag("Player") && !puedeSerRecogida) return;

        float velocidadImpacto = collision.relativeVelocity.magnitude;
        if (velocidadImpacto > 0.8f)
        {
            if (audioSource != null && clipRebote != null)
            {
                // El volumen escala con la velocidad de impacto
                float volumen = Mathf.Clamp01(velocidadImpacto / 12f);
                audioSource.PlayOneShot(clipRebote, volumen);
            }
        }
    }
}
