# API BuscaYa – Imágenes múltiples, foto tienda, foto perfil y ubicación

**Documento para el frontend (web/móvil).** Resumen de lo que implementó el backend, endpoints verificados y cómo consumirlos.

---

## Resumen: lo que implementó el backend (nuevas cosas)

| Funcionalidad | Qué hace el backend | Estado |
|---------------|---------------------|--------|
| **Galería de producto** | Al crear/actualizar producto se acepta `imagenesUrls` (array). Se guarda imagen principal + galería en tabla `ProductoImagenes`. En detalle público se devuelve `galeriaUrls`. | ✅ Verificado |
| **Foto de tienda al crear** | Al crear tienda (`POST /api/cliente/crear-tienda`) se aceptan `fotoTienda` y `logoTienda` (URLs). La tienda se guarda con `FotoUrl` y `LogoUrl`. | ✅ Verificado |
| **Ubicación de tienda (lat/long)** | Al crear tienda se exigen `latitud` y `longitud` (decimal). Se guardan en la tienda para búsquedas cercanas y mapas. | ✅ Verificado |
| **Foto de perfil** | Usuario tiene `fotoPerfilUrl`. Se puede actualizar con `PUT /api/auth/user` (campo `fotoPerfilUrl`) o con `POST /api/auth/user/foto` (sube base64 y actualiza en una llamada). Login y GET perfil devuelven `fotoPerfilUrl`. | ✅ Verificado |

**Base de datos:** Migración `ProductoImagenYFotoPerfil` aplicada: tabla `ProductoImagenes`, columna `FotoPerfilUrl` en `Usuarios`.

---

## 1. Producto con múltiples imágenes (galería)

### Comportamiento
- Al **crear** o **actualizar** un producto se pueden enviar **varias URLs** en `imagenesUrls`.
- La **primera URL** se usa como imagen principal (`fotoUrl`) en listados.
- En detalle público la API devuelve **galeriaUrls**: lista ordenada (principal + resto).

### Subir imágenes antes de crear/actualizar producto

**Opción A – Base64 (recomendada en móvil)**  
`POST /api/s3/image/base64`  
Headers: `Authorization: Bearer {token}`  
Body (JSON):

```json
{
  "prefix": "productos/",
  "imageBase64": "data:image/jpeg;base64,/9j/4AAQ...",
  "previousImageUrl": null
}
```

- **prefix**: obligatorio. Usar `productos/` para fotos de producto.
- **imageBase64**: obligatorio. Puede ser solo el base64 o con prefijo `data:image/jpeg;base64,` (el backend acepta ambos).
- **previousImageUrl**: opcional. Si se reemplaza una imagen existente, pasar su URL para borrarla de S3.

Respuesta 200: `{ "url": "https://..." }`  
Llamar una vez por cada imagen; reunir las URLs en un array para `imagenesUrls`.

**Opción B – Multipart (web)**  
`POST /api/s3/image/webp`  
Form-data: `prefix` = `productos/`, `image` = archivo, `previousImageUrl` (opcional).  
Respuesta 200: `{ "url": "https://..." }`

### Crear producto con galería

`POST /api/tienda/productos`  
Headers: `Authorization: Bearer {token}`  
Body (JSON):

```json
{
  "nombre": "Ejemplo",
  "descripcion": "Descripción",
  "precio": 100,
  "moneda": "C$",
  "categoriaId": 1,
  "fotoUrl": null,
  "imagenesUrls": [
    "https://.../imagen1.jpg",
    "https://.../imagen2.jpg",
    "https://.../imagen3.jpg"
  ]
}
```

- **fotoUrl**: opcional. Si no se envía, se usa la primera de `imagenesUrls` como principal.
- **imagenesUrls**: opcional. Si se envía, se guarda la galería; la primera es la principal si no hay `fotoUrl`.

Respuesta 201: cuerpo = producto creado (incluye `id`, `fotoUrl`; la galería se obtiene con GET).

### Actualizar producto (incluida la galería)

