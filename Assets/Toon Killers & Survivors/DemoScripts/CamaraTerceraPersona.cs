using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo a Seguir")]
    public Transform objetivo;
    
    [Header("Configuración de Distancia")]
    public float distancia = 4.5f; // Aumentada de 3.5f para ver más lejos
    public float offsetAltura = 1.6f;
    public float sensibilidadMouse = 2f;
    
    [Header("Límites de Rotación Vertical (Pitch)")]
    public float limitePitchMin = -20f;
    public float limitePitchMax = 60f;
    
    [Header("Suavizado")]
    public float tiempoSuavizadoRotacion = 0.05f;
    public float velocidadSuavizadoColision = 12f; // Velocidad para interpolar la distancia de colisión
    
    [Header("Colisión de la Cámara")]
    public LayerMask mascaraColision;
    public float radioCamara = 0.2f;
    
    private float yaw = 0f;
    private float pitch = 20f;
    
    private float distanciaActual;
    private float distanciaVisualActual;
    private float offsetHorizontalActual = 0f;
    private ControladorTerceraPersona controlador;
    
    void Start()
    {
        // Bloquear y ocultar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        distanciaActual = distancia;
        distanciaVisualActual = distancia;
        
        if (objetivo != null)
        {
            yaw = objetivo.eulerAngles.y;
            controlador = objetivo.GetComponent<ControladorTerceraPersona>();
        }
    }
    
    void LateUpdate()
    {
        if (objetivo == null) return;
        
        // Intentar obtener el controlador si no se tiene asignado
        if (controlador == null)
        {
            controlador = objetivo.GetComponent<ControladorTerceraPersona>();
        }
        
        float deltaX = 0f;
        float deltaY = 0f;
        
        // Leer la entrada del mouse mediante el nuevo Input System
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            deltaX = mouseDelta.x * 0.05f;
            deltaY = mouseDelta.y * 0.05f;
        }
        
        // Obtener la entrada del mouse
        yaw += deltaX * sensibilidadMouse;
        pitch -= deltaY * sensibilidadMouse;
        pitch = Mathf.Clamp(pitch, limitePitchMin, limitePitchMax);
        
        // Calcular la rotación deseada
        Quaternion rotacionDeseada = Quaternion.Euler(pitch, yaw, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, tiempoSuavizadoRotacion / Time.deltaTime);
        
        // Ajustar distancia y offset de hombro según si está apuntando
        float distanciaObjetivo = distancia;
        float offsetHorizontalObjetivo = 0f;
        
        if (controlador != null && controlador.estaApuntando)
        {
            distanciaObjetivo = 2.6f;        // Aumentada de 2.2f para mejor encuadre
            offsetHorizontalObjetivo = 0.8f;   // Desplazamiento sobre el hombro
        }
        
        distanciaActual = Mathf.Lerp(distanciaActual, distanciaObjetivo, Time.deltaTime * 8f);
        offsetHorizontalActual = Mathf.Lerp(offsetHorizontalActual, offsetHorizontalObjetivo, Time.deltaTime * 8f);
        
        // Posicionar el centro del objetivo (cabeza/hombros)
        Vector3 centroObjetivo = objetivo.position + Vector3.up * offsetAltura + transform.right * offsetHorizontalActual;
        
        // Calcular la distancia deseada considerando colisiones con el entorno
        float distanciaObjetivoColision = distanciaActual;
        RaycastHit hit;
        if (Physics.SphereCast(centroObjetivo, radioCamara, -transform.forward, out hit, distanciaActual, mascaraColision))
        {
            // Puntos de choque restando un pequeño margen de seguridad
            distanciaObjetivoColision = Mathf.Max(0.3f, hit.distance - 0.05f);
        }
        
        // Suavizar la transición de la distancia visual para evitar saltos bruscos (jitter) al chocar con tumbas o árboles
        distanciaVisualActual = Mathf.Lerp(distanciaVisualActual, distanciaObjetivoColision, Time.deltaTime * velocidadSuavizadoColision);
        
        // Calcular y aplicar la posición final de la cámara sin desajustes por el movimiento del personaje
        Vector3 posicionFinal = centroObjetivo - (transform.forward * distanciaVisualActual);
        transform.position = posicionFinal;
    }

    public void SincronizarAngulos()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, limitePitchMin, limitePitchMax);
    }
}
