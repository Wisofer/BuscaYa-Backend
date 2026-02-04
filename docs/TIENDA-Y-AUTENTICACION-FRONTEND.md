# Cambios para frontend: Tienda y JWT

## 1. Estado "Abierto / Cerrado" de la tienda (manual)

El estado **no se calcula** a partir del horario. Es un **switch manual** que el dueño controla desde la app.

### Campo en la API

En `TiendaResponse` (perfil de tienda, detalle público, etc.):

- `estaAbierta: boolean` — valor actual del estado.
- `horarioApertura` y `horarioCierre` siguen existiendo solo para mostrar texto (ej. "Horario: 8:00–18:00"); no determinan el estado.

### Cómo mostrarlo

- Pantalla del dueño (Crear/Editar tienda): switch que el dueño prende/apaga.
- Pantalla pública (lo que ve el cliente): chip/label "Abierto" (verde) o "Cerrado" (rojo/gris) según `estaAbierta`.

### Endpoint para actualizar el estado

El dueño cambia el switch → el frontend llama:

```
PATCH /api/tienda/estado
Authorization: Bearer <token>
Content-Type: application/json

Body:
{
  "estaAbiertaManual": true   // o false
}
```

**Respuesta 200:**

```json
{
  "mensaje": "Estado actualizado correctamente",
  "estaAbierta": true
}
```

**Nota:** Protegido con JWT. Solo el dueño de la tienda puede llamarlo.

### Al crear/editar tienda

- En `CrearTiendaRequest`: opcional `estaAbiertaManual` (default `true`).
- En `ActualizarTiendaRequest`: opcional `estaAbiertaManual`.
- Para cambios rápidos (solo abrir/cerrar) usar `PATCH /api/tienda/estado`.

---

## 2. Duración del JWT (login API)

- Antes: el token JWT vencía aprox. a la **1 hora** (`ExpirationInMinutes = 60`).
- Ahora: el token JWT dura **7 días**:
  - `ExpirationInMinutes = 10080` (7 días × 24 h × 60 min).

**Impacto para frontend:**

- Login sigue igual: `POST /api/auth/login`.
- Respuesta: `token`, `expiraEn` (ahora 10080).
- El usuario casi no tendrá que volver a iniciar sesión.
- El token solo se invalida a los 7 días o al cerrar sesión manualmente.

**Recomendación:**

- Seguir enviando `Authorization: Bearer <token>`.
- Ante 401: limpiar token, redirigir a login.

---

## 3. Resumen para Flutter

| Acción | Endpoint | Body |
|--------|----------|------|
| Ver perfil de tienda (dueño) | GET /api/tienda/perfil | — |
| Ver tienda (público) | GET /api/public/tienda/{id} | — |
| Cambiar switch Abierto/Cerrado | PATCH /api/tienda/estado | `{ "estaAbiertaManual": true/false }` |
| Editar perfil (incluye estado) | PUT /api/tienda/perfil | `ActualizarTiendaRequest` (opcional `estaAbiertaManual`) |

El frontend muestra `estaAbierta` tal cual viene en la respuesta y, al cambiar el switch del dueño, llama a `PATCH /api/tienda/estado`.
