using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PayasoArquero : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    [Tooltip("Velocidad lateral del arquero para atajar")]
    public float velocidadMovimiento = 6.0f;
    
    [Tooltip("Distancia lateral máxima desde el centro del arco")]
    public float limiteLateral = 3.5f;
    
    [Tooltip("Distancia frontal a la que el arquero detecta el tiro y empieza a actuar")]
    public float distanciaActivacion = 25.0f;

    [Header("Ajustes de Vida y Daño")]
    public int vidaMax = 3;
    [Tooltip("Material para las partículas de sangre (ej. color rojo). Si se deja vacío, se generará uno rojo por defecto.")]
    public Material materialSangre;
    private int vidaActual = 3;
    private bool estaDesmayado = false;
    private bool recibiendoDaño = false;

    private Vector3 posicionInicial;
    private float currentOffset = 0f; // Offset actual a lo largo del eje local X (transform.right)
    private PelotaFutbol pelota;
    private Rigidbody rbPelota;
    private Animator animator;
    
    private bool zambullendo = false;
    private string animacionActual = "";

    void Start()
    {
        posicionInicial = transform.position;
        animator = GetComponent<Animator>();
        BuscarPelota();
        vidaActual = vidaMax;
    }

    void Update()
    {
        if (estaDesmayado || recibiendoDaño) return;

        if (pelota == null)
        {
            BuscarPelota();
            return;
        }

        // Vector desde la posición inicial del arquero hasta la pelota
        Vector3 vecToBall = pelota.transform.position - posicionInicial;
        
        // Distancia frontal (eje local Z del arquero)
        float distanciaFrontal = Vector3.Dot(vecToBall, transform.forward);
        
        // Verificar si la pelota está al frente y dentro de la distancia de activación
        bool pelotaAcercandose = distanciaFrontal > 0f && distanciaFrontal < distanciaActivacion;
        
        // Verificar si la pelota viaja hacia el arco (velocidad en dirección opuesta a transform.forward)
        float velocidadHaciaArco = 0f;
        if (rbPelota != null)
        {
            velocidadHaciaArco = -Vector3.Dot(rbPelota.linearVelocity, transform.forward);
        }
        
        if (pelotaAcercandose && velocidadHaciaArco > 0.5f)
        {
            if (!zambullendo)
            {
                // Proyectar la posición de la pelota en el eje lateral (local X)
                float targetOffset = Vector3.Dot(vecToBall, transform.right);
                
                // Limitar el desplazamiento para no golpear los postes
                targetOffset = Mathf.Clamp(targetOffset, -limiteLateral, limiteLateral);
                
                // Desplazarse suavemente
                currentOffset = Mathf.MoveTowards(currentOffset, targetOffset, velocidadMovimiento * Time.deltaTime);
                transform.position = posicionInicial + transform.right * currentOffset;
                
                // Animación de caminar o reposo
                if (Mathf.Abs(targetOffset - currentOffset) > 0.05f)
                {
                    CambiarAnimacion("Walk");
                }
                else
                {
                    CambiarAnimacion("Idle");
                }
                
                // Zambullida si la pelota está muy cerca y se mueve rápido
                float distanciaTotal = Vector3.Distance(transform.position, pelota.transform.position);
                if (distanciaTotal < 4.5f && rbPelota.linearVelocity.magnitude > 4f)
                {
                    StartCoroutine(SecuenciaZambullida());
                }
            }
        }
        else
        {
            // Regresar al centro si la pelota no se aproxima
            if (!zambullendo)
            {
                currentOffset = Mathf.MoveTowards(currentOffset, 0f, (velocidadMovimiento * 0.5f) * Time.deltaTime);
                transform.position = posicionInicial + transform.right * currentOffset;
                
                if (Mathf.Abs(currentOffset) > 0.05f)
                {
                    CambiarAnimacion("Walk");
                }
                else
                {
                    CambiarAnimacion("Idle");
                }
            }
        }
    }

    private void BuscarPelota()
    {
        pelota = FindObjectOfType<PelotaFutbol>();
        if (pelota != null)
        {
            rbPelota = pelota.GetComponent<Rigidbody>();
        }
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

    private IEnumerator SecuenciaDaño()
    {
        recibiendoDaño = true;
        CambiarAnimacion("GetDamage");
        yield return new WaitForSeconds(0.5f);
        recibiendoDaño = false;
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

            // Pintar de color rojo sangre usando el material asignado o uno genérico
            MeshRenderer mr = gota.GetComponent<MeshRenderer>();
            if (materialSangre != null)
            {
                mr.sharedMaterial = materialSangre;
            }
            else
            {
                mr.material.shader = Shader.Find("Standard");
                mr.material.color = new Color(0.9f, 0.0f, 0.0f); // Rojo sangre brillante por defecto
            }

            // Agregar física
            Rigidbody rbGota = gota.AddComponent<Rigidbody>();
            Vector3 dir = (gota.transform.position - posicion).normalized + Vector3.up * 0.5f;
            rbGota.linearVelocity = dir.normalized * Random.Range(3f, 6f);

            // Eliminar colisionadores
            Destroy(gota.GetComponent<Collider>());
        }

        Destroy(contenedor, 1.5f);
    }

    private IEnumerator SecuenciaDesmayo()
    {
        estaDesmayado = true;
        CambiarAnimacion("Death");

        // Cambiar colisionador a trigger para que la pelota pase a gol si es pateada
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;

        Debug.Log("¡El arquero se ha desmayado tras recibir 3 hachazos!");

        // Esperar a que se ejecute la caída de la animación (1.2 segundos)
        yield return new WaitForSeconds(1.2f);

        // Pausar el animator para congelarlo tumbado en el suelo
        if (animator != null) animator.speed = 0f;

        // Esperar los 3.8 segundos restantes en el piso
        yield return new WaitForSeconds(3.8f);

        // Reanudar velocidad normal y volver a levantarse
        if (animator != null) animator.speed = 1f;
        CambiarAnimacion("Idle");
        
        if (col != null) col.isTrigger = false;

        vidaActual = vidaMax;
        estaDesmayado = false;
        
        Debug.Log("¡El arquero se recuperó y vuelve a atajar!");
    }

    private IEnumerator SecuenciaZambullida()
    {
        zambullendo = true;
        CambiarAnimacion("Jump");
        
        Vector3 posInicioZambullida = transform.position;
        
        // Determinar a qué altura y posición lateral va el balón
        Vector3 vecToBall = pelota.transform.position - posicionInicial;
        float targetOffsetLateral = Vector3.Dot(vecToBall, transform.right);
        targetOffsetLateral = Mathf.Clamp(targetOffsetLateral, -limiteLateral, limiteLateral);
        
        // Altura del salto: si la pelota va alta, saltar más (hasta 2.2m sobre el suelo)
        float ballHeight = pelota.transform.position.y;
        float groundY = posicionInicial.y;
        float alturaSalto = Mathf.Clamp(ballHeight - groundY, 0f, 2.2f);
        if (alturaSalto < 0.5f && rbPelota != null && rbPelota.linearVelocity.y > 1f)
        {
            // Si la pelota está subiendo, forzar un salto mínimo
            alturaSalto = 1.0f;
        }
        
        // Destino de la zambullida
        Vector3 posDestino = posicionInicial + transform.right * targetOffsetLateral + Vector3.up * alturaSalto;
        
        // Fase 1: Lanzamiento/Estirada (0.28 segundos para llegar al punto máximo)
        float t = 0f;
        float duracionLanzamiento = 0.28f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionLanzamiento;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);
            
            float actualX = Mathf.Lerp(posInicioZambullida.x, posDestino.x, tSuave);
            float actualZ = Mathf.Lerp(posInicioZambullida.z, posDestino.z, tSuave);
            // Altura parabólica
            float actualY = Mathf.Lerp(posInicioZambullida.y, posDestino.y, Mathf.Sin(tSuave * Mathf.PI * 0.5f));
            
            transform.position = new Vector3(actualX, actualY, actualZ);
            yield return null;
        }
        
        transform.position = posDestino;
        
        // Fase 2: Mantener la postura en el aire para bloquear
        yield return new WaitForSeconds(0.3f);
        
        // Fase 3: Regreso al suelo y recuperación
        t = 0f;
        float duracionRecuperacion = 0.4f;
        Vector3 posAntesDeCaer = transform.position;
        Vector3 posSuelo = new Vector3(posAntesDeCaer.x, groundY, posAntesDeCaer.z);
        
        while (t < 1f)
        {
            t += Time.deltaTime / duracionRecuperacion;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(posAntesDeCaer, posSuelo, tSuave);
            yield return null;
        }
        
        transform.position = posSuelo;
        currentOffset = targetOffsetLateral;
        
        zambullendo = false;
    }

    private void CambiarAnimacion(string nuevaAnimacion)
    {
        if (animacionActual == nuevaAnimacion) return;
        animator.CrossFade(nuevaAnimacion, 0.15f);
        animacionActual = nuevaAnimacion;
    }
}
