#!/usr/bin/env python3
import json
import urllib.request
import urllib.parse
import sys
import argparse
import re
import random
import os

def parse_args():
    parser = argparse.ArgumentParser(description="Seed 50 businesses with 50 products each in San Carlos, Río San Juan into BuscaYa.")
    parser.add_argument("--url", default="http://localhost:5229", help="Base URL of the BuscaYa Backend API")
    parser.add_argument("--front-url", default="https://buscaya.encuentrame.org", help="Base URL of the BuscaYa Frontend website")
    parser.add_argument("--output", default="outreach_report_sancarlos.md", help="Filename to output the outreach report to")
    return parser.parse_args()

# Definition of the 50 businesses in San Carlos, Río San Juan
SAN_CARLOS_STORES = [
    # --- Comida (14 Tiendas) ---
    {"nombre": "Nando Food", "categoria": "Comida", "descripcion": "Las mejores hamburguesas, alitas picantes y comida rápida en San Carlos. ¡Sabor insuperable!", "whatsapp": "50557210250"},
    {"nombre": "Restaurante Kaoma", "categoria": "Comida", "descripcion": "Especialidades en mariscos, guapote entero y camarones de río frente al malecón de San Carlos.", "whatsapp": "50588880002"},
    {"nombre": "Restaurante La Paila", "categoria": "Comida", "descripcion": "Exquisitos asados al carbón, carnes premium y comida típica tradicional nicaragüense.", "whatsapp": "50588880003"},
    {"nombre": "Tree Kafe", "categoria": "Comida", "descripcion": "Cafetería acogedora. Café de altura norteño, repostería fina, frappés y postres deliciosos.", "whatsapp": "50588880004"},
    {"nombre": "Pizza Don Leo", "categoria": "Comida", "descripcion": "Pizzas artesanales con ingredientes frescos y abundantes, hamburguesas y combos familiares.", "whatsapp": "50588880005"},
    {"nombre": "Fritanga La Bendición", "categoria": "Comida", "descripcion": "La fritanga típica con el mejor sazón sanjuaneño: carne asada, tajadas con queso y gallopinto.", "whatsapp": "50588880006"},
    {"nombre": "Asados El Malecón", "categoria": "Comida", "descripcion": "Pollo y carne asada al carbón con vista espectacular al lago Cocibolca. Calidad garantizada.", "whatsapp": "50588880007"},
    {"nombre": "Cafetín San Carlos", "categoria": "Comida", "descripcion": "Desayunos típicos completos, café caliente y comida casera para iniciar tu día con energía.", "whatsapp": "50588880008"},
    {"nombre": "Refresquería El Trópico", "categoria": "Comida", "descripcion": "Fresco de cacao con leche, tiste, pozol y batidos de frutas naturales para refrescar tu tarde.", "whatsapp": "50588880009"},
    {"nombre": "Panadería El Buen Gusto", "categoria": "Comida", "descripcion": "Pan fresco todos los días, picos de queso, repostería fina y pasteles sobre pedido.", "whatsapp": "50588880010"},
    {"nombre": "Restaurante El Mirador", "categoria": "Comida", "descripcion": "Comida a la carta, filetes de pescado frescos del río y excelente atención familiar.", "whatsapp": "50588880011"},
    {"nombre": "Antojitos de mi Tierra", "categoria": "Comida", "descripcion": "Vigorón, quesillos, enchiladas y antojitos tradicionales nicaragüenses al instante.", "whatsapp": "50588880012"},
    {"nombre": "Restaurante Gran Lago", "categoria": "Comida", "descripcion": "Pescados fritos, sopas de mariscos y ambiente agradable para disfrutar con amigos.", "whatsapp": "50588880013"},
    {"nombre": "Fritanga El Shaddai", "categoria": "Comida", "descripcion": "Cenas típicas completas, maduro con queso y refrescos naturales helados.", "whatsapp": "50588880014"},
    
    # --- Repuestos y Talleres (8 Tiendas) ---
    {"nombre": "Repuestos Marinos El Sanjuaneño", "categoria": "Repuestos", "descripcion": "Repuestos originales para motores fuera de borda Yamaha y Suzuki, aceites marinos y hélices.", "whatsapp": "50588880015"},
    {"nombre": "Taller y Repuestos El Rapidito", "categoria": "Repuestos", "descripcion": "Venta de repuestos para motocicletas, llantas, cadenas, aceites y servicio técnico rápido.", "whatsapp": "50588880016"},
    {"nombre": "Multirepuestos Solentiname", "categoria": "Repuestos", "descripcion": "Repuestos náuticos e industriales. Filtros, impulsores y accesorios de lancha.", "whatsapp": "50588880017"},
    {"nombre": "Taller Moto-Náutico El Río", "categoria": "Repuestos", "descripcion": "Especialistas en reparación de motos y motores marinos. Venta de lubricantes de alta gama.", "whatsapp": "50588880018"},
    {"nombre": "Autorepuestos La Uno San Carlos", "categoria": "Repuestos", "descripcion": "Tu parada de confianza para repuestos eléctricos y mecánicos de moto, baterías y accesorios.", "whatsapp": "50588880019"},
    {"nombre": "Repuestos Motores El Castillo", "categoria": "Repuestos", "descripcion": "Todo en repuestos de motores fueraborda, cables, bujías y accesorios de navegación.", "whatsapp": "50588880020"},
    {"nombre": "Taller de Motos Las Tres R", "categoria": "Repuestos", "descripcion": "Mantenimiento preventivo, llantas y repuestos de marcas reconocidas para tu motocicleta.", "whatsapp": "50588880021"},
    {"nombre": "Lubricantes El Diamante", "categoria": "Repuestos", "descripcion": "Amplio surtido de lubricantes marinos y terrestres, grasas especiales y filtros de motor.", "whatsapp": "50588880022"},
    
    # --- Ferreterías (6 Tiendas) ---
    {"nombre": "Ferretería El Río", "categoria": "Ferretería", "descripcion": "Herramientas de campo, machetes, botas de hule y equipamiento impermeable para el clima de San Carlos.", "whatsapp": "50588880023"},
    {"nombre": "Ferretería San Carlos", "categoria": "Ferretería", "descripcion": "Materiales eléctricos, fontanería, tornillos, láminas de zinc y herramientas manuales de calidad.", "whatsapp": "50588880024"},
    {"nombre": "Ferretería La Solución", "categoria": "Ferretería", "descripcion": "Todo en pinturas, solventes, taladros percutores y herramientas eléctricas para construcción.", "whatsapp": "50588880025"},
    {"nombre": "Ferretería El Tornillo", "categoria": "Ferretería", "descripcion": "Venta de pernos, arandelas, clavos y herramientas básicas para carpintería y herrería.", "whatsapp": "50588880026"},
    {"nombre": "Ferretería El Puente", "categoria": "Ferretería", "descripcion": "Especialistas en tuberías PVC, conexiones de agua y accesorios para instalaciones sanitarias.", "whatsapp": "50588880027"},
    {"nombre": "Materiales Río San Juan", "categoria": "Ferretería", "descripcion": "Cemento, hierro, mallas, alambre de púas y agregados para obras de construcción civil.", "whatsapp": "50588880028"},
    
    # --- Ropa, Calzado y Variedades (10 Tiendas) ---
    {"nombre": "Variedades Río San Juan", "categoria": "Ropa", "descripcion": "Artículos para navegación: capas de lluvia, mochilas impermeables y sombreros para el sol.", "whatsapp": "50588880029"},
    {"nombre": "Tienda El Regalo", "categoria": "Ropa", "descripcion": "Ropa de moda casual para caballeros y damas. Jeans, blusas y camisas de marcas.", "whatsapp": "50588880030"},
    {"nombre": "Calzado Masaya en San Carlos", "categoria": "Calzado", "descripcion": "Calzado artesanal de cuero de Masaya: sandalias cómodas, botas de trabajo y zapatos escolares.", "whatsapp": "50588880031"},
    {"nombre": "Boutique El Estilo", "categoria": "Ropa", "descripcion": "Boutique exclusiva de moda femenina: vestidos veraniegos, blusas formales y carteras.", "whatsapp": "50588880032"},
    {"nombre": "Variedades La Quince", "categoria": "Ropa", "descripcion": "Variedades generales, ropa interior, calcetines y ropa cómoda para toda la familia.", "whatsapp": "50588880033"},
    {"nombre": "Tienda Deportiva El Gol", "categoria": "Deportes", "descripcion": "Todo en implementos deportivos, balones, camisetas de equipos de fútbol y calzado deportivo.", "whatsapp": "50588880034"},
    {"nombre": "Variedades J&M", "categoria": "Ropa", "descripcion": "Mochilas reforzadas, bolsos de viaje, billeteras de cuero y accesorios variados.", "whatsapp": "50588880035"},
    {"nombre": "Boutique Las Américas", "categoria": "Ropa", "descripcion": "Jeans clásicos, camisas polo de caballeros y chaquetas cortavientos de alta calidad.", "whatsapp": "50588880036"},
    {"nombre": "Calzado El Éxito", "categoria": "Calzado", "descripcion": "Zapatillas deportivas de marcas, sandalias playeras y calzado cómodo para diario.", "whatsapp": "50588880037"},
    {"nombre": "Novedades San Carlos", "categoria": "Ropa", "descripcion": "Gorras de béisbol, fajas de cuero, lentes de sol polarizados y relojes de pulsera.", "whatsapp": "50588880038"},
    
    # --- Farmacias (5 Tiendas) ---
    {"nombre": "Farmacia San Carlos", "categoria": "Farmacia", "descripcion": "Venta de medicamentos de calidad, repelentes contra insectos tropicales y sueros orales.", "whatsapp": "50588880039"},
    {"nombre": "Farmacia La Salud", "categoria": "Farmacia", "descripcion": "Medicamentos genéricos y de marca, vitaminas, cuidado para bebés y protectores solares.", "whatsapp": "50588880040"},
    {"nombre": "Farmacia Divina Misericordia", "categoria": "Farmacia", "descripcion": "Tu salud es nuestra prioridad: medicamentos recetados, pañales y cuidado personal.", "whatsapp": "50588880041"},
    {"nombre": "Farmacia La Gracia", "categoria": "Farmacia", "descripcion": "Atención rápida en medicamentos comunes, alcohol, gasas estériles y botiquines.", "whatsapp": "50588880042"},
    {"nombre": "Farmacia Río San Juan", "categoria": "Farmacia", "descripcion": "Completo surtido farmacéutico, jarabes naturales para la tos y suplementos alimenticios.", "whatsapp": "50588880043"},
    
    # --- Celulares y Tecnología (5 Tiendas) ---
    {"nombre": "Ciber y Celulares San Carlos", "categoria": "Celulares", "descripcion": "Cargadores rápidos, protectores de vidrio templado, audífonos bluetooth y accesorios de móvil.", "whatsapp": "50588880044"},
    {"nombre": "Servicio Técnico El Pro", "categoria": "Celulares", "descripcion": "Reparación técnica de celulares y tablets, repuestos de pantallas y puertos de carga.", "whatsapp": "50588880045"},
    {"nombre": "Variedades Tecnológicas RSJ", "categoria": "Celulares", "descripcion": "Memorias MicroSD, bocinas inalámbricas recargables y soportes de celular para moto.", "whatsapp": "50588880046"},
    {"nombre": "Tienda Claro y Tigo San Carlos", "categoria": "Celulares", "descripcion": "Venta de chips prepago, recargas telefónicas activas y routers portátiles de internet.", "whatsapp": "50588880047"},
    {"nombre": "Tecno-Accesorios El Puerto", "categoria": "Celulares", "descripcion": "Protectores impermeables para celular para botes, audífonos deportivos e inalámbricos.", "whatsapp": "50588880048"},
    
    # --- Distribuidoras (2 Tiendas) ---
    {"nombre": "Distribuidora El Diamante", "categoria": "Super", "descripcion": "Distribución de abarrotes al por mayor y detalle, granos básicos, aceites y alimentos secos.", "whatsapp": "50588880049"},
    {"nombre": "Pulpería La Esperanza", "categoria": "Super", "descripcion": "Abarrotes generales, refrescos, galletas, productos lácteos y artículos de aseo del hogar.", "whatsapp": "50588880050"}
]

