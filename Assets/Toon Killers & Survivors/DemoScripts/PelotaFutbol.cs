using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PelotaFutbol : MonoBehaviour
{
    [Header("Configuración de Conducción")]
    [Tooltip("Distancia a la que el jugador puede empezar a conducir la pelota")]
    public float rangoConduccion = 1.3f;
    
    public bool estaConducida { get; private set; } = false;
    public bool puedeSerRecogida { get; private set; } = true;
    
    private Rigidbody rb;
    private Transform jugadorConductor;
    private Vector3 posicionInicial;

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
        // Rotar visualmente la pelota cuando está siendo conducida
        if (estaConducida && jugadorConductor != null)
        {
            CharacterController cc = jugadorConductor.GetComponent<CharacterController>();
            Vector3 velocidad = cc != null ? cc.velocity : Vector3.zero;
            velocidad.y = 0; // Descartar movimiento vertical
            
            float rapidez = velocidad.magnitude;
            if (rapidez > 0.1f)
            {
                // El eje de giro es perpendicular al vector de velocidad (producto cruz con Vector3.up)
                Vector3 ejeRotacion = Vector3.Cross(Vector3.up, velocidad.normalized);
                
                // Radio del balón para calcular rotación angular correcta (V = w * r => w = V / r)
                float radio = transform.localScale.x * 0.5f;
                float angulo = (rapidez * Time.deltaTime / radio) * Mathf.Rad2Deg;
                
                // Rotar en el espacio mundial
                transform.Rotate(ejeRotacion, angulo, Space.World);
            }
        }
    }

    void FixedUpdate()
    {
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
}
