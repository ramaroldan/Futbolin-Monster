using UnityEngine;

/// <summary>
/// IK completo para el arquero:
///   · POSTURA DE ESPERA  → manos abiertas hacia la cámara/pelota, pies separados en stance
///   · ATAJADA LATERAL    → manos se estiran hacia la pelota, pies acompañan el vuelo
///   · ATAJADA ALTA       → manos suben hacia la pelota, pies cuelgan naturalmente
///
/// Requiere "IK Pass" activado en el Base Layer del Animator Controller.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ArqueroIK : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    // REFERENCIAS
    // ═══════════════════════════════════════════════════════════════════════════
    [Header("Referencias")]
    [Tooltip("La pelota de fútbol. Se busca automáticamente si se deja vacío.")]
    public Transform pelota;

    // ═══════════════════════════════════════════════════════════════════════════
    // MODO ARQUERO — POSTURA DE ESPERA
    // ═══════════════════════════════════════════════════════════════════════════
    [Header("── Postura de Espera (Stance) ──────────────────────")]
    [Tooltip("Activa la postura de arquero cuando está esperando un tiro (manos abiertas, pies separados).")]
    public bool activarStance = true;

    [Tooltip("Peso del IK de stance (0 = animación normal, 1 = postura completa de arquero).")]
    [Range(0f, 1f)]
    public float pesoStance = 0.65f;

    [Tooltip("Distancia lateral a la que se separan los pies del centro del cuerpo en stance.")]
    public float separacionPiesStance = 0.22f;

    [Tooltip("Separación lateral entre manos en postura de espera.")]
    public float separacionManosStance = 0.30f;

    [Tooltip("Altura de las manos en postura de espera (relativa a la cadera).")]
    public float alturaManosStance = 0.28f;

    [Tooltip("Qué tan adelante del cuerpo van las manos en espera.")]
    public float profundidadManosStance = 0.22f;

    // ═══════════════════════════════════════════════════════════════════════════
    // ATAJADA
    // ═══════════════════════════════════════════════════════════════════════════
    [Header("── Atajada ──────────────────────────────────────────")]
    [Tooltip("Peso máximo del IK durante la zambullida.")]
    [Range(0f, 1f)]
    public float pesoMaxAtajada = 0.95f;

    [Tooltip("Velocidad de transición (lerp) al activar/desactivar el IK de atajada.")]
    public float velocidadTransicion = 8f;

    [Tooltip("Offset hacia adelante para que las manos queden frente al cuerpo al atajar.")]
    public float offsetAdelante = 0.12f;

    [Tooltip("Offset Y extra para atajadas altas (empuja las manos un poco más arriba de la pelota).")]
    public float offsetAlto = 0.35f;

    [Tooltip("Separación entre manos durante la atajada lateral.")]
    public float separacionManosAtajada = 0.20f;

    // ═══════════════════════════════════════════════════════════════════════════
    // PIES — ATAJADA
    // ═══════════════════════════════════════════════════════════════════════════
    [Header("── Pies en Vuelo ────────────────────────────────────")]
    [Tooltip("En zambullida lateral, los pies se abren hacia los lados siguiendo el cuerpo.")]
    public float separacionPiesVuelo = 0.40f;

    [Tooltip("Altura a la que suben los pies respecto al suelo durante la zambullida.")]
    public float alturaPiesVuelo = 0.25f;

    [Tooltip("Peso del IK de los pies durante la zambullida.")]
    [Range(0f, 1f)]
    public float pesoMaxPiesVuelo = 0.80f;

    // ═══════════════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ═══════════════════════════════════════════════════════════════════════════
    public enum ModoIK { Stance, Lateral, Alto }

    [Header("Estado (solo lectura en Inspector)")]
    [HideInInspector] public ModoIK modoActual   = ModoIK.Stance;
    [HideInInspector] public float pesoIKActual  = 0f;
    [HideInInspector] public float pesoPiesActual = 0f;

    private Animator  animator;
    private float     pesoObjetivo    = 0f;
    private float     pesoPiesObjetivo = 0f;
    private bool      enAtajada       = false;
    private float     groundY;          // Y del suelo del arquero

    // ═══════════════════════════════════════════════════════════════════════════
    // INIT
    // ═══════════════════════════════════════════════════════════════════════════
    void Start()
    {
        animator = GetComponent<Animator>();
        groundY  = transform.position.y;

        if (pelota == null)
        {
            PelotaFutbol pb = FindObjectOfType<PelotaFutbol>();
            if (pb != null) pelota = pb.transform;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Activar la postura de espera (stance). Llamar cuando no está zambullendo.</summary>
    public void ActivarStance()
    {
        enAtajada     = false;
        modoActual    = ModoIK.Stance;
        pesoObjetivo  = activarStance ? pesoStance : 0f;
        pesoPiesObjetivo = activarStance ? pesoStance : 0f;
    }

    /// <summary>Activar IK de atajada lateral.</summary>
    public void ActivarAtajaLateral()
    {
        enAtajada        = true;
        modoActual       = ModoIK.Lateral;
        pesoObjetivo     = pesoMaxAtajada;
        pesoPiesObjetivo = pesoMaxPiesVuelo;
    }

    /// <summary>Activar IK de atajada alta.</summary>
    public void ActivarAtajaAlto()
    {
        enAtajada        = true;
        modoActual       = ModoIK.Alto;
        pesoObjetivo     = pesoMaxAtajada;
        pesoPiesObjetivo = pesoMaxPiesVuelo * 0.6f;
    }

    /// <summary>Desactivar atajada y volver a stance suavemente.</summary>
    public void DesactivarIK()
    {
        enAtajada = false;
        // Vuelve a stance tras la zambullida
        modoActual       = ModoIK.Stance;
        pesoObjetivo     = activarStance ? pesoStance : 0f;
        pesoPiesObjetivo = activarStance ? pesoStance : 0f;
    }

    /// <summary>Reset instantáneo (cuando recibe daño en vuelo).</summary>
    public void DesactivarIKInstante()
    {
        enAtajada        = false;
        modoActual       = ModoIK.Stance;
        pesoIKActual     = 0f;
        pesoPiesActual   = 0f;
        pesoObjetivo     = activarStance ? pesoStance : 0f;
        pesoPiesObjetivo = activarStance ? pesoStance : 0f;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ON ANIMATOR IK
    // ═══════════════════════════════════════════════════════════════════════════
    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || pelota == null) return;

        // Actualizar Y del suelo (puede haber cambiado si el personaje se reposicionó)
        if (!enAtajada) groundY = transform.position.y;

        // Interpolación de pesos
        float dt = velocidadTransicion * Time.deltaTime;
        pesoIKActual   = Mathf.Lerp(pesoIKActual,   pesoObjetivo,     dt);
        pesoPiesActual = Mathf.Lerp(pesoPiesActual, pesoPiesObjetivo, dt);

        if (modoActual == ModoIK.Stance)
            AplicarStance();
        else
            AplicarAtajada();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STANCE — Postura de arquero esperando
    // ═══════════════════════════════════════════════════════════════════════════
    private void AplicarStance()
    {
        float w = pesoIKActual;
        if (w < 0.01f)
        {
            DesactivarTodosLosGoals();
            return;
        }

        // ─── MANOS ──────────────────────────────────────────────────────────
        // Postura: codos levantados, palmas abiertas hacia el frente (hacia donde viene la pelota)
        Vector3 hipPos    = ObtenerCadera();
        Vector3 adelante  = transform.forward;
        Vector3 derecha   = transform.right;
        Vector3 arriba    = Vector3.up;

        // Centro de las manos: altura de cadera + offset, adelante del cuerpo
        Vector3 centroManos = hipPos
                            + adelante  * profundidadManosStance
                            + arriba    * alturaManosStance;

        // NOTA: transform.right = -Z, pero LeftHand está en +Z → usar signo negativo para la izquierda
        Vector3 targetLH = centroManos - derecha *  separacionManosStance;  // izquierda → +Z
        Vector3 targetRH = centroManos + derecha *  separacionManosStance;  // derecha   → -Z

        // Orientación de las palmas: mirando hacia la pelota con ligera inclinación hacia afuera
        Quaternion rotBase      = Quaternion.LookRotation(pelota.position - transform.position, arriba);
        Quaternion rotLHStance  = rotBase * Quaternion.Euler(0f, -20f, 0f);  // izquierda abierta
        Quaternion rotRHStance  = rotBase * Quaternion.Euler(0f,  20f, 0f);  // derecha abierta

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,  w);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,  w);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, w);
        animator.SetIKPosition(AvatarIKGoal.LeftHand,  targetLH);
        animator.SetIKPosition(AvatarIKGoal.RightHand, targetRH);
        animator.SetIKRotation(AvatarIKGoal.LeftHand,  rotLHStance);
        animator.SetIKRotation(AvatarIKGoal.RightHand, rotRHStance);

        // Hint de codos: empujar hacia afuera (izquierda → +Z, derecha → -Z)
        Vector3 hintLElbow = centroManos - derecha *  0.45f - adelante * 0.1f + arriba * 0.05f;
        Vector3 hintRElbow = centroManos + derecha *  0.45f - adelante * 0.1f + arriba * 0.05f;
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,  w * 0.7f);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, w * 0.7f);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow,  hintLElbow);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, hintRElbow);

        // ─── PIES ───────────────────────────────────────────────────────────
        // Separación de pies en stance: pies apoyados en el suelo, separados a los lados
        float pw = pesoPiesActual;
        if (pw > 0.01f)
        {
            Vector3 basePos  = new Vector3(transform.position.x, groundY, transform.position.z);
            // Izquierda → +Z (negando transform.right), derecha → -Z
            Vector3 targetLF = basePos - derecha *  separacionPiesStance;
            Vector3 targetRF = basePos + derecha *  separacionPiesStance;

            // Orientación de pies mirando hacia adelante
            Quaternion rotPie = Quaternion.LookRotation(adelante, arriba);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  pw);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, pw);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,  pw * 0.5f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, pw * 0.5f);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot,  targetLF);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, targetRF);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot,  rotPie);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rotPie);

            // Hints de rodillas: siguen a los pies corregidos
            Vector3 hintLKnee = targetLF + adelante * 0.2f + arriba * 0.2f;
            Vector3 hintRKnee = targetRF + adelante * 0.2f + arriba * 0.2f;
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee,  pw * 0.5f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, pw * 0.5f);
            animator.SetIKHintPosition(AvatarIKHint.LeftKnee,  hintLKnee);
            animator.SetIKHintPosition(AvatarIKHint.RightKnee, hintRKnee);
        }
        else
        {
            DesactivarGoalsPies();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ATAJADA — Lateral o Alta
    // ═══════════════════════════════════════════════════════════════════════════
    private void AplicarAtajada()
    {
        float w  = pesoIKActual;
        float wp = pesoPiesActual;

        if (w < 0.01f && wp < 0.01f)
        {
            DesactivarTodosLosGoals();
            return;
        }

        Vector3 pelotaPos = pelota.position;
        Vector3 adelante  = transform.forward;
        Vector3 derecha   = transform.right;
        Vector3 arriba    = Vector3.up;

        // ─── MANOS ──────────────────────────────────────────────────────────
        Vector3 targetLH, targetRH;
        Quaternion rotLH, rotRH;
        Vector3 hintLElbow, hintRElbow;

        if (modoActual == ModoIK.Lateral)
        {
            // Ambas manos hacia la pelota, palmas abiertas apuntando a la pelota
            Vector3 dirHaciaPelota = (pelotaPos - transform.position).normalized;
            Vector3 base_pos       = pelotaPos + adelante * offsetAdelante;

            // Separación perpendicular al vector hacia la pelota
            // NOTA: perp ≈ transform.right (-Z), así que izquierda usa perp negativo
            Vector3 perp = Vector3.Cross(dirHaciaPelota, arriba).normalized;

            targetLH = base_pos - perp *  separacionManosAtajada;  // izquierda → opuesto a perp
            targetRH = base_pos + perp *  separacionManosAtajada;  // derecha   → perp

            // Palmas mirando a la pelota, ligeramente abiertas
            Quaternion rotBase = Quaternion.LookRotation(dirHaciaPelota, arriba);
            rotLH = rotBase * Quaternion.Euler(0f, -15f, 0f);
            rotRH = rotBase * Quaternion.Euler(0f,  15f, 0f);

            // Hints de codos: izquierdo sale hacia -perp, derecho hacia +perp
            hintLElbow = base_pos - perp *  0.5f - adelante * 0.15f;
            hintRElbow = base_pos + perp *  0.5f - adelante * 0.15f;
        }
        else // Alto
        {
            // Manos arriba, palmas abiertas hacia adelante/abajo (esperando la pelota que baja)
            Vector3 base_pos = new Vector3(pelotaPos.x,
                                           pelotaPos.y + offsetAlto,
                                           pelotaPos.z) + adelante * offsetAdelante;

            // Izquierda → -derecha, derecha → +derecha
            targetLH = base_pos - derecha *  separacionManosAtajada;
            targetRH = base_pos + derecha *  separacionManosAtajada;

            // Palmas mirando hacia abajo/adelante
            Quaternion rotBase = Quaternion.LookRotation(Vector3.down * 0.5f + adelante * 0.5f, arriba);
            rotLH = rotBase * Quaternion.Euler(0f, -10f, 0f);
            rotRH = rotBase * Quaternion.Euler(0f,  10f, 0f);

            // Codos arriba, separados correctamente
            hintLElbow = base_pos - derecha *  0.4f + arriba * 0.1f;
            hintRElbow = base_pos + derecha *  0.4f + arriba * 0.1f;
        }

        if (w > 0.01f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,  w);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,  w * 0.85f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, w * 0.85f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand,  targetLH);
            animator.SetIKPosition(AvatarIKGoal.RightHand, targetRH);
            animator.SetIKRotation(AvatarIKGoal.LeftHand,  rotLH);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rotRH);

            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,  w * 0.75f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, w * 0.75f);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow,  hintLElbow);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, hintRElbow);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,  0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,  0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,  0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
        }

        // ─── PIES EN VUELO ───────────────────────────────────────────────────
        if (wp > 0.01f)
        {
            // Los pies acompañan el vuelo: se separan en la dirección del salto y suben un poco
            Vector3 bodyPos    = transform.position;
            Vector3 dirSalto   = derecha * Mathf.Sign(targetLH.x - transform.position.x);

            // Pie trasero (contra el salto) ligeramente recogido, pie delantero extendido
            float pY = groundY + alturaPiesVuelo;

            Vector3 targetLF, targetRF;
            Quaternion rotPieBase = Quaternion.LookRotation(adelante, arriba);

            if (modoActual == ModoIK.Lateral)
            {
                // Pierna del lado del salto: más extendida
                // Pierna contraria: ligeramente recogida
                bool saltaIzq = targetLH.z > transform.position.z; // en este rig Z separa los lados

                targetLF = new Vector3(bodyPos.x, pY, bodyPos.z + separacionPiesVuelo * 0.6f);
                targetRF = new Vector3(bodyPos.x, pY, bodyPos.z - separacionPiesVuelo * 0.6f);

                // Inclinación de pies acorde al roll del cuerpo
                float roll = saltaIzq ? -25f : 25f;
                rotPieBase = Quaternion.LookRotation(adelante, arriba) * Quaternion.Euler(0f, 0f, roll);
            }
            else // Alto
            {
                // Pies separados simétricamente y más abajo (cuelgan hacia el suelo)
                targetLF = new Vector3(bodyPos.x, pY * 0.5f, bodyPos.z + separacionPiesVuelo * 0.4f);
                targetRF = new Vector3(bodyPos.x, pY * 0.5f, bodyPos.z - separacionPiesVuelo * 0.4f);
            }

            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  wp);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, wp);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,  wp * 0.6f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, wp * 0.6f);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot,  targetLF);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, targetRF);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot,  rotPieBase);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rotPieBase);

            // Hints de rodillas: empujar hacia afuera en vuelo para postura abierta
            Vector3 hintLKnee = targetLF + adelante * 0.1f + arriba * 0.25f + derecha *  0.1f;
            Vector3 hintRKnee = targetRF + adelante * 0.1f + arriba * 0.25f + derecha * -0.1f;
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee,  wp * 0.6f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, wp * 0.6f);
            animator.SetIKHintPosition(AvatarIKHint.LeftKnee,  hintLKnee);
            animator.SetIKHintPosition(AvatarIKHint.RightKnee, hintRKnee);
        }
        else
        {
            DesactivarGoalsPies();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    private Vector3 ObtenerCadera()
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        return hips != null ? hips.position : transform.position + Vector3.up * 0.4f;
    }

    private void DesactivarGoalsPies()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,  0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee,  0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
    }

    private void DesactivarTodosLosGoals()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,  0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,  0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,  0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,  0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,  0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee,   0f);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee,  0f);
    }
}
