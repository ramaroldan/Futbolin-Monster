using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Scene Configuration")]
    [SerializeField] private string gameSceneName = "Game"; // O el nombre de tu escena de juego

    private void Start()
    {
        // Asegurarse de que el panel principal esté activo y el de créditos oculto al iniciar
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>
    /// Carga la escena del juego.
    /// </summary>
    public void Jugar()
    {
        Debug.Log("Jugar seleccionado. Cargando escena de juego...");
        
        // Si la escena está en el Build Settings, la cargará. Si no, cargará por índice 1 como fallback.
        try
        {
            // Intentar cargar por nombre si es válido
            if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // Fallback: cargar la siguiente escena en el Build Settings (índice 1)
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
                else
                {
                    Debug.LogWarning("No hay una segunda escena agregada en Build Settings. Añade tu escena de juego a Build Settings.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cargar la escena de juego: " + e.Message);
        }
    }

    /// <summary>
    /// Muestra el panel de créditos.
    /// </summary>
    public void MostrarCreditos()
    {
        Debug.Log("Créditos seleccionados.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    /// <summary>
    /// Oculta el panel de créditos y vuelve al menú principal.
    /// </summary>
    public void VolverAlMenuPrincipal()
    {
        Debug.Log("Volviendo al menú principal.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>
    /// Cierra el juego.
    /// </summary>
    public void Salir()
    {
        Debug.Log("Salir seleccionado. Cerrando aplicación...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
