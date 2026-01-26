# 🚀 Guía de Despliegue - BuscaYa Backend

## 📦 Construcción de la Imagen Docker

### Construir la imagen:
```bash
docker build -t buscaya-backend:latest .
```

### Construir con tag específico:
```bash
docker build -t buscaya-backend:v1.0.0 .
```

---

## 🐳 Ejecutar el Contenedor

### Ejecutar localmente (desarrollo):
```bash
docker run -d \
  --name buscaya-backend \
  -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=buscaya;Username=postgres;Password=wiso;" \
  buscaya-backend:latest
```

### Ejecutar en producción:
```bash
docker run -d \
  --name buscaya-backend \
  -p 5000:5000 \
  --restart unless-stopped \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=tu-host;Database=tu-db;Username=tu-user;Password=tu-password;SSL Mode=Require;" \
  -e JwtSettings__SecretKey="TU_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_AQUI" \
  -e JwtSettings__Issuer="BuscaYa" \
  -e JwtSettings__Audience="BuscaYaUsers" \
  -e JwtSettings__ExpirationInMinutes="60" \
  buscaya-backend:latest
```

---

## 🔐 Variables de Entorno Requeridas

### Obligatorias:

1. **ConnectionStrings__DefaultConnection**
   - Cadena de conexión a PostgreSQL
   - Ejemplo: `Host=host;Database=db;Username=user;Password=pass;SSL Mode=Require;`

2. **JwtSettings__SecretKey**
   - Clave secreta para firmar tokens JWT
   - **IMPORTANTE:** Debe ser una cadena larga y segura (mínimo 32 caracteres)
   - Ejemplo: `"EstaEsUnaClaveSecretaMuyLargaParaJWT2024BuscaYaSystem"`

### Opcionales (tienen valores por defecto):

3. **JwtSettings__Issuer**
   - Emisor del token (default: "BuscaYa")

4. **JwtSettings__Audience**
   - Audiencia del token (default: "BuscaYaUsers")

5. **JwtSettings__ExpirationInMinutes**
   - Tiempo de expiración del token en minutos (default: 60)

6. **ASPNETCORE_ENVIRONMENT**
   - Ambiente de ejecución: `Development`, `Staging`, `Production`
   - (default: `Production` en Dockerfile)

---

## ☁️ Despliegue en Servicios Cloud

### Docker Hub

1. **Hacer login:**
```bash
docker login
```

2. **Tag de la imagen:**
```bash
docker tag buscaya-backend:latest tu-usuario/buscaya-backend:latest
```

3. **Push a Docker Hub:**
```bash
docker push tu-usuario/buscaya-backend:latest
```

### AWS ECS / Fargate

1. Subir imagen a ECR (Elastic Container Registry)
2. Crear task definition con variables de entorno
3. Configurar service con health check

### Azure Container Instances

1. Subir imagen a Azure Container Registry
2. Crear container instance con variables de entorno
3. Configurar puerto público

### Google Cloud Run

1. Subir imagen a Google Container Registry
2. Desplegar con Cloud Run
3. Configurar variables de entorno en la consola

### DigitalOcean App Platform

1. Conectar repositorio Git
2. Configurar Dockerfile como buildpack
3. Agregar variables de entorno en la consola

---

## 🔍 Verificación del Despliegue

### Health Check
El contenedor incluye un health check que verifica:
```
GET /api/public/categorias
```

### Verificar manualmente:
```bash
curl http://localhost:5000/api/public/categorias
```

### Ver logs del contenedor:
```bash
docker logs buscaya-backend
```

### Ver logs en tiempo real:
```bash
docker logs -f buscaya-backend
```

---

## 📊 Monitoreo

### Métricas importantes:
- **Health check status** - Debe estar "healthy"
- **Response time** - Tiempo de respuesta de la API
- **Error rate** - Tasa de errores HTTP
- **Database connections** - Conexiones activas a PostgreSQL

### Endpoints útiles para monitoreo:
- `GET /api/public/categorias` - Verifica que la API responde
- `GET /api/public/buscar?termino=test` - Verifica búsqueda básica

---