# Real, varied Unsplash images for each category
PRODUCT_TEMPLATES = {
    "Comida": {
        "bases": ["Nacatamal", "Vigorón", "Quesillo", "Asado de Res", "Pollo al Carbón", "Filete de Pescado", "Guapote Frito", "Sopa de Mariscos", "Pizza Personal", "Hamburguesa Especial", "Alitas Picantes", "Fresco de Cacao", "Tiste Helado", "Café Negro", "Tres Leches", "Tarta de Chocolate", "Tacos de Pollo", "Enchilada", "Tajadas con Cerdo", "Chancho con Yuca", "Quesadilla", "Sándwich de Pollo", "Hot Dog", "Papas Fritas", "Sopa de Gallina"],
        "modifiers": ["Tradicional", "Gigante", "Familiar", "con Doble Queso", "Especial", "con Papas", "Sanjuaneño", "al Ajillo", "a la Parrilla", "Casero", "Mixto", "Crujiente", "Picante", "Mediano", "Grande"],
        "min_price": 40.0, "max_price": 450.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600",
            "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=600",
            "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600",
            "https://images.unsplash.com/photo-1544025162-d76694265947?w=600",
            "https://images.unsplash.com/photo-1551248429-40975aa4de74?w=600",
            "https://images.unsplash.com/photo-1565958011703-44f9829ba187?w=600",
            "https://images.unsplash.com/photo-1585238342024-78d387f4a707?w=600",
            "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=600"
        ]
    },
    "Repuestos": {
        "bases": ["Hélice de Aluminio", "Bujía Marina", "Impulsor de Agua", "Filtro de Gasolina", "Llanta Montañera", "Cadena Reforzada", "Batería de Gel", "Casco Certificado", "Aceite de 2 Tiempos", "Aceite Yamalube 4T", "Piñón de Arranque", "Espejo Retrovisor", "Amortiguador Trasero", "Pastillas de Freno", "Cable de Acelerador", "Ánodo de Zinc", "Arrancador de Soga", "Kit de Luces LED", "Radiador", "Bobina de Ignición", "Carburador", "Kit de Empaques", "Filtro de Aire", "Llanta de Pista", "Propela de Bronce"],
        "modifiers": ["Yamaha 40HP", "Suzuki 15HP", "Yamaha 75HP", "R18", "R17", "Truper", "Castrol", "Universal", "Reforzado", "Premium", "Yamaha 115HP", "Genuino", "Suzuki 4T", "de alta duración", "Japonesa"],
        "min_price": 80.0, "max_price": 3800.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1486006920555-c77dce18193b?w=600",
            "https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=600",
            "https://images.unsplash.com/photo-1519750783826-e2420f4d687f?w=600",
            "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=600",
            "https://images.unsplash.com/photo-1530047625168-4b18fa29d312?w=600"
        ]
    },
    "Ferretería": {
        "bases": ["Machete con Cacha", "Botas de Hule", "Lámpara Recargable", "Capa Impermeable", "Taladro Percutor", "Caja de Herramientas", "Flexómetro", "Juego de Destornilladores", "Lámina de Zinc", "Tubería PVC", "Pintura para Madera", "Grifo de Cobre", "Varilla de Hierro", "Bolsa de Cemento", "Cinta Métrica", "Candado de Seguridad", "Martillo de Uña", "Alambre de Púas", "Alicate de Presión", "Broca para Concreto"],
        "modifiers": ["Collin's 22\"", "Pantaneras", "LED 50W", "tipo Poncho", "Stanley 600W", "Truper 16\"", "de 5 Metros", "de 6 piezas", "Corrugada", "de 1/2\"", "Anticorrosiva", "de alta presión", "de 3/8\"", "Canal", "Pesado", "Impermeable", "Reforzada", "Rollo 100m", "Profesional", "Acero templado"],
        "min_price": 50.0, "max_price": 2800.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1581244277943-fe4a9c777189?w=600",
            "https://images.unsplash.com/photo-1534224039826-c7a0dea0e66a?w=600",
            "https://images.unsplash.com/photo-1586864387967-d02ef85d93e8?w=600",
            "https://images.unsplash.com/photo-1595853035070-59a39fe84de3?w=600"
        ]
    },
    "Ropa": {
        "bases": ["Sombrero para Sol", "Camisa Manga Larga", "Jeans Clásicos", "Blusa Estampada", "Vestido Casual", "Mochila Impermeable", "Gorra de Béisbol", "Cartera de Hombro", "Faja de Cuero", "Reloj Digital", "Short Deportivo", "Camiseta de Algodón", "Abrigo Ligero", "Lentes de Sol", "Billetera"],
        "modifiers": ["Unisex", "de Algodón", "Caballero", "de Verano", "Floreado", "de 30L", "Ajustable", "de Dama", "Original", "Deportivo", "Impermeable", "Casual", "Polarizados", "Slim Fit", "Clásico"],
        "min_price": 120.0, "max_price": 950.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=600",
            "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?w=600",
            "https://images.unsplash.com/photo-1509319117193-57bab727e09d?w=600",
            "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=600",
            "https://images.unsplash.com/photo-1576566588028-4147f3842f27?w=600"
        ]
    },
    "Calzado": {
        "bases": ["Sandalias de Cuero", "Zapatos Escolares", "Zapatillas Deportivas", "Botas de Trabajo", "Chinelas Playeras", "Zapatos de Vestir", "Zuecos de Goma"],
        "modifiers": ["artesanales de Masaya", "Negros de amarrar", "Comfort", "con punta de acero", "Antideslizantes", "de Cuero legítimo", "para lluvia", "Ultralivianas", "Clásicas"],
        "min_price": 220.0, "max_price": 1200.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=600",
            "https://images.unsplash.com/photo-1603808033192-082d6919d3e1?w=600",
            "https://images.unsplash.com/photo-1539185441755-769473a23570?w=600",
            "https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?w=600"
        ]
    },
    "Deportes": {
        "bases": ["Balón de Fútbol", "Camiseta de Fútbol", "Gorra Deportiva", "Termo de Agua", "Espinilleras", "Zapatillas de Tacos", "Muñequeras"],
        "modifiers": ["Nro 5 Profesional", "Réplica Genuina", "Ajustable Dry-Fit", "Acero Inoxidable 1L", "de alta resistencia", "para grama sintética", "Elásticas"],
        "min_price": 150.0, "max_price": 1800.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1508098682722-e99c43a406b2?w=600",
            "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=600",
            "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=600"
        ]
    },
    "Farmacia": {
        "bases": ["Repelente Off!", "Suero Oral", "Acetaminofén", "Protector Solar", "Multivitamínico", "Loratadina", "Ibuprofeno", "Alcohol Líquido", "Gasa Estéril", "Jarabe para Tos", "Crema Hidratante", "Pastillas Antigripales", "Vitamina C"],
        "modifiers": ["Extra Fuerte", "Electrolit 500ml", "500mg (100 tab)", "SPF 50 Neutrogena", "Centrum 30 tab", "10mg (30 tab)", "400mg (50 tab)", "al 70% 1 Litro", "estéril 10 unidades", "Ambroxol Pediátrico", "con Aloe Vera", "Panadol Ultra", "Masticable 500mg"],
        "min_price": 20.0, "max_price": 680.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=600",
            "https://images.unsplash.com/photo-1607619056574-7b8d304b3b86?w=600",
            "https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=600"
        ]
    },
    "Celulares": {
        "bases": ["Cargador Rápido", "Audífonos Inalámbricos", "Bocina Bluetooth", "Protector de Vidrio", "Estuche Impermeable", "Tarjeta SIM", "Memoria MicroSD", "Soporte para Moto", "Router Portátil", "Cable USB-C", "Celular Inteligente", "Batería Portátil Powerbank"],
        "modifiers": ["20W Tipo C", "Xiaomi Redmi Buds", "JBL Go 3", "Templado 9H", "para Celular", "Claro Prepago", "64GB Kingston", "Universal", "4G LTE", "Reforzado 1.2m", "Gama Entrada Android", "10000mAh"],
        "min_price": 5.0, "max_price": 180.0, "moneda": "$",
        "images": [
            "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600",
            "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600",
            "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=600"
        ]
    },
    "Super": {
        "bases": ["Granos Básicos", "Aceite Vegetal", "Azúcar Refinada", "Café Molido", "Detergente en Polvo", "Leche Entera", "Avena en Hojuelas", "Pastas Alimenticias", "Papel Higiénico", "Jabón de Baño", "Salsa de Tomate", "Atún en lata"],
        "modifiers": ["Arroz/Frijoles 5lb", "de 1 Litro", "de 2lb", "Local Jinotega 1lb", "Multiusos 1kg", "en Polvo 400g", "de 500g", "Espagueti 200g", "paquete 4 rollos", "Antibacterial 3 unidades", "Especial 400g", "en aceite de girasol"],
        "min_price": 25.0, "max_price": 280.0, "moneda": "C$",
        "images": [
            "https://images.unsplash.com/photo-1542838132-92c53300491e?w=600",
            "https://images.unsplash.com/photo-1578916171728-46686eac8d58?w=600"
        ]
    }
}

