#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoConfiguradorDefensores
{
    static AutoConfiguradorDefensores()
    {
        // Se ejecuta automáticamente al cargar el proyecto o compilar el script
        Configurar();
    }

    private static void Configurar()
    {
        string[] nombresDefensores = { "Defensor1", "Defensor2" };
        string log = "";

        foreach (string nombre in nombresDefensores)
        {
            GameObject defensor = GameObject.Find(nombre);
            if (defensor == null) continue;

            // Desempaquetar si es parte de un prefab para poder modificarlo
            if (PrefabUtility.IsPartOfAnyPrefab(defensor))
            {
                PrefabUtility.UnpackPrefabInstance(defensor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            Undo.RegisterCompleteObjectUndo(defensor, $"AutoConfigurar {nombre}");

            // Agregar y configurar DefensorAI
            DefensorAI ai = defensor.GetComponent<DefensorAI>();
            if (ai == null)
            {
                ai = defensor.AddComponent<DefensorAI>();
            }

            // Desactivar scripts obsoletos de la demo (evita que giren sobre su propio eje)
            RotateOver rotateScript = defensor.GetComponent<RotateOver>();
            if (rotateScript != null)
            {
                rotateScript.enabled = false;
            }

            AnimationToButton animToBtn = defensor.GetComponent<AnimationToButton>();
            if (animToBtn != null)
            {
                animToBtn.enabled = false;
            }

            // Configurar CapsuleCollider para recibir impactos físicos del hacha
            CapsuleCollider col = defensor.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = defensor.AddComponent<CapsuleCollider>();
            }
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
            col.height = 1.8f;
            col.isTrigger = false;

            // Configurar Rigidbody cinemático
            Rigidbody rb = defensor.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = defensor.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Límites por defecto de marcación
            ai.xMin = 45f;
            ai.xMax = 58.15f;
            ai.zMin = 55f;
            ai.zMax = 90f;

            // Asignar referencias locales al jugador y pelota
            GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
            if (jugadorObj != null)
            {
                ai.jugador = jugadorObj.transform;
            }

            PelotaFutbol pelotaObj = Object.FindObjectOfType<PelotaFutbol>();
            if (pelotaObj != null)
            {
                ai.pelota = pelotaObj;
            }

            EditorUtility.SetDirty(defensor);
            log += $"{nombre} configurado. ";
        }

        if (log != "")
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[AutoConfigurador] " + log);
        }
    }
}
#endif
