# Notificaciones Push en la app BuscaYa (Flutter)

Este documento describe **todas las APIs del backend** que la **app móvil BuscaYa (Flutter)** debe consumir para **recibir, mostrar y gestionar notificaciones push** con Firebase Cloud Messaging (FCM). El backend está en ASP.NET Core; la app es Flutter.

---

## Índice

1. [Qué hace la app Flutter con las notificaciones](#1-qué-hace-la-app-flutter-con-las-notificaciones)
2. [APIs que la app Flutter debe consumir](#2-apis-que-la-app-flutter-debe-consumir)
3. [Autenticación (JWT)](#3-autenticación-jwt)
4. [Detalle de cada API](#4-detalle-de-cada-api)
5. [Implementación en Flutter (firebase_messaging)](#5-implementación-en-flutter-firebase_messaging)
6. [Datos que llegan en la notificación (FCM)](#6-datos-que-llegan-en-la-notificación-fcm)
7. [Códigos HTTP y errores](#7-códigos-http-y-errores)
8. [Resumen: llamadas por pantalla/acción](#8-resumen-llamadas-por-pantallaacción)

---

## 1. Qué hace la app Flutter con las notificaciones

- **Registrar el dispositivo** en el backend cuando el usuario inicia sesión (para que el backend pueda enviarle push).
- **Actualizar el token FCM** si Firebase lo renueva.
- **Desregistrar el dispositivo** al cerrar sesión (opcional).
- **Recibir** las notificaciones push enviadas por el backend vía FCM.
- **Mostrar** el historial de notificaciones en una pantalla de la app.
- **Marcar como leídas** cuando el usuario abre una notificación o “marcar todas como leídas”.

Todas esas acciones se hacen **consumiendo las APIs** que se detallan más abajo.

---

## 2. APIs que la app Flutter debe consumir

Base URL: la misma que usa la app para el resto del backend (ej. `https://api.buscaya.com` o tu base). Todas las rutas son relativas a esa base.

| # | Acción | Método | Ruta | Cuándo usarla |
|---|--------|--------|------|----------------|
| 1 | Registrar dispositivo | POST | `/api/notifications/devices` | Tras login; cuando tengas el FCM token |
| 2 | Actualizar token FCM | POST | `/api/notifications/devices/refresh-token` | Cuando Firebase notifique un nuevo token |
| 3 | Listar mis dispositivos | GET | `/api/notifications/devices` | Si necesitas mostrar o elegir dispositivo a eliminar |
| 4 | Eliminar dispositivo | DELETE | `/api/notifications/devices/{id}` | Al cerrar sesión (recomendado) |
| 5 | Listar mis notificaciones | GET | `/api/notifications/logs?page=1&pageSize=20` | Pantalla de historial de notificaciones |
| 6 | Marcar una como abierta | POST | `/api/notifications/logs/{id}/opened` | Cuando el usuario abre una notificación |
| 7 | Marcar todas como abiertas | POST | `/api/notifications/logs/opened-all` | Botón “Marcar todas como leídas” |
| 8 | Eliminar una notificación | DELETE | `/api/notifications/logs/{id}` | Botón eliminar en un ítem del historial |

**Rutas alternativas (misma funcionalidad):**

- Marcar una abierta: `POST /api/v1/push/notificationlog/{id}/opened`
- Eliminar una: `DELETE /api/v1/push/notificationlog/{id}`
- Marcar todas abiertas: `POST /api/v1/push/notificationlog/opened-all`

La app Flutter **no** usa las APIs de plantillas ni de “enviar” (esas son del panel admin en backend).

---

## 3. Autenticación (JWT)

Todas las peticiones anteriores requieren que el usuario esté logueado y que envíes el JWT en el header:

```
Authorization: Bearer <token_jwt>
```

- El mismo token que usas para el resto de la API de BuscaYa (login).
- Si el token falta o es inválido, el backend responde **401 Unauthorized**.
- Si el usuario no está logueado, la app no debe llamar a estas rutas (salvo que tengas un flujo especial).

---

## 4. Detalle de cada API

### 4.1 Registrar dispositivo (recibir push)

**Objetivo:** decirle al backend “envía las notificaciones a este dispositivo (este FCM token)” para el usuario actual.

- **Método:** `POST`
- **URL:** `/api/notifications/devices`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <jwt>`
- **Body:**

```json
{
  "fcmToken": "el_token_que_te_da_Firebase",
  "platform": "android"
}
```

- `platform`: usar `"android"` o `"ios"` según `Platform.isAndroid` / `Platform.isIOS` en Flutter.

**Respuesta 200:** el backend devuelve el dispositivo creado o actualizado:

```json
{
  "id": 1,
  "fcmToken": "...",
  "platform": "android",
  "lastActiveAt": "2025-02-09T12:00:00Z",
  "usuarioId": 5,
  "createdAt": "2025-02-09T10:00:00Z",
  "updatedAt": "2025-02-09T12:00:00Z"
}
```

Guarda el `id` si más adelante quieres llamar a `DELETE /api/notifications/devices/{id}` al cerrar sesión.

---

### 4.2 Actualizar token FCM

**Objetivo:** cuando Firebase genera un nuevo token, actualizarlo en el backend para seguir recibiendo push.

- **Método:** `POST`
- **URL:** `/api/notifications/devices/refresh-token`
- **Headers:** `Content-Type: application/json`, `Authorization: Bearer <jwt>`
- **Body:**

```json
{
  "currentFcmToken": "token_viejo",
  "newFcmToken": "token_nuevo",
  "platform": "android"
}
```

**Respuesta:** 200 con el dispositivo actualizado, o 404 si no existe ese dispositivo con `currentFcmToken`.

---

### 4.3 Listar mis dispositivos

- **Método:** `GET`
- **URL:** `/api/notifications/devices`
- **Headers:** `Authorization: Bearer <jwt>`

**Respuesta 200:** array de dispositivos del usuario (mismo formato que en 4.1). Sirve para mostrar lista o para elegir cuál eliminar al cerrar sesión.

---

### 4.4 Eliminar dispositivo

**Objetivo:** dejar de recibir push en este dispositivo (por ejemplo al cerrar sesión).

- **Método:** `DELETE`
- **URL:** `/api/notifications/devices/{id}`
- **Headers:** `Authorization: Bearer <jwt>`

**Respuesta:** 204 sin cuerpo, o 404.

---

### 4.5 Listar mis notificaciones (historial)

- **Método:** `GET`
- **URL:** `/api/notifications/logs?page=1&pageSize=20`
- **Headers:** `Authorization: Bearer <jwt>`
- **Query:** `page` (por defecto 1), `pageSize` (por defecto 20, máximo 50).

**Respuesta 200:**

```json
{
  "total": 42,
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": 10,
      "status": "sent",
      "payload": "{\"messageId\":\"...\"}",
      "sentAt": "2025-02-09T12:00:00Z",
      "deviceId": 1,
      "templateId": 2,
      "usuarioId": 5,
      "createdAt": "2025-02-09T12:00:00Z"
    }
  ]
}
```

- `status`: `"sent"`, `"opened"` o `"failed"`.
- El `id` de cada ítem se usa para “marcar como abierta” y “eliminar”.

---

### 4.6 Marcar una notificación como abierta

- **Método:** `POST`
- **URL:** `/api/notifications/logs/{id}/opened`
- **Headers:** `Authorization: Bearer <jwt>`

**Respuesta:** 204. Usar el `id` del ítem que viene en la lista de logs (o el que tengas cuando el usuario abre la notificación, si en el futuro el payload incluye `logId`).

---

### 4.7 Marcar todas como abiertas

- **Método:** `POST`
- **URL:** `/api/notifications/logs/opened-all`
- **Headers:** `Authorization: Bearer <jwt>`

**Respuesta:** 204.

---

### 4.8 Eliminar una notificación del historial

- **Método:** `DELETE`
- **URL:** `/api/notifications/logs/{id}`
- **Headers:** `Authorization: Bearer <jwt>`

**Respuesta:** 204.

---

## 5. Implementación en Flutter (firebase_messaging)

### Dependencias

En `pubspec.yaml` (o equivalente):

```yaml
dependencies:
  firebase_core: ^3.0.0
  firebase_messaging: ^15.0.0
```

Configura Firebase en el proyecto (Android/iOS) según la documentación de FlutterFire.

### Flujo recomendado en la app

1. **Al iniciar la app (con usuario ya logueado):**
   - Pedir permiso de notificaciones si aplica.
   - Obtener el token FCM: `FirebaseMessaging.instance.getToken()`.
   - Llamar a **POST /api/notifications/devices** con ese token y la plataforma (`android`/`ios`).
   - Guardar el `id` del dispositivo si quieres desregistrarlo al cerrar sesión.

2. **Escuchar renovación del token:**
   - `FirebaseMessaging.instance.onTokenRefresh` (o el equivalente en tu versión).
   - Cuando se dispare, llamar a **POST /api/notifications/devices/refresh-token** con el token anterior y el nuevo.

3. **Al hacer login exitoso:**
   - Obtener token FCM y llamar a **POST /api/notifications/devices** (igual que en el punto 1).

4. **Al cerrar sesión:**
   - Llamar a **DELETE /api/notifications/devices/{id}** por cada dispositivo que hayas registrado para ese usuario (o al menos el actual).

5. **Cuando llegue una notificación (foreground/background/terminated):**
   - Mostrarla según el título/cuerpo que traiga el mensaje FCM.
   - Los datos extra (tipo, pantalla, etc.) vienen en el mapa `data` del `RemoteMessage` (ver sección 6).
   - Si quieres “marcar como abierta” al abrir, necesitas el `id` del log: hoy el backend no lo envía en el payload; puedes implementar “marcar todas” al abrir la pantalla de notificaciones, o que el backend en el futuro envíe `logId` en `data`.

6. **Pantalla de historial de notificaciones:**
   - **GET /api/notifications/logs?page=1&pageSize=20** para cargar la lista.
   - Botón “Marcar todas como leídas”: **POST /api/notifications/logs/opened-all**.
   - Por ítem: “Marcar como leída”: **POST /api/notifications/logs/{id}/opened**; “Eliminar”: **DELETE /api/notifications/logs/{id}**.

### Ejemplo de llamada HTTP (registrar dispositivo)

La app ya debe tener un cliente HTTP (dio, http, etc.) y la base URL. Ejemplo conceptual:

```dart
// Tras obtener el token de Firebase:
final token = await FirebaseMessaging.instance.getToken();
final platform = Platform.isAndroid ? 'android' : 'ios';

final response = await http.post(
  Uri.parse('$baseUrl/api/notifications/devices'),
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $jwt',
  },
  body: jsonEncode({
    'fcmToken': token,
    'platform': platform,
  }),
);
// response.statusCode == 200 → guardar response body (dispositivo con id)
```

El resto de llamadas son igual: misma base URL, mismo header `Authorization: Bearer <jwt>`, y método/body según la tabla de la sección 2.

---

## 6. Datos que llegan en la notificación (FCM)

El backend envía cada push con un bloque **data** en el mensaje FCM. En Flutter lo lees desde `RemoteMessage.data` (es un `Map<String, String>`).

Campos que siempre o casi siempre envía el backend:

| Clave       | Descripción |
|------------|-------------|
| title      | Título de la notificación |
| body       | Cuerpo del mensaje |
| type       | Tipo (ej. `announcement`) |
| templateId | ID de la plantilla (string) |
| imageUrl   | URL de imagen (si la plantilla tiene imagen) |

Si el admin envía **extraData** (p. ej. `screen`, `promoId`), esas claves también vienen en `data`. Puedes usar `data['type']` o `data['screen']` para decidir a qué pantalla navegar cuando el usuario toque la notificación.

---

## 7. Códigos HTTP y errores

| Código | Significado para la app |
|--------|--------------------------|
| 200    | OK; cuerpo con JSON según el endpoint. |
| 204    | OK sin cuerpo (delete, mark opened, etc.). |
| 400    | Error de validación; cuerpo tipo `{ "error": "mensaje" }`. |
| 401    | No autenticado o token caducado; volver a login. |
| 404    | Recurso no encontrado o no pertenece al usuario. |
| 500    | Error del servidor; reintentar o mostrar mensaje genérico. |

Todas las rutas de notificaciones usan el mismo JWT que el resto de la API BuscaYa.

---

## 8. Resumen: llamadas por pantalla/acción

| Dónde / Cuándo en la app Flutter | API a consumir |
|-----------------------------------|----------------|
| Después del login (tener FCM token) | POST `/api/notifications/devices` |
| Cuando Firebase renueva el token | POST `/api/notifications/devices/refresh-token` |
| Cerrar sesión | DELETE `/api/notifications/devices/{id}` |
| Pantalla de notificaciones (lista) | GET `/api/notifications/logs?page=1&pageSize=20` |
| “Marcar todas como leídas” | POST `/api/notifications/logs/opened-all` |
| Marcar una como leída | POST `/api/notifications/logs/{id}/opened` |
| Eliminar una notificación | DELETE `/api/notifications/logs/{id}` |

Con esto la app Flutter tiene definidas **todas** las APIs que debe consumir para recibir y gestionar notificaciones push en BuscaYa.
