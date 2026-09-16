# S05MA - Semana 5 · NetworkTransform

**Desarrollo de Videojuegos para Móviles Avanzado** · 6.º ciclo · C26S

Dos cápsulas, un cofre que se abre manteniendo la E (si otro lo abre, tú no) y
cubos de colores que se agarran, se sueltan y se lanzan. Y seis casos, uno por
cada casilla importante del inspector de `NetworkTransform`.

Unity `6000.3.9f1` · Netcode for GameObjects `2.5.1` · Multiplayer Tools `2.2.12`.

## Cómo se monta

Cada script dice **arriba**, en un comentario: en qué orden crearlo, en qué
GameObject va, qué componentes necesita y qué hay que arrastrar.

**El mundo, en este orden:**

1. `ConnectionManager` — en el `NetworkManager`
2. `CuboDeColor` — prefab del cubo
3. `ChestNode` — el cofre de la escena
4. `PlayerAvatar` — prefab del jugador

**Los casos** (`Scripts/Casos`) son independientes. Los casos 01 y 04 usan
`NetworkTransformMedido`, que hay que crear antes. `RedSimulada` va en el
`NetworkManager`.

## Cómo se prueba

1. **Window → Multiplayer → Play Mode** → 2 o 3 jugadores virtuales → **Play**.
   El editor principal arranca como host y los virtuales como clientes, solos.
2. **P** en una ventana de cliente: 200 ms de latencia. Pulsa otra vez para
   añadir pérdida.
3. Mira siempre desde un cliente: en el host no hay nada que interpolar.

| Tecla | Qué hace |
|---|---|
| `WASD` | mover |
| `E` | abrir el cofre (mantener) / agarrar un cubo / soltarlo |
| `Q` | lanzar el cubo |
| `R` | volver al inicio con `Teleport` |
| `P` | red: perfecta → 200 ms → 200 ms + 20 % pérdida |

## Lo que está mal a propósito

- El cliente decide dónde está (Owner): puede hacer trampa → **semana 12**.
- Los cubos no se guardan en ningún inventario → **semana 7**.
- Cuántos bytes ahorra cada casilla, todavía no lo medimos → **semana 6**.