# Real, varied store cover and logo images based on category
STORE_IMAGES = {
    "Comida": {
        "logo": "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=1000&h=400&fit=crop"
    },
    "Repuestos": {
        "logo": "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1563720223185-11003d516935?w=1000&h=400&fit=crop"
    },
    "Ferretería": {
        "logo": "https://images.unsplash.com/photo-1504148455328-c376907d081c?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1534224039826-c7a0dea0e66a?w=1000&h=400&fit=crop"
    },
    "Ropa": {
        "logo": "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1483985988355-763728e1935b?w=1000&h=400&fit=crop"
    },
    "Calzado": {
        "logo": "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?w=1000&h=400&fit=crop"
    },
    "Deportes": {
        "logo": "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1508098682722-e99c43a406b2?w=1000&h=400&fit=crop"
    },
    "Farmacia": {
        "logo": "https://images.unsplash.com/photo-1607619056574-7b8d304b3b86?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1586015555751-63bb77f4322a?w=1000&h=400&fit=crop"
    },
    "Celulares": {
        "logo": "https://images.unsplash.com/photo-1562408590-e32931084e23?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1551703599-6b3dbb57c243?w=1000&h=400&fit=crop"
    },
    "Super": {
        "logo": "https://images.unsplash.com/photo-1578916171728-46686eac8d58?w=200&h=200&fit=crop",
        "cover": "https://images.unsplash.com/photo-1542838132-92c53300491e?w=1000&h=400&fit=crop"
    }
}

