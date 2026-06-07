# Informe Técnico: Los 12 Principios de Animación Aplicados a MiniJason
**Asignatura:** Animación para Videojuegos  
**Proyecto:** Prototipo Sandbox Interactivo (Fútbol y Combate)  
**Personaje:** **MiniJason** (Chibi Terror Character)  
**Estudiante:** [Completar Nombre y Apellido]  

---

Este documento detalla la aplicación y justificación teórica de los **12 principios fundamentales de la animación** integrados directamente en los movimientos y físicas de nuestro protagonista, **MiniJason**, dentro del sandbox interactivo en Unity.

## Registro y Justificación de los 12 Principios

### 1. Estiramiento y Encogimiento (Squash and Stretch)
* **Aplicación en MiniJason:** 
  * **Salto (Despegue):** Script [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L301-L305) - Al iniciar el salto, el cuerpo chibi de MiniJason se estira verticalmente a escala `Y = 1.22` (con reducción en `XZ = 0.88`) para acentuar el impulso de despegue.
  * **Aterrizaje (Impacto):** [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L157-L163) - Al impactar contra el suelo, su cuerpo rechoncho se comprime verticalmente a escala `Y = 0.72` (con ensanchamiento en `XZ = 1.14`) durante `0.28s`, transmitiendo el peso y la gravedad de su cabeza gigante.
  * **Destrucción de Obstáculos:** En [ObjetoLanzable.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ObjetoLanzable.cs#L230-L233), las calabazas impactadas sufren una distorsión física de escala en sus fragmentos antes de dispersarse.
* **Justificación:** Dada la anatomía caricaturesca de MiniJason (cuerpo pequeño y cabeza enorme), el Squash and Stretch procedural es muy visible y evita que el personaje se sienta rígido, acentuando su inercia física.

### 2. Anticipación (Anticipation)
* **Aplicación en MiniJason:**
  * **Lanzamiento de Hacha Machete:** [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L321-L340) - Antes de lanzar, MiniJason realiza un "wind-up" o preparación de `0.25s` donde rota hacia la cámara, levanta el brazo armado y acumula energía hacia atrás antes de soltar el proyectil.
  * **Patada de Fútbol:** [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L907-L925) - En la patada procedural, MiniJason flexiona el muslo y la rodilla hacia atrás en la "Fase 1" (`0.12s`) preparando el golpe con el pie derecho.
* **Justificación:** Prepara visualmente al jugador para la acción violenta o deportiva de MiniJason, dándole fuerza y credibilidad al impacto final.

### 3. Puesta en Escena (Staging)
* **Aplicación en MiniJason:**
  * **Cámara de Apuntado y Mira UI:** [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L190-L228) - Al apuntar con click derecho, la cámara hace zoom sobre el hombro de MiniJason y bloquea su rotación con la mirada de la cámara, activando una retícula roja central ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L557-L616)).
  * **Carga de Energía:** El UI de la barra de carga ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L785-L821)) cambia gradualmente a rojo intenso debajo del crosshair, escenificando la potencia con la que se pateará el balón.
* **Justificación:** Enfoca la atención directamente en la puntería y la acción de MiniJason, asegurando que el combate a distancia y la conducción del balón sean claras y legibles.

### 4. Acción Directa y Pose a Pose (Straight Ahead and Pose to Pose)
* **Aplicación en MiniJason:**
  * **Animaciones Clave (Pose a Pose):** Los ciclos tradicionales de MiniJason (*Idle* amenazante, *Caminar* con pasos cortos, *Correr* apresurado, *Saltar* y *Muerte*) están animados fotograma a fotograma para mantener poses sólidas y legibles.
  * **Patada Procedural (Pose a Pose en Código):** En [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L907-L959), programamos la patada mediante 3 poses de articulaciones clave: Carga (Pierna atrás), Impacto (Pie extendido) y Recuperación (Retorno a la posición inicial).
* **Justificación:** Garantiza un control preciso de la deformación anatómica de los huesos de MiniJason y el timing exacto del golpe de fútbol.

### 5. Acción Continuada y Superpuesta (Follow Through and Overlapping Action)
* **Aplicación en MiniJason:**
  * **Inercia del Lanzamiento:** [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L380-L384) - Después de que MiniJason suelta el hacha, su brazo continúa el recorrido descendente por `0.45s` antes de volver a la postura por defecto.
  * **Rebote del Proyectil:** En [ObjetoLanzable.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ObjetoLanzable.cs#L143-L167), al impactar al payaso arquero o a los defensores, el hacha rebota físicamente y cae al piso en lugar de detenerse bruscamente, representando el traspaso de energía.
  * **Caída del Arquero Rival:** En [PayasoArquero.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/PayasoArquero.cs#L267-L285), tras su estirada para atajar la pelota de MiniJason, el portero tiene un tiempo de asentamiento y caída antes de reincorporarse.
* **Justificación:** Elimina la rigidez mecánica del personaje, mostrando cómo las distintas partes y objetos continúan moviéndose debido a su propia inercia tras cesar la fuerza principal.

### 6. Aceleración y Desaceleración (Slow In and Slow Out)
* **Aplicación en MiniJason:**
  * **Mezcla de Animación (Animator):** Las transiciones entre caminar, correr y saltar de MiniJason usan un `CrossFade` de `0.15s` ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L535-L541)) para evitar cortes abruptos de poses.
  * **Movimiento Matemático Eased:** El Squash & Stretch ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L919)) y la patada de MiniJason aplican `Mathf.SmoothStep` para que el cambio de escala y articulación acelere gradualmente al salir de la pose y desacelere al entrar.
