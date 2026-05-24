# Reinos del Éter

Juego de mesa digital en Unity para **4 jugadores**, dividido por **elementos**
(Agua, Fuego, Tierra, Aire). Cada jugador controla un elemento, avanza fichas por
un tablero en forma de estrella lanzando un dado, libra combates por cartas con
ventaja elemental y conquista castillos.

> **Idioma del proyecto:** el código, los logs y la UI están en español. Mantén
> ese idioma al escribir mensajes, nombres de cartas, etc.

---

## 1. Concepto y elementos

`ElementType` (`Assets/Scripts/Board/Tile.cs`): `Water=0, Fire=1, Earth=2, Air=3, Center=4`.

- Cada jugador tiene un elemento, un color, un brazo del tablero, un castillo y un mazo.
- El **centro** (Éter) es neutral.

### Rueda elemental (núcleo del combate)
`CombatManager.Beats()`: ciclo tipo piedra-papel-tijera.

```
Agua → vence a → Fuego → vence a → Tierra → vence a → Aire → vence a → Agua
```

Si tu elemento vence al rival, tu carta recibe bonus **×1.4** (`elementBonus`).

---

## 2. Tablero

Generado **en runtime** por `BoardGenerator` (en `Awake()` llama a `GenerateBoard()`,
que primero hace `ClearBoard()` y regenera todo desde los prefabs).

- **4 brazos** en diagonal desde el centro, uno por elemento. La casilla del extremo
  exterior de cada brazo es el **castillo** (`TileType.Start`, propiedad `IsCastle`).
- **Anillo** que recorre el perímetro conectando los castillos entre sí mediante
  casillas `Ring_*` (`pathIndex == -1`).
- **Casilla central** (`TileType.Center`) conectada al extremo interior de cada brazo.
- Parámetros: `tilesPerArm = 5`, `ringTilesPerSide = 6`, `armSpacing = 1.5`.
- Los castillos se pueden **conquistar** (`Tile.ownedByPlayer`, `Conquer`/`Free`) y
  se tiñen del color del jugador.

> ⚠️ **El tablero se regenera al pulsar Play.** Las casillas que se ven horneadas
> en `Game.unity` son una vista previa de edición; en runtime `BoardGenerator` las
> destruye y las recrea desde los prefabs. Cualquier arreglo que deba verse **en el
> juego** va en el generador / los prefabs / los scripts, no solo en los objetos
> guardados de la escena.

### Mallas de las casillas
Las mallas están modeladas **de pie** (plano XY). Los prefabs en
`Assets/Prefabs/Tiles/` (`fireTile`, `waterTile`, `earthTile`, `airTile`, `midTile`)
guardan una rotación de **-90° en X** para acostarlas planas.

---

## 3. Flujo de juego y turnos

`GameManager` (singleton `Instance`) orquesta todo. Estados (`GameManager.GameState`):

`Setup → WaitingForRoll → WaitingForPieceSelect → WaitingForDirection → Moving → Combat → GameOver`

Turno típico:
1. **Lanzar dado** (`OnRollDice`): usa `DiceRoller` (dado 3D físico) o fallback numérico 1-6.
2. **Elegir ficha** (`WaitingForPieceSelect`): click sobre una ficha propia.
3. **Elegir dirección** (`WaitingForDirection`): se resaltan los vecinos válidos.
4. **Movimiento** (`Piece.MoveStep`): salta casilla a casilla.
   - En casillas lineales avanza automático.
   - En el **centro** siempre pide dirección (`forceJunction`).
   - Si se topa con un enemigo → **combate** automático.
5. **Post-movimiento** (`AfterMove`): conquista de castillo si aplica, restaura energía, siguiente turno.

Setup inicial (`GameManager.Start`): asigna elementos al azar (`AssignRandomElements`),
reparte cartas (`DealCards`), instancia fichas (`SpawnPieces`, `piecesPerPlayer = 3`).
Lee config del menú vía `GameConfig.Instance` si existe.

---

## 4. Combate

`CombatManager` resuelve combates **por cartas**. Soporta **combate grupal**: N fichas
atacantes combinan fuerza contra 1 defensor.

- Cada participante elige una carta (la UI espera el click; diseño con timeout `cardSelectTimeout = 5s`).
- Se calcula ATK total vs DEF total, aplicando el bonus elemental ×1.4 cuando corresponde.
- El perdedor recibe daño (`PlayerData.TakeDamage`), el ganador suma `score`.
- Se aplican efectos de carta (`CardEffectType`): `Heal`, `DoubleDamage`, `Shield`,
  `StealCard`, `ExtraMove`.
- Las cartas usadas consumen energía y se descartan de la mano.
- La ficha derrotada **vuelve a su castillo** (`PlaceOnTile(home)`).

---

## 5. Cartas y mazos

- `CardData` (ScriptableObject, menú `Reinos del Éter/Card`): `cardName`, `description`,
  `element`, `cardArt` (sprite opcional), `attackPower`, `defensePower`, `energyCost`,
  `effectType`, `effectValue`. Si `cardArt` es null se usa un placeholder de color.
- `DeckManager` reparte **8 cartas** (`CardsPerPlayer = 8`) por jugador desde pools por
  elemento (`waterCards`, `fireCards`, `earthCards`, `airCards`). Si no hay assets
  asignados, genera cartas placeholder temáticas (`GeneratePlaceholderHand`).

