# Flujo del Juego - Creativo

## Turno completo (con ficha B)

1. **GameManager/GameSync** anuncia el turno via RPC a todos los clientes
2. **Dado** se activa solo para el jugador con turno (`GameSync.EsMiTurno`)
3. **Jugador tira el dado** (tecla Espacio o botón táctil)
4. **Panel SeleccionFichaUI** aparece con resultado del dado — elige Ficha A o Ficha B
5. **GameSync.EnviarResultadoDado(pasos, indiceFicha, esFichaB)** — RPC a todos los clientes
6. **RPC_RecibirMovimiento** ejecuta el movimiento en todos los clientes (ficha animada en todas las pantallas)
7. **MovimientoFicha/FichaInversa** llega a la casilla, revela carta si hay
8. **Panel CardPlayUI** aparece solo para el jugador local — elige Usar o Guardar
9. **GameManager.SiguienteTurno()** — solo el MasterClient decide, via `GameSync.NotificarFinDeMovimiento()`

## Turno sin ficha B

Igual pero el paso 4 (panel selección) se salta — el dado mueve Ficha A directo via RPC.

## Autoridad de red

- **MasterClient (host)**: decide turnos, detecta colisiones, activa batallas PPS
- **Todos los clientes**: animan movimientos, muestran UI local (cartas, energía)
- **Jugador local**: solo interactúa con su propio dado y sus propias cartas

## Scripts clave

| Script | Responsabilidad |
|--------|----------------|
| `GameManager.cs` | Estado del juego, lista de jugadores, turnos locales |
| `GameSync.cs` | RPCs de red — turno, movimiento, posición |
| `DadoLogico.cs` | Animación del dado, confirmar movimiento |
| `SeleccionFichaUI.cs` | Panel elegir Ficha A o B (aparece DESPUÉS del dado) |
| `MovimientoFicha.cs` | Movimiento Ficha A, revelar carta, continuar turno |
| `FichaInversa.cs` | Movimiento Ficha B (va de meta a inicio) |
| `CardPlayUI.cs` | Panel Usar/Guardar carta (solo jugador local) |
| `CardManager.cs` | Efectos de cartas |
| `GestorDeRutas.cs` | Lista de casillas del tablero (asignar en Inspector) |

## Errores frecuentes

- **Jugadores en (0,0,0)**: `GestorDeRutas` no encontrado o sin casillas asignadas en Inspector
- **Dado no responde**: `GameSync.EsMiTurno` false — actor number no coincide
- **Ficha no se mueve en otro cliente**: falta llamar `GameSync.EnviarResultadoDado` (no llamar `Avanzar` directo)
- **PhotonView missing**: Players 1-4 necesitan componente `PhotonView` asignado en Inspector
- **GestorDeRutas not found**: clase se llama `GestorDeRutas` (con s) — si Unity pierde la ref, reasignar script en Inspector y asignar casillas

## Nombre de clases importantes

- `GestorDeRutas` (CON s) — no cambiar, la escena lo referencia por nombre