def make_request(url, method="GET", data=None, headers=None, retries=3):
    import time
    if headers is None:
        headers = {}
    
    req_data = None
    if data is not None:
        req_data = json.dumps(data).encode("utf-8")
        headers["Content-Type"] = "application/json"
    
    import ssl
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE

    for attempt in range(retries):
        req = urllib.request.Request(url, data=req_data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, context=ctx) as response:
                res_data = response.read().decode("utf-8")
                return response.status, json.loads(res_data) if res_data else {}
        except urllib.error.HTTPError as e:
            # Si hay un error del servidor o rate limit, esperar e intentar de nuevo
            if e.code >= 500 or e.code == 429:
                if attempt < retries - 1:
                    time.sleep(2 ** attempt)
                    continue
            err_body = e.read().decode("utf-8")
            try:
                return e.code, json.loads(err_body)
            except json.JSONDecodeError:
                return e.code, {"error": err_body}
        except Exception as e:
            if attempt < retries - 1:
                time.sleep(2 ** attempt)
                continue
            return 500, {"error": str(e)}

def fetch_categories(base_url):
    print(f"[*] Obteniendo categorías del backend en {base_url}...")
    status, res = make_request(f"{base_url}/api/public/categorias")
    if status == 200:
        cat_map = {cat["nombre"].lower(): cat["id"] for cat in res}
        print(f"[+] Categorías cargadas correctamente: {list(cat_map.keys())}")
        return cat_map
    else:
        print(f"[!] No se pudieron obtener categorías del API: {res}. Usando mapeo estático predeterminado.")
        return {
            "comida": 1, "ropa": 2, "calzado": 3, "accesorios": 4, "belleza": 5, 
            "bebés": 6, "juguetes": 7, "deportes": 8, "celulares": 9, "electrónica": 10, 
            "repuestos": 11, "hogar": 12, "mascotas": 13, "super": 14, "farmacia": 15, "ferretería": 16
        }