## 🔄 Actualización del Contenedor

### Proceso de actualización:

1. **Detener contenedor actual:**
```bash
docker stop buscaya-backend
docker rm buscaya-backend
```

2. **Construir nueva imagen:**
```bash
docker build -t buscaya-backend:latest .
```

3. **Ejecutar nuevo contenedor:**
```bash
docker run -d \
  --name buscaya-backend \
  -p 5000:5000 \
  --restart unless-stopped \
  [variables de entorno...] \
  buscaya-backend:latest
```

### O usar docker-compose (recomendado):

```yaml
version: '3.8'
services:
  buscaya-backend:
    image: buscaya-backend:latest
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - JwtSettings__SecretKey=${JWT_SECRET_KEY}
    restart: unless-stopped
```

Luego:
```bash
docker-compose up -d
```

---

## 🛡️ Seguridad en Producción

### ✅ Checklist de Seguridad:

- [ ] **JWT SecretKey** - Usar una clave segura y única (mínimo 32 caracteres)
- [ ] **Connection String** - No exponer en código, usar variables de entorno
- [ ] **HTTPS** - Configurar reverse proxy (nginx, traefik) con SSL
- [ ] **CORS** - Actualizar `Program.cs` para permitir solo dominios específicos
- [ ] **Firewall** - Solo exponer puerto 5000 internamente, usar reverse proxy
- [ ] **Logs** - No loguear información sensible (passwords, tokens)
- [ ] **Updates** - Mantener .NET y dependencias actualizadas

### Configurar CORS para producción:

En `Program.cs`, cambiar:
```csharp
policy.WithOrigins("*") // ❌ No usar en producción
```

Por:
```csharp
policy.WithOrigins("https://app.buscaya.com", "https://www.buscaya.com") // ✅ Específico
```

---

## 🗄️ Base de Datos

### Migraciones:

Las migraciones se ejecutan automáticamente al iniciar la aplicación si está configurado en `Program.cs`.

### Ejecutar migraciones manualmente:

```bash
# Dentro del contenedor
docker exec -it buscaya-backend bash
dotnet ef database update
```

O desde fuera:
```bash
docker exec buscaya-backend dotnet BuscaYa.dll --migrate
```

---

## 📝 Notas Importantes

1. **Puerto:** El contenedor expone el puerto 5000 internamente. En producción, usar un reverse proxy (nginx, traefik) que escuche en puerto 80/443.

2. **Volúmenes:** El contenedor crea un volumen para DataProtection keys en `/app/dp-keys`. Esto permite persistir las claves entre reinicios.

3. **Health Check:** El health check verifica cada 30 segundos. Si falla 3 veces consecutivas, el contenedor se marca como "unhealthy".

4. **Timezone:** El contenedor está configurado para zona horaria de Nicaragua (America/Managua).

5. **Logs:** Los logs se envían a stdout/stderr y pueden ser capturados por el sistema de logging del host o servicio cloud.

---

## 🆘 Troubleshooting

### El contenedor no inicia:
```bash
docker logs buscaya-backend
```
Verificar:
- Variables de entorno correctas
- Connection string válida
- Puerto 5000 disponible

### Health check falla:
```bash
docker exec buscaya-backend wget --spider http://localhost:5000/api/public/categorias
```
Verificar:
- La aplicación está corriendo
- El endpoint responde correctamente

### Error de conexión a base de datos:
- Verificar connection string
- Verificar que PostgreSQL esté accesible desde el contenedor
- Verificar firewall/security groups

---

## ✅ Checklist Pre-Despliegue

- [ ] Dockerfile construido y probado localmente
- [ ] Variables de entorno configuradas
- [ ] Connection string de producción configurada
- [ ] JWT SecretKey seguro generado
- [ ] CORS configurado para dominios específicos
- [ ] Health check funcionando
- [ ] Migraciones de base de datos aplicadas
- [ ] Logs configurados
- [ ] Reverse proxy configurado (si aplica)
- [ ] SSL/HTTPS configurado (si aplica)
- [ ] Backup de base de datos configurado

---

**¡Listo para producción! 🚀**
