#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ConfiguradorArquero : MonoBehaviour
{
    [MenuItem("Tools/Configurar Arquero en Escena")]
    public static void ConfigurarArquero()
    {
        // 1. Buscar a Payaso_arquero
        GameObject arquero = GameObject.Find("Payaso_arquero");
        if (arquero == null)
        {
            Debug.LogError("No se pudo encontrar el GameObject 'Payaso_arquero' en la escena activa!");
            return;
        }

        // 2. Desempaquetar si es parte de un prefab
        if (PrefabUtility.IsPartOfAnyPrefab(arquero))
        {
            PrefabUtility.UnpackPrefabInstance(arquero, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        Undo.RegisterCompleteObjectUndo(arquero, "Configurar Payaso_arquero");

        // 3. Remover scripts obsoletos de demostración
        AnimationToButton animToBtn = arquero.GetComponent<AnimationToButton>();
        if (animToBtn != null) DestroyImmediate(animToBtn);

        RotateOver rotateScript = arquero.GetComponent<RotateOver>();
        if (rotateScript != null) DestroyImmediate(rotateScript);

        // 4. Agregar y configurar CapsuleCollider para colisiones con el balón
        CapsuleCollider col = arquero.GetComponent<CapsuleCollider>();
        if (col == null) col = arquero.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;
        col.height = 1.8f;
        col.isTrigger = false;

        // 5. Agregar y configurar Rigidbody en modo cinemático (bloquea empujes físicos)
        Rigidbody rb = arquero.GetComponent<Rigidbody>();
        if (rb == null) rb = arquero.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 6. Posicionar y rotar en el arco de fútbol (ArcoFutbol)
        GameObject goalRoot = GameObject.Find("ArcoFutbol");
        if (goalRoot == null)
        {
            Debug.LogError("No se pudo encontrar el GameObject 'ArcoFutbol' en la escena activa! Configura el fútbol primero.");
            return;
        }

        // Lo colocamos ligeramente en frente (0.8m) del centro del arco, alineado en altura y rotación
        arquero.transform.position = goalRoot.transform.position + goalRoot.transform.forward * 0.8f;
        // Hacemos que mire hacia adelante (mismo sentido que el arco, es decir, hacia el jugador)
        arquero.transform.rotation = goalRoot.transform.rotation;

        // 7. Agregar el script de control IA
        PayasoArquero scriptIA = arquero.GetComponent<PayasoArquero>();
        if (scriptIA == null) scriptIA = arquero.AddComponent<PayasoArquero>();
        
        // Ajustar el límite lateral según la escala del arco (por defecto poste está a 3m local, con escala de 1.4 son 4.2m)
        scriptIA.limiteLateral = 3.5f;

        // 8. Crear/Configurar Pared Invisible de Límites (detrás de la portería)
        GameObject paredExistente = GameObject.Find("ParedInvisibleLimites");
        if (paredExistente != null) DestroyImmediate(paredExistente);

        GameObject paredLimites = new GameObject("ParedInvisibleLimites");
        paredLimites.transform.position = goalRoot.transform.position - goalRoot.transform.forward * 2.5f + Vector3.up * 6f;
        paredLimites.transform.rotation = goalRoot.transform.rotation;

        BoxCollider colPared = paredLimites.AddComponent<BoxCollider>();
        colPared.isTrigger = true;
        colPared.size = new Vector3(35f, 15f, 2f);

        paredLimites.AddComponent<ParedLimite>();
        
        Undo.RegisterCreatedObjectUndo(paredLimites, "Crear Pared Invisible Limites");

        // Marcar escena sucia para guardar cambios
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("¡Arquero y pared invisible de límites configurados correctamente en la portería '" + goalRoot.name + "'!");
    }
}
#endif