def register_user(base_url, username, password, fullname, email, phone):
    payload = {
        "NombreUsuario": username,
        "Contrasena": password,
        "NombreCompleto": fullname,
        "Email": email,
        "Telefono": phone
    }
    status, res = make_request(f"{base_url}/api/auth/register", method="POST", data=payload)
    if status == 200:
        return res.get("token"), res.get("usuario", {}).get("id")
    else:
        login_payload = {
            "NombreUsuario": username,
            "Contrasena": password
        }
        l_status, l_res = make_request(f"{base_url}/api/auth/login", method="POST", data=login_payload)
        if l_status == 200:
            return l_res.get("token"), l_res.get("usuario", {}).get("id")
        else:
            return None, None

def get_realistic_location(store_info, index):
    category = store_info["categoria"]
    # Coordenadas centrales de referencia para San Carlos, Río San Juan
    lat = 11.126100
    lng = -84.779700
    
    # Sembrar la semilla para que los offsets sean reproducibles en cada ejecución
    random.seed(index)
    offset_lat = random.uniform(-0.0009, 0.0009)
    offset_lng = random.uniform(-0.0009, 0.0009)
    
    if category == "Comida":
        # Zona del Malecón / Frente al Lago Cocibolca (Sur-Oeste)
        sector_lat = 11.124800 + offset_lat
        sector_lng = -84.781100 + offset_lng
        address_options = [
            "Frente al Malecón de San Carlos, Costado Sur",
            "Del Embarcadero Municipal 100m al Oeste, Calle Costanera",
            "Frente al Lago Cocibolca, Calle de los Restaurantes",
            "Costado Sur del Parque Central, frente al Malecón",
            "Calle del Malecón, frente a la zona de lanchas turísticas"
        ]
        address = address_options[index % len(address_options)]
    elif category == "Repuestos":
        # Repuestos de moto cerca de terminal/salida, repuestos marinos cerca del puerto
        if "marino" in store_info["nombre"].lower() or "panga" in store_info["nombre"].lower() or "náutico" in store_info["nombre"].lower():
            sector_lat = 11.123600 + offset_lat
            sector_lng = -84.780100 + offset_lng
            address = "Zona del Puerto Municipal, contiguo al muelle de pasajeros"
        else:
            sector_lat = 11.129200 + offset_lat
            sector_lng = -84.777900 + offset_lng
            address = "De la Terminal de Buses 2 cuadras al Oeste, Carretera Principal"
    elif category == "Ferretería":
        # Zona comercial del Mercado Municipal y salida norte
        sector_lat = 11.128100 + offset_lat
        sector_lng = -84.777100 + offset_lng
        address_options = [
            "De la rotonda de entrada 150m al Norte, Calle Principal",
            "Sector del Mercado Municipal, frente a Distribuidora El Diamante",
            "De la Terminal de Buses 1 cuadra al Norte, zona ferretera",
            "Calle principal de salida a Managua, frente a gasolinera"
        ]
        address = address_options[index % len(address_options)]
    elif category == "Farmacia":
        # Centro de la ciudad o cerca del hospital/centro de salud
        sector_lat = 11.126400 + offset_lat
        sector_lng = -84.778800 + offset_lng
        address_options = [
            "Calle Central, contiguo al Centro de Salud",
            "De la Iglesia Católica 1 cuadra al Norte",
            "Frente al Parque Central, Costado Este",
            "Calle de la Alcaldía, media cuadra al Oeste"
        ]
        address = address_options[index % len(address_options)]
    elif category in ["Celulares", "Ropa", "Calzado", "Deportes"]:
        # Calle del Comercio y alrededores del Parque Central (Centro)
        sector_lat = 11.125900 + offset_lat
        sector_lng = -84.779300 + offset_lng
        address_options = [
            "Calle del Comercio, de la Alcaldía 50 metros al Sur",
            "Costado Oeste del Parque Central, sector comercial",
            "Calle Principal, contiguo a la sucursal de Claro/Tigo",
            "De Enitel 1/2 cuadra al Este, frente al parque",
            "Avenida Central, contiguo a Tienda El Regalo"
        ]
        address = address_options[index % len(address_options)]
    else: # Super / Abarrotes
        # Sector del Mercado
        sector_lat = 11.127400 + offset_lat
        sector_lng = -84.777600 + offset_lng
        address = "Costado Norte del Mercado Municipal, zona comercial"
        
    return round(sector_lat, 6), round(sector_lng, 6), address

