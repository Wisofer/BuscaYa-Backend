# Producto en oferta — Guía para el frontend

El backend ya soporta **oferta** en productos. Así puede verse y usarse en la app/panel.

---

## 1. Campos en la API

En **crear** y **actualizar** producto, y en todas las respuestas donde se devuelve un producto:

| Campo              | Tipo    | Crear | Actualizar | Respuesta |
|--------------------|--------|-------|------------|-----------|
| `enOferta`         | boolean| ✅    | ✅ (opcional) | ✅ |
| `precioAnterior`   | decimal?| ✅ (opcional) | ✅ (opcional) | ✅ |

Además, en las respuestas el backend envía:

| Campo                | Tipo  | Descripción |
|----------------------|-------|-------------|
| `porcentajeDescuento`| int?  | % de descuento cuando hay oferta (ej. 15). Calculado: `(1 - precio / precioAnterior) * 100`. |

---

## 2. Cómo se usa (ideas para la UI)

### Crear / Editar producto (tienda)

- **Switch:** “En oferta” / “Activar oferta”.
  - Al activarlo, mostrar un campo opcional: **“Precio anterior”** (número).
  - Si el usuario lo llena, en listados y detalle se puede mostrar “Antes C$X, ahora C$Y” y el % de descuento.
- **Validación sugerida:** Si `enOferta` es true y se envía `precioAnterior`, en el frontend puedes validar que `precioAnterior > precio` para que el % tenga sentido (el backend no lo exige).

### Listado de productos (cards)

- Si `enOferta` es true:
  - Mostrar un **badge** tipo “En oferta” o “Oferta”.
  - Precio actual en destacado.
  - Si hay `precioAnterior`: mostrar el precio anterior **tachado** y, si quieres, “-{porcentajeDescuento}%”.

### Detalle de producto

- Si `enOferta` es true:
  - Badge “En oferta”.
  - Si hay `precioAnterior`: “Antes C$X” (tachado), “Ahora C$Y”, “-Z%”.

### Búsqueda / listados públicos

- Las respuestas de producto ya incluyen `enOferta`, `precioAnterior` y `porcentajeDescuento`; el frontend solo debe mostrarlos como arriba.
- Opcional: filtro “Solo ofertas” (filtrar en cliente por `enOferta === true` o pedir al backend un endpoint con filtro si lo añadís).

---

## 3. Ejemplo de body (crear producto)

```json
{
  "nombre": "Taladro Black & Decker",
  "descripcion": "...",
  "precio": 2300,
  "enOferta": true,
  "precioAnterior": 2800,
  "moneda": "C$",
  "categoriaId": 2,
  "fotoUrl": "https://..."
}
```

---

## 4. Ejemplo de respuesta (producto)

```json
{
  "id": 1,
  "nombre": "Taladro Black & Decker",
  "precio": 2300,
  "moneda": "C$",
  "enOferta": true,
  "precioAnterior": 2800,
  "porcentajeDescuento": 18,
  "fotoUrl": "...",
  "tienda": { ... },
  "categoria": { ... }
}
```

---

## 5. Resumen para el frontend

1. **Formulario crear/editar:** switch “En oferta” + campo opcional “Precio anterior”.
2. **Listados:** badge “En oferta”, precio tachado y % cuando haya `precioAnterior`.
3. **Detalle:** mismo criterio; mensaje tipo “Antes X, ahora Y, -Z%”.
4. Usar siempre los campos que ya devuelve la API: `enOferta`, `precioAnterior`, `porcentajeDescuento`.
