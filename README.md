# S05MA - Semana 5

**Desarrollo de Videojuegos para Móviles Avanzado** · 6.º ciclo · C26S
Laboratorio 5: *NetworkTransform — lo que hay detrás de cada casilla del inspector*

Desde la semana 2 la posición viaja por RPC: unos 60 mensajes fiables por
segundo por jugador, los demás te ven a saltos y al que entra tarde no le llega
nada. Hoy la posición pasa a ser lo que siempre fue, **estado**, y la replica
`NetworkTransform`.

Casi todos lo habéis usado alguna vez. Lo que casi nadie ha hecho es **abrir el
inspector y entender cada casilla**. Hoy sí, porque cada una responde a una
pregunta de red que ya sabemos formular.

> Un movimiento se **interpola**. Un salto se **teletransporta**.

---

## Cómo abrir el proyecto

1. **Unity Hub → Add → Add project from disk**
2. Elige la carpeta `S05MA`
3. Ábrelo con **Unity 6** (`6000.3.9f1`)

La primera vez tarda unos minutos: descarga Netcode for GameObjects `2.5.1`,
Multiplayer Play Mode y, **nuevo esta semana**, **Multiplayer Tools `2.2.12`**,
que trae el *Network Simulator*. Todo está en `Packages/manifest.json`.

## La escena ya viene hecha

`Assets/Scenes/Semana05.unity`. Si la rompes, **Tools → S05MA → Preparar escena
Semana 5** la vuelve a generar con los prefabs.

> 📖 **Lee `Assets/Editor/SetupSemana05.cs`.** Ahí está escrita, con su motivo,
> **cada casilla** del inspector que se toca hoy: por qué la cápsula es `Owner` y
> el cubo `Server`, por qué el cubo lleva *Switch Transform Space When Parented*
> y la cápsula no, por qué *Use Rigid Body For Motion* va apagado.

## Arranca

1. **Window → Multiplayer → Play Mode** → *Enable* → **Virtual Players: 2 o 3**
2. **Play**
3. En la ventana principal pulsa **Host**; en las otras, **Client**

> 🔑 **Esta semana la latencia no es opcional.** En el aula la red mide 1 ms y
> todo se ve perfecto. Pulsa **`P` en una ventana de cliente** para meterle
> **200 ms**, y otra vez para **200 ms + 20 % de pérdida**. Va por ventana: cada
> jugador virtual tiene su propio simulador. La conexión activa se ve arriba a la
> derecha.

> ⚠️ **Mira siempre desde una ventana de cliente.** En la del host no hay nada
> que interpolar: el host *es* el servidor.

## Controles

| Tecla | Qué hace |
|---|---|
| `WASD` | mover tu cápsula |
| `ESPACIO` | talar el árbol más cercano *(semana 3)* |
| `F` | golpear al jugador más cercano *(semana 4)* |
| **`E` (mantener)** | **abrir el cofre** *(nuevo)* |
| **`E`** | **agarrar un cubo / soltarlo** *(nuevo)* |
| **`Q`** | **lanzar el cubo que llevas** *(nuevo)* |
| **`P`** | **red simulada:** perfecta → 200 ms → 200 ms + 20 % pérdida |
| `1` `2` `3` | caso 01 — informe, parar el baile, umbral a 0 |
| `4` `5` · `I` `J` `K` `L` | caso 02 — hazte dueño, informe · mover los maniquís |
| `6` `7` | caso 03 — parar el zigzag, informe de latencia |
| `9` `0` | caso 04 — informe, contadores a cero |
| `H` `G` | caso 05 — agarrar los dos cubos, soltarlos |
| `N` `M` `B` | caso 06 — mover con `position`, mover con `Teleport`, `Teleport` desde un cliente |

## Los seis casos

Uno por hito. Cada uno vive en su zona del mapa, con su nombre flotando encima,
se lee entero en su archivo y se ejecuta por etapas.

| # | Caso | Qué demuestra |
|---|---|---|
| 01 | Lo que viaja | dos maniquís con el mismo baile: el de todos los ejes y el recortado como la cápsula. Lo que no se marca no viaja; quieto no se envía nada, y con el umbral a 0 se envía en cada *tick* |
| 02 | Quién escribe ⭐ | eres dueño de los dos maniquís y solo se mueve el de `Owner`. `CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner`. Y la trampa del host, donde se mueven los dos |
| 03 | Tipos de interpolación ⭐ | cuatro filas con el mismo zigzag a 200 ms: sin interpolar, `LegacyLerp`, `Lerp` y `SmoothDampening`. Y cuánto va lo remoto por detrás |
| 04 | Lo que pesa un estado | el mismo giro en tres ejes, estándar frente a comprimido, con pérdida: uno lo recibe todo tarde, al otro le faltan estados y se ve igual |
| 05 | El hijo que se queda atrás ⭐ | dos cubos en la mano a 200 ms: el de espacio de mundo te persigue, el de espacio local va pegado. Y por qué *Tick Sync Children* no lo salva |
| 06 | Salto o vuelo | el mismo cambio de esquina con `transform.position` (vuela) y con `Teleport` (aparece), y la excepción al teletransportar sin autoridad |