def create_store(base_url, token, store_info, index):
    headers = {"Authorization": f"Bearer {token}"}
    clean_name = store_info["nombre"].lower().replace(" ", "").replace("'", "")
    
    # Dynamic real cover/logo depending on category
    category_key = store_info["categoria"]
    img_conf = STORE_IMAGES.get(category_key, STORE_IMAGES["Super"])
    logo_url = img_conf["logo"]
    cover_url = img_conf["cover"]

    # Obtener ubicación y dirección específicas y realistas
    lat, lng, address = get_realistic_location(store_info, index)

    payload = {
        "NombreTienda": store_info["nombre"],
        "DescripcionTienda": store_info["descripcion"],
        "TelefonoTienda": store_info["whatsapp"],
        "WhatsAppTienda": store_info["whatsapp"],
        "EmailTienda": f"contacto@{clean_name}.com",
        "DireccionTienda": address,
        "Latitud": lat,
        "Longitud": lng,
        "Ciudad": "San Carlos",
        "Departamento": "Río San Juan",
        "LogoTienda": logo_url,
        "FotoTienda": cover_url,
        "DiasAtencion": "Lunes a Sábado",
        "HorarioApertura": "08:00:00",
        "HorarioCierre": "18:00:00"
    }
    
    status, res = make_request(f"{base_url}/api/cliente/crear-tienda", method="POST", data=payload, headers=headers)
    if status == 200:
        usuario_info = res.get("usuario", {})
        return usuario_info.get("tiendaId")
    else:
        p_status, p_res = make_request(f"{base_url}/api/tienda/perfil", headers=headers)
        if p_status == 200:
            return p_res.get("id")
        else:
            return None

def clear_store_products(base_url, token):
    headers = {"Authorization": f"Bearer {token}"}
    status, res = make_request(f"{base_url}/api/tienda/productos", headers=headers)
    if status == 200 and isinstance(res, list):
        if len(res) > 0:
            print(f"  [*] Limpiando {len(res)} productos antiguos de la tienda...")
            for prod in res:
                p_id = prod.get("id")
                if p_id:
                    make_request(f"{base_url}/api/tienda/productos/{p_id}", method="DELETE", headers=headers)
            print("  [+] Limpieza completada con éxito.")

def add_product(base_url, token, product_payload):
    headers = {"Authorization": f"Bearer {token}"}
    status, res = make_request(f"{base_url}/api/tienda/productos", method="POST", data=product_payload, headers=headers)
    return status == 201 or status == 200

def clean_price(raw_price, moneda):
    if moneda == "C$":
        val = round(raw_price)
        if val < 50:
            return float(val)
        elif val < 500:
            return float(round(val / 5) * 5)
        else:
            return float(round(val / 10) * 10)
    else: # USD "$"
        val = round(raw_price)
        if random.random() < 0.7:
            return float(val) - 0.01
        else:
            return float(val)

def generate_50_products(category_name, category_id, store_id):
    template = PRODUCT_TEMPLATES.get(category_name)
    if not template:
        template = PRODUCT_TEMPLATES["Super"]
    
    generated = []
    attempts = 0
    names_seen = set()
    
    while len(generated) < 50 and attempts < 300:
        attempts += 1
        base = random.choice(template["bases"])
        mod = random.choice(template["modifiers"])
        prod_name = f"{base} {mod}"
        
        if prod_name in names_seen:
            continue
            
        names_seen.add(prod_name)
        
        raw_price = random.uniform(template["min_price"], template["max_price"])
        precio = clean_price(raw_price, template["moneda"])
        
        en_oferta = random.random() < 0.3
        precio_anterior = None
        if en_oferta:
            precio_anterior = clean_price(precio * random.uniform(1.15, 1.30), template["moneda"])
            if precio_anterior <= precio:
                precio_anterior = precio + (10.0 if template["moneda"] == "C$" else 1.00)
            
        foto_url = random.choice(template["images"])
        
        product_payload = {
            "Nombre": prod_name,
            "Descripcion": f"{prod_name} de alta calidad. Ideal para el día a día. Garantía y soporte directo en San Carlos.",
            "Precio": precio,
            "EnOferta": en_oferta,
            "PrecioAnterior": precio_anterior,
            "Moneda": template["moneda"],
            "CategoriaId": category_id,
            "FotoUrl": foto_url,
            "ImagenesUrls": [foto_url]
        }
        generated.append(product_payload)
        
    index = 1
    while len(generated) < 50:
        base = random.choice(template["bases"])
        prod_name = f"{base} Especial Serie #{index}"
        raw_price = random.uniform(template["min_price"], template["max_price"])
        precio = clean_price(raw_price, template["moneda"])
        
        product_payload = {
            "Nombre": prod_name,
            "Descripcion": f"Producto premium {prod_name}. Disponible en nuestro catálogo local.",
            "Precio": precio,
            "EnOferta": False,
            "PrecioAnterior": None,
            "Moneda": template["moneda"],
            "CategoriaId": category_id,
            "FotoUrl": random.choice(template["images"]),
            "ImagenesUrls": [random.choice(template["images"])]
        }
        generated.append(product_payload)
        index += 1
        
    return generated

