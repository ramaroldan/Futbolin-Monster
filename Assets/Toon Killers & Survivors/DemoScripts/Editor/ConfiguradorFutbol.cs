#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ConfiguradorFutbol : MonoBehaviour
{
    [MenuItem("Tools/Configurar Futbol en Escena")]
    public static void ConfigurarFutbol()
    {
        // 1. Buscar al jugador (MiniJason o Killer2)
        GameObject jugador = GameObject.Find("MiniJason");
        if (jugador == null)
        {
            jugador = GameObject.Find("Killer2");
        }
        
        if (jugador == null)
        {
            Debug.LogError("No se pudo encontrar al jugador (MiniJason o Killer2) en la escena. ¡Por favor, configura al personaje primero!");
            return;
        }

        // 2. Limpiar elementos previos si existen
        LimpiarElementosPrevios();

        // 3. Crear Materiales Especiales
        Material matPelota = CrearMaterialNeon("MaterialPelotaFutbol", new Color(1.0f, 0.4f, 0.0f)); // Naranja Neón
        Material matArco = CrearMaterialStandard("MaterialArco", Color.white, 0f);
        Material matRed = CrearMaterialRedTransparent();

        // 4. Instanciar Pelota de Fútbol
        GameObject pelota = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pelota.name = "PelotaFutbol";
        pelota.tag = "Untagged";
        pelota.transform.position = new Vector3(58f, 9.8f, 78f);
        pelota.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        
        // Agregar físicas a la pelota
        Rigidbody rb = pelota.GetComponent<Rigidbody>();
        if (rb == null) rb = pelota.AddComponent<Rigidbody>();
        rb.mass = 0.8f;
        rb.linearDamping = 0.6f;
        rb.angularDamping = 0.6f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Añadir material físico para rebote
        SphereCollider colPelota = pelota.GetComponent<SphereCollider>();
        PhysicsMaterial pmPelota = new PhysicsMaterial("FisicaPelota");
        pmPelota.bounciness = 0.7f;
        pmPelota.bounceCombine = PhysicsMaterialCombine.Maximum;
        pmPelota.dynamicFriction = 0.3f;
        pmPelota.staticFriction = 0.3f;
        colPelota.material = pmPelota;

        // Pintar y añadir script de lógica
        pelota.GetComponent<MeshRenderer>().sharedMaterial = matPelota;
        pelota.AddComponent<PelotaFutbol>();
        Undo.RegisterCreatedObjectUndo(pelota, "Crear Pelota Futbol");

        // 5. Instanciar Arco de Fútbol
        GameObject goalRoot = new GameObject("ArcoFutbol");
        goalRoot.transform.position = new Vector3(58f, 9.5f, 105f);
        goalRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Undo.RegisterCreatedObjectUndo(goalRoot, "Crear Arco Futbol");

        // Postes del Arco
        CrearCuboHijo(goalRoot.transform, "PosteIzq", new Vector3(-3f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), matArco);
        CrearCuboHijo(goalRoot.transform, "PosteDer", new Vector3(3f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), matArco);
        CrearCuboHijo(goalRoot.transform, "Travesaño", new Vector3(0f, 2.5f, 0f), new Vector3(6.18f, 0.18f, 0.18f), matArco);

        // Red visual (Semi-transparente)
        GameObject redVisual = CrearCuboHijo(goalRoot.transform, "RedVisual", new Vector3(0f, 1.25f, -0.6f), new Vector3(6f, 2.5f, 1.2f), matRed);
        // Quitar colisiones físicas a la red visual
        Collider colRed = redVisual.GetComponent<Collider>();
        if (colRed != null) DestroyImmediate(colRed);

        // Caja de Colisión Trigger (Línea de Gol)
        GameObject triggerGol = new GameObject("TriggerGol");
        triggerGol.transform.SetParent(goalRoot.transform, false);
        triggerGol.transform.localPosition = new Vector3(0f, 1.25f, -0.1f);
        BoxCollider colTrigger = triggerGol.AddComponent<BoxCollider>();
        colTrigger.isTrigger = true;
        colTrigger.size = new Vector3(6f, 2.5f, 0.4f);
        
        ArcoDeFutbol scriptArco = triggerGol.AddComponent<ArcoDeFutbol>();

        // Luz de Celebración (Cian Neón parpadeante al hacer gol)
        GameObject goLuz = new GameObject("LuzCelebracion");
        goLuz.transform.SetParent(goalRoot.transform, false);
        goLuz.transform.localPosition = new Vector3(0f, 2.3f, -0.5f);
        Light luz = goLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 15f;
        luz.intensity = 0f;
        luz.color = new Color(0.0f, 1.0f, 0.8f);
        luz.enabled = false;
        scriptArco.luzCelebracion = luz;

        // Punto de origen de confeti
        GameObject goParticulas = new GameObject("PuntoParticulas");
        goParticulas.transform.SetParent(goalRoot.transform, false);
        goParticulas.transform.localPosition = new Vector3(0f, 2.3f, -0.4f);
        scriptArco.puntoParticulas = goParticulas.transform;

        // 6. Crear Canvas UI de Fútbol
        GameObject canvasFutbol = new GameObject("CanvasFutbol");
        Canvas canvas = canvasFutbol.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        UnityEngine.UI.CanvasScaler scaler = canvasFutbol.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasFutbol.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasFutbol, "Crear Canvas UI Futbol");

        // Fuente por defecto
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Marcador de Goles (Esquina superior derecha)
        GameObject goMarcador = new GameObject("MarcadorGoles");
        goMarcador.transform.SetParent(canvasFutbol.transform, false);
        RectTransform rtMarcador = goMarcador.AddComponent<RectTransform>();
        rtMarcador.anchorMin = new Vector2(1f, 1f);
        rtMarcador.anchorMax = new Vector2(1f, 1f);
        rtMarcador.pivot = new Vector2(1f, 1f);
        rtMarcador.anchoredPosition = new Vector2(-40f, -40f);
        rtMarcador.sizeDelta = new Vector2(400f, 80f);

        UnityEngine.UI.Text txtMarcador = goMarcador.AddComponent<UnityEngine.UI.Text>();
        txtMarcador.font = fuente;
        txtMarcador.fontSize = 42;
        txtMarcador.fontStyle = FontStyle.Bold;
        txtMarcador.alignment = TextAnchor.UpperRight;
        txtMarcador.color = new Color(0.2f, 1.0f, 0.2f); // Verde brillante
        
        // Agregar sombra para legibilidad
        UnityEngine.UI.Shadow shadowM = goMarcador.AddComponent<UnityEngine.UI.Shadow>();
        shadowM.effectColor = Color.black;
        shadowM.effectDistance = new Vector2(2f, -2f);

        scriptArco.textoMarcador = txtMarcador;

        // Cartel de ¡GOL! (Centro de pantalla)
        GameObject goGolUI = new GameObject("PanelGolUI");
        goGolUI.transform.SetParent(canvasFutbol.transform, false);
        RectTransform rtGol = goGolUI.AddComponent<RectTransform>();
        rtGol.anchorMin = new Vector2(0.5f, 0.5f);
        rtGol.anchorMax = new Vector2(0.5f, 0.5f);
        rtGol.pivot = new Vector2(0.5f, 0.5f);
        rtGol.anchoredPosition = Vector2.zero;
        rtGol.sizeDelta = new Vector2(900f, 200f);

        UnityEngine.UI.Text txtGol = goGolUI.AddComponent<UnityEngine.UI.Text>();
        txtGol.font = fuente;
        txtGol.fontSize = 110;
        txtGol.fontStyle = FontStyle.Bold;
        txtGol.alignment = TextAnchor.MiddleCenter;
        txtGol.color = new Color(0.0f, 1.0f, 1.0f); // Cian Neón
        txtGol.text = "¡GOOOOL!";

        UnityEngine.UI.Shadow shadowG = goGolUI.AddComponent<UnityEngine.UI.Shadow>();
        shadowG.effectColor = new Color(1.0f, 0.0f, 0.5f, 0.8f); // Sombra rosa fucsia
        shadowG.effectDistance = new Vector2(4f, -4f);

        scriptArco.panelGolUI = goGolUI;

        // Guardar escena sucia
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("¡Futbol configurado en escena correctamente! Se crearon PelotaFutbol, ArcoFutbol y el marcador UI.");
    }

    private static void LimpiarElementosPrevios()
    {
        GameObject p = GameObject.Find("PelotaFutbol");
        if (p != null) DestroyImmediate(p);

        GameObject a = GameObject.Find("ArcoFutbol");
        if (a != null) DestroyImmediate(a);

        GameObject c = GameObject.Find("CanvasFutbol");
        if (c != null) DestroyImmediate(c);
    }

    private static GameObject CrearCuboHijo(Transform padre, string nombre, Vector3 posLocal, Vector3 escalaLocal, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale = escalaLocal;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static Material CrearMaterialNeon(string nombre, Color color)
    {
        Material mat = new Material(Shader.Find("Legacy Shaders/Self-Illumin/Diffuse"));
        mat.name = nombre;
        mat.color = color;
        return mat;
    }

    private static Material CrearMaterialStandard(string nombre, Color color, float metallic)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = nombre;
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", 0.5f);
        return mat;
    }

    private static Material CrearMaterialRedTransparent()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = "MaterialRed";
        mat.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);
        
        // Ajustes para transparencia Standard Shader
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        return mat;
    }
}
#endif
