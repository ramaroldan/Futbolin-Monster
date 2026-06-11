using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class SkyEngine : MonoBehaviour
{
    public enum WeatherType { Clear, Foggy, Rain, Storm }

    [Header("Tiempo y Duración")]
    [Range(0f, 24f)]
    [Tooltip("Hora actual en formato de 24hs (12 = Mediodía, 0/24 = Medianoche)")]
    public float timeOfDay = 12f;
    
    [Tooltip("Duración en segundos de un día completo de juego (24hs)")]
    public float dayDurationInSeconds = 120f;
    
    [Tooltip("¿El tiempo transcurre de forma automática?")]
    public bool isTimeFlowing = true;
    
    [Tooltip("Multiplicador de la velocidad del tiempo")]
    public float timeMultiplier = 1f;

    [Header("Referencias de Iluminación")]
    [Tooltip("Luz direccional del Sol (Día)")]
    public Light sunLight;
    
    [Tooltip("Luz direccional de la Luna (Noche)")]
    public Light moonLight;
    
    [Tooltip("Volumen global nocturno (para atenuar su efecto durante el día)")]
    public Volume globalVolumeNight;

    [Header("Configuración del Sol y Luna")]
    public Gradient sunColorGradient;
    public AnimationCurve sunIntensityCurve;
    public Gradient moonColorGradient;
    public AnimationCurve moonIntensityCurve;
    
    [Header("Configuración del Cielo (Skybox Procedural)")]
    [Tooltip("Material del Skybox (se instanciará una copia para no alterar el archivo en disco)")]
    public Material skyboxMaterial;
    public Gradient skyTintGradient;
    public Gradient groundColorGradient;
    public AnimationCurve exposureCurve;
    public AnimationCurve atmosphereThicknessCurve;

    [Header("Iluminación Ambiental")]
    public Gradient ambientColorGradient;
    public float ambientIntensityScale = 1f;

    [Header("Parámetros de Clima")]
    public WeatherType currentWeather = WeatherType.Clear;
    public WeatherType targetWeather = WeatherType.Clear;
    [Range(0.01f, 5f)]
    public float weatherTransitionSpeed = 0.5f;

    [Header("Referencias de Partículas (Clima)")]
    [Tooltip("Si se deja vacío, se creará un sistema de partículas de lluvia automáticamente siguiendo a la cámara")]
    public ParticleSystem rainParticleSystem;

    // Estado interno del clima e interpolaciones
    private float weatherProgress = 1f; 
    private WeatherType previousWeather = WeatherType.Clear;

    private float currentFogDensity;
    private Color currentFogColor;
    private float currentLightMultiplier = 1f;
    private float currentRainIntensity = 0f;

    // Material de Skybox copiado en runtime para evitar modificar assets físicos
    private Material runtimeSkybox;

    // Control de relámpagos para Tormenta
    private bool isLightningFlashing = false;
    private float nextLightningTime = 0f;
    private float lightningIntensityOffset = 0f;

#if UNITY_EDITOR
    void Reset()
    {
        InicializarValoresPorDefecto();
        AutoConfigurarReferencias();
        if (RenderSettings.skybox != null && skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
    }
#endif

    void Awake()
    {
        // 1. Inicializar curvas y gradientes si están vacíos
        InicializarValoresPorDefecto();

        // 2. Intentar buscar o configurar luces y volúmenes automáticamente
        AutoConfigurarReferencias();

        // 3. Crear instancia del material de Skybox para no sobreescribir el asset del proyecto
        if (skyboxMaterial != null)
        {
            runtimeSkybox = Instantiate(skyboxMaterial);
            RenderSettings.skybox = runtimeSkybox;
        }
        else if (RenderSettings.skybox != null)
        {
            runtimeSkybox = Instantiate(RenderSettings.skybox);
            RenderSettings.skybox = runtimeSkybox;
        }
    }

    void Start()
    {
        // Forzar inicialización del clima
        previousWeather = currentWeather;
        targetWeather = currentWeather;
        weatherProgress = 1f;
        AplicarEstadoClima(currentWeather);
        
        if (rainParticleSystem == null && (currentWeather == WeatherType.Rain || currentWeather == WeatherType.Storm))
        {
            CrearSistemaDeLluviaProcedural();
        }
    }

    void Update()
    {
        // 1. Avanzar el tiempo
        if (isTimeFlowing && Application.isPlaying)
        {
            float horasPorSegundo = 24f / dayDurationInSeconds;
            timeOfDay += horasPorSegundo * Time.deltaTime * timeMultiplier;
            if (timeOfDay >= 24f) timeOfDay -= 24f;
        }

        // 2. Procesar transiciones de clima
        ActualizarClima();

        // 3. Procesar relámpagos si es tormenta
        ActualizarRelampagos();

        // 4. Aplicar iluminación y ciclo de día y noche
        ActualizarCicloDiaNoche();
    }

    private void ActualizarCicloDiaNoche()
    {
        // Calcular el tiempo normalizado de 0 a 1 (0 = Medianoche, 0.5 = Mediodía)
        float t = timeOfDay / 24f;

        // --- 1. Rotación de las luces (Sol y Luna) ---
        // Rotar el Sol en el eje X de 0 a 360 grados.
        // Desplazamos -90 grados para que a las 06:00 (t=0.25) esté en X=0 (horizonte este),
        // a las 12:00 (t=0.5) en X=90 (cénit), y a las 18:00 (t=0.75) en X=180 (horizonte oeste).
        float sunAngleX = (t * 360f) - 90f;
        if (sunLight != null)
        {
            // Mantenemos una inclinación lateral constante de Y=45 y Z=0 para que pase en diagonal
            sunLight.transform.rotation = Quaternion.Euler(sunAngleX, 45f, 0f);
        }

        if (moonLight != null)
        {
            // La luna está desfasada 180 grados del sol
            float moonAngleX = sunAngleX + 180f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngleX, 45f, 0f);
        }

        // --- 2. Intensidad y Color de las luces ---
        float sunIntensity = sunIntensityCurve.Evaluate(t) * currentLightMultiplier;
        float moonIntensity = moonIntensityCurve.Evaluate(t) * currentLightMultiplier;

        if (sunLight != null)
        {
            sunLight.color = sunColorGradient.Evaluate(t);
            // Si hay un relámpago, sumamos la intensidad temporal
            sunLight.intensity = sunIntensity + lightningIntensityOffset;
            sunLight.enabled = (sunIntensity + lightningIntensityOffset > 0.01f);
        }

        if (moonLight != null)
        {
            moonLight.color = moonColorGradient.Evaluate(t);
            moonLight.intensity = moonIntensity;
            moonLight.enabled = (moonIntensity > 0.01f);
        }

        // Definir cuál es la luz direccional principal para el Skybox
        bool isDay = (t > 0.22f && t < 0.78f); // Aprox entre las 05:15 y las 18:45 es de día
        if (isDay)
        {
            if (sunLight != null) RenderSettings.sun = sunLight;
        }
        else
        {
            if (moonLight != null) RenderSettings.sun = moonLight;
        }

        // --- 3. Actualizar Skybox Procedural ---
        Material targetSkybox = runtimeSkybox != null ? runtimeSkybox : RenderSettings.skybox;
        if (targetSkybox != null)
        {
            targetSkybox.SetColor("_SkyTint", skyTintGradient.Evaluate(t));
            targetSkybox.SetColor("_GroundColor", groundColorGradient.Evaluate(t));
            targetSkybox.SetFloat("_Exposure", exposureCurve.Evaluate(t));
            targetSkybox.SetFloat("_AtmosphereThickness", atmosphereThicknessCurve.Evaluate(t));
        }

        // --- 4. Actualizar Iluminación Ambiental ---
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientLight = ambientColorGradient.Evaluate(t) * ambientIntensityScale;
        
        // --- 5. Peso del Volumen Nocturno ---
        if (globalVolumeNight != null)
        {
            // El volumen nocturno tendrá peso 1 durante la noche profunda (t < 0.2 o t > 0.8)
            // y se desvanecerá a 0 a la mañana y tarde
            float volumeWeight = 0f;
            if (t <= 0.2f) // 00:00 a 04:48
            {
                volumeWeight = Mathf.Lerp(1f, 1f, t / 0.2f);
            }
            else if (t > 0.2f && t < 0.25f) // Desvanecer a la mañana: 04:48 a 06:00
            {
                volumeWeight = Mathf.Lerp(1f, 0f, (t - 0.2f) / 0.05f);
            }
            else if (t >= 0.25f && t <= 0.75f) // Día completo
            {
                volumeWeight = 0f;
            }
            else if (t > 0.75f && t < 0.8f) // Aparecer al atardecer: 18:00 a 19:12
            {
                volumeWeight = Mathf.Lerp(0f, 1f, (t - 0.75f) / 0.05f);
            }
            else // Noche profunda: 19:12 a 24:00
            {
                volumeWeight = 1f;
            }
            globalVolumeNight.weight = volumeWeight;
        }
    }

    private void ActualizarClima()
    {
        // Si cambió el clima objetivo, iniciamos una nueva transición
        if (currentWeather != targetWeather)
        {
            previousWeather = currentWeather;
            currentWeather = targetWeather;
            weatherProgress = 0f;
        }

        if (weatherProgress < 1f)
        {
            weatherProgress += Time.deltaTime * weatherTransitionSpeed;
            if (weatherProgress > 1f) weatherProgress = 1f;

            // Obtener valores de origen y destino
            float startDensity, targetDensity;
            Color startColor, targetColor;
            float startLightMult, targetLightMult;
            float startRain, targetRain;

            ObtenerValoresClima(previousWeather, out startDensity, out startColor, out startLightMult, out startRain);
            ObtenerValoresClima(currentWeather, out targetDensity, out targetColor, out targetLightMult, out targetRain);

            // Interpolar
            currentFogDensity = Mathf.Lerp(startDensity, targetDensity, weatherProgress);
            currentFogColor = Color.Lerp(startColor, targetColor, weatherProgress);
            currentLightMultiplier = Mathf.Lerp(startLightMult, targetLightMult, weatherProgress);
            currentRainIntensity = Mathf.Lerp(startRain, targetRain, weatherProgress);

            // Aplicar niebla
            RenderSettings.fog = (currentFogDensity > 0.001f);
            RenderSettings.fogDensity = currentFogDensity;
            RenderSettings.fogColor = currentFogColor;

            // Manejo del sistema de partículas de lluvia
            if (currentRainIntensity > 0.01f)
            {
                if (rainParticleSystem == null)
                {
                    CrearSistemaDeLluviaProcedural();
                }

                if (rainParticleSystem != null)
                {
                    if (!rainParticleSystem.isPlaying) rainParticleSystem.Play();
                    
                    var emission = rainParticleSystem.emission;
                    emission.rateOverTime = currentRainIntensity * 300f; // Escala máxima de partículas
                }
            }
            else
            {
                if (rainParticleSystem != null && rainParticleSystem.isPlaying)
                {
                    rainParticleSystem.Stop();
                }
            }
        }
        else
        {
            // Mantener alineadas las partículas con la cámara activa
            if (rainParticleSystem != null && rainParticleSystem.isPlaying)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    // Mover el emisor encima de la cámara
                    rainParticleSystem.transform.position = cam.transform.position + Vector3.up * 8f;
                }
            }
        }
    }

    private void AplicarEstadoClima(WeatherType type)
    {
        float density, lightMult, rain;
        Color color;
        ObtenerValoresClima(type, out density, out color, out lightMult, out rain);

        currentFogDensity = density;
        currentFogColor = color;
        currentLightMultiplier = lightMult;
        currentRainIntensity = rain;

        RenderSettings.fog = (currentFogDensity > 0.001f);
        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = currentFogColor;

        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            emission.rateOverTime = currentRainIntensity * 300f;
            if (currentRainIntensity > 0.01f)
            {
                if (!rainParticleSystem.isPlaying) rainParticleSystem.Play();
            }
            else
            {
                if (rainParticleSystem.isPlaying) rainParticleSystem.Stop();
            }
        }
    }

    private void ObtenerValoresClima(WeatherType type, out float density, out Color color, out float lightMult, out float rain)
    {
        // Color base de la niebla según el cielo nocturno actual y niebla neutra de día
        float t = timeOfDay / 24f;
        bool isNight = (t < 0.2f || t > 0.8f);
        Color baseFogColor = isNight ? new Color(0.1f, 0.1f, 0.2f) : new Color(0.5f, 0.55f, 0.6f);

        switch (type)
        {
            case WeatherType.Clear:
                density = 0.002f; // Niebla mínima
                color = baseFogColor;
                lightMult = 1f;
                rain = 0f;
                break;
            case WeatherType.Foggy:
                density = 0.035f; // Niebla densa
                color = isNight ? new Color(0.08f, 0.08f, 0.15f) : new Color(0.6f, 0.6f, 0.65f);
                lightMult = 0.5f; // Oscurece
                rain = 0f;
                break;
            case WeatherType.Rain:
                density = 0.015f;
                color = isNight ? new Color(0.06f, 0.06f, 0.1f) : new Color(0.4f, 0.42f, 0.45f);
                lightMult = 0.45f;
                rain = 0.6f;
                break;
            case WeatherType.Storm:
                density = 0.02f;
                color = isNight ? new Color(0.04f, 0.04f, 0.08f) : new Color(0.3f, 0.3f, 0.35f);
                lightMult = 0.3f; // Muy oscuro
                rain = 1.0f; // Lluvia pesada
                break;
            default:
                density = 0f;
                color = Color.white;
                lightMult = 1f;
                rain = 0f;
                break;
        }
    }

    private void ActualizarRelampagos()
    {
        if (currentWeather != WeatherType.Storm || !Application.isPlaying)
        {
            lightningIntensityOffset = 0f;
            return;
        }

        if (Time.time > nextLightningTime)
        {
            // Planificar el siguiente relámpago en 4 a 12 segundos
            nextLightningTime = Time.time + Random.Range(4f, 12f);
            StartCoroutine(SecuenciaRelampago());
        }
    }

    private IEnumerator SecuenciaRelampago()
    {
        isLightningFlashing = true;

        // Primer destello rápido
        lightningIntensityOffset = Random.Range(1.5f, 3.0f);
        yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        
        lightningIntensityOffset = 0f;
        yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));

        // Segundo destello (a veces ocurre)
        if (Random.value > 0.3f)
        {
            lightningIntensityOffset = Random.Range(1.0f, 2.0f);
            yield return new WaitForSeconds(Random.Range(0.08f, 0.25f));
        }

        // Apagar relámpago
        lightningIntensityOffset = 0f;
        isLightningFlashing = false;
    }

    private void CrearSistemaDeLluviaProcedural()
    {
        GameObject go = new GameObject("RainParticlesProcedural");
        go.transform.parent = this.transform;
        
        rainParticleSystem = go.AddComponent<ParticleSystem>();
        
        // Configurar el sistema de partículas para que parezca lluvia
        var main = rainParticleSystem.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(25f, 35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;
        
        // Rotar el emisor hacia abajo
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Configurar forma (Shape) como una caja amplia sobre la cabeza del jugador
        var shape = rainParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 30f, 1f);

        // Configurar velocidad sobre el tiempo para simular caída y viento lateral leve
        var velocityOverLifetime = rainParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
        velocityOverLifetime.y = 0f;
        velocityOverLifetime.z = 0f; // Cae en dirección local Z (hacia abajo debido a la rotación de 90)

        // Configurar color sobre lifetime (transparencia suave al final)
        var colorOverLifetime = rainParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 0.0f), new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Configurar el Renderer
        var renderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3f; // Estirar para dar efecto de gotas veloces
        
        // Buscar un material simple o usar el de Default en Editor
        #if UNITY_EDITOR
        Material defaultMat = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        if (defaultMat != null)
        {
            renderer.sharedMaterial = defaultMat;
        }
        #endif

        // Configurar Emisión inicial
        var emission = rainParticleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = currentRainIntensity * 300f;
    }

    private void AutoConfigurarReferencias()
    {
        // Buscar luces direccionales existentes
        if (moonLight == null)
        {
            GameObject moonGo = GameObject.Find("Directional Light Moon");
            if (moonGo != null) moonLight = moonGo.GetComponent<Light>();
        }

        if (sunLight == null)
        {
            GameObject sunGo = GameObject.Find("Directional Light Sun");
            if (sunGo != null)
            {
                sunLight = sunGo.GetComponent<Light>();
            }
            else
            {
                // Crear una nueva luz para el sol si no existe
                GameObject newSun = new GameObject("Directional Light Sun");
                newSun.transform.parent = this.transform.parent;
                sunLight = newSun.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
                sunLight.intensity = 0f;
                // Configurar URP additional light data si existe
                newSun.AddComponent<UniversalAdditionalLightData>();
            }
        }

        // Buscar el volumen global nocturno
        if (globalVolumeNight == null)
        {
            GameObject volGo = GameObject.Find("Global Volume Night");
            if (volGo != null)
            {
                globalVolumeNight = volGo.GetComponent<Volume>();
            }
        }
    }

    private void InicializarValoresPorDefecto()
    {
        // 1. Gradiente del Color del Sol (Día cálido, amanecer/atardecer naranja, noche negro)
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length <= 2)
        {
            sunColorGradient = new Gradient();
            sunColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.black, 0.0f),         // Medianoche
                    new GradientColorKey(Color.black, 0.2f),         // Noche
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.28f), // Amanecer
                    new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f), // Mediodía
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.72f), // Atardecer
                    new GradientColorKey(Color.black, 0.8f),         // Noche
                    new GradientColorKey(Color.black, 1.0f)          // Medianoche
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        // 2. Curva de Intensidad del Sol
        if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
        {
            sunIntensityCurve = new AnimationCurve();
            sunIntensityCurve.AddKey(new Keyframe(0f, 0f));         // Medianoche
            sunIntensityCurve.AddKey(new Keyframe(0.2f, 0f));        // Noche
            sunIntensityCurve.AddKey(new Keyframe(0.28f, 0.5f));     // Amanecer
            sunIntensityCurve.AddKey(new Keyframe(0.5f, 1.2f));      // Mediodía
            sunIntensityCurve.AddKey(new Keyframe(0.72f, 0.5f));     // Atardecer
            sunIntensityCurve.AddKey(new Keyframe(0.8f, 0f));        // Noche
            sunIntensityCurve.AddKey(new Keyframe(1f, 0f));         // Medianoche
            
            // Suavizar tangentes de la curva
            for (int i = 0; i < sunIntensityCurve.length; i++)
            {
                sunIntensityCurve.SmoothTangents(i, 0.5f);
            }
        }

        // 3. Gradiente del Color de la Luna (Color cian azulado de la noche actual)
        if (moonColorGradient == null || moonColorGradient.colorKeys.Length <= 2)
        {
            Color moonColor = new Color(0.4f, 0.85f, 1f); // El color original de Directional Light Moon
            moonColorGradient = new Gradient();
            moonColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(moonColor, 0f),
                    new GradientColorKey(moonColor, 0.2f),
                    new GradientColorKey(Color.black, 0.3f), // Se apaga de día
                    new GradientColorKey(Color.black, 0.7f),
                    new GradientColorKey(moonColor, 0.8f),
                    new GradientColorKey(moonColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        // 4. Curva de Intensidad de la Luna (Pico en la noche 0.65f, se apaga de día)
        if (moonIntensityCurve == null || moonIntensityCurve.length == 0)
        {
            moonIntensityCurve = new AnimationCurve();
            moonIntensityCurve.AddKey(new Keyframe(0f, 0.65f));      // Medianoche
            moonIntensityCurve.AddKey(new Keyframe(0.2f, 0.65f));     // Fin noche
            moonIntensityCurve.AddKey(new Keyframe(0.28f, 0f));       // Día amanece
            moonIntensityCurve.AddKey(new Keyframe(0.5f, 0f));        // Mediodía
            moonIntensityCurve.AddKey(new Keyframe(0.72f, 0f));       // Tarde
            moonIntensityCurve.AddKey(new Keyframe(0.8f, 0.65f));      // Noche cae
            moonIntensityCurve.AddKey(new Keyframe(1f, 0.65f));       // Medianoche
            
            for (int i = 0; i < moonIntensityCurve.length; i++)
            {
                moonIntensityCurve.SmoothTangents(i, 0.5f);
            }
        }

        // 5. Gradiente Tinte del Cielo (Procedural _SkyTint)
        // Mantiene el tinte nocturno original [0, 0.107, 0.736] en la noche
        if (skyTintGradient == null || skyTintGradient.colorKeys.Length <= 2)
        {
            Color nightSky = new Color(0.0f, 0.107f, 0.736f);
            Color daySky = new Color(0.3f, 0.52f, 0.85f); // Un celeste cielo agradable
            Color sunsetSky = new Color(0.8f, 0.35f, 0.2f);
            
            skyTintGradient = new Gradient();
            skyTintGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(nightSky, 0f),
                    new GradientColorKey(nightSky, 0.2f),
                    new GradientColorKey(sunsetSky, 0.27f),
                    new GradientColorKey(daySky, 0.5f),
                    new GradientColorKey(sunsetSky, 0.73f),
                    new GradientColorKey(nightSky, 0.8f),
                    new GradientColorKey(nightSky, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        // 6. Gradiente Color del Suelo (Procedural _GroundColor)
        // Mantiene el color original de la noche [0.113, 0.069, 0.33]
        if (groundColorGradient == null || groundColorGradient.colorKeys.Length <= 2)
        {
            Color nightGround = new Color(0.113f, 0.069f, 0.33f);
            Color dayGround = new Color(0.2f, 0.22f, 0.25f);
            
            groundColorGradient = new Gradient();
            groundColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(nightGround, 0f),
                    new GradientColorKey(nightGround, 0.2f),
                    new GradientColorKey(dayGround, 0.5f),
                    new GradientColorKey(nightGround, 0.8f),
                    new GradientColorKey(nightGround, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        // 7. Curva de Exposición del Cielo (Procedural _Exposure)
        // Noche original = 0.06f, Día = 1.0f (máximo detalle de iluminación)
        if (exposureCurve == null || exposureCurve.length == 0)
        {
            exposureCurve = new AnimationCurve();
            exposureCurve.AddKey(new Keyframe(0f, 0.06f));
            exposureCurve.AddKey(new Keyframe(0.2f, 0.06f));
            exposureCurve.AddKey(new Keyframe(0.3f, 0.9f));
            exposureCurve.AddKey(new Keyframe(0.5f, 1.0f));
            exposureCurve.AddKey(new Keyframe(0.7f, 0.9f));
            exposureCurve.AddKey(new Keyframe(0.8f, 0.06f));
            exposureCurve.AddKey(new Keyframe(1f, 0.06f));

            for (int i = 0; i < exposureCurve.length; i++)
            {
                exposureCurve.SmoothTangents(i, 0.5f);
            }
        }

        // 8. Curva de Espesor de la Atmósfera (Procedural _AtmosphereThickness)
        // Noche original = 0.98f
        if (atmosphereThicknessCurve == null || atmosphereThicknessCurve.length == 0)
        {
            atmosphereThicknessCurve = new AnimationCurve();
            atmosphereThicknessCurve.AddKey(new Keyframe(0f, 0.98f));
            atmosphereThicknessCurve.AddKey(new Keyframe(0.2f, 0.98f));
            atmosphereThicknessCurve.AddKey(new Keyframe(0.3f, 1.1f));
            atmosphereThicknessCurve.AddKey(new Keyframe(0.5f, 1.0f));
            atmosphereThicknessCurve.AddKey(new Keyframe(0.7f, 1.1f));
            atmosphereThicknessCurve.AddKey(new Keyframe(0.8f, 0.98f));
            atmosphereThicknessCurve.AddKey(new Keyframe(1f, 0.98f));

            for (int i = 0; i < atmosphereThicknessCurve.length; i++)
            {
                atmosphereThicknessCurve.SmoothTangents(i, 0.5f);
            }
        }

        // 9. Gradiente de Color de la Luz Ambiental
        if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length <= 2)
        {
            Color ambientNight = new Color(0.346f, 0.53f, 0.896f); // Del ambiente actual de la escena
            Color ambientDay = new Color(0.6f, 0.65f, 0.72f);
            
            ambientColorGradient = new Gradient();
            ambientColorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(ambientNight, 0f),
                    new GradientColorKey(ambientNight, 0.2f),
                    new GradientColorKey(ambientDay, 0.5f),
                    new GradientColorKey(ambientNight, 0.8f),
                    new GradientColorKey(ambientNight, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }
}
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(SkyEngine))]
public class SkyEngineEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        SkyEngine engine = (SkyEngine)target;
        
        GUILayout.Space(15);
        GUILayout.Label("Controles Rápidos de Prueba", UnityEditor.EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Fijar Día (12hs)"))
        {
            engine.timeOfDay = 12f;
        }
        if (GUILayout.Button("Fijar Noche (00hs)"))
        {
            engine.timeOfDay = 0f;
        }
        if (GUILayout.Button("Amanecer (06hs)"))
        {
            engine.timeOfDay = 6f;
        }
        if (GUILayout.Button("Atardecer (18hs)"))
        {
            engine.timeOfDay = 18f;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("Cambio de Clima Objetivo:", UnityEditor.EditorStyles.miniBoldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Despejado"))
        {
            engine.targetWeather = SkyEngine.WeatherType.Clear;
        }
        if (GUILayout.Button("Niebla"))
        {
            engine.targetWeather = SkyEngine.WeatherType.Foggy;
        }
        if (GUILayout.Button("Lluvia"))
        {
            engine.targetWeather = SkyEngine.WeatherType.Rain;
        }
        if (GUILayout.Button("Tormenta"))
        {
            engine.targetWeather = SkyEngine.WeatherType.Storm;
        }
        GUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            UnityEditor.EditorUtility.SetDirty(engine);
        }
    }
}
#endif