def save_store_markdown(outdir, store, tienda_id, username, password, live_url, products, lat, lng, address):
    clean_name = re.sub(r'[^a-zA-Z0-9]', '_', store['nombre']).lower()
    filename = os.path.join(outdir, f"{clean_name}.md")
    
    # Generate WhatsApp message link
    message = (
        f"Hola, un gusto saludarles. 👋 Soy del equipo de BuscaYa de Cowib.\n\n"
        f"Hemos lanzado BuscaYa (buscaya.encuentrame.org) con el fin de impulsar y digitalizar el comercio local en San Carlos y todo Río San Juan.\n\n"
        f"Viendo su excelente catálogo, nos tomamos la libertad de crearles un demo gratuito de su negocio para que vea cómo sus clientes pueden buscar sus productos y ordenarles directo a su WhatsApp:\n\n"
        f"👉 {live_url}\n\n"
        f"Les entregamos el acceso de forma 100% gratuita y sin compromisos para que puedan administrarlo y agregar más cosas. ¿Les interesaría coordinar una llamada breve para explicarles cómo funciona?"
    )
    encoded_message = urllib.parse.quote(message)
    whatsapp_link = f"https://wa.me/{store['whatsapp']}?text={encoded_message}"

    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"# 🏪 Credenciales de Acceso - {store['nombre']}\n\n")
        f.write(f"Este archivo contiene la información administrativa, accesos y la propuesta digital creada para **{store['nombre']}** en San Carlos, Río San Juan.\n\n")
        
        f.write("## 📌 Datos Comerciales\n")
        f.write(f"- **Nombre Comercial:** {store['nombre']}\n")
        f.write(f"- **Categoría:** {store['categoria']}\n")
        f.write(f"- **WhatsApp:** `+{store['whatsapp']}`\n")
        f.write(f"- **Dirección:** {address}\n")
        f.write(f"- **Ubicación Geográfica:** Latitud `{lat}`, Longitud `{lng}` ([Ver en Google Maps](https://www.google.com/maps/search/?api=1&query={lat},{lng}))\n\n")
        
        f.write("## 🔑 Credenciales para Entrega Directa\n")
        f.write("Proporciona estos accesos al dueño del comercio cuando reclame su tienda para que pueda modificar precios y agregar productos:\n\n")
        f.write(f"- **Nombre de Usuario:** `{username}`\n")
        f.write(f"- **Contraseña Temporal:** `{password}`\n")
        f.write(f"- **Enlace de la Tienda:** [{live_url}]({live_url})\n\n")
        
        f.write("## 💬 Mensaje de Outreach WhatsApp (Listo para enviar)\n")
        f.write(f"Haz clic en el enlace para iniciar el chat con el mensaje pre-cargado:\n")
        f.write(f"- **👉 [Enviar Mensaje Directo]({whatsapp_link})**\n\n")
        f.write("### Texto del Mensaje:\n")
        f.write("```text\n")
        f.write(message)
        f.write("\n```\n\n")
        
        f.write("## 📦 Catálogo de Productos Precargados (Demo de 50 Productos)\n\n")
        f.write("| # | Producto | Precio | Oferta? | Imagen |\n")
        f.write("| :---: | --- | :---: | :---: | --- |\n")
        for i, prod in enumerate(products):
            oferta_str = "✅ Sí" if prod["EnOferta"] else "❌ No"
            precio_str = f"{prod['Precio']} {prod['Moneda']}"
            if prod["EnOferta"] and prod.get("PrecioAnterior"):
                precio_str = f"~~{prod['PrecioAnterior']} {prod['Moneda']}~~ {precio_str}"
            
            f.write(f"| {i+1} | {prod['Nombre']} | {precio_str} | {oferta_str} | [Ver Foto]({prod['FotoUrl']}) |\n")

def main():
    args = parse_args()
    base_url = args.url.rstrip('/')
    front_url = args.front_url.rstrip('/')
    
    print("=" * 70)
    print("      🚀 INICIANDO SIEMBRA MASIVA - 50 TIENDAS x 50 PRODUCTOS RSJ 🚀")
    print("=" * 70)
    print(f"[*] Backend: {base_url}")
    print(f"[*] Frontend: {front_url}")
    print(f"[*] Total Tiendas a crear: {len(SAN_CARLOS_STORES)}")
    print(f"[*] Total Productos a crear: {len(SAN_CARLOS_STORES) * 50} (2,500 en total)\n")
    
    # Create output directory for individual md credentials
    outdir = "credenciales_outreach"
    os.makedirs(outdir, exist_ok=True)
    print(f"[*] Directorio de credenciales individuales creado: {outdir}/")
    
    categories = fetch_categories(base_url)
    
    results = []
    
    for idx, store in enumerate(SAN_CARLOS_STORES):
        store_num = idx + 1
        print("-" * 60)
        print(f"[{store_num}/{len(SAN_CARLOS_STORES)}] Tienda: {store['nombre']} ({store['categoria']})")
        
        clean_name = re.sub(r'[^a-zA-Z0-9]', '', store['nombre']).lower()
        username = f"owner_{clean_name}"
        password = f"RSJ{store_num:02d}2026!"
        email = f"{username}@buscaya.app"
        
        print(f"  [*] Registrando/Logueando usuario '{username}'...")
        token, user_id = register_user(base_url, username, password, f"Dueño de {store['nombre']}", email, store['whatsapp'])
        
        if not token:
            print(f"  [!] Fallo de autenticación para {store['nombre']}. Omitiendo.")
            continue
            
        print(f"  [+] Usuario OK (ID: {user_id}). Creando tienda...")
        tienda_id = create_store(base_url, token, store, idx)
        
        if not tienda_id:
            print(f"  [!] Fallo al crear/obtener tienda. Omitiendo.")
            continue
            
        print(f"  [+] Tienda creada con ID: {tienda_id}")
        
        # Clean existing products to make it idempotent and avoid double population
        clear_store_products(base_url, token)
        
        cat_key = store["categoria"].lower()
        if cat_key == "deportes":
            category_id = categories.get("deportes", categories.get("hogar", 12))
        elif cat_key == "celulares":
            category_id = categories.get("celulares", categories.get("electrónica", 10))
        elif cat_key == "super":
            category_id = categories.get("super", categories.get("hogar", 12))
        else:
            category_id = categories.get(cat_key, categories.get("hogar", 12))
            
        print(f"  [*] Generando 50 productos para {store['nombre']}...")
        products = generate_50_products(store["categoria"], category_id, tienda_id)
        
        inserted = 0
        for i, prod in enumerate(products):
            success = add_product(base_url, token, prod)
            if success:
                inserted += 1
            if (i+1) % 10 == 0:
                print(f"    [>] Progreso: {i+1}/50 productos procesados...")
                
        print(f"  [+] Tienda '{store['nombre']}' completada. {inserted} productos agregados con éxito.")
        
        # Get coordinates and address to pass to save_store_markdown
        lat, lng, address = get_realistic_location(store, idx)
        
        # Save individual store md file
        save_store_markdown(outdir, store, tienda_id, username, password, f"{front_url}/tienda/{tienda_id}", products, lat, lng, address)
        print(f"  [+] Archivo de credenciales individuales creado: {outdir}/{re.sub(r'[^a-zA-Z0-9]', '_', store['nombre']).lower()}.md")
        
        results.append({
            "nombre": store["nombre"],
            "whatsapp": store["whatsapp"],
            "tienda_id": tienda_id,
            "username": username,
            "password": password,
            "live_url": f"{front_url}/tienda/{tienda_id}",
            "productos_count": inserted
        })

    generate_report(args.output, results, front_url)