`PUT /api/tienda/productos/{id}`  
Body (JSON): mismos campos que crear. Si envías **imagenesUrls**, se **reemplaza toda** la galería; la primera URL pasa a ser la imagen principal si no envías **fotoUrl**.

### Obtener producto (dueño de tienda)

`GET /api/tienda/productos/{id}`  
Respuesta 200: entidad `Producto` con:
- `fotoUrl`: imagen principal.
- `imagenes`: `[{ "id", "productoId", "url", "orden" }, ...]`

### Detalle público del producto

`GET /api/public/producto/{id}?lat=...&lng=...`  
Respuesta 200 incluye:
- `fotoUrl`: imagen principal.
- **galeriaUrls**: `["urlPrincipal", "url2", "url3", ...]` en orden. Usar para carrusel/galería en la pantalla de detalle.

---

## 2. Crear tienda (imagen de tienda + ubicación lat/long)

### Endpoint
`POST /api/cliente/crear-tienda`  
Headers: `Authorization: Bearer {token}`

### Body (JSON)

```json
{
  "nombreTienda": "Mi Farmacia",
  "descripcionTienda": "Farmacia 24 horas",
  "telefonoTienda": "88881234",
  "whatsAppTienda": "88881234",
  "emailTienda": "farmacia@mail.com",
  "direccionTienda": "De la iglesia 2 c al sur",
  "latitud": 12.136389,
  "longitud": -86.251389,
  "ciudad": "Managua",
  "departamento": "Managua",
  "horarioApertura": "08:00:00",
  "horarioCierre": "20:00:00",
  "diasAtencion": "Lunes a Sábado",
  "logoTienda": "https://...",
  "fotoTienda": "https://..."
}
```

- **latitud** y **longitud**: obligatorios (decimal). Coordenadas de la tienda (GPS o mapa).
- **fotoTienda**: opcional. URL de la foto física del local. Obtenerla subiendo antes a S3 con `prefix: "tiendas/"`.
- **logoTienda**: opcional. URL del logo (subir a S3 con `prefix: "tiendas/"`).

### Cómo obtener la URL de la foto de tienda

1. Subir imagen: `POST /api/s3/image/base64` con `prefix: "tiendas/"` (o `POST /api/s3/image/webp` en web).
2. Usar la `url` de la respuesta en **fotoTienda** y/o **logoTienda**.

---

## 3. Foto de perfil del usuario

### Obtener perfil (incluye foto)
`GET /api/auth/user`  
Headers: `Authorization: Bearer {token}`  
Respuesta 200: objeto usuario con `fotoPerfilUrl` y resto de campos.

Login (`POST /api/auth/login`) y `PUT /api/auth/user` también devuelven el usuario con **fotoPerfilUrl**.

### Subir foto de perfil en una sola llamada (recomendado en móvil)

`POST /api/auth/user/foto`  
Headers: `Authorization: Bearer {token}`  
Body (JSON):

```json
{
  "imageBase64": "data:image/jpeg;base64,/9j/4AAQ..."
}
```

Respuesta 200:

```json
{
  "mensaje": "Foto de perfil actualizada",
  "url": "https://...",
  "usuario": {
    "id": 1,
    "nombreUsuario": "...",
    "nombreCompleto": "...",
    "rol": "...",
    "email": "...",
    "telefono": "...",
    "fotoPerfilUrl": "https://...",
    "tiendaId": null
  }
}
```

La imagen se sube a S3 (prefix `perfil/`) y se actualiza el usuario; no hace falta llamar después a `PUT /api/auth/user`.

### Actualizar perfil (incluida la URL de foto)

`PUT /api/auth/user`  
Body (JSON):

```json
{
  "nombreCompleto": "Juan Pérez",
  "telefono": "88881234",
  "email": "juan@mail.com",
  "fotoPerfilUrl": "https://..."
}
```

- **fotoPerfilUrl**: opcional. Si la app sube antes con `POST /api/s3/image/base64` (prefix `perfil/`), puede enviar aquí la `url` devuelta.

---

## 4. Resumen de endpoints (imágenes y ubicación)

