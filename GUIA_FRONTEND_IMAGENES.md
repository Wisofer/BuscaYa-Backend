# 📸 Guía Frontend - Sistema de Imágenes BuscaYa

> **Documento completo para desarrolladores frontend**  
> Esta guía explica cómo integrar el sistema de subida de imágenes en la aplicación móvil/web.

---

## 🔑 Requisitos Previos

- ✅ Autenticación JWT (token válido)
- ✅ Header `Authorization: Bearer {token}` en todas las peticiones
- ✅ Header `Accept: application/json`

---

## 📡 Endpoints Disponibles

**Base URL:** `https://tu-dominio.com/api/s3`

Todos los endpoints requieren autenticación JWT.

---

## 🚀 1. Subir Imagen a WebP (Recomendado)

### Endpoint
```
POST /api/s3/image/webp
```

### Descripción
Convierte automáticamente cualquier imagen (JPG, PNG, GIF) a WebP optimizado. **Recomendado para todas las imágenes nuevas.**

### Content-Type
```
multipart/form-data
```

### Parámetros

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `prefix` | string | ✅ Sí | Carpeta destino (ej: "productos/", "tiendas/logos/") |
| `image` | file | ✅ Sí | Archivo de imagen (JPG, PNG, GIF, WebP) |
| `previousImageUrl` | string | ❌ No | URL de imagen anterior a eliminar |

### Ejemplo Flutter/Dart

```dart
import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';

Future<String?> uploadImageToWebP({
  required XFile imageFile,
  required String prefix,
  String? previousImageUrl,
  required String token,
}) async {
  try {
    final dio = Dio();
    
    // Crear FormData
    final formData = FormData.fromMap({
      'prefix': prefix,
      'image': await MultipartFile.fromFile(
        imageFile.path,
        filename: imageFile.name,
      ),
      if (previousImageUrl != null) 'previousImageUrl': previousImageUrl,
    });

    // Hacer petición
    final response = await dio.post(
      'https://tu-dominio.com/api/s3/image/webp',
      data: formData,
      options: Options(
        headers: {
          'Authorization': 'Bearer $token',
          'Accept': 'application/json',
        },
      ),
    );

    if (response.statusCode == 200) {
      return response.data['url'] as String;
    }
    
    return null;
  } catch (e) {
    print('Error al subir imagen: $e');
    return null;
  }
}

// Uso:
final imageUrl = await uploadImageToWebP(
  imageFile: pickedImage,
  prefix: 'productos/',
  previousImageUrl: productoActual.fotoUrl,
  token: userToken,
);

if (imageUrl != null) {
  productoActual.fotoUrl = imageUrl;
  // Actualizar en backend
}
```

### Ejemplo JavaScript/TypeScript (React/React Native)

```typescript
async function uploadImageToWebP(
  file: File,
  prefix: string,
  token: string,
  previousImageUrl?: string
): Promise<string | null> {
  try {
    const formData = new FormData();
    formData.append('prefix', prefix);
    formData.append('image', file);
    if (previousImageUrl) {
      formData.append('previousImageUrl', previousImageUrl);
    }

    const response = await fetch('https://tu-dominio.com/api/s3/image/webp', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json',
      },
      body: formData,
    });

    if (response.ok) {
      const data = await response.json();
      return data.url;
    }

    return null;
  } catch (error) {
    console.error('Error al subir imagen:', error);
    return null;
  }
}

// Uso:
const imageUrl = await uploadImageToWebP(
  selectedFile,
  'productos/',
  token,
  currentProduct.fotoUrl
);
```

### Respuesta Exitosa (200 OK)
```json
{
  "url": "https://3ffdf4cdffc5a63e50e11e6b844ce0d2.r2.cloudflarestorage.com/buscaya-images/productos/abc123-def456.webp"
}
```

### Errores Posibles

| Código | Error | Solución |
|--------|-------|----------|
| 400 | "Prefix es requerido" | Verificar que `prefix` no esté vacío |
| 400 | "Imagen es requerida" | Verificar que el archivo se esté enviando |
| 400 | "Formato de imagen no válido" | Solo acepta JPG, PNG, GIF, WebP |
| 401 | Unauthorized | Token JWT inválido o expirado |
| 400 | "No se pudo subir la imagen" | Error del servidor, verificar logs |

---

## 🖼️ 2. Subir Imagen a JPEG

### Endpoint
```
POST /api/s3/image/jpg
```

### Descripción
Convierte la imagen a JPEG con calidad 80. Útil cuando necesitas compatibilidad máxima.

