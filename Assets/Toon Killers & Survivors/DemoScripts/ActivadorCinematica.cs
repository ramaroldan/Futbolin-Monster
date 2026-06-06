using UnityEngine;
using System.Collections;

public class ActivadorCinematica : MonoBehaviour
{
    [Header("Objetivo de Puntería")]
    [Tooltip("El arco de fútbol u objeto que la cámara mirará")]
    public Transform objetivoMirada;
    
    [Header("Ajustes de Zoom")]
    [Tooltip("El valor de FOV para hacer zoom en el arco (ej. 22f para un zoom potente)")]
    public float fovZoom = 22f;
    
    private bool activa = false;

    void OnTriggerEnter(Collider other)
    {
        if (activa) return;
        
        if (other.CompareTag("Player"))
        {
            // Intentar buscar el arco de fútbol en la escena si no se asignó en el inspector
            if (objetivoMirada == null)
            {
                GameObject arco = GameObject.Find("ArcoFutbol");
                if (arco != null)
                {
                    objetivoMirada = arco.transform;
                }
            }
            
            if (objetivoMirada != null)
            {
                StartCoroutine(SecuenciaCinematica(other.gameObject));
            }
            else
            {
                Debug.LogWarning("No se pudo iniciar la cinemática porque no se encontró el ArcoFutbol.");
            }
        }
    }

    private IEnumerator SecuenciaCinematica(GameObject jugador)
    {
        activa = true;
        
        // 1. Desactivar scripts de control para congelar al jugador y la cámara orbital
        ControladorTerceraPersona jugadorCtrl = jugador.GetComponent<ControladorTerceraPersona>();
        Camera camaraComp = Camera.main;
        CamaraTerceraPersona camaraCtrl = null;
        
        if (camaraComp != null)
        {
            camaraCtrl = camaraComp.GetComponent<CamaraTerceraPersona>();
        }
        
        if (jugadorCtrl != null) jugadorCtrl.enabled = false;
        if (camaraCtrl != null) camaraCtrl.enabled = false;
        
        if (camaraComp != null && objetivoMirada != null)
        {
            Transform camTransform = camaraComp.transform;
            
            // Guardar posición, rotación y FOV originales
            Vector3 posOriginal = camTransform.position;
            Quaternion rotOriginal = camTransform.rotation;
            float fovOriginal = camaraComp.fieldOfView;
            
            // Calcular el punto central del arco (elevado 1.25 metros desde la base)
            Vector3 puntoArco = objetivoMirada.position + Vector3.up * 1.25f;
            
            // Dirección hacia el arco
            Vector3 direccionHaciaArco = (puntoArco - posOriginal).normalized;
            
            // Posición final de zoom (avanzar 8 metros hacia el arco a lo largo de la línea de visión)
            Vector3 posZoomDestino = posOriginal + direccionHaciaArco * 8f;
            
            // FASE 1: Transición hacia el Arco (Paneo, Zoom y Desplazamiento - 2.2 segundos)
            float duracionFase1 = 2.2f;
            float tiempo = 0f;
            while (tiempo < duracionFase1)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionFase1);
                
                // Rotar para mirar al arco
                Vector3 dirActual = (puntoArco - camTransform.position).normalized;
                Quaternion rotObjetivo = Quaternion.LookRotation(dirActual);
                camTransform.rotation = Quaternion.Slerp(rotOriginal, rotObjetivo, t);
                
                // Desplazar posición un poco hacia adelante
                camTransform.position = Vector3.Lerp(posOriginal, posZoomDestino, t);
                
                // Hacer zoom en el FOV
                camaraComp.fieldOfView = Mathf.Lerp(fovOriginal, fovZoom, t);
                
                yield return null;
            }
            
            // Asegurar posiciones exactas al final de la transición
            camTransform.position = posZoomDestino;
            camTransform.rotation = Quaternion.LookRotation(puntoArco - posZoomDestino);
            camaraComp.fieldOfView = fovZoom;
            
            // FASE 2: Mantener Toma (1.5 segundos)
            yield return new WaitForSeconds(1.5f);
            
            // FASE 3: Transición de Retorno (1.5 segundos)
            float duracionFase3 = 1.5f;
            tiempo = 0f;
            Vector3 posZoomActual = camTransform.position;
            Quaternion rotZoomActual = camTransform.rotation;
            
            while (tiempo < duracionFase3)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionFase3);
                
                camTransform.position = Vector3.Lerp(posZoomActual, posOriginal, t);
                camTransform.rotation = Quaternion.Slerp(rotZoomActual, rotOriginal, t);
                camaraComp.fieldOfView = Mathf.Lerp(fovZoom, fovOriginal, t);
                
                yield return null;
            }
            
            // Asegurar retorno exacto
            camTransform.position = posOriginal;
            camTransform.rotation = rotOriginal;
            camaraComp.fieldOfView = fovOriginal;
            
            // 2. Sincronizar los ángulos de órbita de la cámara antes de reactivarla para evitar saltos bruscos
            if (camaraCtrl != null)
            {
                camaraCtrl.SincronizarAngulos();
            }
        }
        
        // 3. Reactivar controles del jugador y de la cámara orbital
        if (jugadorCtrl != null) jugadorCtrl.enabled = true;
        if (camaraCtrl != null) camaraCtrl.enabled = true;
        
        // 4. Autodestruirse para que la cinemática solo ocurra una vez en el juego
        Destroy(gameObject);
    }
}