| Acción | Método | Ruta | Auth | Notas |
|--------|--------|------|------|--------|
| Subir imagen (base64) | POST | `/api/s3/image/base64` | Sí | prefix: `productos/`, `tiendas/`, `perfil/` |
| Subir imagen (multipart) | POST | `/api/s3/image/webp` | Sí | idem |
| Crear producto | POST | `/api/tienda/productos` | Sí (tienda) | body con `imagenesUrls` (array) |
| Actualizar producto | PUT | `/api/tienda/productos/{id}` | Sí (tienda) | body con `imagenesUrls` reemplaza galería |
| Detalle producto público | GET | `/api/public/producto/{id}` | No | respuesta con `galeriaUrls` |
| Crear tienda | POST | `/api/cliente/crear-tienda` | Sí (cliente) | body con `latitud`, `longitud`, `fotoTienda`, `logoTienda` |
| Subir foto de perfil | POST | `/api/auth/user/foto` | Sí | body `{ "imageBase64": "..." }` |
| Actualizar perfil | PUT | `/api/auth/user` | Sí | body puede incluir `fotoPerfilUrl` |
| Obtener perfil | GET | `/api/auth/user` | Sí | respuesta con `fotoPerfilUrl` |

Todos los endpoints de S3 y de tienda/perfil requieren **Authorization: Bearer {token}** (excepto detalle público de producto).

---

## 5. Códigos de respuesta y errores

| Código | Significado |
|--------|-------------|
| 200 | OK (GET, PUT, POST foto perfil). |
| 201 | Created (POST producto). |
| 400 | Bad Request: datos inválidos, imagen no válida, prefix faltante, etc. Cuerpo: `{ "error": "mensaje" }`. |
| 401 | Unauthorized: token faltante o inválido. |
| 403 | Forbidden: no tienes permiso (ej. producto de otra tienda). |
| 404 | Not Found: producto/tienda/usuario no encontrado. |
| 500 | Error interno. Cuerpo puede incluir `mensaje`. |

Para S3/base64, si la imagen no se puede procesar (formato o tamaño), la API devuelve 400 con `{ "error": "No se pudo subir la imagen" }`.

---

## 6. Migración de base de datos

Antes de usar en producción, aplicar la migración en el servidor:

```bash
dotnet ef database update
```

La migración `ProductoImagenYFotoPerfil` crea la tabla **ProductoImagenes** y agrega la columna **FotoPerfilUrl** a **Usuarios**. Sin ella, las nuevas funciones de galería y foto de perfil fallan en BD.

---

## 7. Escenarios y flujos de ejemplo (resumidos)

### A: Crear producto con 3 fotos (móvil)
1. Usuario elige 3 fotos.  
2. Por cada foto: `POST /api/s3/image/base64` con `prefix: "productos/"` → guardar cada `url`.  
3. `POST /api/tienda/productos` con `imagenesUrls: [url1, url2, url3]` y resto de datos.  
4. Backend guarda producto y galería; la primera URL es la principal.

### B: Ver detalle de producto (galería)
1. `GET /api/public/producto/{id}?lat=...&lng=...`.  
2. Respuesta incluye `fotoUrl` y `galeriaUrls`.  
3. Mostrar carrusel con `galeriaUrls`; usar `fotoUrl` para miniatura/listado.

### C: Crear tienda con foto y ubicación
1. (Opc.) Subir foto: `POST /api/s3/image/base64`, `prefix: "tiendas/"` → `fotoTiendaUrl`.  
2. Obtener `latitud` y `longitud` (GPS o mapa).  
3. `POST /api/cliente/crear-tienda` con todos los datos, `latitud`, `longitud`, `fotoTienda`, `logoTienda`.  
4. Usuario pasa a ser dueño de tienda.

### D: Cambiar foto de perfil
1. Usuario elige foto.  
2. `POST /api/auth/user/foto` con `{ "imageBase64": "..." }`.  
3. Usar `usuario.fotoPerfilUrl` de la respuesta para actualizar la UI.

