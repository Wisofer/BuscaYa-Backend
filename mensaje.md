Quiero que edites mi archivo S3BucketService.cs y lo dejes listo para que el backend devuelva URLs públicas usando el dominio r2.dev en vez del endpoint S3 privado.

Contexto: Ya activé “Public Development URL” en Cloudflare R2 y mi base pública es:
https://pub-94253f5fce9049b0accf419de3178334.r2.dev

Objetivo:

Inicializar una variable _publicBaseUrl en el constructor, leyendo primero R2:PublicBaseUrl desde appsettings.json y usando como fallback el valor anterior.

Cambiar TODOS los retornos y logs de URL que hoy usan https://{_baseUrl}/{_bucketName}/{filePath} para que usen "{_publicBaseUrl}/{filePath}".

Actualizar DeleteFileIfExistsAsync para que detecte y elimine archivos tanto si la URL guardada es privada (S3 endpoint) como si es pública (r2.dev), extrayendo correctamente el key en ambos casos.

Mantener el resto del código igual.

Cambios exactos a aplicar:
A) Campos

Asegúrate de tener:

private readonly string _publicBaseUrl;

B) Constructor

Después de setear _baseUrl, agrega:

_publicBaseUrl = r2Settings["PublicBaseUrl"] 
    ?? "https://pub-94253f5fce9049b0accf419de3178334.r2.dev";

C) URLs públicas (todos los métodos)

Donde sea que construyo o retorno:

$"https://{_baseUrl}/{_bucketName}/{filePath}"


reemplázalo por:

$"{_publicBaseUrl}/{filePath}"


Esto aplica en:

UploadImageToWebPAsync (incluyendo el Console.WriteLine)

UploadImageToJpgAsync

UploadImageAsync

UploadImageFromBase64ToJpgAsync

UploadIconAsync

D) DeleteFileIfExistsAsync (reemplazar el método completo)

Reemplaza TODO el método por este:

public async Task DeleteFileIfExistsAsync(string? fileUrl)
{
    if (string.IsNullOrWhiteSpace(fileUrl)) return;

    // 1) URL privada S3
    var privatePrefix = $"https://{_baseUrl}/{_bucketName}/";

    // 2) URL pública r2.dev
    var publicPrefix = _publicBaseUrl.EndsWith("/")
        ? _publicBaseUrl
        : _publicBaseUrl + "/";

    string? key = null;

    if (fileUrl.StartsWith(privatePrefix, StringComparison.OrdinalIgnoreCase))
        key = fileUrl.Substring(privatePrefix.Length);
    else if (fileUrl.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
        key = fileUrl.Substring(publicPrefix.Length);

    if (string.IsNullOrWhiteSpace(key)) return;

    try
    {
        await _s3Client.DeleteObjectAsync(_bucketName, key);
        await PurgeCloudflareCacheAsync(fileUrl);
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // ok
    }
}

E) appsettings.json (sugerencia)

Si existe appsettings.json, agrega dentro de "R2":

"PublicBaseUrl": "https://pub-94253f5fce9049b0accf419de3178334.r2.dev"


Resultado esperado:
Cuando se suba una imagen, el backend debe devolver y guardar una URL tipo:
https://pub-94253f5fce9049b0accf419de3178334.r2.dev/productos/xxxx.webp

No cambies la lógica de subida, solo la construcción de URL y el delete.