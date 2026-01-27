# 📱 Guía de Integración Frontend - BuscaYa API

> **Documento técnico para desarrolladores frontend**  
> Esta guía contiene toda la información necesaria para integrar la aplicación móvil/web con el backend de BuscaYa.

---

## 🔧 Configuración Base

### URL Base de la API
```
http://localhost:5229/api  (Desarrollo)
```

### Headers Requeridos

**Para TODAS las peticiones:**
```javascript
{
  'Content-Type': 'application/json',
  'Accept': 'application/json'  // ⚠️ CRÍTICO: Sin esto recibirás HTML
}
```

**Para endpoints autenticados:**
```javascript
{
  'Authorization': 'Bearer {token}',
  'Content-Type': 'application/json',
  'Accept': 'application/json'
}
```

---

## 🔑 Autenticación JWT

### 1. Login
**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "nombreUsuario": "martin_busca",
  "contrasena": "wisofer17"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "usuario": {
    "id": 7,
    "nombreUsuario": "martin_busca",
    "nombreCompleto": "Martín Rodríguez",
    "rol": "Cliente",
    "email": "martin.rodriguez@email.com",
    "telefono": "50588886789",
    "tiendaId": null
  },
  "expiraEn": 60
}
```

**Errores:**
- `400` - Campos requeridos faltantes
- `401` - Usuario o contraseña incorrectos

**Ejemplo Flutter/Dart:**
```dart
final response = await http.post(
  Uri.parse('$baseUrl/auth/login'),
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
  body: jsonEncode({
    'nombreUsuario': 'martin_busca',
    'contrasena': 'wisofer17',
  }),
);

if (response.statusCode == 200) {
  final data = jsonDecode(response.body);
  final token = data['token'];
  final usuario = data['usuario'];
  // Guardar token en SharedPreferences o similar
}
```

### 2. Registro
**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "nombreUsuario": "nuevo_usuario",
  "contrasena": "password123",
  "nombreCompleto": "Juan Pérez",
  "telefono": "50512345678",
  "email": "juan@email.com"
}
```

**Response:** Igual que login (retorna token y usuario)

---

## 🔍 Búsqueda y Productos (Públicos - Sin autenticación)

### 1. Buscar Productos
**Endpoint:** `GET /api/public/buscar`

**Query Parameters:**
- `termino` (string, requerido) - Término de búsqueda
- `latitud` (decimal, opcional) - Latitud del usuario
- `longitud` (decimal, opcional) - Longitud del usuario
- `radioKm` (double, opcional, default: 5.0) - Radio de búsqueda en km
- `categoriaId` (int, opcional) - Filtrar por categoría
- `pagina` (int, opcional, default: 1) - Número de página
- `tamanoPagina` (int, opcional, default: 20) - Tamaño de página

**Ejemplo:**
```
GET /api/public/buscar?termino=cemento&latitud=11.1333&longitud=84.7833&pagina=1&tamanoPagina=10
```

**Response (200 OK):**
```json
{
  "productos": [
    {
      "id": 1,
      "nombre": "Cemento Portland",
      "descripcion": "Saco de 50kg",
      "precio": 250.00,
      "moneda": "C$",
      "fotoUrl": null,
      "tienda": {
        "id": 1,
        "nombre": "Ferretería El Constructor",
        "direccion": "Calle Principal, 2 cuadras al sur del parque central",
        "ciudad": "San Carlos",
        "whatsApp": "50588881234",
        "telefono": "50588881234",
        "logoUrl": null,
        "latitud": 11.1333,
        "longitud": 84.7833
      },
      "categoria": {
        "id": 1,
        "nombre": "Construcción",
        "icono": "🔨"
      },
      "distanciaKm": 0.5
    }
  ],
  "total": 1,
  "pagina": 1,
  "tamanoPagina": 10,
  "totalPaginas": 1
}
```

**Ejemplo Flutter:**
```dart
final response = await http.get(
  Uri.parse('$baseUrl/public/buscar').replace(queryParameters: {
    'termino': 'cemento',
    'latitud': '11.1333',
    'longitud': '84.7833',
    'pagina': '1',
    'tamanoPagina': '10',
  }),
  headers: {
    'Accept': 'application/json',
  },
);

if (response.statusCode == 200) {
  final data = jsonDecode(response.body);
  final productos = data['productos'] as List;
  // Mostrar productos en la UI
}
```

