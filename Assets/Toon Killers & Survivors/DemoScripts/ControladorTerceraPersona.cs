using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControladorTerceraPersona : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 7f;
    public float velocidadRotacion = 10f;
    public float gravedad = -9.81f;
    public float alturaSalto = 1.8f;
    
    [Header("Configuración de Lanzamiento")]
    public GameObject prefabLanzable;
    public Transform puntoDeLanzamiento;
    public float enfriamientoLanzamiento = 0.8f;
    
    [Header("Ajustes del Objeto en Mano")]
    public Vector3 offsetPosicionMano = new Vector3(0.02f, 0.05f, 0f);
    public Vector3 offsetRotacionMano = new Vector3(0f, 90f, 90f);
    
    [Header("Ajustes del Proyectil Lanzado")]
    public Vector3 rotacionAdicionalProyectil = new Vector3(0f, 0f, 90f);
    
    [HideInInspector]
    public bool estaApuntando = false;
    
    [Header("Configuración de Fútbol")]
    public PelotaFutbol pelotaDriblando = null;
    public float fuerzaPatada = 14f;
    
    [Header("Ajustes de Conducción de Balón")]
    [Tooltip("Velocidad de seguimiento de la pelota al moverse (menor = más inercia, mayor = más pegada a los pies)")]
    public float suavizadoMovimientoConduccion = 10f;
    [Tooltip("Velocidad de frenado de la pelota al detenerse")]
    public float suavizadoParadaConduccion = 15f;
    [Tooltip("Frecuencia base de la oscilación lateral de la pelota (dribling)")]
    public float frecuenciaDribling = 7f;
    [Tooltip("Amplitud de la oscilación lateral de la pelota (dribling)")]
    public float amplitudDribling = 0.08f;
    
    private bool estaPateando = false;
    private float cooldownRecogidaPatada = 0f;
    
    [Header("Carga de Patada")]
    public float tiempoCargaMax = 1.2f;
    
    [Header("Agacharse y Cuerpo a Tierra")]
    public float velocidadAgachado = 1.8f;
    public float velocidadCuerpoTierra = 0.8f;
    private bool estaAgachado = false;
    private bool estaCuerpoTierra = false;
    private float tiempoInicioCarga = 0f;
    private bool cargandoPatada = false;
    private GameObject barraCargaFondo;
    private UnityEngine.UI.Image barraCargaRelleno;
    
    private CharacterController controlador;
    private Animator animador;
    private Transform camaraPrincipal;
    
    private Transform rightThigh;
    private Transform rightShin;
    private Transform leftThigh;
    private Transform leftShin;
    private Transform hips;
    private Vector3 rotThighOffset = Vector3.zero;
    private Vector3 rotShinOffset = Vector3.zero;
    private bool pateandoProcedural = false;
    
    private Vector3 velocidad;
    private bool estaEnElSuelo;
    private bool estaLanzando = false;
    private float tiempoUltimoLanzamiento = -10f;
    
    private string estadoAnimacionActual = "";
    private GameObject miraCanvas;
    private GameObject miraContenedor;
    
    // Nombres de los estados de animación en Dagger.controller
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_CAMINAR = "Walk";
    private const string ANIM_CORRER = "Run";
    private const string ANIM_SALTAR = "Jump";
    private const string ANIM_LANZAR = "MeleeKnifeAttack";
    private const string ANIM_AGACHADO = "LyingWalk";

    void Start()
    {
        controlador = GetComponent<CharacterController>();
        animador = GetComponent<Animator>();
        
        if (animador != null && animador.isHuman)
        {
            rightThigh = animador.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rightShin = animador.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            leftThigh = animador.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            leftShin = animador.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            hips = animador.GetBoneTransform(HumanBodyBones.Hips);
        }
        
        if (Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }
        else
        {
            GameObject objetoCamara = GameObject.FindGameObjectWithTag("MainCamera");
            if (objetoCamara != null)
            {
                camaraPrincipal = objetoCamara.transform;
            }
        }
        
        gameObject.tag = "Player";
        
        // Crear la mira en pantalla al iniciar
        CrearMiraUI();
        CrearBarraCargaUI();
    }

    void OnDestroy()
    {
        // Limpiar el objeto UI creado al destruirse el componente
        if (miraCanvas != null)
        {
            Destroy(miraCanvas);
        }
    }

    void Update()
    {
        estaEnElSuelo = controlador.isGrounded;
        if (estaEnElSuelo && velocidad.y < 0)
        {
            velocidad.y = -2f;
        }
        
        // Manejar la mecánica de apuntado
        ManejarApuntado();
        
        ManejarAgachado();
        
        if (!estaLanzando && !estaPateando)
        {
            MoverYRotar();
            ManejarSalto();
        }
        
        ManejarLanzamiento();
        ManejarFutbol();
        
        // Aplicar gravedad
        velocidad.y += gravedad * Time.deltaTime;
        controlador.Move(velocidad * Time.deltaTime);
        
        // Manejar el estado de animación
        ActualizarEstadoAnimacion();
    }

    private void ManejarApuntado()
    {
        var mouse = Mouse.current;
        bool clickDerechoPresionado = mouse != null && mouse.rightButton.isPressed;
        
        // Forzar el apuntado durante el inicio de la secuencia de lanzamiento (wind-up)
        // para que la cámara no se mueva/desajuste justo antes de instanciar el proyectil.
        bool enWindUp = estaLanzando && (Time.time < tiempoUltimoLanzamiento + 0.25f);
        
        if (estaEnElSuelo && (clickDerechoPresionado || enWindUp))
        {
            estaApuntando = true;
        }
        else
        {
            estaApuntando = false;
        }

        // Mostrar u ocultar la mira en la UI
        if (miraCanvas != null)
        {
            miraCanvas.SetActive(estaApuntando || cargandoPatada);
        }
        if (miraContenedor != null)
        {
            miraContenedor.SetActive(estaApuntando);
        }
        
        // Si está apuntando, rotar suavemente al personaje hacia la dirección de la cámara
        if (estaApuntando && camaraPrincipal != null)
        {
            Vector3 direccionMirada = camaraPrincipal.forward;
            direccionMirada.y = 0;
            if (direccionMirada != Vector3.zero)
            {
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMirada);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
            }
        }
    }

    private void MoverYRotar()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
        // Leer teclado mediante el nuevo Input System
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
        }
        
        Vector3 direccionEntrada = new Vector3(horizontal, 0f, vertical).normalized;
        
        if (direccionEntrada.magnitude >= 0.1f)
        {
            bool estaCorriendo = keyboard != null && keyboard.leftShiftKey.isPressed;
            float velocidadObjetivo;
            if (estaCuerpoTierra)
            {
                velocidadObjetivo = velocidadCuerpoTierra;
            }
            else if (estaAgachado)
            {
                velocidadObjetivo = velocidadAgachado;
            }
            else
            {
                velocidadObjetivo = estaCorriendo ? velocidadCorrer : velocidadCaminar;
            }
            
            if (camaraPrincipal != null)
            {
                Vector3 adelanteCam = camaraPrincipal.forward;
                Vector3 derechaCam = camaraPrincipal.right;
                adelanteCam.y = 0;
                derechaCam.y = 0;
                adelanteCam.Normalize();
                derechaCam.Normalize();
                
                Vector3 direccionMovimiento = (adelanteCam * direccionEntrada.z + derechaCam * direccionEntrada.x).normalized;
                
                // Mover el personaje
                controlador.Move(direccionMovimiento * velocidadObjetivo * Time.deltaTime);
                
                // Rotar el personaje en la dirección del movimiento (si no está apuntando)
                if (!estaApuntando)
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
                }
            }
            else
            {
                // Fallback a movimiento de ejes globales si no hay cámara
                controlador.Move(direccionEntrada * velocidadObjetivo * Time.deltaTime);
                if (!estaApuntando)
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionEntrada);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
                }
            }
        }
    }

    private void ManejarSalto()
    {
        if (estaAgachado || estaCuerpoTierra) return; // Impedir saltar si está agachado o cuerpo a tierra
        
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && estaEnElSuelo)
        {
            velocidad.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
        }
    }

    private void ManejarLanzamiento()
    {
        var mouse = Mouse.current;
        // Solo permite lanzar con Click Izquierdo si se está apuntando con el Click Derecho
        if (estaApuntando && mouse != null && mouse.leftButton.wasPressedThisFrame && 
            Time.time >= tiempoUltimoLanzamiento + enfriamientoLanzamiento && estaEnElSuelo && !estaLanzando)
        {
            StartCoroutine(SecuenciaDeLanzamiento());
        }
    }

    private IEnumerator SecuenciaDeLanzamiento()
    {
        estaLanzando = true;
        tiempoUltimoLanzamiento = Time.time;
        
        // Activar animación de lanzamiento
        CambiarEstadoAnimacion(ANIM_LANZAR);
        
        // Asegurar que el personaje mire directamente hacia la cámara
        if (camaraPrincipal != null)
        {
            Vector3 direccionMirada = camaraPrincipal.forward;
            direccionMirada.y = 0;
            if (direccionMirada != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direccionMirada);
            }
        }
        
        // Encontrar la mano derecha y equipar el proyectil visualmente
        Transform manoDerecha = BuscarManoDerecha(transform);
        GameObject proyectilVisual = null;
        if (manoDerecha != null && prefabLanzable != null)
        {
            proyectilVisual = Instantiate(prefabLanzable, manoDerecha);
            proyectilVisual.transform.localPosition = offsetPosicionMano;
            proyectilVisual.transform.localRotation = Quaternion.Euler(offsetRotacionMano);
            proyectilVisual.transform.localScale = Vector3.one;
            
            // Deshabilitar física y colisiones para el objeto sostenido en la mano
            Rigidbody rbVisual = proyectilVisual.GetComponent<Rigidbody>();
            if (rbVisual != null) rbVisual.isKinematic = true;
            
            Collider colVisual = proyectilVisual.GetComponent<Collider>();
            if (colVisual != null) colVisual.enabled = false;
            
            ObjetoLanzable scriptVisual = proyectilVisual.GetComponent<ObjetoLanzable>();
            if (scriptVisual != null) Destroy(scriptVisual);
        }
        
        // Esperar al momento de liberación en la animación (aprox. 0.25s)
        yield return new WaitForSeconds(0.25f);
        
        // Destruir el hacha visual de la mano
        if (proyectilVisual != null)
        {
            Destroy(proyectilVisual);
        }
        
        // Instanciar y lanzar el proyectil físico real
        InstanciarProyectil(manoDerecha);
        
        // Esperar a que termine la animación para devolver el control al jugador
        yield return new WaitForSeconds(0.45f);
        
        estaLanzando = false;
    }

    private void InstanciarProyectil(Transform manoDerecha)
    {
        if (prefabLanzable == null) return;
        
        // Para evitar trayectorias cruzadas y desvíos, instanciamos el proyectil en una posición central frente al jugador.
        Vector3 posLanzamiento = transform.position + transform.forward * 0.8f + Vector3.up * 1.3f;
        if (puntoDeLanzamiento != null)
        {
            posLanzamiento = puntoDeLanzamiento.position;
        }
        
        // El punto de impacto por defecto ahora usa la dirección frontal de la cámara (donde señala la mira)
        Vector3 puntoImpacto = camaraPrincipal != null ? (camaraPrincipal.position + camaraPrincipal.forward * 50f) : (posLanzamiento + transform.forward * 30f);
        
        if (camaraPrincipal != null)
        {
            Ray rayoMira = new Ray(camaraPrincipal.position, camaraPrincipal.forward);
            
            // Usar Physics.RaycastAll para recopilar todos los impactos e ignorar los inválidos (jugador, triggers invisibles, colisiones pegadas a la cámara)
            RaycastHit[] hits = Physics.RaycastAll(rayoMira, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            
            // Ordenar los impactos por distancia de menor a mayor
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            foreach (var hit in hits)
            {
                // Ignorar colisiones con el propio jugador o sus hijos
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }
                
                // Ignorar colisiones extremadamente cercanas (clipping de la cámara, etc.)
                if (hit.distance < 1.5f)
                {
                    continue;
                }
                
                puntoImpacto = hit.point;
                break;
            }
        }
        
        // Dirección de lanzamiento real y precisa desde el origen hacia el punto apuntado por la mira
        Vector3 direccionLanzamiento = (puntoImpacto - posLanzamiento).normalized;
        Quaternion rotacionHaciaDestino = Quaternion.LookRotation(direccionLanzamiento);
        
        // Instanciar aplicando la dirección hacia el objetivo
        GameObject proyectil = Instantiate(prefabLanzable, posLanzamiento, rotacionHaciaDestino * Quaternion.Euler(rotacionAdicionalProyectil));
        
        // Inicializar el script ObjetoLanzable con la dirección de lanzamiento real y precisa
        ObjetoLanzable scriptProyectil = proyectil.GetComponent<ObjetoLanzable>();
        if (scriptProyectil == null)
        {
            scriptProyectil = proyectil.AddComponent<ObjetoLanzable>();
        }
        
        scriptProyectil.Lanzar(direccionLanzamiento);
    }

    private void ActualizarEstadoAnimacion()
    {
        if (estaLanzando) return;
        if (estaPateando) return;
        
        // Restablecer velocidad del animator por defecto
        animador.speed = 1f;
        
        if (!estaEnElSuelo)
        {
            CambiarEstadoAnimacion(ANIM_SALTAR);
        }
        else if (estaCuerpoTierra)
        {
            CambiarEstadoAnimacion(ANIM_AGACHADO);
            
            // Pausar el animator si está quieto para simular CrouchIdle tumbado (cuerpo a tierra)
            float horizontal = 0f;
            float vertical = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
            }
            if (new Vector3(horizontal, 0f, vertical).magnitude < 0.1f)
            {
                animador.speed = 0f;
            }
        }
        else if (estaAgachado)
        {
            // Para el modo agachado con Ctrl, usamos animaciones normales de caminar/idle,
            // y en LateUpdate() doblamos procedimentalmente las rodillas y bajamos la cadera
            float horizontal = 0f;
            float vertical = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
            }
            if (new Vector3(horizontal, 0f, vertical).magnitude >= 0.1f)
            {
                CambiarEstadoAnimacion(ANIM_CAMINAR);
            }
            else
            {
                CambiarEstadoAnimacion(ANIM_IDLE);
            }
        }
        else
        {
            float horizontal = 0f;
            float vertical = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
            }
            
            Vector3 direccionEntrada = new Vector3(horizontal, 0f, vertical);
            
            if (direccionEntrada.magnitude >= 0.1f)
            {
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                {
                    CambiarEstadoAnimacion(ANIM_CORRER);
                }
                else
                {
                    CambiarEstadoAnimacion(ANIM_CAMINAR);
                }
            }
            else
            {
                CambiarEstadoAnimacion(ANIM_IDLE);
            }
        }
    }

    private void CambiarEstadoAnimacion(string nuevoEstado)
    {
        if (estadoAnimacionActual == nuevoEstado) return;
        
        animador.CrossFade(nuevoEstado, 0.15f);
        estadoAnimacionActual = nuevoEstado;
    }

    private Transform BuscarManoDerecha(Transform parent)
    {
        if (parent.name.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }
        foreach (Transform child in parent)
        {
            Transform found = BuscarManoDerecha(child);
            if (found != null) return found;
        }
        return null;
    }

    private void CrearMiraUI()
    {
        // Crear Canvas de la mira
        miraCanvas = new GameObject("MiraCanvas");
        miraCanvas.layer = 5; // Asignar a la capa UI (Layer 5) para que la cámara del juego lo renderice obligatoriamente
        
        Canvas canvas = miraCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.enabled = true;
        
        // Agregar CanvasScaler con escala adaptativa según resolución
        UnityEngine.UI.CanvasScaler scaler = miraCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        miraCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Contenedor de la mira
        miraContenedor = new GameObject("MiraContenedor");
        miraContenedor.layer = 5;
        miraContenedor.transform.SetParent(miraCanvas.transform, false);
        
        RectTransform rectContenedor = miraContenedor.AddComponent<RectTransform>();
        rectContenedor.sizeDelta = new Vector2(40f, 40f);
        rectContenedor.anchoredPosition = Vector2.zero;
        miraContenedor.transform.localScale = Vector3.one;
        
        // 1. Punto central rojo (mira central)
        CrearElementoMira(miraContenedor, "Punto", new Vector2(6f, 6f), Vector2.zero, Color.red);
        
        // 2. Línea izquierda
        CrearElementoMira(miraContenedor, "LineaIzq", new Vector2(10f, 2f), new Vector2(-12f, 0f), Color.white);
        
        // 3. Línea derecha
        CrearElementoMira(miraContenedor, "LineaDer", new Vector2(10f, 2f), new Vector2(12f, 0f), Color.white);
        
        // 4. Línea superior
        CrearElementoMira(miraContenedor, "LineaSup", new Vector2(2f, 10f), new Vector2(0f, 12f), Color.white);
        
        // 5. Línea inferior
        CrearElementoMira(miraContenedor, "LineaInf", new Vector2(2f, 10f), new Vector2(0f, -12f), Color.white);
        
        // Desactivado por defecto
        miraCanvas.SetActive(false);
    }

    private void CrearElementoMira(GameObject padre, string nombre, Vector2 tamaño, Vector2 posicion, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.layer = 5; // Capa UI
        go.transform.SetParent(padre.transform, false);
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = color;
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = tamaño;
        rect.anchoredPosition = posicion;
        go.transform.localScale = Vector3.one;
    }

    public bool EstaDisponibleParaConducir()
    {
        // No se puede conducir la pelota si está en el aire, lanzando, pateando, o en cooldown
        return estaEnElSuelo && !estaLanzando && !estaPateando && Time.time > cooldownRecogidaPatada;
    }

    public void IniciarDribling(PelotaFutbol pelota)
    {
        pelotaDriblando = pelota;
        pelotaDriblando.IniciarConduccion(transform);
    }

    private void ManejarFutbol()
    {
        // Si estamos conduciendo el balón, actualizar su posición frente a los pies
        if (pelotaDriblando != null)
        {
            if (!pelotaDriblando.estaConducida)
            {
                pelotaDriblando = null;
                // Si la pelota se perdió, ocultar la barra de carga si estaba activa
                if (cargandoPatada)
                {
                    cargandoPatada = false;
                    if (barraCargaFondo != null) barraCargaFondo.SetActive(false);
                    if (miraCanvas != null) miraCanvas.SetActive(estaApuntando);
                }
                return;
            }
            
            // Colocar la pelota adelante de los pies del jugador con oscilación lateral (dribling de pies) y suavizado
            float velocidadJugador = controlador != null ? controlador.velocity.magnitude : 0f;
            
            // Oscilación lateral según el movimiento para simular llevarla con ambos pies
            float multiplicadorVelocidadSway = velocidadJugador > 4f ? 1.7f : 1f;
            float frecuenciaSway = frecuenciaDribling * multiplicadorVelocidadSway;
            float amplitudSway = velocidadJugador > 0.1f ? amplitudDribling : 0f;
            Vector3 oscilacionLateral = transform.right * Mathf.Sin(Time.time * frecuenciaSway) * amplitudSway;
            
            // Retraso leve al acelerar/frenar (oscilación adelante-atrás)
            float oscilacionFrontal = velocidadJugador > 0.1f ? (Mathf.Cos(Time.time * frecuenciaSway) * 0.02f) : 0f;
            
            // Altura y offset según el radio del balón
            float radioPelota = pelotaDriblando.transform.localScale.x * 0.5f;
            Vector3 posIdealBalon = transform.position + transform.forward * (0.6f + radioPelota + oscilacionFrontal) + Vector3.up * radioPelota + oscilacionLateral;
            
            // Interpolar suavemente para que la pelota tenga inercia y no parezca rígidamente pegada
            float velocidadSuavizado = velocidadJugador > 0.1f ? suavizadoMovimientoConduccion : suavizadoParadaConduccion;
            pelotaDriblando.transform.position = Vector3.Lerp(pelotaDriblando.transform.position, posIdealBalon, Time.deltaTime * velocidadSuavizado);
            
            // Forzar velocidad a cero mientras se conduce para evitar física errática
            Rigidbody rbPelota = pelotaDriblando.GetComponent<Rigidbody>();
            if (rbPelota != null)
            {
                rbPelota.linearVelocity = Vector3.zero;
                rbPelota.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // Si no tenemos la pelota, asegurar que la barra de carga esté oculta
            if (cargandoPatada)
            {
                cargandoPatada = false;
                if (barraCargaFondo != null) barraCargaFondo.SetActive(false);
                if (miraCanvas != null) miraCanvas.SetActive(estaApuntando);
            }
        }
        
        // Manejar la carga de la patada al mantener presionada la tecla E
        var keyboard = Keyboard.current;
        if (keyboard != null && pelotaDriblando != null && !estaPateando && !estaLanzando)
        {
            if (keyboard.eKey.wasPressedThisFrame && !cargandoPatada)
            {
                cargandoPatada = true;
                tiempoInicioCarga = Time.time;
                if (miraCanvas != null) miraCanvas.SetActive(true);
                if (barraCargaFondo != null)
                {
                    barraCargaFondo.SetActive(true);
                    if (barraCargaRelleno != null)
                    {
                        barraCargaRelleno.rectTransform.anchorMax = new Vector2(0f, 1f);
                        barraCargaRelleno.color = Color.yellow;
                    }
                }
            }
            
            if (cargandoPatada)
            {
                float ratio = Mathf.Clamp01((Time.time - tiempoInicioCarga) / tiempoCargaMax);
                
                // Actualizar la barra de UI
                if (barraCargaRelleno != null)
                {
                    barraCargaRelleno.rectTransform.anchorMax = new Vector2(ratio, 1f);
                    // Cambiar color de amarillo a rojo según la carga
                    barraCargaRelleno.color = Color.Lerp(Color.yellow, Color.red, ratio);
                }
                
                // Si suelta la tecla E o alcanza la carga máxima, se realiza la patada
                if (!keyboard.eKey.isPressed || ratio >= 1.0f)
                {
                    cargandoPatada = false;
                    if (barraCargaFondo != null) barraCargaFondo.SetActive(false);
                    if (miraCanvas != null) miraCanvas.SetActive(estaApuntando);
                    StartCoroutine(SecuenciaPatada(ratio));
                }
            }
        }
    }

    private IEnumerator SecuenciaPatada(float chargeRatio)
    {
        estaPateando = true;
        cooldownRecogidaPatada = Time.time + 0.8f; // Impedir que el pie aspire el balón inmediatamente (cooldown de 0.8s)
        
        StartCoroutine(SecuenciaPatadaProcedural());
        
        // Ejecutar animación de patada usando MeleeWeaponAttack2
        CambiarEstadoAnimacion("MeleeWeaponAttack2");
        
        // Esperar al momento de impacto físico en la animación (0.15s)
        yield return new WaitForSeconds(0.15f);
        
        if (pelotaDriblando != null)
        {
            Vector3 dirPatada;
            
            // Si está apuntando con la cámara, usar la dirección en que apunta la cámara
            if (estaApuntando && camaraPrincipal != null)
            {
                dirPatada = camaraPrincipal.forward;
                // Si la cámara apunta muy hacia abajo, forzar que la dirección del chuto no vaya hacia el suelo
                if (dirPatada.y < 0) dirPatada.y = 0;
            }
            else
            {
                // Si no está apuntando, usar el frente del personaje en el plano horizontal
                dirPatada = transform.forward;
                dirPatada.y = 0;
                dirPatada.Normalize();
            }
            
            // Agregar elevación vertical en base a la carga.
            // Para tiros con poca carga (tiros rasos o a media altura), la elevación es baja (de 0.08f a 0.15f).
            // Para tiros con mucha carga, queremos que se eleve bastante (hasta 0.75f).
            float elevacionVertical = Mathf.Lerp(0.08f, 0.75f, chargeRatio);
            dirPatada.y += elevacionVertical;
            dirPatada = dirPatada.normalized;
            
            // Fuerza de patada: cuanto más carga, más fuerte sale (de 9f a 24f)
            float fuerza = Mathf.Lerp(9f, 24f, chargeRatio);
            Vector3 fuerzaTotal = dirPatada * fuerza;
            
            // Soltar el balón antes de patear
            PelotaFutbol pelotaAPatear = pelotaDriblando;
            pelotaDriblando = null;
            
            pelotaAPatear.Patear(fuerzaTotal);
        }
        
        // Esperar a que acabe la animación antes de devolver el control (0.35s)
        yield return new WaitForSeconds(0.35f);
        estaPateando = false;
    }

    private void CrearBarraCargaUI()
    {
        if (miraCanvas == null) return;
        
        // Fondo de la barra
        barraCargaFondo = new GameObject("BarraCargaFondo");
        barraCargaFondo.layer = 5; // Layer UI
        barraCargaFondo.transform.SetParent(miraCanvas.transform, false);
        
        RectTransform rtFondo = barraCargaFondo.AddComponent<RectTransform>();
        rtFondo.anchorMin = new Vector2(0.5f, 0.5f);
        rtFondo.anchorMax = new Vector2(0.5f, 0.5f);
        rtFondo.pivot = new Vector2(0.5f, 0.5f);
        rtFondo.anchoredPosition = new Vector2(0f, -120f); // Posicionada abajo del crosshair
        rtFondo.sizeDelta = new Vector2(250f, 20f);
        
        UnityEngine.UI.Image imgFondo = barraCargaFondo.AddComponent<UnityEngine.UI.Image>();
        imgFondo.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        
        // Relleno de la barra
        GameObject goRelleno = new GameObject("BarraCargaRelleno");
        goRelleno.layer = 5;
        goRelleno.transform.SetParent(barraCargaFondo.transform, false);
        
        RectTransform rtRelleno = goRelleno.AddComponent<RectTransform>();
        rtRelleno.anchorMin = new Vector2(0f, 0f);
        rtRelleno.anchorMax = new Vector2(0f, 1f);
        rtRelleno.pivot = new Vector2(0f, 0.5f);
        rtRelleno.anchoredPosition = Vector2.zero;
        rtRelleno.sizeDelta = Vector2.zero;
        
        barraCargaRelleno = goRelleno.AddComponent<UnityEngine.UI.Image>();
        barraCargaRelleno.color = Color.yellow;
        
        // Ocultar por defecto
        barraCargaFondo.SetActive(false);
    }

    private void ManejarAgachado()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        bool quererCuerpoTierra = keyboard.cKey.isPressed;
        bool quererAgacharse = keyboard.leftCtrlKey.isPressed;
        
        if (estaEnElSuelo)
        {
            if (quererCuerpoTierra)
            {
                if (!estaCuerpoTierra)
                {
                    estaCuerpoTierra = true;
                    estaAgachado = false;
                    controlador.height = 0.5f;
                    controlador.center = new Vector3(0f, 0.25f, 0f);
                }
            }
            else if (quererAgacharse)
            {
                if (!estaAgachado || estaCuerpoTierra)
                {
                    estaCuerpoTierra = false;
                    estaAgachado = true;
                    controlador.height = 1.1f;
                    controlador.center = new Vector3(0f, 0.55f, 0f);
                }
            }
            else
            {
                if (estaAgachado || estaCuerpoTierra)
                {
                    // Comprobar si hay techo arriba (Raycast vertical de 1.5m hacia arriba)
                    bool techoBloqueado = false;
                    Ray rayoTecho = new Ray(transform.position + Vector3.up * 0.5f, Vector3.up);
                    if (Physics.Raycast(rayoTecho, out RaycastHit hitInfo, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hitInfo.transform != transform && !hitInfo.transform.IsChildOf(transform))
                        {
                            techoBloqueado = true;
                        }
                    }
                    
                    if (!techoBloqueado)
                    {
                        estaCuerpoTierra = false;
                        estaAgachado = false;
                        controlador.height = 1.8f;
                        controlador.center = new Vector3(0f, 0.9f, 0f);
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (pateandoProcedural)
        {
            if (rightThigh != null)
            {
                rightThigh.localRotation *= Quaternion.Euler(rotThighOffset);
            }
            if (rightShin != null)
            {
                rightShin.localRotation *= Quaternion.Euler(rotShinOffset);
            }
        }
        else if (estaAgachado && !estaCuerpoTierra)
        {
            // Agachado procedural (Ctrl): doblar las piernas y bajar la cadera para que no se acueste
            if (hips != null)
            {
                hips.localPosition += new Vector3(0f, -0.45f, 0f);
            }
            if (rightThigh != null) rightThigh.localRotation *= Quaternion.Euler(-35f, 0f, 0f);
            if (leftThigh != null) leftThigh.localRotation *= Quaternion.Euler(-35f, 0f, 0f);
            if (rightShin != null) rightShin.localRotation *= Quaternion.Euler(70f, 0f, 0f);
            if (leftShin != null) leftShin.localRotation *= Quaternion.Euler(70f, 0f, 0f);
        }
    }

    private IEnumerator SecuenciaPatadaProcedural()
    {
        pateandoProcedural = true;
        rotThighOffset = Vector3.zero;
        rotShinOffset = Vector3.zero;
        
        // Fase 1: Cargar la pierna hacia atrás (Wind-up) - 0.12 segundos
        float t = 0f;
        float duracionWindup = 0.12f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionWindup;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);
            
            // Flexionar muslo hacia atrás (+25) y rodilla hacia atrás (+65)
            rotThighOffset.x = Mathf.Lerp(0f, 25f, tSuave);
            rotShinOffset.x = Mathf.Lerp(0f, 65f, tSuave);
            yield return null;
        }
        
        // Fase 2: Impulso/Lanzar el pie hacia adelante (Swing/Impacto) - 0.10 segundos
        t = 0f;
        float duracionSwing = 0.10f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionSwing;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);
            
            // Extender pierna hacia adelante (-55) y estirar rodilla (+5)
            rotThighOffset.x = Mathf.Lerp(25f, -55f, tSuave);
            rotShinOffset.x = Mathf.Lerp(65f, 5f, tSuave);
            yield return null;
        }
        
        // Fase 3: Retorno a la postura normal (Recovery) - 0.28 segundos
        t = 0f;
        float duracionRecovery = 0.28f;
        Vector3 rotThighFinal = rotThighOffset;
        Vector3 rotShinFinal = rotShinOffset;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionRecovery;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);
            
            rotThighOffset.x = Mathf.Lerp(rotThighFinal.x, 0f, tSuave);
            rotShinOffset.x = Mathf.Lerp(rotShinFinal.x, 0f, tSuave);
            yield return null;
        }
        
        rotThighOffset = Vector3.zero;
        rotShinOffset = Vector3.zero;
        pateandoProcedural = false;
    }
}
