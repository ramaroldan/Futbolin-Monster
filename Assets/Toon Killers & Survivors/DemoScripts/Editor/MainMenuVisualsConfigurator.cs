#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuVisualsConfigurator
{
    [MenuItem("Tools/Configurar Visuales de Menu")]
    public static void ConfigurarVisualesMenu()
    {
        // 1. Cargar Assets
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Horror UI Kit/SpookyHorrorUI/Sprites/Panels/rectangle_4.png");
        Sprite skullSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Horror UI Kit/SpookyHorrorUI/Sprites/Panels/bat_skull.png");
        Sprite btnEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Horror UI Kit/SpookyHorrorUI/Sprites/Panels/button_empty.png");
        Sprite btnSelected1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Horror UI Kit/SpookyHorrorUI/Sprites/Panels/buttons_selected_1.png");
        Sprite btnSelected2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Horror UI Kit/SpookyHorrorUI/Sprites/Panels/button_selected_2.png");
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Creepster-Regular SDF.asset");

        if (panelSprite == null) Debug.LogError("No se pudo cargar panelSprite (rectangle_4.png)");
        if (skullSprite == null) Debug.LogError("No se pudo cargar skullSprite (bat_skull.png)");
        if (btnEmptySprite == null) Debug.LogError("No se pudo cargar btnEmptySprite (button_empty.png)");
        if (btnSelected1Sprite == null) Debug.LogError("No se pudo cargar btnSelected1Sprite (buttons_selected_1.png)");
        if (btnSelected2Sprite == null) Debug.LogError("No se pudo cargar btnSelected2Sprite (button_selected_2.png)");

        if (panelSprite == null || skullSprite == null || btnEmptySprite == null || btnSelected1Sprite == null || btnSelected2Sprite == null)
        {
            Debug.LogError("No se pudieron cargar todos los sprites del Horror UI Kit. Revisa los mensajes anteriores.");
            return;
        }

        if (fontAsset == null)
        {
            Debug.LogError("No se pudo cargar la fuente 'Assets/Creepster-Regular SDF.asset'.");
            return;
        }

        // 2. Buscar Canvas y Panel en la escena
        GameObject canvasObj = GameObject.Find("MainMenuCanvas");
        if (canvasObj == null)
        {
            canvasObj = GameObject.FindObjectOfType<Canvas>()?.gameObject;
        }

        if (canvasObj == null)
        {
            Debug.LogError("No se encontró ningún Canvas en la escena activa.");
            return;
        }

        GameObject panelObj = GameObject.Find("MainMenuPanel");
        if (panelObj == null)
        {
            // Buscar por hijo si es que tiene otro nombre o buscar dentro del canvas
            Transform panelTransform = canvasObj.transform.Find("MainMenuPanel");
            if (panelTransform != null)
            {
                panelObj = panelTransform.gameObject;
            }
        }

        if (panelObj == null)
        {
            Debug.LogError("No se encontró el GameObject 'MainMenuPanel'. Crea un panel con ese nombre dentro de tu Canvas.");
            return;
        }

        // Registrar acción para Deshacer (Undo) en el panel
        Undo.RegisterCompleteObjectUndo(panelObj, "Configurar Visuales de Menu - Panel");

        // 3. Configurar Imagen del Panel de Fondo
        Image panelImage = panelObj.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelObj.AddComponent<Image>();
        }

        panelImage.sprite = panelSprite;
        panelImage.color = Color.white; // Evitar tintes oscuros
        panelImage.type = Image.Type.Sliced;

        // Ajustar el RectTransform del Panel para que se vea más estilizado
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(380f, 480f);
        }

        // 4. Configurar Calavera con Alas (Header)
        Transform skullTransform = panelObj.transform.Find("HeaderSkull");
        GameObject skullObj;
        if (skullTransform == null)
        {
            skullObj = new GameObject("HeaderSkull");
            skullObj.transform.SetParent(panelObj.transform, false);
            Undo.RegisterCreatedObjectUndo(skullObj, "Crear Calavera Header");
        }
        else
        {
            skullObj = skullTransform.gameObject;
            Undo.RegisterCompleteObjectUndo(skullObj, "Modificar Calavera Header");
        }

        Image skullImage = skullObj.GetComponent<Image>();
        if (skullImage == null)
        {
            skullImage = skullObj.AddComponent<Image>();
        }
        skullImage.sprite = skullSprite;
        skullImage.color = Color.white;
        skullImage.SetNativeSize();

        RectTransform skullRect = skullObj.GetComponent<RectTransform>();
        if (skullRect != null)
        {
            // Anclar al centro-arriba del panel
            skullRect.anchorMin = new Vector2(0.5f, 1f);
            skullRect.anchorMax = new Vector2(0.5f, 1f);
            skullRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Reducir escala un poco si la imagen nativa es muy grande
            skullRect.sizeDelta = skullRect.sizeDelta * 0.8f;
            
            // Posicionar para que sobresalga
            skullRect.anchoredPosition = new Vector2(0f, 15f);
        }

        // 5. Configurar Botones (Jugar, Créditos, Salir)
        Button[] buttons = canvasObj.GetComponentsInChildren<Button>(true);
        int configurados = 0;

        foreach (Button btn in buttons)
        {
            // Identificar los botones por su nombre de GameObject o por el texto del TMPro
            string nameLower = btn.gameObject.name.ToLower();
            TMP_Text tmpText = btn.GetComponentInChildren<TMP_Text>();
            string textLower = tmpText != null ? tmpText.text.ToLower() : "";

            bool esJugar = nameLower.Contains("jugar") || nameLower.Contains("play") || textLower.Contains("jugar") || textLower.Contains("play");
            bool esCreditos = nameLower.Contains("credito") || nameLower.Contains("credit") || textLower.Contains("credito") || textLower.Contains("credit");
            bool esSalir = nameLower.Contains("salir") || nameLower.Contains("exit") || nameLower.Contains("quit") || textLower.Contains("salir") || textLower.Contains("exit");

            if (esJugar || esCreditos || esSalir)
            {
                Undo.RegisterCompleteObjectUndo(btn.gameObject, $"Configurar Botón {btn.gameObject.name}");

                // Configurar Image component del botón
                Image btnImage = btn.targetGraphic as Image;
                if (btnImage == null)
                {
                    btnImage = btn.GetComponent<Image>();
                    btn.targetGraphic = btnImage;
                }

                if (btnImage != null)
                {
                    Undo.RegisterCompleteObjectUndo(btnImage, $"Configurar Imagen Botón {btn.gameObject.name}");
                    btnImage.sprite = btnEmptySprite;
                    btnImage.color = Color.white;
                    btnImage.type = Image.Type.Sliced;
                }

                // Configurar Sprite Swap Transition
                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState ss = new SpriteState
                {
                    highlightedSprite = btnSelected1Sprite,
                    pressedSprite = btnSelected2Sprite,
                    selectedSprite = btnSelected1Sprite,
                    disabledSprite = null
                };
                btn.spriteState = ss;

                // Configurar el texto de TextMeshPro
                if (tmpText != null)
                {
                    Undo.RegisterCompleteObjectUndo(tmpText, $"Configurar Texto Botón {btn.gameObject.name}");
                    tmpText.font = fontAsset;
                    
                    // Color verde espectral brillante (#67E612)
                    tmpText.color = new Color(103f / 255f, 230f / 255f, 18f / 255f, 1f);
                    
                    // Forzar actualización visual
                    tmpText.ForceMeshUpdate();
                }

                configurados++;
            }
        }

        // Marcar la escena como modificada (sucia)
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log($"¡Visuales de menú configurados correctamente! Panel modificado y {configurados} botones estilizados.");
    }

    [MenuItem("Tools/Generar HUD en Escena")]
    public static void GenerarHUDEscena()
    {
        // 1. Buscar o crear el GameObject MarcadorHUD
        GameObject hudObj = GameObject.Find("MarcadorHUD");
        if (hudObj == null)
        {
            hudObj = GameObject.Find("MarcadorHUD_AutoGenerated");
        }
        if (hudObj == null)
        {
            hudObj = new GameObject("MarcadorHUD");
            Undo.RegisterCreatedObjectUndo(hudObj, "Crear MarcadorHUD");
        }

        MarcadorHUD hudScript = hudObj.GetComponent<MarcadorHUD>();
        if (hudScript == null)
        {
            hudScript = hudObj.AddComponent<MarcadorHUD>();
            Undo.RegisterCreatedObjectUndo(hudScript, "Agregar MarcadorHUD Script");
        }

        // 2. Buscar si ya existe HUD_Canvas
        GameObject goCanvas = GameObject.Find("HUD_Canvas");
        if (goCanvas != null)
        {
            Debug.LogWarning("Ya existe un 'HUD_Canvas' en la escena. Si quieres volver a generarlo, bórralo primero.");
            return;
        }

        // Asegurar que exista un EventSystem en la escena para procesar los clicks
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Crear EventSystem");
            Debug.Log("Se creó el EventSystem en la escena con InputSystemUIInputModule.");
        }
        else
        {
            // Si ya existe, limpiar el StandaloneInputModule viejo si lo tiene para evitar el crash con el nuevo Input System
            UnityEngine.EventSystems.StandaloneInputModule oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                Undo.DestroyObjectImmediate(oldModule);
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("Se actualizó el EventSystem existente para usar InputSystemUIInputModule.");
            }
        }

        // 3. Crear Canvas Principal
        goCanvas = new GameObject("HUD_Canvas");
        Undo.RegisterCreatedObjectUndo(goCanvas, "Crear HUD_Canvas");
        
        Canvas canvas = goCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        
        CanvasScaler scaler = goCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        goCanvas.AddComponent<GraphicRaycaster>();
        
        // 4. Panel superior del Marcador (Estilo Glassmorphism Premium)
        GameObject goPanel = new GameObject("PanelMarcador");
        goPanel.transform.SetParent(goCanvas.transform, false);
        
        RectTransform rtPanel = goPanel.AddComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 1f);
        rtPanel.anchorMax = new Vector2(0.5f, 1f);
        rtPanel.pivot = new Vector2(0.5f, 1f);
        rtPanel.anchoredPosition = new Vector2(0f, -15f);
        rtPanel.sizeDelta = new Vector2(580f, 75f);
        
        Image imgPanel = goPanel.AddComponent<Image>();
        imgPanel.color = new Color(0.04f, 0.04f, 0.07f, 0.85f);
        
        Outline panelOutline = goPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.0f, 0.8f, 1.0f, 0.8f); // Cyan neón
        panelOutline.effectDistance = new Vector2(2f, 2f);

        // 5. Texto del Temporizador (En el centro del panel)
        GameObject goTiempo = new GameObject("TextoTiempo");
        goTiempo.transform.SetParent(goPanel.transform, false);
        Text textoTiempo = goTiempo.AddComponent<Text>();
        textoTiempo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoTiempo.fontSize = 28;
        textoTiempo.alignment = TextAnchor.MiddleCenter;
        textoTiempo.color = Color.white;
        
        RectTransform rtTiempo = goTiempo.GetComponent<RectTransform>();
        rtTiempo.anchorMin = new Vector2(0.5f, 0.5f);
        rtTiempo.anchorMax = new Vector2(0.5f, 0.5f);
        rtTiempo.anchoredPosition = Vector2.zero;
        rtTiempo.sizeDelta = new Vector2(160f, 60f);

        Outline outlineTiempo = goTiempo.AddComponent<Outline>();
        outlineTiempo.effectColor = Color.black;
        outlineTiempo.effectDistance = new Vector2(1.5f, 1.5f);

        // 6. Texto de Goles (Lado izquierdo del panel)
        GameObject goGoles = new GameObject("TextoGoles");
        goGoles.transform.SetParent(goPanel.transform, false);
        Text textoGoles = goGoles.AddComponent<Text>();
        textoGoles.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoGoles.fontSize = 24;
        textoGoles.alignment = TextAnchor.MiddleLeft;
        textoGoles.color = new Color(0.2f, 1f, 0.4f); // Verde neón
        
        RectTransform rtGoles = goGoles.GetComponent<RectTransform>();
        rtGoles.anchorMin = new Vector2(0f, 0.5f);
        rtGoles.anchorMax = new Vector2(0f, 0.5f);
        rtGoles.pivot = new Vector2(0f, 0.5f);
        rtGoles.anchoredPosition = new Vector2(25f, 0f);
        rtGoles.sizeDelta = new Vector2(180f, 50f);

        Outline outlineGoles = goGoles.AddComponent<Outline>();
        outlineGoles.effectColor = Color.black;
        outlineGoles.effectDistance = new Vector2(1.5f, 1.5f);

        // 7. Texto de Noqueos (Lado derecho del panel)
        GameObject goNoqueos = new GameObject("TextoNoqueos");
        goNoqueos.transform.SetParent(goPanel.transform, false);
        Text textoNoqueos = goNoqueos.AddComponent<Text>();
        textoNoqueos.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoNoqueos.fontSize = 24;
        textoNoqueos.alignment = TextAnchor.MiddleRight;
        textoNoqueos.color = new Color(1f, 0.2f, 0.2f); // Rojo neón
        
        RectTransform rtNoqueos = goNoqueos.GetComponent<RectTransform>();
        rtNoqueos.anchorMin = new Vector2(1f, 0.5f);
        rtNoqueos.anchorMax = new Vector2(1f, 0.5f);
        rtNoqueos.pivot = new Vector2(1f, 0.5f);
        rtNoqueos.anchoredPosition = new Vector2(-25f, 0f);
        rtNoqueos.sizeDelta = new Vector2(180f, 50f);

        Outline outlineNoqueos = goNoqueos.AddComponent<Outline>();
        outlineNoqueos.effectColor = Color.black;
        outlineNoqueos.effectDistance = new Vector2(1.5f, 1.5f);

        // 8. Texto de Anuncios Centrales
        GameObject goAnuncios = new GameObject("TextoAnuncios");
        goAnuncios.transform.SetParent(goCanvas.transform, false);
        Text textoAnuncios = goAnuncios.AddComponent<Text>();
        textoAnuncios.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoAnuncios.fontSize = 52;
        textoAnuncios.alignment = TextAnchor.MiddleCenter;
        textoAnuncios.color = Color.yellow;
        textoAnuncios.text = "¡PREPARATE!";
        textoAnuncios.gameObject.SetActive(false);
        
        RectTransform rtAnuncios = goAnuncios.GetComponent<RectTransform>();
        rtAnuncios.anchorMin = new Vector2(0.5f, 0.5f);
        rtAnuncios.anchorMax = new Vector2(0.5f, 0.5f);
        rtAnuncios.anchoredPosition = new Vector2(0f, 120f);
        rtAnuncios.sizeDelta = new Vector2(900f, 120f);

        Outline outlineAnuncios = goAnuncios.AddComponent<Outline>();
        outlineAnuncios.effectColor = Color.black;
        outlineAnuncios.effectDistance = new Vector2(3f, 3f);

        // 9. Panel de Fin de Partido (Fondo oscuro total a pantalla completa)
        GameObject panelFinPartido = new GameObject("PanelFinPartido");
        panelFinPartido.transform.SetParent(goCanvas.transform, false);
        panelFinPartido.SetActive(false);
        
        RectTransform rtFin = panelFinPartido.AddComponent<RectTransform>();
        rtFin.anchorMin = new Vector2(0f, 0f);
        rtFin.anchorMax = new Vector2(1f, 1f);
        rtFin.pivot = new Vector2(0.5f, 0.5f);
        rtFin.anchoredPosition = Vector2.zero;
        rtFin.sizeDelta = Vector2.zero;
        
        Image imgFin = panelFinPartido.AddComponent<Image>();
        imgFin.color = new Color(0.02f, 0.02f, 0.04f, 0.8f);
        
        panelFinPartido.AddComponent<CanvasGroup>();

        // Tarjeta de estadísticas
        GameObject goCard = new GameObject("TarjetaEstadisticas");
        goCard.transform.SetParent(panelFinPartido.transform, false);
        
        RectTransform rtCard = goCard.AddComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.pivot = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = Vector2.zero;
        rtCard.sizeDelta = new Vector2(580f, 500f);
        
        Image imgCard = goCard.AddComponent<Image>();
        imgCard.color = new Color(0.06f, 0.06f, 0.10f, 0.95f);
        
        Outline outlineCard = goCard.AddComponent<Outline>();
        outlineCard.effectColor = new Color(1f, 0.84f, 0f, 0.75f);
        outlineCard.effectDistance = new Vector2(3f, 3f);
        
        Shadow shadowCard = goCard.AddComponent<Shadow>();
        shadowCard.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadowCard.effectDistance = new Vector2(8f, -8f);

        // Título "RESUMEN DEL PARTIDO"
        GameObject goFinTitulo = new GameObject("FinTitulo");
        goFinTitulo.transform.SetParent(goCard.transform, false);
        Text txtFinTitulo = goFinTitulo.AddComponent<Text>();
        txtFinTitulo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtFinTitulo.fontSize = 36;
        txtFinTitulo.alignment = TextAnchor.MiddleCenter;
        txtFinTitulo.text = "RESUMEN DEL PARTIDO";
        txtFinTitulo.color = new Color(1f, 0.84f, 0f);
        
        RectTransform rtFinTitulo = goFinTitulo.GetComponent<RectTransform>();
        rtFinTitulo.anchorMin = new Vector2(0.5f, 1f);
        rtFinTitulo.anchorMax = new Vector2(0.5f, 1f);
        rtFinTitulo.pivot = new Vector2(0.5f, 1f);
        rtFinTitulo.anchoredPosition = new Vector2(0f, -35f);
        rtFinTitulo.sizeDelta = new Vector2(500f, 50f);

        Outline outlineFinTitulo = goFinTitulo.AddComponent<Outline>();
        outlineFinTitulo.effectColor = Color.black;
        outlineFinTitulo.effectDistance = new Vector2(2f, 2f);

        // Texto Resumen Estadísticas
        GameObject goResumen = new GameObject("TextoResumen");
        goResumen.transform.SetParent(goCard.transform, false);
        Text textoResumenFin = goResumen.AddComponent<Text>();
        textoResumenFin.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoResumenFin.fontSize = 22;
        textoResumenFin.alignment = TextAnchor.MiddleCenter;
        textoResumenFin.supportRichText = true;
        textoResumenFin.color = Color.white;
        textoResumenFin.text = "Goles: 0\nK.O.: 0\nBarridas: 0\nRango: -";
        
        RectTransform rtResumen = goResumen.GetComponent<RectTransform>();
        rtResumen.anchorMin = new Vector2(0.5f, 0.5f);
        rtResumen.anchorMax = new Vector2(0.5f, 0.5f);
        rtResumen.pivot = new Vector2(0.5f, 0.5f);
        rtResumen.anchoredPosition = new Vector2(0f, 0f);
        rtResumen.sizeDelta = new Vector2(500f, 260f);

        // Botón de Reiniciar
        GameObject goBoton = new GameObject("BotonReiniciar");
        goBoton.transform.SetParent(goCard.transform, false);
        
        RectTransform rtBoton = goBoton.AddComponent<RectTransform>();
        rtBoton.anchorMin = new Vector2(0.5f, 0f);
        rtBoton.anchorMax = new Vector2(0.5f, 0f);
        rtBoton.pivot = new Vector2(0.5f, 0f);
        rtBoton.anchoredPosition = new Vector2(-130f, 40f);
        rtBoton.sizeDelta = new Vector2(240f, 50f);
        
        Image imgBoton = goBoton.AddComponent<Image>();
        imgBoton.color = new Color(0.12f, 0.53f, 0.28f);
        
        Outline outlineBoton = goBoton.AddComponent<Outline>();
        outlineBoton.effectColor = new Color(0.5f, 1f, 0.6f, 0.4f);
        outlineBoton.effectDistance = new Vector2(1.5f, 1.5f);

        Button botonReiniciar = goBoton.AddComponent<Button>();
        ColorBlock coloresBoton = botonReiniciar.colors;
        coloresBoton.normalColor = new Color(0.12f, 0.53f, 0.28f);
        coloresBoton.highlightedColor = new Color(0.18f, 0.7f, 0.38f);
        coloresBoton.pressedColor = new Color(0.08f, 0.38f, 0.2f);
        botonReiniciar.colors = coloresBoton;

        GameObject goBotonTexto = new GameObject("TextoBoton");
        goBotonTexto.transform.SetParent(goBoton.transform, false);
        Text txtBoton = goBotonTexto.AddComponent<Text>();
        txtBoton.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtBoton.fontSize = 18;
        txtBoton.alignment = TextAnchor.MiddleCenter;
        txtBoton.text = "🔄 JUGAR OTRA VEZ";
        txtBoton.color = Color.white;
        
        RectTransform rtBotonTexto = goBotonTexto.GetComponent<RectTransform>();
        rtBotonTexto.anchorMin = Vector2.zero;
        rtBotonTexto.anchorMax = Vector3.one;
        rtBotonTexto.sizeDelta = Vector2.zero;

        Outline outlineTxtBoton = goBotonTexto.AddComponent<Outline>();
        outlineTxtBoton.effectColor = Color.black;
        outlineTxtBoton.effectDistance = new Vector2(1.5f, 1.5f);

        // Botón de Salir
        GameObject goBotonSalir = new GameObject("BotonSalir");
        goBotonSalir.transform.SetParent(goCard.transform, false);
        
        RectTransform rtBotonSalir = goBotonSalir.AddComponent<RectTransform>();
        rtBotonSalir.anchorMin = new Vector2(0.5f, 0f);
        rtBotonSalir.anchorMax = new Vector2(0.5f, 0f);
        rtBotonSalir.pivot = new Vector2(0.5f, 0f);
        rtBotonSalir.anchoredPosition = new Vector2(130f, 40f);
        rtBotonSalir.sizeDelta = new Vector2(240f, 50f);
        
        Image imgBotonSalir = goBotonSalir.AddComponent<Image>();
        imgBotonSalir.color = new Color(0.6f, 0.2f, 0.2f);
        
        Outline outlineBotonSalir = goBotonSalir.AddComponent<Outline>();
        outlineBotonSalir.effectColor = new Color(1f, 0.6f, 0.6f, 0.4f);
        outlineBotonSalir.effectDistance = new Vector2(1.5f, 1.5f);

        Button botonSalir = goBotonSalir.AddComponent<Button>();
        ColorBlock coloresBotonSalir = botonSalir.colors;
        coloresBotonSalir.normalColor = new Color(0.6f, 0.2f, 0.2f);
        coloresBotonSalir.highlightedColor = new Color(0.8f, 0.3f, 0.3f);
        coloresBotonSalir.pressedColor = new Color(0.4f, 0.15f, 0.15f);
        botonSalir.colors = coloresBotonSalir;

        GameObject goBotonSalirTexto = new GameObject("TextoBotonSalir");
        goBotonSalirTexto.transform.SetParent(goBotonSalir.transform, false);
        Text txtBotonSalir = goBotonSalirTexto.AddComponent<Text>();
        txtBotonSalir.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtBotonSalir.fontSize = 18;
        txtBotonSalir.alignment = TextAnchor.MiddleCenter;
        txtBotonSalir.text = "❌ SALIR DEL JUEGO";
        txtBotonSalir.color = Color.white;
        
        RectTransform rtBotonSalirTexto = goBotonSalirTexto.GetComponent<RectTransform>();
        rtBotonSalirTexto.anchorMin = Vector2.zero;
        rtBotonSalirTexto.anchorMax = Vector3.one;
        rtBotonSalirTexto.sizeDelta = Vector2.zero;

        Outline outlineTxtBotonSalir = goBotonSalirTexto.AddComponent<Outline>();
        outlineTxtBotonSalir.effectColor = Color.black;
        outlineTxtBotonSalir.effectDistance = new Vector2(1.5f, 1.5f);

        // 10. Asignar referencias en el MarcadorHUD y guardar escena
        Undo.RecordObject(hudScript, "Asignar Referencias HUD");
        hudScript.canvasHUD = canvas;
        hudScript.textoTiempo = textoTiempo;
        hudScript.textoGoles = textoGoles;
        hudScript.textoNoqueos = textoNoqueos;
        hudScript.textoAnuncios = textoAnuncios;
        hudScript.panelFinPartido = panelFinPartido;
        hudScript.textoResumenFin = textoResumenFin;
        hudScript.botonReiniciar = botonReiniciar;

        // Registrar los eventos en los botones creados de forma persistente
        UnityEventTools.AddPersistentListener(botonReiniciar.onClick, hudScript.RecomenzarPartido);
        UnityEventTools.AddPersistentListener(botonSalir.onClick, hudScript.SalirDelJuego);

        // Cambiar el nombre del objeto para indicar que es el oficial configurado y evitar que se borre o se recree
        hudObj.name = "MarcadorHUD";

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("¡HUD_Canvas generado exitosamente en la escena activa! Referencias vinculadas al script MarcadorHUD.");
    }
}
#endif