### Uso
Igual que WebP, solo cambia el endpoint:

```dart
// Flutter
final response = await dio.post(
  'https://tu-dominio.com/api/s3/image/jpg',
  data: formData,
  // ... resto igual
);
```

```typescript
// JavaScript
const response = await fetch('https://tu-dominio.com/api/s3/image/jpg', {
  // ... resto igual
});
```

---

## 📤 3. Subir Imagen Sin Conversión

### Endpoint
```
POST /api/s3/image
```

### Descripción
Mantiene el formato original (JPG, PNG, GIF, WebP) sin conversión.

### Uso
Mismo formato que los anteriores, solo cambia el endpoint.

---

## 📱 4. Subir desde Base64 (Apps Móviles)

### Endpoint
```
POST /api/s3/image/base64
```

### Content-Type
```
application/json
```

### Body (JSON)
```json
{
  "prefix": "productos/",
  "imageBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "previousImageUrl": null
}
```

### Ejemplo Flutter/Dart

```dart
Future<String?> uploadImageFromBase64({
  required String base64String,
  required String prefix,
  String? previousImageUrl,
  required String token,
}) async {
  try {
    final dio = Dio();
    
    final response = await dio.post(
      'https://tu-dominio.com/api/s3/image/base64',
      data: {
        'prefix': prefix,
        'imageBase64': base64String,
        if (previousImageUrl != null) 'previousImageUrl': previousImageUrl,
      },
      options: Options(
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    if (response.statusCode == 200) {
      return response.data['url'] as String;
    }
    
    return null;
  } catch (e) {
    print('Error al subir imagen: $e');
    return null;
  }
}

// Convertir imagen a Base64 antes de subir
final bytes = await imageFile.readAsBytes();
final base64String = base64Encode(bytes);
final dataUri = 'data:image/jpeg;base64,$base64String';

final imageUrl = await uploadImageFromBase64(
  base64String: dataUri,
  prefix: 'productos/',
  token: userToken,
);
```

### Ejemplo JavaScript

```typescript
async function uploadImageFromBase64(
  base64String: string,
  prefix: string,
  token: string,
  previousImageUrl?: string
): Promise<string | null> {
  try {
    const response = await fetch('https://tu-dominio.com/api/s3/image/base64', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      body: JSON.stringify({
        prefix,
        imageBase64: base64String,
        previousImageUrl,
      }),
    });

    if (response.ok) {
      const data = await response.json();
      return data.url;
    }

    return null;
  } catch (error) {
    console.error('Error al subir imagen:', error);
    return null;
  }
}
```

---

## 🎯 5. Generar Ícono Cuadrado

### Endpoint
```
POST /api/s3/icon
```

### Descripción
Genera un ícono cuadrado optimizado en WebP. Ideal para avatares, logos pequeños, etc.

### Parámetros

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `prefix` | string | ✅ Sí | Carpeta destino |
| `image` | file | ✅ Sí | Archivo de imagen |
| `previousImageUrl` | string | ❌ No | URL de imagen anterior |
| `size` | int | ❌ No | Tamaño en píxeles (32-512, default: 200) |

### Ejemplo Flutter

```dart
Future<String?> uploadIcon({
  required XFile imageFile,
  required String prefix,
  String? previousImageUrl,
  int size = 200,
  required String token,
}) async {
  try {
    final dio = Dio();
    
    final formData = FormData.fromMap({
      'prefix': prefix,
      'image': await MultipartFile.fromFile(imageFile.path),
      'size': size.toString(),
      if (previousImageUrl != null) 'previousImageUrl': previousImageUrl,
    });

    final response = await dio.post(
      'https://tu-dominio.com/api/s3/icon',
      data: formData,
      options: Options(
        headers: {
          'Authorization': 'Bearer $token',
          'Accept': 'application/json',
        },
      ),
    );

    if (response.statusCode == 200) {
      return response.data['url'] as String;
    }
    
    return null;
  } catch (e) {
    print('Error al generar ícono: $e');
    return null;
  }
}

// Uso: Generar ícono de 64x64 píxeles
final iconUrl = await uploadIcon(
  imageFile: profileImage,
  prefix: 'perfiles/',
  size: 64,
  token: userToken,
);
```

---

## 🗑️ 6. Eliminar Imagen

### Endpoint
```
DELETE /api/s3/image?url={imageUrl}
```

### Ejemplo Flutter

