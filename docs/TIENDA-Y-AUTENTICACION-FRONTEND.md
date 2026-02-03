## Cambios para frontend: Tienda y JWT

### 1. Bandera `estaAbierta` en la tienda

- **Nuevo campo** en la respuesta de tienda (`TiendaResponse` en la API):
  - `estaAbierta: boolean`
- Cómo se calcula en el backend:
  - Usa `horarioApertura` y `horarioCierre` de la tienda.
  - Si no hay horario configurado → se considera `estaAbierta = false`.
  - Si el horario es normal (ej. 08:00–18:00) → abierta si la hora actual está entre apertura (incluida) y cierre (excluida).
  - Si el horario cruza medianoche (ej. 20:00–02:00) → abierta si la hora actual es **>= apertura** o **< cierre**.
  - La hora se toma con `DateTime.Now` del servidor (hora local del servidor).

**Uso recomendado en Flutter:**

- Mostrar un switch/label basado en `estaAbierta`:
  - `true` → texto \"Abierto\", color verde.
  - `false` → texto \"Cerrado\", color rojo/gris.
- `horarioApertura` y `horarioCierre` siguen llegando para mostrar el horario textual al usuario.

Ejemplo de JSON simplificado:

```json
{
  "id": 1,
  "nombre": "Mi Tienda",
  "horarioApertura": "08:00:00",
  "horarioCierre": "18:00:00",
  "estaAbierta": true,
  ...
}
```

---

### 2. Duración del JWT (login API)

- Antes: el token JWT vencía aprox. a la **1 hora** (`ExpirationInMinutes = 60`).
- Ahora: el token JWT se configuró para durar **7 días**:
  - `ExpirationInMinutes = 10080` (7 días * 24 h * 60 min).

Impacto para frontend:

- Endpoint de login sigue igual: `POST /api/auth/login`.
- La respuesta sigue trayendo:
  - `token`: string JWT.
  - `expiraEn`: ahora será `10080` (minutos).
- El frontend puede seguir guardando el token igual (local storage, secure storage, etc.).
- En la práctica, el usuario no tendrá que loguearse tan seguido; el token solo se invalida:
  - al pasar los 7 días,
  - o si el usuario cierra sesión manualmente (según implementes el logout en frontend).

**Recomendación para frontend:**

- Seguir enviando el header:

```http
Authorization: Bearer <token>
```

- Opcionalmente, usar `expiraEn` para mostrar algo en UI o para saber cuándo renovar token (si más adelante se implementa refresh token).
- Al recibir un **401 Unauthorized**:
  - Tratarlo como sesión expirada o inválida.
  - Limpiar token guardado.
  - Redirigir a pantalla de login.

Con estos cambios, el frontend puede:

- Mostrar claramente si la tienda está **abierta/cerrada** con un simple boolean.
- Mantener sesiones de usuario mucho más largas (similar a apps que “casi nunca te sacan”), siempre que el token sea guardado de forma persistente.

