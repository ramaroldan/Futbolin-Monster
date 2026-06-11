using UnityEngine;

public class FloatingGhost : MonoBehaviour
{
    [Header("Configuración de Flotación")]
    public float amplitude = 0.25f;
    public float frequency = 1.2f;
    
    [Header("Configuración de Rotación")]
    public float rotateAmount = 5f;

    private Vector3 startPos;
    private float randomOffset;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        
        // Desfasar los fantasmas para que no se muevan al unísono
        randomOffset = Random.Range(0f, 100f);
        frequency += Random.Range(-0.2f, 0.2f);
        amplitude += Random.Range(-0.05f, 0.05f);
    }

    void Update()
    {
        float time = Time.time + randomOffset;
        
        // Movimiento de flotación vertical suave
        float newY = startPos.y + Mathf.Sin(time * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotación temblorosa/espeluznante suave
        float wobbleZ = Mathf.Sin(time * frequency * 1.5f) * rotateAmount;
        float wobbleY = Mathf.Cos(time * frequency * 0.8f) * rotateAmount;
        transform.rotation = startRot * Quaternion.Euler(0, wobbleY, wobbleZ);
    }
}
