# API de Reportes — Documentación para Frontend

Documento para implementar en el frontend la funcionalidad de **reportar** productos o tiendas.

---

## ¿El usuario debe estar logueado para reportar?

**Sí. El reporte solo se puede enviar si el usuario está logueado.**

- El endpoint de crear reporte está protegido con **JWT**.
- Si el usuario **no** está logueado y se llama al endpoint, el backend responde **401 Unauthorized**.
- **Recomendación en el frontend:**
  - Si el usuario no tiene token/sesión → no mostrar el botón "Reportar" o mostrarlo deshabilitado y al hacer clic mostrar mensaje: *"Debes iniciar sesión para reportar"* y redirigir al login.
  - Si el usuario está logueado → mostrar el formulario de reporte y enviar el request con el header `Authorization: Bearer <token>`.

---

## Resumen del endpoint

| Aspecto | Valor |
|--------|--------|
| **Método** | `POST` |
| **URL** | `{BASE_URL}/api/reporte` |
| **Autenticación** | **Obligatoria** — JWT Bearer |
| **Content-Type** | `application/json` |

**Ejemplo de URL base (desarrollo):** `http://localhost:5229`  
**Ejemplo de URL completa:** `http://localhost:5229/api/reporte`

---

## Headers requeridos

```http
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>
```

El token es el mismo que devuelve el login en `POST /api/auth/login` dentro de `response.token`.

---

## Body del request (JSON)

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `tipo` | `string` | **Sí** | Solo `"producto"` o `"tienda"` (minúsculas). |
| `recursoId` | `number` | **Sí** | ID del producto o de la tienda que se reporta. Debe ser ≥ 1. |
| `razon` | `string` | **Sí** | Una de las razones permitidas (ver lista más abajo). |
| `detalle` | `string` | No | Texto adicional opcional. Puede ser `null` o omitirse. |

### Razones permitidas (exactas)

El backend solo acepta **exactamente** una de estas cadenas en `razon`:

- `"Contenido inapropiado"`
- `"Información falsa o engañosa"`
- `"Producto o tienda duplicada"`
- `"Spam o publicidad no deseada"`
- `"Otro"`

Cualquier otro valor devuelve **400** con mensaje de razón no válida.

---

## Ejemplo de request

```json
{
  "tipo": "producto",
  "recursoId": 5,
  "razon": "Información falsa o engañosa",
  "detalle": "El precio mostrado no coincide con el real."
}
```

Para tienda:

```json
{
  "tipo": "tienda",
  "recursoId": 3,
  "razon": "Spam o publicidad no deseada",
  "detalle": null
}
```

---

## Respuestas del servidor

### 201 Created — Reporte creado

**Body:**

```json
{
  "id": 1,
  "mensaje": "Reporte recibido. Gracias por tu ayuda."
}
```

Acción en frontend: mostrar mensaje de éxito y cerrar el modal/pantalla de reporte.

---

### 400 Bad Request — Datos inválidos

Posibles causas:

- Campos requeridos faltantes o tipos incorrectos.
- `tipo` distinto de `"producto"` o `"tienda"`.
- `razon` no es una de las 5 permitidas.
- `recursoId` &lt; 1.

**Ejemplo (razón no válida):**

```json
{
  "error": "Razón no válida"
}
```

**Ejemplo (validación de modelo):**

```json
{
  "error": "Datos inválidos",
  "detalles": { ... }
}
```

---

### 401 Unauthorized — No logueado o token inválido

- Usuario no envió header `Authorization`.
- Token expirado o inválido.

**Ejemplo:**

```json
{
  "error": "Token inválido"
}
```

Acción en frontend: redirigir al login y guardar la intención de reportar (ej. volver a la pantalla del producto/tienda tras login).

---

### 404 Not Found — Recurso no existe

El producto o tienda con ese `recursoId` no existe o no está activo.

**Ejemplo:**

```json
{
  "error": "El producto con ID 999 no existe"
}
```

o

```json
{
  "error": "La tienda con ID 999 no existe"
}
```

---

### 500 Internal Server Error

Error interno. Mostrar mensaje genérico al usuario y opcionalmente reintentar.

```json
{
  "error": "Error interno del servidor"
}
```

---

## Validaciones que hace el backend (resumen)

1. **Autenticación:** Usuario identificado por JWT; si no, 401.
2. **Tipo:** Solo `"producto"` o `"tienda"` (minúsculas).
3. **RecursoId:** Entero ≥ 1.
4. **Razon:** Una de las 5 cadenas listadas arriba.
5. **Existencia del recurso:** El producto o tienda debe existir y estar activo; si no, 404.

El `detalle` es opcional; si se envía vacío o solo espacios, el backend lo guarda como `null`.

---

## Flujo recomendado en el frontend

1. **Pantalla de producto o tienda**
   - Si el usuario **no** está logueado: botón "Reportar" deshabilitado o con mensaje "Inicia sesión para reportar" (y enlace al login).
   - Si está logueado: botón "Reportar" activo.

2. **Al hacer clic en "Reportar"**
   - Abrir modal o pantalla con:
     - Selector de **razón** (dropdown con las 5 opciones).
     - Campo opcional **detalle** (textarea).
     - Botón "Enviar reporte".

