# Revisión API BuscaYa – Confirmación backend vs frontend (Flutter)

Revisión realizada según lo implementado en el frontend. **Todo coincide**; se añadieron dos alias y una corrección en PUT perfil.

---

## 1. Autenticación / perfil de usuario

### GET perfil
- **Rutas:** `GET /api/auth/user` y **`GET /api/auth/profile`** (alias añadido para el fallback del frontend).
- **Respuesta:** Objeto usuario en la **raíz** del JSON (no envuelto en `usuario`):
  - `id`, `nombreUsuario`, `nombreCompleto`, `rol`, `email`, `telefono`, `fotoPerfilUrl`, `tiendaId`.
- **Confirmado:** Login y registro ya devuelven `Usuario` con `fotoPerfilUrl` (puede ser `null`).

### PUT perfil
- **Rutas:** `PUT /api/auth/user` y **`PUT /api/auth/profile`** (alias añadido).
- **Body aceptado:** `nombreCompleto`, `email`, `telefono`, `fotoPerfilUrl` (opcional).  
  **Nota:** `nombreUsuario` no es editable en el backend (el login no se cambia por seguridad).
- **Respuesta:** `{ "mensaje": "Perfil actualizado correctamente", "usuario": { id, nombreUsuario, nombreCompleto, rol, email, telefono, fotoPerfilUrl, tiendaId } }`.
- **Corrección aplicada:** El backend ahora **sí envía `fotoPerfilUrl`** al servicio en PUT; si el frontend manda `fotoPerfilUrl` en el body, se persiste.

### POST foto de perfil
- **Ruta:** `POST /api/auth/user/foto`.
- **Body:** `{ "imageBase64": "data:image/jpeg;base64,..." }`.
- **Respuesta:** `{ "mensaje": "Foto de perfil actualizada", "url": "https://...", "usuario": { id, nombreUsuario, nombreCompleto, rol, email, telefono, fotoPerfilUrl, tiendaId } }`.
- **Confirmado:** Coincide con lo esperado por el frontend.

### Login / registro
- **Respuesta:** Incluye `usuario` con `fotoPerfilUrl` (y el resto de campos). Puede ser `null` si aún no tiene foto.
- **Confirmado:** Ya implementado.

---

## 2. Productos (galería)

### POST y PUT producto
- **Rutas:** `POST /api/tienda/productos`, `PUT /api/tienda/productos/{id}`.
- **Body:** El frontend envía `imagenesUrls` (array de strings). La primera URL se usa como imagen principal si no se envía `fotoUrl`.
- **Confirmado:** Se guarda la galería en `ProductoImagenes` y la primera URL queda como imagen principal (`fotoUrl`).

### GET producto (dueño de tienda)
- **Ruta:** `GET /api/tienda/productos/{id}`.
- **Respuesta:** Incluye la entidad producto con **`imagenes`**: array de `{ "id", "productoId", "url", "orden" }` ordenado por `orden` (el servicio hace `Include(p => p.Imagenes.OrderBy(i => i.Orden))`).
- **Confirmado:** Coincide.

### GET producto (detalle público)
- **Ruta:** `GET /api/public/producto/{id}` (opcional: `?lat=...&lng=...`).
- **Respuesta:** Incluye **`galeriaUrls`** (array de strings: principal + resto en orden) y **`fotoUrl`** (imagen principal).
- **Confirmado:** Coincide.

---

## 3. Crear tienda

- **Ruta:** `POST /api/cliente/crear-tienda`.
- **Body:** El frontend envía `latitud`, `longitud` (obligatorios), `fotoTienda`, `logoTienda` (opcionales) y el resto de datos de la tienda.
- **Confirmado:** Se persisten `latitud`, `longitud`, `fotoTienda` (`FotoUrl` en tienda) y `logoTienda` (`LogoUrl` en tienda) en la entidad Tienda.

---

## 4. S3

- **Ruta:** `POST /api/s3/image/base64`.
- **Body:** `prefix` (obligatorio), `imageBase64` (obligatorio), `previousImageUrl` (opcional).
- **Respuesta:** `{ "url": "https://..." }`.
- **Prefijos usados por el frontend:** `productos/`, `tiendas/`, `perfil/` (o foto de perfil vía `POST /api/auth/user/foto` sin este endpoint).
- **Confirmado:** Coincide.

---

## 5. Códigos de error

- **400:** Body con `{ "error": "mensaje" }` (y en algunos casos `mensaje` adicional). El frontend puede mostrar `error` en la app.
- **401:** Token faltante o inválido; típicamente `{ "error": "Usuario no autenticado" }` o similar.
- **404:** Recurso no encontrado (producto, tienda, usuario); body con `{ "error": "..." }`.
- **Confirmado:** El backend usa este formato; el frontend puede usarlo para mostrar mensajes.

---

## Resumen de cambios realizados en el backend

1. **Alias de perfil:** Se añadieron **`GET /api/auth/profile`** y **`PUT /api/auth/profile`** como alias de `GET /api/auth/user` y `PUT /api/auth/user` para que el fallback del frontend funcione sin cambios.
2. **PUT perfil y fotoPerfilUrl:** Se corrigió la llamada a `ActualizarPerfil` para que reciba y persista **`fotoPerfilUrl`** cuando el frontend lo envía en el body de `PUT /api/auth/user` (o `PUT /api/auth/profile`).

---

## Resumen de lo que el frontend hace (ya alineado)

| Funcionalidad | Estado |
|---------------|--------|
| Galería de producto: subir N imágenes a S3 (`productos/`), enviar `imagenesUrls` en crear/actualizar; en detalle público usar `galeriaUrls` para el carrusel | ✅ Backend coincide |
| Crear tienda: subir foto a S3 (`tiendas/`), enviar `fotoTienda`; enviar siempre `latitud` y `longitud` | ✅ Backend coincide |
| Foto de perfil: mostrar `fotoPerfilUrl` en perfil; subir con `POST /api/auth/user/foto`; GET perfil y login devuelven `fotoPerfilUrl` | ✅ Backend coincide (+ alias profile y PUT con fotoPerfilUrl) |
| Todas las llamadas a S3, tienda, perfil y crear-tienda con `Authorization: Bearer {token}` | ✅ Requerido en backend |

Las rutas, cuerpos y respuestas descritas coinciden con el backend actual (con los alias y la corrección de PUT perfil aplicados). El frontend queda alineado al 100% con el backend.