### 2. Obtener Detalle de Producto
**Endpoint:** `GET /api/public/producto/{id}`

**Query Parameters (opcionales):**
- `lat` (decimal) - Latitud para calcular distancia
- `lng` (decimal) - Longitud para calcular distancia

**Ejemplo:**
```
GET /api/public/producto/1?lat=11.1333&lng=84.7833
```

**Response (200 OK):**
```json
{
  "id": 1,
  "nombre": "Cemento Portland",
  "descripcion": "Saco de 50kg",
  "precio": 250.00,
  "moneda": "C$",
  "fotoUrl": null,
  "tienda": {
    "id": 1,
    "nombre": "Ferretería El Constructor",
    "direccion": "Calle Principal, 2 cuadras al sur del parque central",
    "ciudad": "San Carlos",
    "whatsApp": "50588881234",
    "telefono": "50588881234",
    "logoUrl": null,
    "latitud": 11.1333,
    "longitud": 84.7833
  },
  "categoria": {
    "id": 1,
    "nombre": "Construcción",
    "icono": "🔨"
  },
  "distanciaKm": 0.5
}
```

**Errores:**
- `404` - Producto no encontrado o inactivo

### 3. Obtener Categorías
**Endpoint:** `GET /api/public/categorias`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "nombre": "Construcción",
    "icono": "🔨",
    "activo": true,
    "orden": 1
  },
  {
    "id": 2,
    "nombre": "Farmacia",
    "icono": "💊",
    "activo": true,
    "orden": 2
  }
]
```

### 4. Obtener Sugerencias de Búsqueda
**Endpoint:** `GET /api/public/sugerencias`

**Query Parameters:**
- `termino` (string, requerido) - Término de búsqueda
- `limite` (int, opcional, default: 10) - Número máximo de sugerencias

**Ejemplo:**
```
GET /api/public/sugerencias?termino=zapa&limite=5
```

**Response (200 OK):**
```json
[
  "zapatos nike",
  "zapatos deportivos",
  "zapatos adidas"
]
```

---

## 👤 Endpoints de Cliente (Requieren autenticación JWT)

### 1. Ver Favoritos
**Endpoint:** `GET /api/cliente/favoritos`

**Headers:**
```
Authorization: Bearer {token}
Accept: application/json
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "tipo": "Tienda",
    "tienda": {
      "id": 1,
      "nombre": "Ferretería El Constructor",
      "direccion": "Calle Principal...",
      "ciudad": "San Carlos",
      "whatsApp": "50588881234"
    },
    "producto": null
  },
  {
    "id": 2,
    "tipo": "Producto",
    "tienda": null,
    "producto": {
      "id": 1,
      "nombre": "Cemento Portland",
      "precio": 250.00,
      "moneda": "C$"
    }
  }
]
```

### 2. Agregar Tienda a Favoritos
**Endpoint:** `POST /api/cliente/favoritos/tienda/{tiendaId}`

**Response (200 OK):**
```json
{
  "mensaje": "Tienda agregada a favoritos"
}
```

### 3. Agregar Producto a Favoritos
**Endpoint:** `POST /api/cliente/favoritos/producto/{productoId}`

**Response (200 OK):**
```json
{
  "mensaje": "Producto agregado a favoritos"
}
```

### 4. Eliminar Favorito
**Endpoint:** `DELETE /api/cliente/favoritos/{id}`

**Response (200 OK):**
```json
{
  "mensaje": "Favorito eliminado"
}
```

### 5. Ver Historial de Búsquedas
**Endpoint:** `GET /api/cliente/historial`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "termino": "cemento",
    "fechaBusqueda": "2026-01-27T10:30:00"
  }
]
```