* **Justificación:** Representa de forma creíble la masa física de MiniJason; los objetos y extremidades pesadas no alcanzan velocidades máximas instantáneamente.

### 7. Arcos (Arcs)
* **Aplicación en MiniJason:**
  * **Trayectoria Parabólica del Hacha:** El hacha arrojada por MiniJason vuela utilizando gravedad física ([ObjetoLanzable.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ObjetoLanzable.cs#L81)), describiendo una elegante curva balística en el espacio.
  * **Giro de Extremidades:** La pierna de MiniJason al patear y el brazo al lanzar hachas giran dibujando un arco circular limpio alrededor del pivote de su cadera y hombro.
  * **Zambullida del Portero:** La atajada del Payaso Arquero ([PayasoArquero.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/PayasoArquero.cs#L258-L261)) sigue una trayectoria de salto en arco mediante una función sinusoidal.
* **Justificación:** Todas las trayectorias de lanzamiento y movimientos biológicos de MiniJason fluyen en curvas naturales, mejorando la fluidez visual.

### 8. Acción Secundaria (Secondary Action)
* **Aplicación en MiniJason:**
  * **Giro del Hacha:** Mientras el hacha vuela en su arco parabólico, realiza un giro secundario continuo de `720°/s` sobre su propio eje ([ObjetoLanzable.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ObjetoLanzable.cs#L117-L126)).
  * **Oscilación de Conducción (Dribbling):** En [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L648-L665), la pelota de fútbol oscila de izquierda a derecha al pie de MiniJason mientras corre, simulando la conducción alternada de pies.
  * **Sangre de Impacto:** Los enemigos golpeados por el hacha de MiniJason despiden un spray de pequeñas esferas rojas con física ([PayasoArquero.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/PayasoArquero.cs#L154-L189)).
* **Justificación:** Aporta dinamismo secundario que enriquece y da soporte a las acciones principales del gameplay (correr y atacar).

### 9. Timing y Spacing (Timing)
* **Aplicación en MiniJason:**
  * **Caminar vs. Correr:** MiniJason camina a una velocidad de `3f` ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L10)) y corre velozmente a `7f` (LShift). Esto ajusta drásticamente la frecuencia de sus pisadas y la separación entre cuadros (spacing) en el suelo.
  * **Contraste Rítmico de la Patada:** En [ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L915-L945), la pierna tarda `0.12s` en cargar (lento) y solo `0.10s` en impactar (muy rápido), seguido de una recuperación de `0.28s`.
* **Justificación:** La alternancia entre movimientos rápidos (lanzamiento y golpe) y lentos (preparación y reposo) define la sensación de fuerza e intención física de MiniJason.

### 10. Exageración (Exaggeration)
* **Aplicación en MiniJason:**
  * **Destrucción de Calabazas:** Al ser golpeadas por el hacha de MiniJason, las calabazas del escenario estallan con un destello de luz naranja temporal de intensidad `4f` y generan `12` escombros físicos que salen despedidos violentamente ([ObjetoLanzable.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ObjetoLanzable.cs#L211-L254)).
  * **Knockback y Aturdimiento:** Si un defensor tacklea a MiniJason ([DefensorAI.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/DefensorAI.cs#L368-L382)), este sale volando con una fuerza exagerada de `6.5f` y entra en un estado de parálisis facial/aturdimiento por `0.9s`.
* **Justificación:** Acentúa la naturaleza cómica/arcade del sandbox, haciendo que los impactos y las interacciones tengan un feedback visual muy divertido y gratificante.

### 11. Modelado Sólido (Solid Drawing)
* **Aplicación en MiniJason:**
  * **Preservación del Rig Chibi:** A pesar del diseño cabezón y caricaturesco de MiniJason, el rig 3D y las colisiones mantienen volumen y peso consistentes en el espacio tridimensional.
  * **Agachado y Cuerpo a Tierra Procedural:** Al presionar Ctrl ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L893-L905)), la cadera (`hips`) desciende `-0.45f` y las rodillas de sus piernas cortas se doblan proporcionalmente sin romper ni colapsar la malla del modelo 3D de MiniJason.
* **Justificación:** Mantiene la solidez, profundidad y balance de las proporciones del personaje desde cualquier ángulo de la cámara de juego.

### 12. Atractivo (Appeal)
* **Aplicación en MiniJason:**
  * **Identidad Carismática:** MiniJason es una parodia chibi de un icónico asesino de películas de terror con máscara de hockey, lo que genera un contraste cómico y adorable al interactuar con una pelota de fútbol y enfrentar payasos.
  * **Idle y Actitud Viva:** En estado de reposo, el bucle de respiración de MiniJason ([ControladorTerceraPersona.cs](file:///c:/Users/Rolda/Parcial_Animacion/Assets/Toon%20Killers%20&%20Survivors/DemoScripts/ControladorTerceraPersona.cs#L90)) le da una actitud atenta e inmersiva.
* **Justificación:** El diseño carismático y las respuestas visuales fluidas hacen que controlar a MiniJason sea inmediatamente atractivo y empático para el usuario.