```dart
Future<bool> deleteImage(String imageUrl, String token) async {
  try {
    final dio = Dio();
    
    final response = await dio.delete(
      'https://tu-dominio.com/api/s3/image',
      queryParameters: {'url': imageUrl},
      options: Options(
        headers: {
          'Authorization': 'Bearer $token',
          'Accept': 'application/json',
        },
      ),
    );

    return response.statusCode == 200;
  } catch (e) {
    print('Error al eliminar imagen: $e');
    return false;
  }
}
```

---

## 📁 Carpetas Predefinidas

Usa estos prefijos para organizar las imágenes:

| Prefijo | Uso |
|---------|-----|
| `productos/` | Imágenes de productos |
| `tiendas/logos/` | Logos de tiendas |
| `tiendas/fotos/` | Fotos de tiendas |
| `perfiles/` | Fotos de perfil de usuarios |
| `categorias/` | Íconos de categorías |

---

## 💡 Casos de Uso Comunes

### 1. Subir Foto de Producto

```dart
// 1. Seleccionar imagen
final picker = ImagePicker();
final pickedFile = await picker.pickImage(source: ImageSource.gallery);

if (pickedFile != null) {
  // 2. Subir imagen
  final imageUrl = await uploadImageToWebP(
    imageFile: XFile(pickedFile.path),
    prefix: 'productos/',
    previousImageUrl: producto.fotoUrl, // Elimina la anterior
    token: userToken,
  );

  // 3. Actualizar producto en backend
  if (imageUrl != null) {
    await actualizarProducto(productoId, {'fotoUrl': imageUrl});
  }
}
```

### 2. Subir Logo de Tienda

```dart
final logoUrl = await uploadImageToWebP(
  imageFile: logoFile,
  prefix: 'tiendas/logos/',
  previousImageUrl: tienda.logoUrl,
  token: userToken,
);

if (logoUrl != null) {
  await actualizarTienda(tiendaId, {'logoUrl': logoUrl});
}
```

### 3. Actualizar Foto de Perfil

```dart
// Subir imagen completa
final fotoUrl = await uploadImageToWebP(
  imageFile: fotoFile,
  prefix: 'perfiles/',
  previousImageUrl: usuario.fotoPerfil,
  token: userToken,
);

// Generar ícono pequeño
final iconUrl = await uploadIcon(
  imageFile: fotoFile,
  prefix: 'perfiles/',
  size: 64,
  previousImageUrl: usuario.iconoPerfil,
  token: userToken,
);

if (fotoUrl != null && iconUrl != null) {
  await actualizarPerfil({
    'fotoPerfil': fotoUrl,
    'iconoPerfil': iconUrl,
  });
}
```

---

## ⚠️ Manejo de Errores

### Ejemplo Completo con Manejo de Errores

```dart
Future<String?> uploadImageWithErrorHandling({
  required XFile imageFile,
  required String prefix,
  String? previousImageUrl,
  required String token,
}) async {
  try {
    // Validar tamaño (opcional, máximo 10MB)
    final fileSize = await imageFile.length();
    if (fileSize > 10 * 1024 * 1024) {
      throw Exception('La imagen es demasiado grande (máximo 10MB)');
    }

    // Validar formato
    final extension = imageFile.path.split('.').last.toLowerCase();
    final validExtensions = ['jpg', 'jpeg', 'png', 'gif', 'webp'];
    if (!validExtensions.contains(extension)) {
      throw Exception('Formato no válido. Use JPG, PNG, GIF o WebP');
    }

    // Subir imagen
    final dio = Dio();
    final formData = FormData.fromMap({
      'prefix': prefix,
      'image': await MultipartFile.fromFile(imageFile.path),
      if (previousImageUrl != null) 'previousImageUrl': previousImageUrl,
    });

    final response = await dio.post(
      'https://tu-dominio.com/api/s3/image/webp',
      data: formData,
      options: Options(
        headers: {
          'Authorization': 'Bearer $token',
          'Accept': 'application/json',
        },
      ),
    );

    if (response.statusCode == 200) {
      return response.data['url'] as String;
    } else {
      final error = response.data['error'] ?? 'Error desconocido';
      throw Exception(error);
    }
  } on DioException catch (e) {
    if (e.response != null) {
      final error = e.response?.data['error'] ?? 'Error al subir imagen';
      throw Exception(error);
    } else {
      throw Exception('Error de conexión. Verifique su internet');
    }
  } catch (e) {
    throw Exception('Error: ${e.toString()}');
  }
}

// Uso con try-catch
try {
  final imageUrl = await uploadImageWithErrorHandling(
    imageFile: pickedImage,
    prefix: 'productos/',
    token: userToken,
  );
  
  // Mostrar éxito
  showSuccessMessage('Imagen subida correctamente');
  
  // Actualizar UI
  producto.fotoUrl = imageUrl;
} catch (e) {
  // Mostrar error al usuario
  showErrorMessage(e.toString());
}
```

