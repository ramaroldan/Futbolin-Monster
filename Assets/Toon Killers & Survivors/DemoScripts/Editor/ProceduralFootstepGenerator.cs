#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class ProceduralFootstepGenerator
{
    [MenuItem("Tools/Generar Sonidos de Pisadas Procedurales")]
    public static void GenerarSonidosPisadas()
    {
        string dirPath = "Assets/Toon Killers & Survivors/Sounds";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        AudioClip[] clips = new AudioClip[3];
        for (int i = 0; i < 3; i++)
        {
            string fileName = $"PisadaProcedural_{i + 1}.wav";
            string filePath = Path.Combine(dirPath, fileName);

            byte[] wavData = GenerateFootstepWavBytes(i);
            File.WriteAllBytes(filePath, wavData);

            AssetDatabase.ImportAsset(filePath);
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
        }

        // Buscar el jugador y asignarle los clips
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Killer2");
        }
        if (player == null)
        {
            player = GameObject.Find("MiniJason");
        }

        if (player != null)
        {
            ControladorTerceraPersona controller = player.GetComponent<ControladorTerceraPersona>();
            if (controller != null)
            {
                Undo.RecordObject(controller, "Asignar Clips de Pisadas");
                controller.clipsPisadas = clips;
                EditorUtility.SetDirty(controller);
                Debug.Log($"¡Se generaron los clips de pisadas procedimentales y se asignaron al jugador '{player.name}'!");
            }
            else
            {
                Debug.LogWarning("Se generaron los archivos pero el jugador no tiene el script 'ControladorTerceraPersona'.");
            }
        }
        else
        {
            Debug.LogWarning("Se generaron los archivos pero no se encontró al jugador en la escena activa.");
        }
    }

    private static byte[] GenerateFootstepWavBytes(int index)
    {
        int sampleRate = 44100;
        float duration = 0.15f; // Duración corta de una pisada
        int numSamples = Mathf.RoundToInt(sampleRate * duration);
        short[] pcmData = new short[numSamples];

        // Parámetros según el índice del paso para dar variedad (izquierdo vs derecho vs neutro)
        float lowFreq = 80f + (index * 20f); // Frecuencia del golpe (80Hz, 100Hz, 120Hz)
        float noiseMix = 0.35f - (index * 0.05f); // Mezcla de ruido blanco (crunch)

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;

            // Envolvente de decaimiento exponencial (Decay envelope)
            float envelope = Mathf.Exp(-t * 22f); // Rápido decaimiento

            // Parte 1: El golpe de baja frecuencia (heel impact / peso corporal)
            float heelThump = Mathf.Sin(2f * Mathf.PI * lowFreq * t) * Mathf.Exp(-t * 30f);

            // Parte 2: El crujido de alta frecuencia (friction con la grava/piso)
            float whiteNoise = Random.Range(-1f, 1f);
            float crunch = whiteNoise * envelope;

            // Mezclar y atenuar volumen general para evitar saturación
            float sampleValue = (heelThump * (1f - noiseMix) + crunch * noiseMix) * 0.6f;
            
            // Normalizar a límites seguros de 16 bits (-32768 a 32767)
            pcmData[i] = (short)(Mathf.Clamp(sampleValue, -1f, 1f) * 32767);
        }

        // Escribir bytes del archivo WAV (RIFF formato)
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Cabecera RIFF
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + pcmData.Length * 2); // Tamaño total - 8
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // Formato de audio (fmt subchunk)
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk size (16 para PCM)
                writer.Write((short)1); // AudioFormat (1 para PCM uncompressed)
                writer.Write((short)1); // NumChannels (1 = Mono)
                writer.Write(sampleRate); // SampleRate
                writer.Write(sampleRate * 2); // ByteRate (SampleRate * Channels * BitsPerSample / 8)
                writer.Write((short)2); // BlockAlign (Channels * BitsPerSample / 8)
                writer.Write((short)16); // BitsPerSample

                // Datos de audio (data subchunk)
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(pcmData.Length * 2); // Subchunk size

                // Escribir muestras PCM de 16 bits
                for (int i = 0; i < pcmData.Length; i++)
                {
                    writer.Write(pcmData[i]);
                }
            }
            return stream.ToArray();
        }
    }
}
#endif
