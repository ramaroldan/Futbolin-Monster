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
        ActualizarMarcador();
        ConfigurarFisicaPostes();
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
            if (panelGolUI != null)
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
        
        // Activar UI festiva
        if (panelGolUI != null)
        {
            panelGolUI.SetActive(true);
            panelGolUI.transform.localScale = Vector3.zero;
        }
        
        // Activar luz festiva
        if (luzCelebracion != null)
        {
            luzCelebracion.enabled = true;
            luzCelebracion.color = new Color(0.0f, 1.0f, 0.8f); // Color neón cian
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
            new Color(1f, 0.2f, 0.2f),  // Rojo neón
            new Color(0.2f, 1f, 0.2f),  // Verde neón
            new Color(0.2f, 0.2f, 1f),  // Azul neón
            new Color(1f, 0.8f, 0f),    // Amarillo neón
            new Color(1f, 0.2f, 1f)     // Rosa neón
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
}
