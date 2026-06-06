#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Polyperfect.Universal;

public class ConfiguradorTerceraPersona : MonoBehaviour
{
    [MenuItem("Tools/Configurar Personaje Tercera Persona")]
    public static void ConfigurarPersonaje()
    {
        // 1. Buscar a Killer2 o MiniJason
        GameObject killer = GameObject.Find("Killer2");
        if (killer == null)
        {
            killer = GameObject.Find("MiniJason");
        }
        if (killer == null)
        {
            Debug.LogError("No se pudo encontrar el GameObject 'Killer2' ni 'MiniJason' en la escena activa!");
            return;
        }
        
        if (PrefabUtility.IsPartOfAnyPrefab(killer))
        {
            PrefabUtility.UnpackPrefabInstance(killer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        
        Undo.RegisterCompleteObjectUndo(killer, "Configurar Killer2");
        
        // Limpiar cualquier componente con script faltante
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(killer);
        
        // 2. Remover scripts obsoletos de Killer2
        RotateOver rotateScript = killer.GetComponent<RotateOver>();
        if (rotateScript != null)
        {
            DestroyImmediate(rotateScript);
        }
        
        AnimationToButton animToButton = killer.GetComponent<AnimationToButton>();
        if (animToButton != null)
        {
            DestroyImmediate(animToButton);
        }
        
        // 3. Remover el Controlador de Tercera Persona viejo en inglés si existía
        Component oldController = killer.GetComponent("ThirdPersonController");
        if (oldController != null)
        {
            DestroyImmediate(oldController);
        }

        // 4. Agregar CharacterController si no está presente
        CharacterController cc = killer.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = killer.AddComponent<CharacterController>();
        }
        
        // Configurar valores por defecto del CharacterController para Killer2
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;
        cc.height = 1.8f;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;
        
        // 5. Agregar el nuevo ControladorTerceraPersona en español
        ControladorTerceraPersona controlador = killer.GetComponent<ControladorTerceraPersona>();
        if (controlador == null)
        {
            controlador = killer.AddComponent<ControladorTerceraPersona>();
        }
        
        // Cargar prefab del hacha (Axe) como proyectil lanzable
        GameObject hachaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toon Killers & Survivors/Prefab/Props/Axe.prefab");
        if (hachaPrefab != null)
        {
            controlador.prefabLanzable = hachaPrefab;
            Debug.Log("Se asignó Axe.prefab como objeto lanzable.");
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar Axe.prefab en la ruta Assets/Toon Killers & Survivors/Prefab/Props/Axe.prefab");
        }
        
        // Asegurar que tenga la etiqueta Player
        killer.tag = "Player";
        
        // 6. Buscar First Person Character (activo o inactivo)
        GameObject fpChar = null;
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            if (root.name == "First Person Character")
            {
                fpChar = root;
                break;
            }
        }
        
        if (fpChar != null && PrefabUtility.IsPartOfAnyPrefab(fpChar))
        {
            PrefabUtility.UnpackPrefabInstance(fpChar, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        
        // 7. Buscar la Cámara Principal (activa o inactiva)
        GameObject camaraPrincipal = null;
        if (fpChar != null)
        {
            Transform camTransform = fpChar.transform.Find("Main Camera");
            if (camTransform != null)
            {
                camaraPrincipal = camTransform.gameObject;
            }
        }
        
        if (camaraPrincipal == null)
        {
            camaraPrincipal = GameObject.FindGameObjectWithTag("MainCamera");
            if (camaraPrincipal == null)
            {
                camaraPrincipal = GameObject.Find("Main Camera");
            }
        }
        
        if (camaraPrincipal == null)
        {
            foreach (var root in rootObjects)
            {
                Transform[] childTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var child in childTransforms)
                {
                    if (child.name == "Main Camera" || child.CompareTag("MainCamera"))
                    {
                        camaraPrincipal = child.gameObject;
                        break;
                    }
                }
                if (camaraPrincipal != null) break;
            }
        }
        
        // 8. Configurar la Cámara Principal
        if (camaraPrincipal != null)
        {
            Undo.RegisterCompleteObjectUndo(camaraPrincipal, "Configurar Cámara Principal");
            
            // Limpiar cualquier componente con script faltante
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(camaraPrincipal);
            
            // Desvincular del First Person Character
            camaraPrincipal.transform.SetParent(null);
            
            // Activar la cámara en caso de estar desactivada
            camaraPrincipal.SetActive(true);
            
            // Remover script MouseLook viejo
            MouseLook ml = camaraPrincipal.GetComponent<MouseLook>();
            if (ml != null)
            {
                DestroyImmediate(ml);
            }
            
            // Remover script viejo en inglés si existiera
            Component oldCamScript = camaraPrincipal.GetComponent("ThirdPersonCamera");
            if (oldCamScript != null)
            {
                DestroyImmediate(oldCamScript);
            }
            
            // Agregar CamaraTerceraPersona en español
            CamaraTerceraPersona camTP = camaraPrincipal.GetComponent<CamaraTerceraPersona>();
            if (camTP == null)
            {
                camTP = camaraPrincipal.AddComponent<CamaraTerceraPersona>();
            }
            camTP.objetivo = killer.transform;
            camTP.mascaraColision = LayerMask.GetMask("Default");
            
            Debug.Log("Cámara configurada con CamaraTerceraPersona siguiendo a Killer2.");
        }
        else
        {
            Debug.LogError("No se pudo encontrar la Main Camera en la escena!");
        }
        
        // 9. Desactivar First Person Character
        if (fpChar != null)
        {
            Undo.RegisterCompleteObjectUndo(fpChar, "Desactivar First Person Character");
            fpChar.SetActive(false);
            Debug.Log("First Person Character desactivado.");
        }
        
        // Guardar cambios en la escena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("¡Configuración de Tercera Persona completa en español! Dale a Play para probar.");
    }
}
#endif
