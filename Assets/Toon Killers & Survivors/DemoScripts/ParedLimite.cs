using UnityEngine;

public class ParedLimite : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PelotaFutbol pelota = other.GetComponent<PelotaFutbol>();
        if (pelota != null)
        {
            // Resetear la pelota a su posición de inicio y detener velocidad
            pelota.ResetearPelotaOriginal();
            Debug.Log("¡Pelota fuera del campo! Respawneando en el punto inicial.");
        }
    }
}
