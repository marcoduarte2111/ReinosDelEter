# 🎴 Guía de Prueba: Sistema de Visualización de Cartas en Combate

## ¿Qué se ha implementado?

El sistema ahora muestra cartas de forma visual durante el combate:
- ✅ Las cartas seleccionadas aparecen en el panel de combate
- ✅ Las cartas se oscurecen en la mano cuando se seleccionan
- ✅ Funciona para jugadores humanos y automático para IA
- ✅ Soporta placeholders para cartas sin sprite asignado

---

## 📋 Requisitos previos

Asegúrate de haber ejecutado estos pasos (deberían estar listos):

1. ✅ **Importar PNGs como Sprites**
   - Ve a `Assets/Cards/` y verifica que los PNGs tengan tipo `Sprite`
   - Si no: `Tools > Card System > Setup Card Images` (Editor)

2. ✅ **Crear CardData assets**
   - Navega a `Assets/Cards/`
   - Si no existen archivos `Card_XX.asset`:
     - `Tools > Card System > Create CardData Assets` (Editor)

3. ✅ **Asignar cartas a DeckManager**
   - Si DeckManager está vacío:
     - `Tools > Card System > Assign Cards to Deck` (Editor, requiere elemento)

---

## 🎮 Cómo probar en Play Mode

### Opción 1: Prueba Automática (Recomendado)

1. **Abre la escena principal** que contenga GameManager, HUDController, DeckManager

2. **Añade el script de prueba**:
   - Crea un GameObject vacío en la escena
   - Adjunta el componente `CombatPhaseTestSimulator`
   - Presiona Play

3. **En la pantalla verás dos botones**:
   - **▶️ Iniciar Simulación de Combate** - Simula un combate visual
   - **Diagnóstico Rápido** - Verifica que todo esté conectado

4. **Observa la consola** para ver logs detallados de cada paso

### Opción 2: Prueba Manual

1. **Inicia Play mode**

2. **Ejecuta el diagnóstico** con el script que agregaste:
   - `Tools > Debug > Verificar Sistema de Cartas en Combate` (Editor)
   - Revisa los ✅ y ❌ en la consola

3. **Si todo está bien**:
   - Inicia un combate en el juego normalmente
   - Selecciona una carta como atacante
   - **Deberías ver**:
     - La carta aparece en el cuadro de ataque
     - La carta se oscurece en tu mano
     - Se muestra el mensaje: `[HUD] Carta mostrada en combate`

4. **Selecciona carta como defensor**:
   - Deberías ver lo mismo en el cuadro de defensa

---

## 🔍 Verificación de componentes

Abre `Tools > Debug > Verificar Sistema de Cartas en Combate`:

El check incluye:
- ✅ HUDController existe
- ✅ Panel de combate está configurado
- ✅ `attackerCardDisplay` es RawImage
- ✅ `defenderCardDisplay` es RawImage
- ✅ DeckManager tiene cartas
- ✅ Las cartas tienen sprites asignados
- ✅ GameManager tiene jugadores
- ✅ Cada jugador tiene cartas en mano

Si ves ❌ en alguno, revisa la sección "Solución de problemas" abajo.

---

## 📊 Listado de cartas

Para ver todas las cartas del DeckManager:
- `Tools > Debug > Mostrar Cartas del DeckManager`

Verás:
```
  01. [✓] Card_01          | ATK:25 DEF:15 | Fire
  02. [✗] Card_02          | ATK:20 DEF:20 | Water
  ...
```

- ✓ = Tiene sprite asignado
- ✗ = Usa placeholder (sin sprite)

---

## 🚀 Flujo completo de prueba

```
1️⃣  Editor: Tools > Card System > Setup Card Images
     (Convierte PNGs a Sprite type)

2️⃣  Editor: Tools > Card System > Create CardData Assets
     (Crea Card_01.asset, Card_02.asset, etc.)

3️⃣  Editor: Tools > Card System > Assign Cards to Deck
     (Carga cartas en DeckManager)

4️⃣  Play Mode: Press Play en la escena

5️⃣  Play Mode: Ejecuta CombatPhaseTestSimulator
     (Simula un combate visual)

6️⃣  Play Mode: O inicia un combate normal en el juego

7️⃣  Prueba: Selecciona cartas y verifica:
     ✓ Aparecen en panel de combate
     ✓ Se oscurecen en la mano
     ✓ Ambos jugadores ven lo mismo
```

