using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PayasoArquero : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // DIFICULTAD
    // ─────────────────────────────────────────────────────────────────────────────
    public enum NivelDificultad { Amateur, Intermedio, Profesional, Elite }

    [Header("── Dificultad ──────────────────────────────────────")]
    [Tooltip("Nivel general del arquero. Cambia automáticamente todos los parámetros de rendimiento.")]
    public NivelDificultad nivelDificultad = NivelDificultad.Intermedio;

    [Space(4)]
    [Tooltip("Muestra los valores derivados del nivel de dificultad (solo lectura).")]
    [HideInInspector] public float _vel;
    [HideInInspector] public float _reaccion;
    [HideInInspector] public float _prediccion;
    [HideInInspector] public float _tiempoRecup;

    // ─────────────────────────────────────────────────────────────────────────────
    // AJUSTES MANUALES (OVERRIDE)
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("── Ajustes Manuales (dejan de usarse si Dificultad está activo) ──")]
    [Tooltip("Desactiva la dificultad automática y usa los valores manuales de abajo.")]
    public bool usarAjustesManuales = false;

    [Tooltip("[Manual] Velocidad lateral de desplazamiento (m/s).")]
    public float velocidadMovimiento = 6f;

    [Tooltip("[Manual] Segundos de anticipación para reaccionar al tiro. 0 = sin retraso.")]
    public float tiempoReaccion = 0.1f;

    [Tooltip("[Manual] Factor de predicción de trayectoria. 0 = reactivo, 1 = perfecto.")]
    [Range(0f, 1f)]
    public float factorPrediccion = 0.7f;

    [Tooltip("[Manual] Segundos de cooldown antes de salir de la postura post-zambullida y volver a rastrear.")]
    public float tiempoRecuperacionPost = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────────
    // ZONA Y LÍMITES
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("── Zona del Arco ─────────────────────────────────────")]
    [Tooltip("Distancia lateral máxima desde el centro del arco (postes).")]
    public float limiteLateral = 3.5f;

    [Tooltip("Distancia frontal a la que el arquero detecta el tiro y empieza a actuar.")]
    public float distanciaActivacion = 25.0f;

    [Tooltip("Nombre del GameObject del travesaño en la escena (para calcular la altura máxima del salto).")]
    public string nombreTravesano = "Travesaño";

    [Tooltip("Velocidad de rotación (grados/segundo) con la que el arquero mira hacia la pelota.")]
    public float velocidadRotacion = 90f;

    // ─────────────────────────────────────────────────────────────────────────────
    // ZAMBULLIDA
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("── Zambullida ─────────────────────────────────────────")]
    [Tooltip("Ángulo máximo de rotación (roll) lateral al tirarse.")]
    public float anguloZambullidaMax = 65f;

    [Tooltip("Duración de la fase de estirada (vuelo hacia el balón).")]
    public float duracionLanzamiento = 0.28f;

    [Tooltip("Duración de la retención en el pico del salto.")]
    public float duracionRetencion = 0.25f;

    [Tooltip("Duración de la caída de regreso al suelo.")]
    public float duracionCaida = 0.4f;

    // ─────────────────────────────────────────────────────────────────────────────
    // VIDA / DAÑO
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("── Vida y Daño ────────────────────────────────────────")]
    public int vidaMax = 3;
    [Tooltip("Material para las partículas de sangre. Si se deja vacío, se usa rojo por defecto.")]
    public Material materialSangre;

    // ─────────────────────────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────────────
    private int vidaActual;
    private bool estaDesmayado = false;
    private bool recibiendoDaño = false;

    [Header("── Sonidos ───────────────────────────────────────────")]
    [Tooltip("Clip de sonido que se reproduce al recibir daño")]
    public AudioClip sonidoDaño;
    private AudioSource audioSource;

    private Vector3 posicionInicial;
    private Quaternion rotacionOriginalGk;
    private float currentOffset = 0f;
    private float alturaMaxSalto = 1.8f;   // se recalcula en Start() con el travesaño

    private PelotaFutbol pelota;
    private Rigidbody rbPelota;
    private Animator animator;

    private bool zambullendo = false;
    private bool cooldownPostZambullida = false;
    private string animacionActual = "";
    private ArqueroIK arqueroIK;

    // Parámetros efectivos (combinan dificultad + manuales)
    private float _velEfectiva;
    private float _reaccionEfectiva;
    private float _prediccionEfectiva;
    private float _recupEfectiva;

    // ─────────────────────────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────────────────────
    void Start()
    {
        posicionInicial = transform.position;
        rotacionOriginalGk = transform.rotation;
        animator = GetComponent<Animator>();
        arqueroIK = GetComponent<ArqueroIK>();
        BuscarPelota();
        vidaActual = vidaMax;

        // Calcular la altura máxima del salto según el travesaño real
        GameObject travesano = GameObject.Find(nombreTravesano);
        if (travesano != null)
        {
            // La altura disponible es desde el suelo del arquero hasta el travesaño, menos el radio del personaje (~0.4m)
            alturaMaxSalto = Mathf.Max(0.5f, travesano.transform.position.y - posicionInicial.y - 0.4f);
        }

        AplicarDificultad();

        // Configurar AudioSource para sonidos de daño
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Sonido 3D
    }

    void AplicarDificultad()
    {
        if (usarAjustesManuales)
        {
            _velEfectiva      = velocidadMovimiento;
            _reaccionEfectiva = tiempoReaccion;
            _prediccionEfectiva = factorPrediccion;
            _recupEfectiva    = tiempoRecuperacionPost;
        }
        else
        {
            switch (nivelDificultad)
            {
                case NivelDificultad.Amateur:
                    // Lento, sin predicción, se recupera rápido (mucho cooldown para que el jugador pueda rematar)
                    _velEfectiva        = 3.5f;
                    _reaccionEfectiva   = 0.35f;    // reacciona tarde
                    _prediccionEfectiva = 0.0f;     // cero predicción
                    _recupEfectiva      = 1.2f;     // tarda en volver a posición
                    break;

                case NivelDificultad.Intermedio:
                    _velEfectiva        = 5.5f;
                    _reaccionEfectiva   = 0.18f;
                    _prediccionEfectiva = 0.5f;
                    _recupEfectiva      = 0.7f;
                    break;

                case NivelDificultad.Profesional:
                    _velEfectiva        = 7.5f;
                    _reaccionEfectiva   = 0.06f;
                    _prediccionEfectiva = 0.85f;
                    _recupEfectiva      = 0.35f;
                    break;

                case NivelDificultad.Elite:
                    // Casi perfecto — muy difícil meterle un gol
                    _velEfectiva        = 10f;
                    _reaccionEfectiva   = 0f;
                    _prediccionEfectiva = 1f;
                    _recupEfectiva      = 0.15f;
                    break;
            }
        }

        // Exponer en inspector para referencia visual
        _vel       = _velEfectiva;
        _reaccion  = _reaccionEfectiva;
        _prediccion = _prediccionEfectiva;
        _tiempoRecup = _recupEfectiva;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────────────────
    void Update()
    {
        // Actualizar parámetros si el diseñador cambia el nivel de dificultad en runtime
        AplicarDificultad();

        if (estaDesmayado || recibiendoDaño) return;

        if (pelota == null) { BuscarPelota(); return; }

        Vector3 vecToBall      = pelota.transform.position - posicionInicial;
        float distanciaFrontal = Vector3.Dot(vecToBall, transform.forward);
        bool pelotaAlFrente    = distanciaFrontal > 0f && distanciaFrontal < distanciaActivacion;

        float velocidadHaciaArco = 0f;
        Vector3 velPelota = Vector3.zero;
        if (rbPelota != null)
        {
            velPelota = rbPelota.linearVelocity;
            velocidadHaciaArco = -Vector3.Dot(velPelota, transform.forward);
        }

        bool pelotaEntrando = pelotaAlFrente && velocidadHaciaArco > 0.5f;

        // Rotar suavemente hacia la pelota en todo momento (solo en Y, sin inclinar el cuerpo)
        if (!zambullendo)
            RotarHaciaPelota();

        if (pelotaEntrando)
        {
            if (!zambullendo && !cooldownPostZambullida)
            {
                // ── Calcular offset objetivo ──────────────────────────────────
                float targetOffset = 0f;
                float tiempoHastaArco = 0f;

                if (_prediccionEfectiva > 0f && velocidadHaciaArco > 1.0f)
                {
                    tiempoHastaArco = distanciaFrontal / velocidadHaciaArco;
                    if (tiempoHastaArco > 0f && tiempoHastaArco < 2.5f)
                    {
                        // Interpolar entre posición actual y predicha según el factor de predicción
                        Vector3 posPredicha = pelota.transform.position + velPelota * tiempoHastaArco;
                        float offsetPredicho = Vector3.Dot(posPredicha - posicionInicial, transform.right);
                        float offsetActual   = Vector3.Dot(vecToBall, transform.right);
                        targetOffset = Mathf.Lerp(offsetActual, offsetPredicho, _prediccionEfectiva);
                    }
                }
                else
                {
                    targetOffset = Vector3.Dot(vecToBall, transform.right);
                }

                targetOffset = Mathf.Clamp(targetOffset, -limiteLateral, limiteLateral);

                // Escalar velocidad según la rapidez del tiro
                float velMov = _velEfectiva;
                if (_prediccionEfectiva > 0f)
                {
                    velMov = Mathf.Max(_velEfectiva, _velEfectiva * (velPelota.magnitude / 8f));
                }

                currentOffset = Mathf.MoveTowards(currentOffset, targetOffset, velMov * Time.deltaTime);
                transform.position = posicionInicial + transform.right * currentOffset;

                // Animación y stance IK
                bool moviendose = Mathf.Abs(targetOffset - currentOffset) > 0.05f;
                CambiarAnimacion(moviendose ? "Walk" : "Idle");
                if (arqueroIK != null) arqueroIK.ActivarStance();

                // ── Decidir zambullida ────────────────────────────────────────
                bool triggerZambullida = false;

                // Predicción: si el balón llegará en breve (ventana de reacción ajustada por dificultad)
                float ventanaAnticipacion = Mathf.Lerp(0.55f, 0.2f, _prediccionEfectiva);
                if (tiempoHastaArco > _reaccionEfectiva && tiempoHastaArco < ventanaAnticipacion + _reaccionEfectiva)
                {
                    if (Mathf.Abs(targetOffset) < limiteLateral + 0.5f)
                        triggerZambullida = true;
                }

                // Fallback por proximidad física
                float distTotal = Vector3.Distance(transform.position, pelota.transform.position);
                if (distTotal < 4.5f && velPelota.magnitude > 4f)
                    triggerZambullida = true;

                if (triggerZambullida)
                    StartCoroutine(SecuenciaZambullida());
            }
            else if (!zambullendo && cooldownPostZambullida)
            {
                // Durante el cooldown post-zambullida, rastrear lateralmente pero más despacio
                float offsetSeguimiento = Vector3.Dot(vecToBall, transform.right);
                offsetSeguimiento = Mathf.Clamp(offsetSeguimiento, -limiteLateral, limiteLateral);
                currentOffset = Mathf.MoveTowards(currentOffset, offsetSeguimiento, (_velEfectiva * 0.6f) * Time.deltaTime);
                transform.position = posicionInicial + transform.right * currentOffset;
                CambiarAnimacion(Mathf.Abs(offsetSeguimiento - currentOffset) > 0.05f ? "Walk" : "Idle");
                if (arqueroIK != null) arqueroIK.ActivarStance();
            }
        }
        else
        {
            // Volver al centro cuando la pelota no se acerca
            if (!zambullendo)
            {
                float velRetorno = _velEfectiva * 0.45f;
                currentOffset = Mathf.MoveTowards(currentOffset, 0f, velRetorno * Time.deltaTime);
                transform.position = posicionInicial + transform.right * currentOffset;
                CambiarAnimacion(Mathf.Abs(currentOffset) > 0.05f ? "Walk" : "Idle");
                if (arqueroIK != null) arqueroIK.ActivarStance();

            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ROTACIÓN HACIA LA PELOTA
    // ─────────────────────────────────────────────────────────────────────────────
    private void RotarHaciaPelota()
    {
        if (pelota == null) return;

        // Dirección horizontal hacia la pelota (ignoramos Y para no inclinar el cuerpo)
        Vector3 dirHacia = pelota.transform.position - transform.position;
        dirHacia.y = 0f;

        if (dirHacia.sqrMagnitude < 0.01f) return;  // muy cerca, no rotar

        Quaternion rotObjetivo = Quaternion.LookRotation(dirHacia.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotObjetivo,
            velocidadRotacion * Time.deltaTime
        );

        // Cuando la rotación cambia, actualizar la rotación "original" del arquero
        // para que la zambullida (roll) parta siempre desde la orientación correcta
        rotacionOriginalGk = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // BUSCAR PELOTA
    // ─────────────────────────────────────────────────────────────────────────────
    private void BuscarPelota()
    {
        pelota = FindObjectOfType<PelotaFutbol>();
        if (pelota != null)
            rbPelota = pelota.GetComponent<Rigidbody>();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DAÑO
    // ─────────────────────────────────────────────────────────────────────────────
    public void RecibirDaño(Vector3 puntoImpacto)
    {
        if (estaDesmayado) return;

        if (zambullendo)
        {
            StopAllCoroutines();
            zambullendo = false;
            cooldownPostZambullida = false;
            transform.rotation = rotacionOriginalGk;
            transform.position = new Vector3(transform.position.x, posicionInicial.y, transform.position.z);
            if (arqueroIK != null) arqueroIK.DesactivarIKInstante();
        }

        vidaActual--;
        GenerarSangre(puntoImpacto);

        // Reproducir sonido de daño
        if (audioSource != null && sonidoDaño != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoDaño, 0.9f);
        }

        if (vidaActual <= 0)
            StartCoroutine(SecuenciaDesmayo());
        else
            StartCoroutine(SecuenciaDaño());
    }

    private IEnumerator SecuenciaDaño()
    {
        recibiendoDaño = true;
        CambiarAnimacion("GetDamage");
        yield return new WaitForSeconds(0.5f);
        recibiendoDaño = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ZAMBULLIDA
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator SecuenciaZambullida()
    {
        zambullendo = true;
        CambiarAnimacion("Jump");

        // Activar IK según tipo de atajada (se decide después de calcular la altura)
        // Lo activamos después del cálculo de posición más abajo

        Vector3 posInicio  = transform.position;
        Quaternion rotInicio = transform.rotation;

        // ── Calcular posición de destino ──────────────────────────────────────
        Vector3 posObjetivo = pelota.transform.position;

        if (rbPelota != null && _prediccionEfectiva > 0f)
        {
            Vector3 vel = rbPelota.linearVelocity;
            float velHaciaArco = -Vector3.Dot(vel, transform.forward);
            if (velHaciaArco > 0.5f)
            {
                float distF = Vector3.Dot(pelota.transform.position - posicionInicial, transform.forward);
                float t = distF / velHaciaArco;
                if (t > 0f)
                {
                    Vector3 predicha = pelota.transform.position + vel * t;
                    posObjetivo = Vector3.Lerp(pelota.transform.position, predicha, _prediccionEfectiva);
                }
            }
        }

        // Offset lateral
        float targetOffsetLateral = Vector3.Dot(posObjetivo - posicionInicial, transform.right);
        targetOffsetLateral = Mathf.Clamp(targetOffsetLateral, -limiteLateral, limiteLateral);

        // ── Altura coherente con el arco ──────────────────────────────────────
        float groundY = posicionInicial.y;
        float ballY   = posObjetivo.y;

        // La altura del salto sigue la altura real del balón, pero NUNCA supera el travesaño
        float alturaSalto = Mathf.Clamp(ballY - groundY, 0f, alturaMaxSalto);

        // Si la pelota está subiendo y parece baja, saltar al menos a media altura del arco
        if (alturaSalto < 0.3f && rbPelota != null && rbPelota.linearVelocity.y > 1f)
            alturaSalto = alturaMaxSalto * 0.5f;

        Vector3 posDestino = posicionInicial + transform.right * targetOffsetLateral + Vector3.up * alturaSalto;

        // ── Activar IK según tipo de atajada ─────────────────────────────────
        if (arqueroIK != null)
        {
            // Si la atajada es principalmente lateral (offset > umbral) → IK lateral
            // Si es principalmente alta → IK alto
            bool esLateral = Mathf.Abs(targetOffsetLateral) > 0.5f;
            bool esAlto    = alturaSalto > alturaMaxSalto * 0.4f;

            if (esLateral)
                arqueroIK.ActivarAtajaLateral();
            else if (esAlto)
                arqueroIK.ActivarAtajaAlto();
            else
                arqueroIK.ActivarAtajaLateral(); // por defecto lateral aunque sea central
        }

        // ── Rotación lateral (roll) ───────────────────────────────────────────
        float offsetDiferencia = targetOffsetLateral - currentOffset;
        float anguloGiro = 0f;
        if (Mathf.Abs(offsetDiferencia) > 0.15f)
            anguloGiro = offsetDiferencia > 0f ? -anguloZambullidaMax : anguloZambullidaMax;

        Quaternion rotDestino = rotacionOriginalGk * Quaternion.Euler(0f, 0f, anguloGiro);

        // ── Fase 1: Estirada ─────────────────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < duracionLanzamiento)
        {
            elapsed += Time.deltaTime;
            float tSuave = Mathf.SmoothStep(0f, 1f, elapsed / duracionLanzamiento);

            float x = Mathf.Lerp(posInicio.x, posDestino.x, tSuave);
            float z = Mathf.Lerp(posInicio.z, posDestino.z, tSuave);
            float y = Mathf.Lerp(posInicio.y, posDestino.y, Mathf.Sin(tSuave * Mathf.PI * 0.5f));

            transform.position = new Vector3(x, y, z);
            transform.rotation = Quaternion.Slerp(rotInicio, rotDestino, tSuave);
            yield return null;
        }

        transform.position = posDestino;
        transform.rotation = rotDestino;

        // ── Fase 2: Retención en el pico ────────────────────────────────────
        yield return new WaitForSeconds(duracionRetencion);

        // ── Fase 3: Caída al suelo ────────────────────────────────────────────
        Vector3 posAntesCaida  = transform.position;
        Quaternion rotAntesCaida = transform.rotation;
        Vector3 posSuelo       = new Vector3(posAntesCaida.x, groundY, posAntesCaida.z);

        elapsed = 0f;
        while (elapsed < duracionCaida)
        {
            elapsed += Time.deltaTime;
            float tSuave = Mathf.SmoothStep(0f, 1f, elapsed / duracionCaida);
            transform.position = Vector3.Lerp(posAntesCaida, posSuelo, tSuave);
            transform.rotation = Quaternion.Slerp(rotAntesCaida, rotacionOriginalGk, tSuave);
            yield return null;
        }

        transform.position = posSuelo;
        transform.rotation = rotacionOriginalGk;
        currentOffset = targetOffsetLateral;

        // Desactivar IK suavemente al aterrizar
        if (arqueroIK != null) arqueroIK.DesactivarIK();

        zambullendo = false;

        // ── Cooldown post-zambullida: rastrear la pelota pero no volver a zambullirse ──
        cooldownPostZambullida = true;
        yield return new WaitForSeconds(_recupEfectiva);
        cooldownPostZambullida = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DESMAYO
    // ─────────────────────────────────────────────────────────────────────────────
    private IEnumerator SecuenciaDesmayo()
    {
        estaDesmayado = true;
        CambiarAnimacion("Death");

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;

        Debug.Log("¡El arquero se ha desmayado tras recibir 3 hachazos!");

        if (MarcadorHUD.Instancia != null)
            MarcadorHUD.Instancia.RegistrarNoqueo("Arquero Payaso");

        yield return new WaitForSeconds(1.2f);
        if (animator != null) animator.speed = 0f;

        yield return new WaitForSeconds(3.8f);
        if (animator != null) animator.speed = 1f;
        CambiarAnimacion("Idle");

        if (col != null) col.isTrigger = false;

        vidaActual = vidaMax;
        estaDesmayado = false;

        Debug.Log("¡El arquero se recuperó y vuelve a atajar!");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // SANGRE
    // ─────────────────────────────────────────────────────────────────────────────
    private void GenerarSangre(Vector3 posicion)
    {
        GameObject contenedor = new GameObject("ParticulasSangre");
        contenedor.transform.position = posicion;

        for (int i = 0; i < 15; i++)
        {
            GameObject gota = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gota.transform.position = posicion + Random.insideUnitSphere * 0.1f;
            gota.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);
            gota.transform.parent = contenedor.transform;

            MeshRenderer mr = gota.GetComponent<MeshRenderer>();
            if (materialSangre != null)
                mr.sharedMaterial = materialSangre;
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

    // ─────────────────────────────────────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────────────────────────────────────
    private void CambiarAnimacion(string nuevaAnimacion)
    {
        if (animacionActual == nuevaAnimacion) return;
        animator.CrossFade(nuevaAnimacion, 0.15f);
        animacionActual = nuevaAnimacion;
    }
}
