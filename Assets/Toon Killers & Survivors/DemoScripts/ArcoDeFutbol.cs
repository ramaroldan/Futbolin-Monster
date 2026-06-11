using UnityEngine;
using System.Collections;

public class ArcoDeFutbol : MonoBehaviour
{
    [Header("Referencias de UI")]
    public UnityEngine.UI.Text textoMarcador; // Texto que muestra "Goles: X"
    public GameObject panelGolUI;           // Panel del cartel de ¡GOL!
    
    [Header("Efectos de Celebración")]
    public Light luzCelebracion;            // Luz del arco que parpadea
    public Transform puntoParticulas;       // Origen de las partículas festivas
    [Tooltip("Materiales personalizados para el confeti de gol. Si se asigna uno o más, se seleccionarán al azar.")]
    public Material[] materialesConfeti;
    
    private int contadorGoles = 0;
    private bool celebrando = false;
    private float tiempoCelebracion = 0f;
    
    private AudioClip clipSpookyGoal;
    private AudioSource audioSource;

    void Start()
    {
        if (panelGolUI != null)
        {
            panelGolUI.SetActive(false);
        }
        if (luzCelebracion != null)
        {
            luzCelebracion.enabled = false;
        }

        // Si ya existe MarcadorHUD (nuevo sistema unificado), desactivamos la UI antigua
        if (MarcadorHUD.Instancia != null)
        {
            if (textoMarcador != null) textoMarcador.gameObject.SetActive(false);
            if (panelGolUI != null) panelGolUI.SetActive(false);
            
            Canvas canvasViejo = GetComponentInParent<Canvas>();
            if (canvasViejo != null && canvasViejo.name == "CanvasFutbol")
            {
                canvasViejo.gameObject.SetActive(false);
            }
        }

        ActualizarMarcador();
        ConfigurarFisicaPostes();
        PrepararSonidoSpooky();
    }