---

## 🆘 Solución de problemas

### Las cartas no aparecen en el panel de combate

**Causa 1**: `attackerCardDisplay` no es RawImage
- ¿Cómo verificar?: Abre HUDController en Inspector, busca "attackerCardDisplay"
- ¿Cómo arreglar?: Debe ser componente `RawImage`, no `Image`

**Causa 2**: Las cartas no tienen sprites asignados
- ¿Cómo verificar?: `Tools > Debug > Mostrar Cartas del DeckManager`
- Busca carrtas con ✗ en lugar de ✓
- ¿Cómo arreglar?: `Tools > Card System > Setup Card Images` (convierte tipos)

**Causa 3**: El panel de combate no está activo
- ¿Cómo verificar?: En el Inspector, SearchPath: `combatPanel` en HUDController
- ¿Ver que esté en la jerarquía?
- ¿Cómo arreglar?: Crea manualmente:
  - Canvas > combatPanel (Panel)
  - Dentro: combatResultLabel, attackerCardDisplay (RawImage), defenderCardDisplay (RawImage)

### Las cartas aparecen pero muy oscuras/pequeñas

**Problema**: Texture2D tiene tamaño muy pequeño o color incorrecto
- ¿Cómo arreglar?: Verifica el tamaño de los PNG originales (mínimo 128×180 px recomendado)
- En `Assets/Cards/` selecciona PNG, Inspector: "Texture Import Settings → Sprite"

### El panel dice "Player VS Player" pero nunca llena las cartas

**Problema**: HUDController.ShowCombatPanel() se llama pero DisplayCombatCard() no
- ¿Cómo verificar?: Busca en Console: "Carta mostrada en combate" (no debería haber)
- ¿Cómo arreglar?: Verifica que CombatManager.OnCardSelectedByPlayer() se está llamando:
  - Agrega breakpoint en CombatManager.OnCardSelectedByPlayer()
  - Selecciona una carta durante combate
  - ¿Se detiene? Si no, el evento no se está disparando

### El sistema se congela en combate

**Problema**: CombatCoroutine infinite loop
- ¿Cómo arreglar?: En CombatManager, revisa timeout:
  - Debe ser al menos 5 segundos (default: `cardSelectTimeout = 5f`)
  - Verifica que `_atkSelected` y `_defSelected` se actualizan

---

## 📝 Scripts principales

| Script | Ubicación | Función |
|--------|-----------|---------|
| **HUDController** | `Assets/Scripts/Board/` | Gestiona todos los paneles UI |
| **CombatManager** | `Assets/Scripts/Board/` | Resuelve combate y moestra cartas |
| **CardData** | `Assets/Scripts/Core/` | Define propiedades de carta |
| **CombatCardDisplayDiagnostics** | `Assets/Scripts/Editor/` | Herramientas de diagnóstico |
| **CombatPhaseTestSimulator** | `Assets/Scripts/Debugging/` | Simula combate para pruebas |

---

## 🎯 Siguientes pasos después de verificar

1. **AI Auto-Select**: Ya implementado, verifica que se muestre la carta elegida por IA antes del resultado

2. **Animaciones**: Añade transiciones suaves entre selección y resultado

3. **Efectos de sonido**: Reproduce sonido cuando se muestra cada carta

4. **Multi-ataque**: Si hay múltiples atacantes, verifica que cada uno vea su carta

---

## ❓ Preguntas frecuentes

**P: ¿Por qué solo RawImage y no Image?**
A: RawImage soporta Texture2D directo, mientras Image solo acepta Sprite. Como necesitamos mostrar Sprite.texture para RawImage, RawImage es más flexible.

**P: ¿Se queda la carta oscura en la mano después del combate?**
A: No, el script `CombatCoroutine` las remueve definitivamente cuando el combate termina (RemoveCard()).

**P: ¿Qué pasa si un jugador no selecciona antes del timeout?**
A: CombatCoroutine llama a BestCard() automáticamente y lo muestra en el panel antes de resolver.

---

## 📞 Para reportar problemas

Si algo no funciona:
1. Ejecuta `Tools > Debug > Verificar Sistema de Cartas`
2. Copia los logs de Console
3. Menciona qué acción esperabas vs qué pasó
4. Adjunta screenshot o video si es visual
