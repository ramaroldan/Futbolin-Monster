using UnityEngine;
using System.Collections;

public class DefensorAI : MonoBehaviour
{
    public enum EstadoAI
    {
        DefendiendoZona,
        Temporizando, // Jockeying (sombra)
        Persiguiendo,  // Sprint e interceptación
        Barriendo,
        Recuperando,
        Regresando
    }

    [Header("Referencias")]
    public Transform jugador;
    public PelotaFutbol pelota;
    private Animator animador;
    private ControladorTerceraPersona jugadorControlador;
    private DefensorAI compañero;

    [Header("Movimiento")]
    public float velocidadCaminar = 2.2f;
    public float velocidadCorrer = 5.2f;
    public float velocidadRotacion = 10f;

    [Header("Marcación por Zona")]
    public float xMin = 45f;
    public float xMax = 65f;
    public float zMin = 0f;
    public float zMax = 0f;
    
    [HideInInspector]
    public Vector3 posicionInicial;

    [Header("Posición de la Portería")]
    public Vector3 porteriaPropia = new Vector3(47.27f, 9.03f, 72.81f);

    [Header("Parámetros de Barrida (Tackle)")]
    public float distanciaBarrida = 2.2f;
    public float velocidadBarrida = 9.5f;
    public float duracionBarrida = 0.55f;
    public float cooldownBarridaMax = 3.8f;
    public float fuerzaDespeje = 16f;
    public float fuerzaPase = 11f;
    public float radioImpactoBarrida = 1.35f;
    public float duracionAturdimientoJugador = 0.9f;
    public float fuerzaKnockbackJugador = 6.5f;

    [Header("Ajustes de Táctica")]
    [Tooltip("Distancia a la que el defensor mantendrá la sombra (Jockeying)")]
    public float distanciaJockey = 2.6f;
    [Tooltip("Tiempo de anticipación para interceptar la trayectoria del jugador")]
    public float tiempoAnticipacion = 0.22f;

    [Header("Estado de la IA")]
    public EstadoAI estadoActual = EstadoAI.DefendiendoZona;
    private float cooldownBarrida = 0f;
    private Vector3 direccionBarrida;
    private float tiempoInicioBarrida;
    private bool haGolpeadoEnBarrida = false;

    [Header("Ajustes de Vida y Daño")]
    public int vidaMax = 1;
    [Tooltip("Material para las partículas de sangre (ej. color rojo). Si se deja vacío, se generará uno rojo por defecto.")]
    public Material materialSangre;
    private int vidaActual = 1;
    private bool estaDesmayado = false;
    private bool recibiendoDaño = false;

    void Start()
    {
        animador = GetComponent<Animator>();
        posicionInicial = transform.position;

        // Asegurar que tenga CapsuleCollider y Rigidbody configurados para colisiones
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
        }
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;
        col.height = 1.8f;
        col.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        vidaActual = vidaMax;

        // Auto-inicializar límites X si están en 0
        if (xMin == 0f && xMax == 0f)
        {
            xMin = 45f;
            xMax = 65f;
        }

        // Auto-inicializar límites Z basados en el lado inicial del campo
        if (zMin == 0f && zMax == 0f)
        {
            if (posicionInicial.z < 73f)
            {
                zMin = 55f;
                zMax = 73f;
            }
            else
            {
                zMin = 73f;
                zMax = 90f;
            }
        }

        // Buscar portería dinámica en el escenario
        ArcoDeFutbol arco = FindObjectOfType<ArcoDeFutbol>();
        if (arco != null)
        {
            porteriaPropia = arco.transform.position;
        }