### 6. Ver Direcciones Guardadas
**Endpoint:** `GET /api/cliente/direcciones`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "nombre": "Casa",
    "direccion": "Barrio Centro, Calle Principal",
    "latitud": 11.12345678,
    "longitud": -84.45678901,
    "esPrincipal": true
  }
]
```

### 7. Crear Dirección
**Endpoint:** `POST /api/cliente/direcciones`

**Request:**
```json
{
  "nombre": "Casa",
  "direccion": "Barrio Centro, Calle Principal",
  "latitud": 11.12345678,
  "longitud": -84.45678901,
  "esPrincipal": true
}
```

### 8. Crear Tienda (Convertir Cliente a TiendaOwner)
**Endpoint:** `POST /api/cliente/crear-tienda`

**Request:**
```json
{
  "nombreTienda": "La Esquinita",
  "descripcionTienda": "Ropa y calzado deportivo",
  "telefonoTienda": "50587654321",
  "whatsAppTienda": "50587654321",
  "emailTienda": "julio@email.com",
  "direccionTienda": "Calle Principal #45",
  "latitud": 11.125,
  "longitud": -84.458,
  "ciudad": "San Carlos",
  "departamento": "Río San Juan",
  "horarioApertura": "08:00:00",
  "horarioCierre": "18:00:00",
  "diasAtencion": "Lunes-Sábado",
  "logoTienda": "https://...",
  "fotoTienda": "https://..."
}
```

**Campos requeridos:**
- `nombreTienda`
- `whatsAppTienda`
- `direccionTienda`
- `ciudad`
- `departamento`
- `latitud`
- `longitud`

**Response (200 OK):**
```json
{
  "mensaje": "Tienda creada exitosamente",
  "usuario": {
    "id": 11,
    "nombreUsuario": "julio_tienda",
    "nombreCompleto": "Julio Ramírez",
    "rol": "TiendaOwner",
    "tiendaId": 3
  }
}
```

---

## 🏪 Endpoints de Tienda (Requieren autenticación JWT + rol TiendaOwner)

### 1. Ver Perfil de Tienda
**Endpoint:** `GET /api/tienda/perfil`

**Response (200 OK):**
```json
{
  "id": 1,
  "nombre": "Ferretería El Constructor",
  "descripcion": "Materiales de construcción",
  "telefono": "50588881234",
  "whatsApp": "50588881234",
  "email": "carlos@ferreteria.com",
  "direccion": "Calle Principal...",
  "latitud": 11.1333,
  "longitud": 84.7833,
  "ciudad": "San Carlos",
  "departamento": "Río San Juan",
  "horarioApertura": "08:00:00",
  "horarioCierre": "18:00:00",
  "diasAtencion": "Lunes-Sábado",
  "logoUrl": null,
  "fotoUrl": null,
  "plan": "Free",
  "productos": []
}
```

### 2. Actualizar Perfil de Tienda
**Endpoint:** `PUT /api/tienda/perfil`

**Request:** (Todos los campos son opcionales, solo envía los que quieres actualizar)
```json
{
  "nombre": "Nuevo Nombre",
  "descripcion": "Nueva descripción",
  "telefono": "50588881234",
  "whatsApp": "50588881234",
  "email": "nuevo@email.com",
  "direccion": "Nueva Dirección",
  "latitud": 11.125,
  "longitud": -84.458,
  "ciudad": "San Carlos",
  "departamento": "Río San Juan",
  "horarioApertura": "08:00:00",
  "horarioCierre": "18:00:00",
  "diasAtencion": "Lunes-Sábado",
  "logoTienda": "https://...",
  "fotoTienda": "https://..."
}
```

### 3. Listar Productos de Mi Tienda
**Endpoint:** `GET /api/tienda/productos`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "nombre": "Cemento Portland",
    "descripcion": "Saco de 50kg",
    "precio": 250.00,
    "moneda": "C$",
    "categoriaId": 1,
    "fotoUrl": null,
    "activo": true,
    "categoria": {
      "id": 1,
      "nombre": "Construcción",
      "icono": "🔨"
    }
  }
]
```

### 4. Crear Producto
**Endpoint:** `POST /api/tienda/productos`

**Request:**
```json
{
  "nombre": "Zapatos Nike Air Max",
  "descripcion": "Talla 42, color negro",
  "precio": 2500.00,
  "moneda": "C$",
  "categoriaId": 2,
  "fotoUrl": "https://..."
}
```