    private void ConfigurarFisicaPostes()
    {
        // Configurar material físico para postes y travesaño para que la pelota rebote fuertemente
        PhysicsMaterial pmPoste = new PhysicsMaterial("FisicaPoste");
        pmPoste.bounciness = 0.85f; // Alto rebote en los palos
        pmPoste.bounceCombine = PhysicsMaterialCombine.Maximum;
        pmPoste.dynamicFriction = 0.2f;
        pmPoste.staticFriction = 0.2f;
        
        if (transform.parent != null)
        {
            foreach (Transform child in transform.parent)
            {
                if (child.name.Contains("Poste") || child.name.Contains("Travesaño"))
                {
                    Collider col = child.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.material = pmPoste;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (celebrando)
        {
            tiempoCelebracion += Time.deltaTime;
            
            // Animación pulsante del cartel de ¡GOL! en la pantalla
            if (panelGolUI != null && panelGolUI.activeSelf)
            {
                float escala = 1.0f + Mathf.Abs(Mathf.Sin(tiempoCelebracion * 10f)) * 0.25f;
                panelGolUI.transform.localScale = new Vector3(escala, escala, escala);
            }
            
            // Parpadeo de la luz neón de celebración
            if (luzCelebracion != null)
            {
                luzCelebracion.intensity = 8f + Mathf.PingPong(tiempoCelebracion * 25f, 8f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (celebrando) return;
        
        PelotaFutbol pelota = other.GetComponent<PelotaFutbol>();
        if (pelota != null)
        {
            StartCoroutine(SecuenciaGol(pelota));
        }
    }

    private IEnumerator SecuenciaGol(PelotaFutbol pelota)
    {
        celebrando = true;
        tiempoCelebracion = 0f;
        contadorGoles++;
        ActualizarMarcador();
        
        if (MarcadorHUD.Instancia != null)
        {
            MarcadorHUD.Instancia.RegistrarGol();
        }
        
        // Activar UI festiva
        if (panelGolUI != null && MarcadorHUD.Instancia == null)
        {
            panelGolUI.SetActive(true);
            panelGolUI.transform.localScale = Vector3.zero;
        }
        
        // Activar luz festiva
        if (luzCelebracion != null)
        {
            luzCelebracion.enabled = true;
            luzCelebracion.color = Random.value > 0.5f ? new Color(1.0f, 0.4f, 0.0f) : new Color(0.1f, 1.0f, 0.1f); // Naranja o Verde Halloween
        }
        
        // Reproducir sonido espeluznante sintetizado
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // Lanzar partículas festivas físicas (confeti neón)
        LanzarConfeti();
        
        // Detener físicamente la pelota para la celebración
        Rigidbody rbPelota = pelota.GetComponent<Rigidbody>();
        if (rbPelota != null)
        {
            rbPelota.linearVelocity = Vector3.zero;
            rbPelota.angularVelocity = Vector3.zero;
            rbPelota.isKinematic = true;
        }
        
        // Esperar la duración de la celebración (2.5 segundos)
        yield return new WaitForSeconds(2.5f);
        
        // Apagar efectos y ocultar UI
        if (panelGolUI != null)
        {
            panelGolUI.SetActive(false);
        }
        if (luzCelebracion != null)
        {
            luzCelebracion.enabled = false;
        }
        
        // Devolver la pelota al punto inicial de juego
        pelota.ResetearPelotaOriginal();
        
        celebrando = false;
    }

    private void ActualizarMarcador()
    {
        if (textoMarcador != null)
        {
            textoMarcador.text = "GOLES: " + contadorGoles;
        }
    }

    private void LanzarConfeti()
    {
        Vector3 origen = puntoParticulas != null ? puntoParticulas.position : transform.position + Vector3.up * 2f;
        GameObject contenedor = new GameObject("ConfetiGol");
        contenedor.transform.position = origen;
        
        Color[] colores = new Color[] {
            new Color(1.0f, 0.4f, 0.0f),  // Naranja Halloween
            new Color(0.1f, 1.0f, 0.1f),  // Verde Espectral
            new Color(0.5f, 0.0f, 0.8f),  // Púrpura Tenebroso
            new Color(1.0f, 0.8f, 0.0f)   // Amarillo Vela
        };
        
        int cantidadDePedazos = 30;
        for (int i = 0; i < cantidadDePedazos; i++)
        {
            GameObject pedazo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedazo.transform.position = origen + Random.insideUnitSphere * 0.5f;
            pedazo.transform.localScale = new Vector3(Random.Range(0.08f, 0.15f), Random.Range(0.08f, 0.15f), Random.Range(0.08f, 0.15f));
            pedazo.transform.parent = contenedor.transform;
            
            // Colores festivos o materiales personalizados
            MeshRenderer mr = pedazo.GetComponent<MeshRenderer>();
            if (materialesConfeti != null && materialesConfeti.Length > 0)
            {
                mr.sharedMaterial = materialesConfeti[Random.Range(0, materialesConfeti.Length)];
            }
            else
            {
                mr.material.shader = Shader.Find("Legacy Shaders/Self-Illumin/Diffuse"); // Auto-iluminado para brillar en la oscuridad
                mr.material.color = colores[Random.Range(0, colores.Length)];
            }
            
            // Agregar física
            Rigidbody rbPedazo = pedazo.AddComponent<Rigidbody>();
            Vector3 direccionSalida = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.5f), Random.Range(-0.8f, -0.2f)).normalized;
            rbPedazo.linearVelocity = direccionSalida * Random.Range(6f, 12f);
            rbPedazo.angularVelocity = Random.insideUnitSphere * 40f;
            
            // Destruir los colisionadores de las partículas para que no obstruyan el juego
            Destroy(pedazo.GetComponent<Collider>());
        }
        
        Destroy(contenedor, 3f);
    }

    private void PrepararSonidoSpooky()
    {
        int sampleRate = 44100;
        float duration = 2.2f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];
        
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            
            // Generar risa espectral (barrido de frecuencia descendente)
            // La frecuencia baja de 220Hz a 60Hz, con oscilación para simular carcajadas
            float sweep = Mathf.Max(0.1f, 1.0f - (t / duration));
            float laughter = Mathf.Sin(t * 14f); // 14 risotadas por segundo
            float freq = 180f * sweep + laughter * 40f * sweep;
            
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            
            // Añadir ruido blanco para textura de viento fantasmal
            float noise = (Random.value * 2f - 1f) * 0.12f * sweep;
            
            // Envolvente: ataque rápido y caída lenta modulada
            float env;
            if (t < 0.15f) env = t / 0.15f;
            else env = Mathf.Max(0f, 1f - (t - 0.15f) / (duration - 0.15f));
            
            // Añadir una vibración extra espeluznante
            float tremolo = 0.7f + 0.3f * Mathf.Sin(t * 30f);
            
            samples[i] = (wave + noise) * env * tremolo * 0.45f;
        }
        
        clipSpookyGoal = AudioClip.Create("SpookyGoalSound", numSamples, 1, sampleRate, false);
        clipSpookyGoal.SetData(samples, 0);
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clipSpookyGoal;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // Sonido 3D
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 60f;
    }
}