### E: Editar galería del producto
1. Subir nuevas fotos a S3 (`prefix: "productos/"`) → obtener URLs.  
2. `PUT /api/tienda/productos/{id}` con `imagenesUrls: [urlA, urlB, ...]`.  
3. Backend reemplaza toda la galería; la primera URL es la principal.

---

## 8. Checklist para el frontend

- [ ] **Producto con galería:** Subir N imágenes a S3 (`prefix: "productos/"`), luego `POST /api/tienda/productos` con `imagenesUrls`.
- [ ] **Detalle producto:** Consumir `galeriaUrls` de `GET /api/public/producto/{id}` para el carrusel.
- [ ] **Crear tienda:** Enviar `latitud`, `longitud`; opcionalmente subir foto a S3 (`prefix: "tiendas/"`) y enviar `fotoTienda` y/o `logoTienda`.
- [ ] **Foto de perfil:** Usar `POST /api/auth/user/foto` con base64 o actualizar `fotoPerfilUrl` en `PUT /api/auth/user`; mostrar `fotoPerfilUrl` en login y GET perfil.
- [ ] **Auth:** Incluir `Authorization: Bearer {token}` en todas las llamadas a S3, tienda, perfil y crear-tienda.
- [ ] **Base64:** Aceptar tanto `data:image/jpeg;base64,...` como solo el string base64 en `imageBase64`.

---

## 9. Verificación en backend (realizada)

- **ProductoService:** Crear producto con `imagenesUrls` guarda imagen principal y filas en `ProductoImagenes`; actualizar con `imagenesUrls` reemplaza la galería.
- **PublicController:** `GET /api/public/producto/{id}` incluye `galeriaUrls` (principal + resto por orden).
- **ClienteController:** Crear tienda recibe `fotoTienda`, `logoTienda`, `latitud`, `longitud` y los persiste.
- **AuthApiController:** `POST /api/auth/user/foto` sube a S3 y actualiza `FotoPerfilUrl`; login y GET perfil devuelven `fotoPerfilUrl`.
- **S3BucketController:** `POST /api/s3/image/base64` acepta `prefix`, `imageBase64`, `previousImageUrl` y devuelve `{ "url": "..." }`.
- **Migración:** Tabla `ProductoImagenes` y columna `FotoPerfilUrl` en `Usuarios` creadas y aplicadas.

Proyecto compila correctamente; endpoints y contratos están alineados con este documento.

---

## 10. Tests automatizados (imágenes de producto)

El backend incluye tests que **simulan y verifican** el flujo de imágenes de producto al 100%:

**Proyecto:** `BuscaYa.Tests` (xunit + EF InMemory)

**Tests ejecutados (todos pasan):**

| Test | Qué verifica |
|------|-------------------------------|
| `Crear_ConImagenesUrls_GuardaProductoYGalería` | Crear producto con 3 URLs guarda producto, `fotoUrl` = primera URL, y 3 filas en `ProductoImagenes` con orden 0,1,2. |
| `Crear_SinImagenesUrls_SoloGuardaFotoUrlSiSeEnvía` | Crear solo con `fotoUrl` no crea filas en `ProductoImagenes`. |
| `ObtenerPorId_ConImagenes_DevuelveImagenesOrdenadas` | `ObtenerPorId` devuelve producto con colección `Imagenes` ordenada por `Orden`. |
| `GaleriaUrls_SeConstruyeComoEnPublicController` | La misma lógica que usa `PublicController` (principal + resto sin duplicar) produce una lista de 3 URLs en orden. |
| `Actualizar_ConImagenesUrls_ReemplazaGalería` | Actualizar con nuevas `imagenesUrls` borra las anteriores, guarda las nuevas, primera URL = principal. |
| `Actualizar_ConImagenesUrlsYFotoUrl_UsaFotoUrlComoPrincipal` | Si se envía `fotoUrl` y `imagenesUrls`, la principal es `fotoUrl`. |

**Ejecutar tests:**

```bash
dotnet test BuscaYa.Tests/BuscaYa.Tests.csproj
```

Salida esperada: **Superado: 6**. Con esto se confirma que la lógica de galería de producto (crear, actualizar, obtener, construir `galeriaUrls`) funciona correctamente.