        // Buscar jugador
        if (jugador == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                jugador = playerGO.transform;
                jugadorControlador = playerGO.GetComponent<ControladorTerceraPersona>();
            }
        }
        else
        {
            jugadorControlador = jugador.GetComponent<ControladorTerceraPersona>();
        }

        // Buscar pelota
        if (pelota == null)
        {
            pelota = FindObjectOfType<PelotaFutbol>();
        }

        // Buscar compañero defensor
        DefensorAI[] defensores = FindObjectsOfType<DefensorAI>();
        foreach (var def in defensores)
        {
            if (def != this)
            {
                compañero = def;
                break;
            }
        }
        
        // Desactivar scripts obsoletos de la demo
        RotateOver rotateOver = GetComponent<RotateOver>();
        if (rotateOver != null) rotateOver.enabled = false;

        AnimationToButton animToButton = GetComponent<AnimationToButton>();
        if (animToButton != null) animToButton.enabled = false;
    }

    void Update()
    {
        if (estaDesmayado || recibiendoDaño) return;

        if (jugador == null || pelota == null)
        {
            Start();
            if (jugador == null || pelota == null) return;
        }

        if (cooldownBarrida > 0f)
        {
            cooldownBarrida -= Time.deltaTime;
        }

        // Comprobación de Cobertura de ayuda (Help Defense)
        bool necesitaCobertura = VerificarNecesidadDeCobertura();

        switch (estadoActual)
        {
            case EstadoAI.DefendiendoZona:
                ActualizarDefendiendoZona(necesitaCobertura);
                break;
            case EstadoAI.Temporizando:
                ActualizarTemporizando(necesitaCobertura);
                break;
            case EstadoAI.Persiguiendo:
                ActualizarPersiguiendo(necesitaCobertura);
                break;
            case EstadoAI.Barriendo:
                ActualizarBarriendo();
                break;
            case EstadoAI.Recuperando:
                ActualizarRecuperando();
                break;
            case EstadoAI.Regresando:
                ActualizarRegresando(necesitaCobertura);
                break;
        }
    }

    bool EstaEnZona(Vector3 posicion)
    {
        return posicion.x >= xMin && posicion.x <= xMax && posicion.z >= zMin && posicion.z <= zMax;
    }

    bool VerificarNecesidadDeCobertura()
    {
        if (jugador.position.x >= 53f) return false; // El jugador no está tan cerca del arco
        
        // El jugador superó a nuestro compañero o el compañero está caído
        bool compañeroSuperado = compañero != null && jugador.position.x < compañero.transform.position.x;
        bool compañeroIncapacitado = compañero != null && compañero.estadoActual == EstadoAI.Recuperando;

        return compañeroSuperado || compañeroIncapacitado;
    }

    void ActualizarDefendiendoZona(bool necesitaCobertura)
    {
        CambiarAnimacion("Idle");
        RotarHacia(pelota.transform.position);

        if (necesitaCobertura)
        {
            estadoActual = EstadoAI.Persiguiendo;
            return;
        }

        if (EstaEnZona(jugador.position) || EstaEnZona(pelota.transform.position))
        {
            // Entrar en modo temporización (Jockeying) primero para marcar de cerca antes de correr a barrer
            estadoActual = EstadoAI.Temporizando;
        }
    }

    void ActualizarTemporizando(bool necesitaCobertura)
    {
        if (necesitaCobertura)
        {
            estadoActual = EstadoAI.Persiguiendo;
            return;
        }

        // Si el jugador o el balón se salen de la zona, regresar
        if (!EstaEnZona(jugador.position) && !EstaEnZona(pelota.transform.position))
        {
            estadoActual = EstadoAI.Regresando;
            return;
        }

        Vector3 targetPos = pelota.estaConducida ? jugador.position : pelota.transform.position;
        float distAlTarget = Vector3.Distance(transform.position, targetPos);

        // Si el jugador se acerca mucho (menos de 3m) o el balón está suelto y cerca, salir a presionar / perseguir
        if (distAlTarget < 3.2f || !pelota.estaConducida)
        {
            estadoActual = EstadoAI.Persiguiendo;
            return;
        }

        // Jockeying: posicionarse entre el balón y la portería propia
        Vector3 dirPorteriaAlTarget = (targetPos - porteriaPropia).normalized;
        Vector3 posicionJockey = targetPos - dirPorteriaAlTarget * distanciaJockey;
        posicionJockey.y = transform.position.y; // Mantener altura

        MoverHacia(posicionJockey, velocidadCaminar);
        CambiarAnimacion("Walk");
        RotarHacia(targetPos);
    }

    void ActualizarPersiguiendo(bool necesitaCobertura)
    {
        // Si no necesita cobertura y salieron de la zona, regresar
        if (!necesitaCobertura && !EstaEnZona(jugador.position) && !EstaEnZona(pelota.transform.position))
        {
            estadoActual = EstadoAI.Regresando;
            return;
        }

        Vector3 targetPos = pelota.estaConducida ? jugador.position : pelota.transform.position;
        Vector3 posicionObjetivo = targetPos;

        // Si perseguimos al jugador, aplicar anticipación (interceptar su ruta futura)
        if (pelota.estaConducida && jugadorControlador != null)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            Vector3 velJugador = cc != null ? cc.velocity : Vector3.zero;
            posicionObjetivo = targetPos + velJugador * tiempoAnticipacion;
            
            // Limitar para no salirse de la cancha en la predicción
            posicionObjetivo.x = Mathf.Clamp(posicionObjetivo.x, 44f, 66f);
            posicionObjetivo.z = Mathf.Clamp(posicionObjetivo.z, 55f, 90f);
        }

        MoverHacia(posicionObjetivo, velocidadCorrer);
        CambiarAnimacion("Run");

        // Decidir si barrer
        float distancia = Vector3.Distance(transform.position, targetPos);
        if (distancia <= distanciaBarrida && cooldownBarrida <= 0f)
        {
            // Solo barrer si el jugador tiene el balón o si el balón está suelto
            // Y si estamos orientados relativamente hacia el objetivo (evitar barrer de espaldas)
            Vector3 dirAlTarget = (targetPos - transform.position).normalized;
            float dotFrontal = Vector3.Dot(transform.forward, dirAlTarget);
            
            if (dotFrontal > 0.4f)
            {
                IniciarBarrida(targetPos);
            }
        }
        else if (distancia > 4.5f && pelota.estaConducida && !necesitaCobertura)
        {
            // Si el jugador se alejó de nuevo, volver a temporizar
            estadoActual = EstadoAI.Temporizando;
        }
    }

    void IniciarBarrida(Vector3 posicionObjetivo)
    {
        estadoActual = EstadoAI.Barriendo;
        tiempoInicioBarrida = Time.time;
        direccionBarrida = (posicionObjetivo - transform.position).normalized;
        direccionBarrida.y = 0f;
        direccionBarrida.Normalize();

        haGolpeadoEnBarrida = false;
        CambiarAnimacion("LyingWalk");
    }

    void ActualizarBarriendo()
    {
        float tiempoTranscurrido = Time.time - tiempoInicioBarrida;

        if (tiempoTranscurrido >= duracionBarrida)
        {
            estadoActual = EstadoAI.Recuperando;
            tiempoInicioBarrida = Time.time; // Reutilizar para tiempo de recuperación
            return;
        }

        transform.position += direccionBarrida * velocidadBarrida * Time.deltaTime;
        
        if (direccionBarrida != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionBarrida);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        if (!haGolpeadoEnBarrida)
        {
            float distJugador = Vector3.Distance(transform.position, jugador.position);
            if (distJugador <= radioImpactoBarrida)
            {
                TaclearJugador();
            }
            else
            {
                float distPelota = Vector3.Distance(transform.position, pelota.transform.position);
                if (distPelota <= radioImpactoBarrida && !pelota.estaConducida)
                {
                    ControlarODespejarPelota();
                    haGolpeadoEnBarrida = true;
                }
            }
        }
    }

    void TaclearJugador()
    {
        haGolpeadoEnBarrida = true;
        
        Vector3 dirKnockback = (jugador.position - transform.position).normalized;
        dirKnockback.y = 0.2f;
        dirKnockback = dirKnockback.normalized * fuerzaKnockbackJugador;

        if (jugadorControlador != null)
        {
            jugadorControlador.RecibirBarrida(duracionAturdimientoJugador, dirKnockback);
        }

        ControlarODespejarPelota();
    }

    void ControlarODespejarPelota()
    {
        pelota.DetenerConduccion();

        // Táctica realista: si nuestro compañero defensor está libre y adelante, le damos un pase.
        // De lo contrario, despejamos el balón hacia las bandas o hacia el campo contrario.
        bool compañeroLibre = compañero != null && Vector3.Distance(compañero.transform.position, jugador.position) > 6f;

        if (compañeroLibre)
        {
            // Dar pase al compañero libre
            Vector3 dirPase = (compañero.transform.position - pelota.transform.position).normalized;
            dirPase.y = 0.08f; // Pase raso/medio
            pelota.Patear(dirPase * fuerzaPase);
            Debug.Log(gameObject.name + " realizó un pase limpio a " + compañero.name);
        }
        else
        {
            // Despeje de seguridad hacia las bandas (lejos del centro) para evitar contraataques
            float zDestino = transform.position.z < 73f ? 57f : 88f; // Despejar al lateral más cercano
            Vector3 puntoDespeje = new Vector3(transform.position.x + 18f, transform.position.y + 1.8f, zDestino);
            Vector3 dirDespeje = (puntoDespeje - pelota.transform.position).normalized;
            
            pelota.Patear(dirDespeje * fuerzaDespeje);
            Debug.Log(gameObject.name + " despejó el balón hacia la banda.");
        }
    }

    void ActualizarRecuperando()
    {
        // Detener animación/movimiento y simular levantarse
        CambiarAnimacion("Idle");
        
        float tiempoTranscurrido = Time.time - tiempoInicioBarrida;
        
        // Si la barrida fue exitosa, se levanta rápido (0.35s)
        // Si la barrida falló (miss), tiene penalización y tarda más (1.4s) en reincorporarse
        float tiempoRecuperacion = haGolpeadoEnBarrida ? 0.35f : 1.4f;

        if (tiempoTranscurrido >= tiempoRecuperacion)
        {
            cooldownBarrida = cooldownBarridaMax;
            estadoActual = EstadoAI.Persiguiendo;
        }
    }

    void ActualizarRegresando(bool necesitaCobertura)
    {
        if (necesitaCobertura)
        {
            estadoActual = EstadoAI.Persiguiendo;
            return;
        }

        float distAHome = Vector3.Distance(transform.position, posicionInicial);
        if (distAHome <= 0.5f)
        {
            estadoActual = EstadoAI.DefendiendoZona;
            return;
        }

        if (EstaEnZona(jugador.position) || EstaEnZona(pelota.transform.position))
        {
            estadoActual = EstadoAI.Temporizando;
            return;
        }

        MoverHacia(posicionInicial, velocidadCaminar);
        CambiarAnimacion("Walk");
    }

    void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector3 direccion = (destino - transform.position);
        direccion.y = 0f;
        if (direccion.magnitude > 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, transform.position + direccion.normalized, velocidad * Time.deltaTime);
        }
    }

    void RotarHacia(Vector3 destino)
    {
        Vector3 direccion = (destino - transform.position);
        direccion.y = 0f;
        if (direccion.magnitude > 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }
    }

    private string animacionActual = "";
    void CambiarAnimacion(string nuevaAnimacion)
    {
        if (animacionActual == nuevaAnimacion) return;
        animador.CrossFade(nuevaAnimacion, 0.15f);
        animacionActual = nuevaAnimacion;
    }

    public void RecibirDaño(Vector3 puntoImpacto)
    {
        if (estaDesmayado) return;

        vidaActual--;
        GenerarSangre(puntoImpacto);

        if (vidaActual <= 0)
        {
            StartCoroutine(SecuenciaDesmayo());
        }
        else
        {
            StartCoroutine(SecuenciaDaño());
        }
    }

    private void GenerarSangre(Vector3 posicion)
    {
        GameObject contenedor = new GameObject("ParticulasSangre");
        contenedor.transform.position = posicion;

        int cantidadDeGotas = 15;
        for (int i = 0; i < cantidadDeGotas; i++)
        {
            GameObject gota = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gota.transform.position = posicion + Random.insideUnitSphere * 0.1f;
            gota.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);
            gota.transform.parent = contenedor.transform;

            MeshRenderer mr = gota.GetComponent<MeshRenderer>();
            if (materialSangre != null)
            {
                mr.sharedMaterial = materialSangre;
            }
            else
            {
                mr.material.shader = Shader.Find("Standard");
                mr.material.color = new Color(0.9f, 0.0f, 0.0f);
            }

            Rigidbody rbGota = gota.AddComponent<Rigidbody>();
            Vector3 dir = (gota.transform.position - posicion).normalized + Vector3.up * 0.5f;
            rbGota.linearVelocity = dir.normalized * Random.Range(3f, 6f);

            Destroy(gota.GetComponent<Collider>());
        }

        Destroy(contenedor, 1.5f);
    }

    private IEnumerator SecuenciaDaño()
    {
        recibiendoDaño = true;
        CambiarAnimacion("GetDamage");
        yield return new WaitForSeconds(0.5f);
        recibiendoDaño = false;
    }

    private IEnumerator SecuenciaDesmayo()
    {
        estaDesmayado = true;
        CambiarAnimacion("Death");

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;

        Debug.Log("¡El defensor " + gameObject.name + " se ha desmayado!");

        yield return new WaitForSeconds(1.2f);

        if (animador != null) animador.speed = 0f;

        yield return new WaitForSeconds(3.8f);

        if (animador != null) animador.speed = 1f;
        CambiarAnimacion("Idle");
        
        if (col != null) col.isTrigger = false;

        vidaActual = vidaMax;
        estaDesmayado = false;
        
        Debug.Log("¡El defensor " + gameObject.name + " se recuperó y vuelve a defender!");
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Auto-inicializar límites X si están en 0
            if (xMin == 0f && xMax == 0f)
            {
                xMin = 45f;
                xMax = 65f;
            }

            // Auto-inicializar límites Z basados en la posición
            if (zMin == 0f && zMax == 0f)
            {
                if (transform.position.z < 73f)
                {
                    zMin = 55f;
                    zMax = 73f;
                }
                else
                {
                    zMin = 73f;
                    zMax = 90f;
                }
            }

            // Asegurar colisionadores y físicas para recibir daño en el editor
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.9f, 0f);
                col.radius = 0.35f;
                col.height = 1.8f;
                col.isTrigger = false;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
