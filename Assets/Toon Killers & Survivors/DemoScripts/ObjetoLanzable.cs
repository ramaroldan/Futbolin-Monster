using UnityEngine;

public class ObjetoLanzable : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    [Tooltip("Tiempo de vida del objeto antes de destruirse automáticamente")]
    [SerializeField] private float tiempoDeVida = 3.0f;
    
    [Tooltip("Fuerza con la que es lanzado el objeto")]
    [SerializeField] private float fuerzaDeLanzamiento = 20f;
    
    [Header("Efecto de Giro")]
    [Tooltip("Velocidad de rotación al volar (grados por segundo)")]
    [SerializeField] private float velocidadGiro = 720f;
    
    private Rigidbody rb;
    private Collider col;
    private bool haImpactado = false;
    
    private Vector3 direccionLanzamiento;
    private bool inicializado = false;
    private Transform hijoVisual;

    public void Lanzar(Vector3 direccion)
    {
        direccionLanzamiento = direccion;
        inicializado = true;
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Aplicar la velocidad en la dirección real de lanzamiento
        rb.linearVelocity = direccionLanzamiento * fuerzaDeLanzamiento;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }

        // Ignorar colisiones físicas con el jugador y sus hijos para evitar bloqueos
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            Collider[] colJugador = jugador.GetComponentsInChildren<Collider>();
            foreach (var c in colJugador)
            {
                if (c != null && col != null)
                {
                    Physics.IgnoreCollision(col, c);
                }
            }
        }

        // Separar la visual del objeto del colisionador de física para que no rote erráticamente al volar
        CrearHijoVisual();

        // Aplicar velocidad inicial si no se hizo mediante el método Lanzar()
        if (!inicializado)
        {
            rb.linearVelocity = transform.forward * fuerzaDeLanzamiento;
        }
        else
        {
            rb.linearVelocity = direccionLanzamiento * fuerzaDeLanzamiento;
        }
        
        // Habilitar gravedad para una caída realista
        rb.useGravity = true;

        // Autodestrucción después del tiempo de vida
        Destroy(gameObject, tiempoDeVida);
    }

    private void CrearHijoVisual()
    {
        // Crear un objeto hijo para la representación visual
        GameObject goVisual = new GameObject("Visuales");
        goVisual.transform.SetParent(transform, false);
        goVisual.transform.localPosition = Vector3.zero;
        goVisual.transform.localRotation = Quaternion.identity;
        goVisual.transform.localScale = Vector3.one;
        
        // Transferir MeshFilter
        MeshFilter mfOriginal = GetComponent<MeshFilter>();
        if (mfOriginal != null)
        {
            MeshFilter mfNuevo = goVisual.AddComponent<MeshFilter>();
            mfNuevo.sharedMesh = mfOriginal.sharedMesh;
            Destroy(mfOriginal);
        }
        
        // Transferir MeshRenderer
        MeshRenderer mrOriginal = GetComponent<MeshRenderer>();
        if (mrOriginal != null)
        {
            MeshRenderer mrNuevo = goVisual.AddComponent<MeshRenderer>();
            mrNuevo.sharedMaterials = mrOriginal.sharedMaterials;
            Destroy(mrOriginal);
        }
        
        hijoVisual = goVisual.transform;
    }

    void Update()
    {
        if (!haImpactado && hijoVisual != null)
        {
            // Rotar alrededor de Vector3.up (el eje local Y) de la visual.
            // Dado que el proyectil se instancia con una inclinación de 90 grados alrededor de Z (roll),
            // el eje local Y representa la horizontal perpendicular, logrando un giro vertical limpio hacia adelante.
            hijoVisual.Rotate(Vector3.up, -velocidadGiro * Time.deltaTime, Space.Self);
        }
    }

    void OnCollisionEnter(Collision colision)
    {
        // No colisionar con el jugador que lo lanzó
        if (colision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // Verificar si colisionó con el arquero
        PayasoArquero arquero = colision.gameObject.GetComponent<PayasoArquero>();
        if (arquero != null)
        {
            Vector3 puntoImpacto = colision.contacts.Length > 0 ? colision.contacts[0].point : colision.transform.position;
            arquero.RecibirDaño(puntoImpacto);
            
            // Hacer que el hacha rebote de forma elástica hacia atrás y caiga al suelo
            haImpactado = true;
            rb.isKinematic = false;
            rb.linearVelocity = (transform.forward * -4f + Vector3.up * 5f);
            
            // Destruir el proyectil después de 1 segundo
            Destroy(gameObject, 1.0f);
            return;
        }

        // Verificar si colisionó con un defensor
        DefensorAI defensor = colision.gameObject.GetComponent<DefensorAI>();
        if (defensor != null)
        {
            Vector3 puntoImpacto = colision.contacts.Length > 0 ? colision.contacts[0].point : colision.transform.position;
            defensor.RecibirDaño(puntoImpacto);
            
            // Hacer que el hacha rebote de forma elástica hacia atrás y caiga al suelo
            haImpactado = true;
            rb.isKinematic = false;
            rb.linearVelocity = (transform.forward * -4f + Vector3.up * 5f);
            
            // Destruir el proyectil después de 1 segundo
            Destroy(gameObject, 1.0f);
            return;
        }

        // Verificar si colisionó con una calabaza (Pumpkin)
        bool esCalabaza = colision.gameObject.name.ToLower().Contains("pumpkin") || 
                           (colision.transform.parent != null && colision.transform.parent.name.Equals("Pumpkins", System.StringComparison.OrdinalIgnoreCase));
                           
        if (esCalabaza)
        {
            // Obtener el material de la calabaza para que los trozos coincidan
            Material matCalabaza = null;
            MeshRenderer mr = colision.gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                matCalabaza = mr.sharedMaterial;
            }
            
            // Generar la explosión física y lumínica en la posición de la calabaza
            CrearExplosionDeEscombros(colision.gameObject.transform.position, matCalabaza);
            
            // Destruir la calabaza colisionada
            Destroy(colision.gameObject);
            
            // Hacer que el hacha rebote ligeramente hacia atrás y caiga al suelo
            haImpactado = true;
            rb.isKinematic = false;
            rb.linearVelocity = (transform.forward * -3f + Vector3.up * 4f);
            
            // Destruir el proyectil después de 1 segundo en el suelo
            Destroy(gameObject, 1.0f);
            return;
        }

        haImpactado = true;

        // Comportamiento normal (clavarse en la superficie)
        rb.isKinematic = true;
        col.enabled = false;
        transform.parent = colision.transform;
        
        // Programar destrucción final tras adherirse
        Destroy(gameObject, 1.5f);
    }

    private void CrearExplosionDeEscombros(Vector3 posicion, Material materialCalabaza)
    {
        // Crear un contenedor temporal para las partículas
        GameObject contenedor = new GameObject("ExplosionCalabaza");
        contenedor.transform.position = posicion;
        
        // Agregar un destello de luz naranja temporal
        GameObject destelloLuz = new GameObject("DestelloLuz");
        destelloLuz.transform.position = posicion;
        Light luz = destelloLuz.AddComponent<Light>();
        luz.color = new Color(1.0f, 0.4f, 0.0f);
        luz.range = 6f;
        luz.intensity = 4f;
        Destroy(destelloLuz, 0.2f);
        
        int cantidadDePedazos = 12;
        for (int i = 0; i < cantidadDePedazos; i++)
        {
            // Crear un pequeño cubo como trozo de calabaza
            GameObject pedazo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedazo.transform.position = posicion + Random.insideUnitSphere * 0.2f;
            pedazo.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
            pedazo.transform.parent = contenedor.transform;
            
            // Aplicar el material de la calabaza original
            if (materialCalabaza != null)
            {
                pedazo.GetComponent<MeshRenderer>().sharedMaterial = materialCalabaza;
            }
            else
            {
                pedazo.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.5f, 0.0f);
            }
            
            // Agregar física para que salgan disparados
            Rigidbody rbPedazo = pedazo.AddComponent<Rigidbody>();
            Vector3 direccionExplosion = (pedazo.transform.position - posicion).normalized + Vector3.up * 0.4f;
            rbPedazo.linearVelocity = direccionExplosion.normalized * Random.Range(4f, 8f);
            rbPedazo.angularVelocity = Random.insideUnitSphere * 20f;
        }
        
        // Destruir el contenedor y todos sus trozos después de 2.0 segundos
        Destroy(contenedor, 2.0f);
    }
}
