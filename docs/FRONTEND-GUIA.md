# Guía para el frontend — BuscaYa

Este documento resume **qué debe implementar el frontend** (app Flutter u otra) y **qué hace solo el backend**. Incluye autenticación (login, Google, Apple), producto en oferta, notificaciones push y disparos automáticos.

**Base URL de la API:** la misma que usen para el resto del backend (ej. `https://api.buscaya.com` o la configurada).

---

## Índice

1. [Autenticación](#1-autenticación)
2. [Producto en oferta](#2-producto-en-oferta)
3. [Notificaciones push](#3-notificaciones-push)
4. [Disparos automáticos (solo backend)](#4-disparos-automáticos-solo-backend)
5. [Documentos relacionados](#5-documentos-relacionados)

---

## 1. Autenticación

Todas las respuestas de login exitosas devuelven un **JWT** que debe enviarse en:

```http
Authorization: Bearer <token>
```

### 1.1 Login clásico (usuario y contraseña)

| Método | Ruta | Body |
|--------|------|------|
| POST | `/api/auth/login` | `{ "nombreUsuario": "...", "contrasena": "..." }` |

**Respuesta 200:** `{ "token": "...", "usuario": { ... }, "expiraEn": 10080 }`  
**401:** Usuario/contraseña incorrectos o usuario inactivo.

---

### 1.2 Login con Google

El frontend obtiene el **idToken** de Google Sign-In y lo envía al backend.

| Método | Ruta | Body |
|--------|------|------|
| POST | `/api/auth/google` | `{ "idToken": "..." }` |

**Si el usuario ya existe:** 200 con `token`, `usuario`, `expiraEn`.  
**Si es nuevo (primera vez con Google):** 200 con `needsCompletion: true`, `email`, `nombre`, `fotoPerfilUrl`. En ese caso el frontend debe mostrar pantalla para completar registro y llamar a **completar registro con Google**.

| Método | Ruta | Body |
|--------|------|------|
| POST | `/api/auth/google/complete` | `{ "idToken": "...", "nombreUsuario": "...", "contrasena": "...", "nombreCompleto": "...", "telefono": "...", "email": "..." }` |

**Respuesta 200:** `token`, `usuario`, `expiraEn`.  
**400:** "Este usuario de Google ya tiene una cuenta" → indicar que use "Iniciar sesión con Google".

---

### 1.3 Login con Apple

El frontend obtiene el **identityToken** (JWT) de Sign in with Apple y lo envía al backend. **No se usa Firebase** para Apple; el backend verifica el token con las claves públicas de Apple.

| Método | Ruta | Body |
|--------|------|------|
| POST | `/api/auth/apple` | `{ "identityToken": "..." }` |

**Si el usuario ya existe:** 200 con `token`, `usuario`, `expiraEn`.  
**Si es nuevo (primera vez con Apple):** 200 con `needsCompletion: true`, `email`, `nombre`. Mostrar pantalla para completar registro y llamar a **completar registro con Apple**.

| Método | Ruta | Body |
|--------|------|------|
| POST | `/api/auth/apple/complete` | `{ "identityToken": "...", "nombreUsuario": "...", "contrasena": "...", "nombreCompleto": "...", "telefono": "...", "email": "..." }` |

**Respuesta 200:** `token`, `usuario`, `expiraEn`.  
**400:** "Este usuario de Apple ya tiene una cuenta" → indicar que use "Iniciar sesión con Apple".

**Nota:** Apple solo envía el nombre completo la primera vez; si no viene en la respuesta de Apple, el usuario puede llenarlo en la pantalla de completar registro.

---

### 1.4 Resumen auth

| Acción | Endpoint | Body principal |
|--------|----------|----------------|
| Login usuario/contraseña | POST /api/auth/login | nombreUsuario, contrasena |
| Login Google | POST /api/auth/google | idToken |
| Completar registro Google | POST /api/auth/google/complete | idToken, nombreUsuario, contrasena, nombreCompleto, telefono?, email? |
| Login Apple | POST /api/auth/apple | identityToken |
| Completar registro Apple | POST /api/auth/apple/complete | identityToken, nombreUsuario, contrasena, nombreCompleto, telefono?, email? |

Ante **401** en cualquier endpoint protegido: limpiar token y redirigir a login.

---

## 2. Producto en oferta

El backend ya soporta **oferta** en productos. El frontend debe:

- **Crear/editar producto (dueño de tienda):** mostrar un **switch "En oferta"** y, al activarlo, un campo opcional **"Precio anterior"**.
- **Listados y detalle:** si `enOferta` es true, mostrar badge "En oferta", precio actual destacado y, si existe `precioAnterior`, precio anterior tachado y `porcentajeDescuento` (ej. "-18%").

### Campos en la API

- **Request (crear/actualizar):** `enOferta` (boolean), `precioAnterior` (decimal?, opcional).
- **Response (producto):** `enOferta`, `precioAnterior`, `porcentajeDescuento` (int?, calculado por el backend).

**Detalle completo:** ver [docs/PRODUCTO-OFERTA-FRONTEND.md](PRODUCTO-OFERTA-FRONTEND.md).

---

## 3. Notificaciones push

El backend envía notificaciones push (FCM) y guarda un historial por usuario. El **frontend debe**:

1. **Registrar el dispositivo** al iniciar sesión (FCM token + plataforma).
2. **Actualizar el token** si Firebase lo renueva.
3. **Desregistrar** al cerrar sesión (opcional pero recomendado).
4. **Mostrar el historial** de notificaciones en una pantalla (lista con título, cuerpo, etc.).
5. **Marcar como abiertas** cuando el usuario abre una notificación o "marcar todas como leídas".

Todas las peticiones de notificaciones requieren **JWT** (`Authorization: Bearer <token>`).

### APIs que debe consumir el frontend

| Acción | Método | Ruta |
|--------|--------|------|
| Registrar dispositivo | POST | `/api/notifications/devices` |
| Actualizar token FCM | POST | `/api/notifications/devices/refresh-token` |
| Listar mis dispositivos | GET | `/api/notifications/devices` |
| Eliminar dispositivo | DELETE | `/api/notifications/devices/{id}` |
| Listar mis notificaciones (historial) | GET | `/api/notifications/logs?page=1&pageSize=20` |
| Marcar una como abierta | POST | `/api/notifications/logs/{id}/opened` |
| Marcar todas como abiertas | POST | `/api/notifications/logs/opened-all` |
| Eliminar una notificación | DELETE | `/api/notifications/logs/{id}` |

En **GET logs** cada ítem incluye `title` y `body` (además de `payload`) para mostrar "título" y "cuerpo" en el historial.

**Detalle de bodies, respuestas y payload FCM:** si existe en el repo, ver el documento específico de notificaciones push (tabla de APIs, Flutter, códigos HTTP).

---

## 4. Disparos automáticos (solo backend)

El backend **envía solo** estas notificaciones cuando ocurren ciertos eventos. **El frontend no tiene que hacer nada para dispararlas**; solo debe estar registrado (FCM token) y mostrar/abrir lo que llegue en el payload.

| Tipo | Cuándo se dispara | A quién | Ejemplo de mensaje |
|------|-------------------|--------|---------------------|
| **Nueva tienda cerca** | Se crea una tienda nueva | Usuarios con dirección guardada a ≤ 5 km | "Nueva tienda cerca: Ferretería El Constructor" |
| **Bajó de precio** | Se actualiza un producto y el precio baja | Usuarios que tienen ese producto en Favoritos | "Taladro ahora C$2,300" |
| **Volvió a haber stock** | El producto pasa de sin stock a con stock | Usuarios que tienen ese producto en Favoritos | "Taladro ya está disponible en Ferretería X" |

En el payload FCM vendrá `type` (`NEW_STORE_NEARBY`, `PRICE_DROP`, `BACK_IN_STOCK`) y datos como `storeId`, `productId`, `productName`, `storeName`, etc., para que el frontend pueda **navegar** a la tienda o al producto al tocar la notificación.

**Anti-spam:** el backend limita a 3 notificaciones por usuario en 24 h y no repite la misma entidad (misma tienda/producto) en ese periodo.

---

## 5. Documentos relacionados

| Tema | Documento |
|------|-----------|
| Tienda (estado abierto/cerrado), JWT (duración) | [TIENDA-Y-AUTENTICACION-FRONTEND.md](TIENDA-Y-AUTENTICACION-FRONTEND.md) |
| Producto en oferta (campos, UI, ejemplos) | [PRODUCTO-OFERTA-FRONTEND.md](PRODUCTO-OFERTA-FRONTEND.md) |
| Notificaciones push (APIs detalladas, Flutter, payload) | Ver en el repo el doc de notificaciones push si existe (ej. NOTIFICACIONES-PUSH.md) |
| Reportes | [API-REPORTES-FRONTEND.md](API-REPORTES-FRONTEND.md) |

---

**Resumen:** El frontend debe implementar login (clásico, Google, Apple), producto en oferta en formularios y listados, y el flujo de notificaciones (registro de dispositivo, historial, marcar abiertas). Los disparos de notificaciones (tienda cerca, bajó precio, restock) los hace el backend automáticamente.