---

## 6. Jugadores

`PlayerData` (clase plana, no MonoBehaviour): `element`, `hand`, `pieces`,
`health = 20`, `energy = 3`, `score`. Helpers: `TakeDamage`, `Heal`, `RestoreEnergy`,
`SpendEnergy`, `RemoveCard`. Colores y nombres por elemento (`ElementColor`, `ElementName`).

---

## 7. Estructura de scripts (`Assets/Scripts/`)

Namespace: `ReinosDelEter` en todo el código.

### `Board/` — núcleo de juego
| Script | Rol |
|---|---|
| `GameManager.cs` | Orquesta turnos, estados, movimiento, combate, conquista. Singleton. |
| `BoardGenerator.cs` | Genera el tablero (brazos, anillo, centro, castillos) en runtime. |
| `Tile.cs` | Casilla: elemento, tipo, vecinos, conquista, visuales. Define `ElementType` y `TileType`. |
| `Piece.cs` | Ficha: movimiento por saltos, cruces, dirección. |
| `CombatManager.cs` | Combate por cartas (1v1 y grupal), bonus elemental. |
| `CardData.cs` | ScriptableObject de carta + `CardEffectType`. |
| `DeckManager.cs` | Reparte manos por elemento; placeholders si faltan assets. |
| `PlayerData.cs` | Estado del jugador (vida, energía, mano, fichas, score). |
| `HUDController.cs` / `PlayerHUDPanel.cs` / `CardSlotUI.cs` | UI del juego (turnos, dado, cartas, combate). |
| `BoardCameraController.cs` | Cámara del tablero. |
| `Dice/DiceRoller.cs` | Dado 3D físico (con fallback numérico). |
| `BoardDebugger.cs`, `MovementDebugger.cs`, `CardSystemDiagnostics.cs`, `CardArtVerifier.cs` | Herramientas de depuración. |
| `Editor/*` | Utilidades de editor para crear/asignar/renombrar cartas y reconstruir paneles. |

### `MainMenu/`
- `GameConfig.cs` — config persistente entre escenas (nombres, `tilesPerArm`, `ringTilesPerSide`).
- `MainMenuBuilder.cs`, `MainMenuController.cs` — UI y lógica del menú.

### `Debugging/` y `Editor/`
- Simuladores y diagnósticos (`CombatPhaseTestSimulator.cs`, `CombatCardDisplayDiagnostics.cs`).

---

## 8. Escenas (`Assets/Scenes/`)

- **`Game.unity`** — la escena de juego. **Aquí se hace todo el trabajo de gameplay.**
- **`MainMenu.unity`** — menú principal. **NO TOCAR** salvo petición explícita.
- `Scenes_In_Progress/SampleScene.unity`, `_Recovery/0.unity` — escenas auxiliares/respaldo.

---

## 9. Cambios recientes (sesión de correcciones)

1. **Rotación de las casillas** — se veían verticales en runtime.
   - `BoardGenerator.SpawnTile`: ahora instancia con `prefab.transform.rotation` en vez
     de `Quaternion.identity` (que borraba el -90° X del prefab y dejaba las tiles de pie).
   - `Game.unity`: las 45 casillas horneadas se rotaron a -90° X para que la vista de
     edición coincida con el runtime. `MainMenu.unity` intacta.

2. **Continuación de movimiento tras combate** — al ganar, salía la opción de devolverse.
   - Ahora la ficha **sigue recta en la misma dirección** automáticamente; solo el
     **centro** abre elección de camino. Por los castillos sigue por el anillo sin menú.
   - `Piece.cs`: nuevos `LastStepFrom`, `StepsLeftAtStop`, `GetForwardStep()` y la bandera
     `continueStraight` (sigue por el anillo, `pathIndex == -1`, en cruces fuera del centro).
   - `GameManager.cs`: nuevo `ContinueAfterCombat()`; `HandleCombat` usa los pasos reales
     (`StepsLeftAtStop`); `BeginTurn` resetea `continueStraight` para no afectar el turno normal.

---

## 10. En qué estamos trabajando

- **Corrección de bugs de lógica/gameplay** en la escena Game (sesión en curso).
- **Pendiente: definir el final del juego / condición de victoria** (¿conquistar los 4
  castillos?, ¿llegar al centro?, ¿eliminar por HP?). Aún por cuadrar con el usuario.
- Confirmar detalles del reparto/robo de cartas.

---

## 11. Convenciones y notas importantes

- **Trabajar solo en `Game.unity`** para gameplay. **Nunca modificar `MainMenu.unity`**
  salvo petición explícita.
- **El tablero se regenera en runtime** (`BoardGenerator.Awake`). Para que un arreglo se
  vea en el juego, hazlo en el generador / prefabs / scripts, no solo en los objetos
  horneados de la escena.
- **Proyecto Unity activo: la carpeta `Proyecto/`** (este directorio). La carpeta externa
  `ReinosDelEter/` es el repo git pero solo contiene un `Assets/Cards` residual; el juego
  vive aquí.
- Las mallas de las casillas se modelaron de pie; los prefabs aplican -90° X para acostarlas.
- Los archivos de escena (`.unity`) en este repo usan finales de línea **LF**.
- No puedo ejecutar Unity desde aquí: los cambios de gameplay deben probarse en Play mode.