3. **Al enviar**
   - Construir body: `tipo` (`"producto"` o `"tienda"`), `recursoId` (id de la página actual), `razon`, `detalle` (opcional).
   - Enviar `POST` a `{BASE_URL}/api/reporte` con header `Authorization: Bearer <token>`.

4. **Según respuesta**
   - **201:** Mostrar "Reporte enviado. Gracias por tu ayuda." y cerrar modal.
   - **401:** Redirigir a login (y opcionalmente volver después a esta pantalla).
   - **400:** Mostrar mensaje de error (ej. "Razón no válida" o "Revisa los datos").
   - **404:** Mostrar "Este producto/tienda ya no existe".
   - **500:** Mensaje genérico de error.

---

## Ejemplo con `fetch` (JavaScript)

```javascript
const BASE_URL = 'http://localhost:5229'; // o tu variable de entorno

async function enviarReporte(token, tipo, recursoId, razon, detalle = null) {
  const response = await fetch(`${BASE_URL}/api/reporte`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
    },
    body: JSON.stringify({
      tipo,        // "producto" o "tienda"
      recursoId,
      razon,
      detalle: detalle || null,
    }),
  });

  const data = await response.json().catch(() => ({}));

  if (response.status === 201) {
    return { ok: true, id: data.id, mensaje: data.mensaje };
  }

  return {
    ok: false,
    status: response.status,
    error: data.error || 'Error al enviar el reporte',
  };
}

// Uso (solo si el usuario está logueado):
// enviarReporte(token, 'producto', 5, 'Información falsa o engañosa', 'El precio está mal');
```

---

## Ejemplo con Axios (JavaScript/TypeScript)

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5229',
  headers: { 'Content-Type': 'application/json' },
});

// Interceptor para añadir el token si existe
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token'); // o tu store de auth
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

async function enviarReporte(payload) {
  try {
    const { data } = await api.post('/api/reporte', payload);
    return { ok: true, id: data.id, mensaje: data.mensaje };
  } catch (err) {
    const status = err.response?.status;
    const error = err.response?.data?.error || 'Error al enviar el reporte';
    return { ok: false, status, error };
  }
}

// Payload: { tipo, recursoId, razon, detalle? }
// enviarReporte({ tipo: 'tienda', recursoId: 3, razon: 'Otro', detalle: null });
```

---

## Constantes sugeridas para el frontend

```javascript
export const RAZONES_REPORTE = [
  'Contenido inapropiado',
  'Información falsa o engañosa',
  'Producto o tienda duplicada',
  'Spam o publicidad no deseada',
  'Otro',
];

export const TIPOS_RECURSO = ['producto', 'tienda'];
```

---

## Checklist de implementación

- [ ] Ocultar o deshabilitar "Reportar" si el usuario no está logueado; mostrar mensaje "Inicia sesión para reportar".
- [ ] En pantalla de producto: enviar `tipo: "producto"` y `recursoId` = id del producto.
- [ ] En pantalla de tienda: enviar `tipo: "tienda"` y `recursoId` = id de la tienda.
- [ ] Usar solo las 5 razones permitidas en el selector (copiar las cadenas exactas).
- [ ] Incluir header `Authorization: Bearer <token>` en el `POST /api/reporte`.
- [ ] Manejar 201 (éxito), 401 (redirigir a login), 400, 404 y 500 con mensajes claros al usuario.
- [ ] Opcional: guardar intención de reporte al redirigir a login para volver a la pantalla tras autenticarse.

---

## Cómo verificar que la API funciona

Ejecuta el backend (`dotnet run` en la raíz del proyecto) y en otra terminal:

1. **Sin token → debe devolver 401**
   ```bash
   curl -X POST http://localhost:5229/api/reporte \
     -H "Content-Type: application/json" \
     -d '{"tipo":"producto","recursoId":1,"razon":"Otro"}'
   ```
   Respuesta esperada: `401` y body con `"error":"Unauthorized"` o similar.

2. **Login para obtener token**
   ```bash
   curl -X POST http://localhost:5229/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"nombreUsuario":"TU_USUARIO","contrasena":"TU_PASSWORD"}'
   ```
   Copia el valor de `token` de la respuesta.

3. **Con token y datos válidos → debe devolver 201**
   ```bash
   curl -X POST http://localhost:5229/api/reporte \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer TU_TOKEN_AQUI" \
     -d '{"tipo":"producto","recursoId":1,"razon":"Otro","detalle":"Prueba desde curl"}'
   ```
   Si existe un producto con ID 1 activo: respuesta `201` y body con `id` y `mensaje`.  
   Si no existe: `404` con mensaje de recurso no encontrado.

4. **Razón inválida → debe devolver 400**
   ```bash
   curl -X POST http://localhost:5229/api/reporte \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer TU_TOKEN_AQUI" \
     -d '{"tipo":"producto","recursoId":1,"razon":"Razon inventada"}'
   ```
   Respuesta esperada: `400` y body con `"error":"Razón no válida"`.

Con esto el frontend puede implementar el reporte de productos y tiendas de forma completa y alineada con la API.