def generate_report(output_file, results, front_url):
    print("\n" + "=" * 60)
    print(f"[*] Generando Reporte de Outreach en {output_file}...")
    print("=" * 60)
    
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# 🚀 Reporte de Outreach y Siembra de Negocios en San Carlos\n\n")
        total_p = sum(r["productos_count"] for r in results)
        f.write(f"Se ha completado la carga masiva de **{len(results)} tiendas** en San Carlos, Río San Juan, sumando un total de **{total_p} productos**.\n\n")
        
        f.write(f"**Resumen Técnico:**\n")
        f.write(f"- **Total Comercios Creados:** {len(results)}\n")
        f.write(f"- **Total Productos Sembrados:** {total_p} items\n")
        f.write(f"- **Ubicación de Referencia:** San Carlos, Río San Juan (Coordenadas de búsqueda activas)\n")
        f.write(f"- **Fichas Individuales:** Almacenadas en la carpeta `credenciales_outreach/` para entrega ordenada.\n\n")
        
        f.write("## 🏪 Directorio de Comercios Digitalizados\n\n")
        f.write("| Negocio | Productos | ID Tienda | Ficha Individual | Enlace de Prueba en Vivo |\n")
        f.write("| --- | :---: | :---: | :---: | --- |\n")
        for r in results:
            clean_filename = re.sub(r'[^a-zA-Z0-9]', '_', r['nombre']).lower() + ".md"
            f.write(f"| **{r['nombre']}** | {r['productos_count']} | `{r['tienda_id']}` | [Ver Ficha](./credenciales_outreach/{clean_filename}) | [Ver Tienda en BuscaYa]({r['live_url']}) |\n")
            
        f.write("\n---\n\n")
        f.write("## 🔑 Credenciales para Entrega Administrativa\n")
        f.write("Cuando contactes a un negocio y desees cederle el control de su inventario, facilítale estos accesos:\n\n")
        
        for r in results:
            f.write(f"### 🏪 {r['nombre']}\n")
            f.write(f"- **Usuario:** `{r['username']}`\n")
            f.write(f"- **Contraseña Temporal:** `{r['password']}`\n")
            f.write(f"- **Enlace en BuscaYa:** [{r['live_url']}]({r['live_url']})\n\n")
            
        f.write("---\n\n")
        f.write("## 💬 Enlaces Rápidos de Prospección por WhatsApp (San Carlos)\n")
        f.write("Haz clic en los enlaces para abrir WhatsApp con un mensaje personalizado que destaca el apoyo al desarrollo local de Río San Juan:\n\n")
        
        for r in results:
            message = (
                f"Hola, un gusto saludarles. 👋 Soy del equipo de BuscaYa de Cowib.\n\n"
                f"Hemos lanzado BuscaYa (buscaya.encuentrame.org) con el fin de impulsar y digitalizar el comercio local en San Carlos y todo Río San Juan.\n\n"
                f"Viendo su excelente catálogo, nos tomamos la libertad de crearles un demo gratuito de su negocio para que vea cómo sus clientes pueden buscar sus productos y ordenarles directo a su WhatsApp:\n\n"
                f"👉 {r['live_url']}\n\n"
                f"Les entregamos el acceso de forma 100% gratuita y sin compromisos para que puedan administrarlo y agregar más cosas. ¿Les interesaría coordinar una llamada breve para explicarles cómo funciona?"
            )
            
            encoded_message = urllib.parse.quote(message)
            whatsapp_link = f"https://wa.me/{r['whatsapp']}?text={encoded_message}"
            
            f.write(f"### 📨 Outreach: **{r['nombre']}**\n")
            f.write(f"- **Contacto:** `+{r['whatsapp']}`\n")
            f.write(f"- **👉 [Enviar Mensaje por WhatsApp]({whatsapp_link})**\n\n")
            
    print(f"[+] Reporte completado con éxito en '{output_file}'.")

if __name__ == "__main__":
    main()