**Campos requeridos:**
- `nombre`
- `categoriaId`

**Response (201 Created):**
```json
{
  "id": 5,
  "nombre": "Zapatos Nike Air Max",
  "descripcion": "Talla 42, color negro",
  "precio": 2500.00,
  "moneda": "C$",
  "categoriaId": 2,
  "fotoUrl": "https://...",
  "activo": true
}
```

**Errores:**
- `400` - Límite de productos alcanzado (plan Free: 10 productos)
- `401` - No autenticado / No tienes tienda asociada

### 5. Ver Producto Específico
**Endpoint:** `GET /api/tienda/productos/{id}`

**Response (200 OK):**
```json
{
  "id": 5,
  "nombre": "Zapatos Nike Air Max",
  "descripcion": "Talla 42, color negro",
  "precio": 2500.00,
  "moneda": "C$",
  "categoriaId": 2,
  "fotoUrl": "https://...",
  "activo": true,
  "categoria": {
    "id": 2,
    "nombre": "Zapatos",
    "icono": "👟"
  }
}
```

### 6. Actualizar Producto
**Endpoint:** `PUT /api/tienda/productos/{id}`

**Request:** (Todos los campos son opcionales)
```json
{
  "nombre": "Zapatos Nike Air Max Actualizado",
  "descripcion": "Nueva descripción",
  "precio": 2300.00,
  "moneda": "C$",
  "categoriaId": 2,
  "fotoUrl": "https://...",
  "activo": true
}
```

**Response (200 OK):**
```json
{
  "mensaje": "Producto actualizado correctamente"
}
```

### 7. Eliminar Producto
**Endpoint:** `DELETE /api/tienda/productos/{id}`

**Response (200 OK):**
```json
{
  "mensaje": "Producto eliminado correctamente"
}
```

**Nota:** Hace "soft delete" (marca como inactivo, no lo borra físicamente)

### 8. Ver Estadísticas de la Tienda
**Endpoint:** `GET /api/tienda/estadisticas`

**Query Parameters:**
- `desde` (DateTime, opcional) - Fecha desde (default: 30 días atrás)
- `hasta` (DateTime, opcional) - Fecha hasta (default: hoy)

**Response (200 OK):**
```json
{
  "totalVistas": 150,
  "totalClicksWhatsApp": 45,
  "totalClicksLlamar": 12,
  "totalBusquedas": 89,
  "productosMasBuscados": [
    {
      "productoId": 1,
      "nombre": "Cemento Portland",
      "vecesBuscado": 25
    }
  ]
}
```

---

## ⚠️ Manejo de Errores

### Códigos de Estado HTTP

- `200 OK` - Petición exitosa
- `201 Created` - Recurso creado exitosamente
- `400 Bad Request` - Datos inválidos o faltantes
- `401 Unauthorized` - No autenticado o token inválido/expirado
- `403 Forbidden` - No tienes permisos (rol incorrecto)
- `404 Not Found` - Recurso no encontrado
- `500 Internal Server Error` - Error del servidor

### Formato de Error
```json
{
  "error": "Mensaje de error descriptivo",
  "mensaje": "Detalles adicionales (opcional)"
}
```

### Ejemplo de Manejo de Errores (Flutter)
```dart
try {
  final response = await http.get(
    Uri.parse('$baseUrl/tienda/productos'),
    headers: {
      'Authorization': 'Bearer $token',
      'Accept': 'application/json',
    },
  );

  if (response.statusCode == 200) {
    // Éxito
    final data = jsonDecode(response.body);
  } else if (response.statusCode == 401) {
    // Token expirado o inválido
    // Redirigir a login
  } else if (response.statusCode == 400) {
    // Error de validación
    final error = jsonDecode(response.body);
    print('Error: ${error['error']}');
  } else {
    // Otro error
    print('Error ${response.statusCode}');
  }
} catch (e) {
  // Error de red o parsing
  print('Error de conexión: $e');
}
```

---

## 🔐 Gestión del Token JWT

### Almacenamiento
- **Flutter:** Usar `shared_preferences` o `flutter_secure_storage`
- **React Native:** Usar `AsyncStorage` o `react-native-keychain`
- **Web:** Usar `localStorage` o `sessionStorage`