`NetworkTransformMedido` y `RedSimulada`, en `Scripts/Comun`, son andamiaje: el
primero es un `NetworkTransform` que cuenta lo que envía y recibe, y el segundo
cambia la conexión con la `P`. No son el tema de hoy.

## El avance del mundo compartido

Al terminar la sesión el mundo hace dos cosas que ayer no hacía:

1. **Los demás jugadores caminan** en vez de dar saltos.
2. **Hay un cofre, como en Fortnite pero simplificado.** Mantienes la `E` y se
   va abriendo; **mientras alguien lo abre, nadie más puede**. Al abrirse suelta
   **cuatro cubos de colores** y desaparece. Los cubos se agarran, se sueltan y
   se lanzan, y si alguien lleva uno, nadie más puede quitárselo.

| Pieza | Qué hace |
|---|---|
| `PlayerAvatar` | **sin RPC de movimiento.** Lo replica `NetworkTransform` con autoridad `Owner`. La reaparición la decide el servidor y la ejecuta el dueño con `Teleport` |
| `ChestNode` | quién lo abre y el progreso son **estado** (`NetworkVariable`, semana 3); la barra se ve en todas las pantallas. Los RPC van con `RequireOwnership = false`: el cofre no es de nadie |
| `CuboDeColor` | `NetworkTransform` `Server` con espacio local al emparentarse y rotación comprimida; `NetworkRigidbody` cinemático en los clientes. Si cae del mapa, vuelve con `Teleport` |
| `PlayerCarry` | la intención (`agarrar`, `soltar`, `lanzar`) con `RequireOwnership = true`; el servidor decide, emparenta y pone la velocidad |

Dos líneas que se usan sin explicar, porque no son el tema: `cubo.Spawn()`, que
ya vimos en la semana 2, y `NetworkObject.Despawn()`, que es la semana 7.

### La matriz de autoridad, a día de hoy

| Entidad | Variable | Tipo | Decide | Replica | Visualiza |
|---|---|---|---|---|---|
| `PlayerHealth` | `vida` | estado | servidor | todos | todos |
| `PlayerHealth` | `IsDead` | **derivado** | — | nadie | todos |
| `PlayerCombat` | golpe | evento fiable | servidor | todos | todos |
| `PlayerAvatar` | `miColor` | estado | **dueño** ¹ | todos | todos |
| `PlayerAvatar` | **posición y rotación Y** | **estado (`NetworkTransform`)** | **dueño** ² | todos | todos |
| `TreeNode` | color del dueño, vida | estado | servidor | todos | todos |
| `ChestNode` | `abriendo`, `progreso` | estado | servidor | todos | todos |
| `CuboDeColor` | posición, rotación | estado (`NetworkTransform`) | servidor | todos | todos |
| `CuboDeColor` | padre (quién lo lleva) | estado (parentesco de red) | servidor | todos | todos |
| `CuboDeColor` | `color` | estado | servidor | todos | todos |

¹ Mentir sobre tu color no te da ventaja.
² **Fila nueva que deja escribir al cliente.** Se acepta porque un WASD con
200 ms de retardo en el dedo no se puede jugar. Tiene fecha de caducidad: la
semana 12.

## Lo que está mal a propósito

- **El cliente decide dónde está.** Puede correr el doble o teletransportarse, y
  el servidor lo acepta → **semana 12**.
- **Con autoridad de servidor, tu propio movimiento se sentiría con retardo.**
  Hoy lo esquivamos con `Owner` → **semana 10**, predicción y reconciliación.
- **El golpe y la tala no tienen ritmo**: van tan rápido como pulses →
  **semana 6**, el *tick* como pulso del juego.
- **Los cubos no se pueden guardar**, y la muerte sigue sin destruir nada →
  **semana 7**, *spawn*, *despawn* e inventario con `NetworkList`.
- **El servidor no comprueba que estés cerca** del cofre ni del cubo →
  **semana 12**.
- **Cuántos bytes ahorra cada casilla** no lo sabemos todavía: solo contamos
  estados → **semana 6**, Network Profiler.

## Entregable

Repositorio con tu mundo funcionando y vídeo con **dos ventanas visibles a la
vez, a 200 ms**, donde se vea:

1. los tipos de interpolación lado a lado (caso 03);
2. un cubo agarrado que va pegado a la cápsula del otro jugador sin perseguirla;
3. un cubo lanzado que vuela igual en las dos pantallas;
4. la tabla de tu inspector: qué casillas marcaste en la cápsula y en el cubo, y
   por qué.
