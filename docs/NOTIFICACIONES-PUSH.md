ienda nueva cerca de ti
Disparo: al crear una tienda (TiendaService.Crear).
A quién: usuarios con al menos una dirección guardada a ≤ 5 km de la tienda (excluyendo al dueño).
Mensaje: título "Nueva tienda cerca de ti", cuerpo "Nueva tienda cerca: {NombreTienda}".
Payload FCM: type=NEW_STORE_NEARBY, storeId, storeName.
2. Bajó de precio
Disparo: al actualizar un producto y el precio baja (request.Precio &lt; precio anterior).
A quién: usuarios que tienen ese producto en Favoritos.
Mensaje: título "Bajó de precio", cuerpo "{NombreProducto} ahora C$X".
Payload FCM: type=PRICE_DROP, productId, storeId, productName, storeName, oldPrice, newPrice.
3. Volvió a haber stock
Disparo: al actualizar un producto y Stock pasa de 0 (o null) a &gt; 0.
A quién: usuarios que tienen ese producto en Favoritos.
Mensaje: título "Volvió a haber stock", cuerpo "{NombreProducto} ya está disponible en {NombreTienda}".
Payload FCM: type=BACK_IN_STOCK, productId, storeId, productName, storeName.
4. Cambios en el modelo
Producto: campo Stock (int?, opcional). Se usa para detectar restock.
ActualizarProductoRequest: propiedad Stock (int?) para que el cliente pueda actualizar stock.
NotificationLog: NotificationType y EntityId para anti-spam y filtros.
5. Anti-spam
Máximo 3 notificaciones por usuario en 24 h (cualquier tipo).
No se repite la misma notificación para el mismo usuario y entidad en 24 h (mismo tipo + mismo storeId o productId).