---

## 🎨 Mejores Prácticas

### 1. Siempre Usar WebP para Imágenes Nuevas

✅ **Recomendado:**
```dart
await uploadImageToWebP(...); // Ahorra 30-40% de espacio
```

❌ **Evitar (a menos que sea necesario):**
```dart
await uploadImage(...); // Mantiene formato original, más pesado
```

### 2. Eliminar Imágenes Anteriores

✅ **Recomendado:**
```dart
await uploadImageToWebP(
  prefix: 'productos/',
  imageFile: newImage,
  previousImageUrl: producto.fotoUrl, // Elimina la anterior
  token: token,
);
```

### 3. Mostrar Indicador de Carga

```dart
bool isUploading = false;

Future<void> uploadProductImage() async {
  setState(() => isUploading = true);
  
  try {
    final url = await uploadImageToWebP(...);
    // Actualizar producto
  } finally {
    setState(() => isUploading = false);
  }
}

// En el UI
if (isUploading) {
  return CircularProgressIndicator();
}
```

### 4. Validar Antes de Subir

```dart
// Validar tamaño
if (fileSize > 10 * 1024 * 1024) {
  showError('La imagen es demasiado grande');
  return;
}

// Validar formato
if (!validExtensions.contains(extension)) {
  showError('Formato no válido');
  return;
}
```

### 5. Usar Prefijos Organizados

✅ **Recomendado:**
```dart
'productos/'      // Para productos
'tiendas/logos/' // Para logos
'perfiles/'       // Para perfiles
```

❌ **Evitar:**
```dart
''                // Sin prefijo
'images/'         // Muy genérico
```

---

## 📝 Checklist de Implementación

- [ ] Instalar dependencias (dio, image_picker, etc.)
- [ ] Crear función helper para subir imágenes
- [ ] Implementar selección de imagen (galería/cámara)
- [ ] Agregar indicador de carga
- [ ] Manejar errores y mostrar mensajes al usuario
- [ ] Validar tamaño y formato antes de subir
- [ ] Actualizar UI después de subir exitosamente
- [ ] Probar con diferentes formatos (JPG, PNG, GIF)
- [ ] Probar con imágenes grandes y pequeñas

---

## 🔗 Integración con Endpoints Existentes

### Actualizar Producto con Imagen

```dart
// 1. Subir imagen
final imageUrl = await uploadImageToWebP(
  imageFile: imageFile,
  prefix: 'productos/',
  previousImageUrl: producto.fotoUrl,
  token: token,
);

// 2. Actualizar producto en backend
if (imageUrl != null) {
  await http.put(
    Uri.parse('https://tu-dominio.com/api/tienda/productos/$productoId'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'fotoUrl': imageUrl,
    }),
  );
}
```

### Crear Producto con Imagen

```dart
// 1. Subir imagen primero
final imageUrl = await uploadImageToWebP(
  imageFile: imageFile,
  prefix: 'productos/',
  token: token,
);

// 2. Crear producto con la URL
if (imageUrl != null) {
  await http.post(
    Uri.parse('https://tu-dominio.com/api/tienda/productos'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'nombre': 'Producto Nuevo',
      'precio': 100.0,
      'fotoUrl': imageUrl, // URL de la imagen subida
      // ... otros campos
    }),
  );
}
```

---

## 🚨 Troubleshooting

### Error: "Formato de imagen no válido"
**Causa:** El archivo no es JPG, PNG, GIF o WebP  
**Solución:** Convertir la imagen a uno de estos formatos antes de subir

### Error: "No se pudo subir la imagen"
**Causa:** Error del servidor o imagen corrupta  
**Solución:** Verificar que la imagen no esté corrupta, intentar con otra imagen

### Error: 401 Unauthorized
**Causa:** Token JWT inválido o expirado  
**Solución:** Renovar el token haciendo login nuevamente

### La imagen no se muestra después de subir
**Causa:** URL incorrecta o problema de cache  
**Solución:** Verificar que la URL sea correcta, esperar unos segundos para que el cache se actualice

---

## 📞 Soporte

Si encuentras problemas:
1. Verifica que el token JWT sea válido
2. Verifica que el formato de imagen sea correcto
3. Verifica que el tamaño no exceda 10MB
4. Revisa los logs del servidor para más detalles

---

**¡Listo para implementar!** 🚀
