# 📸 Sistema de Gestión de Imágenes - BuscaYa

## ✅ Implementación Completada

El sistema de gestión de imágenes está completamente implementado y listo para usar.

---

## 🚀 Endpoints Disponibles

Todos los endpoints requieren autenticación JWT y están bajo:
```
/api/s3
```

### 1. Subir Imagen a WebP (Recomendado)
```
POST /api/s3/image/webp
```
**Form Data:**
- `prefix` (string, requerido): Carpeta destino (ej: "productos/", "tiendas/logos/")
- `image` (file, requerido): Archivo de imagen
- `previousImageUrl` (string, opcional): URL de imagen anterior a eliminar

**Ejemplo Flutter:**
```dart
var formData = FormData.fromMap({
  'prefix': 'productos/',
  'image': await MultipartFile.fromFile(imagePath),
  'previousImageUrl': oldImageUrl, // opcional
});

var response = await dio.post(
  '/api/s3/image/webp',
  data: formData,
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);
```

**Respuesta:**
```json
{
  "url": "https://3ffdf4cdffc5a63e50e11e6b844ce0d2.r2.cloudflarestorage.com/buscaya-images/productos/abc123.webp"
}
```

---

### 2. Subir Imagen a JPEG
```
POST /api/s3/image/jpg
```
Mismo formato que WebP, pero convierte a JPEG con calidad 80.

---

### 3. Subir Imagen Sin Conversión
```
POST /api/s3/image
```
Mantiene el formato original (JPG, PNG, GIF, WebP).

---

### 4. Subir desde Base64
```
POST /api/s3/image/base64
```
**Body (JSON):**
```json
{
  "prefix": "productos/",
  "imageBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "previousImageUrl": null
}
```

---

### 5. Generar Ícono Cuadrado
```
POST /api/s3/icon
```
**Form Data:**
- `prefix` (string, requerido)
- `image` (file, requerido)
- `previousImageUrl` (string, opcional)
- `size` (int, opcional, default: 200): Tamaño en píxeles (32-512)

Genera un ícono cuadrado optimizado en WebP.

---

### 6. Eliminar Imagen
```
DELETE /api/s3/image?url={imageUrl}
```

---

### 7. Listar Carpetas
```
GET /api/s3/folders?prefix=
```

---

### 8. Listar Archivos
```
GET /api/s3/files?prefix=&recursive=false
```

---

## 📁 Carpetas Predefinidas (SD.cs)

```csharp
SD.Folder_Productos          // "productos/"
SD.Folder_Tiendas            // "tiendas/"
SD.Folder_Tiendas_Logos      // "tiendas/logos/"
SD.Folder_Tiendas_Fotos      // "tiendas/fotos/"
SD.Folder_Perfiles           // "perfiles/"
SD.Folder_Categorias         // "categorias/"
```

---

## 💡 Ejemplos de Uso

### Subir Foto de Producto
```csharp
var imageUrl = await _s3Service.UploadImageToWebPAsync(
    SD.Folder_Productos,
    imageFile,
    producto.FotoUrl  // Elimina la anterior automáticamente
);
producto.FotoUrl = imageUrl;
```

### Subir Logo de Tienda
```csharp
var logoUrl = await _s3Service.UploadImageToWebPAsync(
    SD.Folder_Tiendas_Logos,
    logoFile,
    tienda.LogoUrl
);
tienda.LogoUrl = logoUrl;
```

### Generar Ícono de Usuario
```csharp
var iconUrl = await _s3Service.UploadIconAsync(
    SD.Folder_Perfiles,
    profileImage,
    usuario.FotoPerfil,
    size: 64
);
usuario.FotoPerfil = iconUrl;
```

---

## ⚙️ Configuración

Las credenciales están en `appsettings.json`:
```json
{
  "R2": {
    "AccountId": "3ffdf4cdffc5a63e50e11e6b844ce0d2",
    "AccessKey": "...",
    "SecretKey": "...",
    "BucketName": "buscaya-images"
  }
}
```

---

## ✅ Características Implementadas

- ✅ Conversión automática a WebP (ahorro 30-40%)
- ✅ Conversión a JPEG con calidad 80
- ✅ Generación de íconos cuadrados
- ✅ Validación de formatos (JPG, PNG, GIF, WebP)
- ✅ Eliminación automática de imágenes anteriores
- ✅ Soporte Base64 para apps móviles
- ✅ URLs públicas directas
- ✅ Sin costos de transferencia (R2)

---

## 🎯 Próximos Pasos

1. **Probar los endpoints** con Postman o similar
2. **Integrar en los controladores** existentes (ProductoController, TiendaController)
3. **Actualizar DTOs** si es necesario para incluir URLs de imágenes

---

**¡Sistema listo para usar!** 🚀
