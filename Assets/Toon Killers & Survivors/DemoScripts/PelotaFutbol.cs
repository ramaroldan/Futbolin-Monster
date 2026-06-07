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
    private Transform jugadorConductor;
    private Vector3 posicionInicial;
    private Vector3 ultimaPosicion;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        posicionInicial = transform.position;
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

        ultimaPosicion = transform.position;
    }

    public void IniciarConduccion(Transform jugador)
    {
        estaConducida = true;
        jugadorConductor = jugador;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
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
    }

    public void Patear(Vector3 fuerza)
    {
        DetenerConduccion();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Activar detección continua al patear
        StartCoroutine(CooldownRecogida(0.6f));
        
        if (trail != null)
        {
            trail.emitting = true;
            trail.Clear();
            trail.AddPosition(transform.position); // Forzar inicio inmediato del rastro en la posición actual del impacto
        }
        
        rb.AddForce(fuerza, ForceMode.Impulse);
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
        ResetearPelota(posicionInicial);
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
                float radio = transform.localScale.x * 0.5f;
                float angulo = (distanciaMover / radio) * Mathf.Rad2Deg * multiplicadorRotacion;
                
                // Rotar en el espacio mundial
                transform.Rotate(ejeRotacion, angulo, Space.World);
            }
        }

        ultimaPosicion = transform.position;
    }

    void FixedUpdate()
    {
        // Aplicar gravedad adicional en el aire para una caída realista y un arco más pronunciado
        if (!estaConducida && !rb.isKinematic)
        {
            bool enSuelo = ChequearSuelo();
            if (!enSuelo)
            {
                rb.AddForce(Physics.gravity * (multiplicadorGravedad - 1f), ForceMode.Acceleration);
            }
            else
            {
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
            // La base del jugador está en sus pies, lo que da una lectura perfecta al balón en el suelo
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
        float radio = transform.localScale.x * 0.5f;
        // Comenzar el raycast un poco más abajo del centro (por ejemplo, a 0.8 * radio) para salir del colisionador de la pelota
        Vector3 origen = transform.position + Vector3.down * (radio * 0.8f);
        float distancia = radio * 0.4f; // Distancia para sobrepasar el radio de la pelota + margen
        
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
}