#endif
        DrawZoneGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawZoneGizmo(true);
    }

    private void DrawZoneGizmo(bool selected)
    {
        float actualXMin = xMin;
        float actualXMax = xMax;
        if (actualXMin == 0f && actualXMax == 0f)
        {
            actualXMin = 45f;
            actualXMax = 65f;
        }

        float actualZMin = zMin;
        float actualZMax = zMax;
        if (actualZMin == 0f && actualZMax == 0f)
        {
            Vector3 pos = Application.isPlaying ? posicionInicial : transform.position;
            if (pos.z < 73f)
            {
                actualZMin = 55f;
                actualZMax = 73f;
            }
            else
            {
                actualZMin = 73f;
                actualZMax = 90f;
            }
        }

        float centerX = (actualXMin + actualXMax) * 0.5f;
        float centerZ = (actualZMin + actualZMax) * 0.5f;
        float sizeX = actualXMax - actualXMin;
        float sizeZ = actualZMax - actualZMin;
        
        Vector3 centro = new Vector3(centerX, transform.position.y, centerZ);
        Vector3 tamaño = new Vector3(sizeX, 0.1f, sizeZ);

        Color colorGizmo = gameObject.name.Contains("2") ? new Color(0.2f, 0.5f, 0.9f) : new Color(0.2f, 0.8f, 0.2f);

        if (selected)
        {
            Gizmos.color = new Color(colorGizmo.r, colorGizmo.g, colorGizmo.b, 0.2f);
            Gizmos.DrawCube(centro, tamaño);
        }

        Gizmos.color = new Color(colorGizmo.r, colorGizmo.g, colorGizmo.b, selected ? 0.9f : 0.4f);
        Gizmos.DrawWireCube(centro, tamaño);
    }
}