### Expiración
- El token expira en **60 minutos** por defecto
- Guardar también `expiraEn` del login para saber cuándo renovar
- Implementar renovación automática antes de que expire

### Ejemplo de Cliente HTTP con Interceptor (Flutter)
```dart
class ApiClient {
  final String baseUrl = 'http://localhost:5229/api';
  String? _token;

  Future<Map<String, String>> _getHeaders() async {
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (_token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }

    return headers;
  }

  Future<http.Response> get(String endpoint) async {
    return await http.get(
      Uri.parse('$baseUrl$endpoint'),
      headers: await _getHeaders(),
    );
  }

  Future<http.Response> post(String endpoint, Map<String, dynamic> body) async {
    return await http.post(
      Uri.parse('$baseUrl$endpoint'),
      headers: await _getHeaders(),
      body: jsonEncode(body),
    );
  }

  void setToken(String token) {
    _token = token;
  }
}
```

---

## 📝 Checklist de Implementación

### Configuración Inicial
- [ ] Configurar URL base de la API
- [ ] Implementar cliente HTTP con headers correctos
- [ ] Configurar manejo de errores global
- [ ] Implementar almacenamiento seguro de tokens

### Autenticación
- [ ] Pantalla de login
- [ ] Pantalla de registro
- [ ] Guardar token después de login/registro
- [ ] Implementar renovación de token
- [ ] Manejar logout y limpiar token

### Búsqueda y Productos
- [ ] Pantalla de búsqueda
- [ ] Lista de resultados de búsqueda
- [ ] Pantalla de detalle de producto
- [ ] Lista de categorías
- [ ] Sugerencias de búsqueda (autocompletado)

### Funcionalidades de Cliente
- [ ] Ver favoritos
- [ ] Agregar/eliminar favoritos
- [ ] Historial de búsquedas
- [ ] Direcciones guardadas
- [ ] Crear tienda desde perfil

### Funcionalidades de Tienda
- [ ] Ver perfil de tienda
- [ ] Editar perfil de tienda
- [ ] Listar productos de mi tienda
- [ ] Crear producto
- [ ] Editar producto
- [ ] Eliminar producto
- [ ] Ver estadísticas

---

## 🧪 Usuarios de Prueba

### Cliente
- **Usuario:** `martin_busca`
- **Contraseña:** `wisofer17`
- **Rol:** Cliente

### Tienda Owner
- **Usuario:** `ferreteria_constructor`
- **Contraseña:** `wisofer17`
- **Rol:** TiendaOwner
- **Tienda:** Ferretería El Constructor

### Otros Usuarios Disponibles
- `farmacia_sancarlos` / `wisofer17` (TiendaOwner)
- `super_ahorro` / `wisofer17` (TiendaOwner)
- `ferreteria_esquina` / `wisofer17` (TiendaOwner)
- `moda_joven` / `wisofer17` (TiendaOwner)
- `laura_cliente` / `wisofer17` (Cliente)
- `pedro_cliente` / `wisofer17` (Cliente)

---

## 🚨 Errores Comunes y Soluciones

### Error: Recibo HTML en lugar de JSON
**Causa:** Falta el header `Accept: application/json`  
**Solución:** Agregar `'Accept': 'application/json'` a todos los headers

### Error: 401 Unauthorized
**Causa:** Token expirado o no incluido  
**Solución:** Verificar que el token esté en el header `Authorization: Bearer {token}`

### Error: 404 Not Found
**Causa:** URL incorrecta o recurso no existe  
**Solución:** Verificar que la URL incluya `/api/` al inicio

### Error: Referencia circular al parsear JSON
**Causa:** El backend ya está configurado para ignorar ciclos  
**Solución:** Si persiste, verificar que estés usando `Accept: application/json`

---

## 📞 Soporte

Si encuentras algún problema o tienes dudas sobre la integración:
1. Verifica que el servidor esté corriendo
2. Revisa los logs del servidor
3. Verifica que los headers estén correctos
4. Prueba los endpoints con Postman o curl primero

---

**Última actualización:** 27 de Enero, 2026
