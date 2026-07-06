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
    parser = argparse.ArgumentParser(description="Seed 100 businesses in Managua with 50 products each into BuscaYa.")
    parser.add_argument("--url", default="http://localhost:5229", help="Base URL of the BuscaYa Backend API")
    parser.add_argument("--front-url", default="https://buscaya.encuentrame.org", help="Base URL of the BuscaYa Frontend website")
    parser.add_argument("--output", default="outreach_report_managua.md", help="Filename to output the outreach report to")
    return parser.parse_args()

# Zones in Managua
ZONES_CONFIG = {
    "Altamira": {
        "lat": 12.1270, "lng": -86.2655,
        "addresses": [
            "Calle Principal de Altamira, de la Vicky 1 cuadra al Sur",
            "Los Robles, del Hotel Hilton Princess 1 cuadra abajo",
            "De los semáforos de la Vicky 75 varas al Oeste",
            "Altamira, frente a las oficinas centrales de Banpro",
            "Los Robles, contiguo a Plaza La Fe",
            "Altamira, frente a la Iglesia San Agustín",
            "Calle Principal de Los Robles, frente al parque"
        ]
    },
    "Bello Horizonte": {
        "lat": 12.1432, "lng": -86.2365,
        "addresses": [
            "De la Rotonda de Bello Horizonte 2 cuadras al Sur",
            "Bello Horizonte, frente a la Iglesia Pío X",
            "Del Multicentro Las Américas 150 vrs al Oeste",
            "Bello Horizonte, sobre la pista principal, contiguo a la gasolinera",
            "Bello Horizonte, de los semáforos del colonial 1 cuadra arriba",
            "Bello Horizonte, del Parque Central 1 cuadra al Este",
            "Frente a la Rotonda de Bello Horizonte, sector de restaurantes"
        ]
    },
    "Mercado Oriental": {
        "lat": 12.1465, "lng": -86.2605,
        "addresses": [
            "Sector Ciudad Jardín, del BAC 1 cuadra al Este",
            "Mercado Oriental, del Gancho de Caminos 2 cuadras al Norte",
            "Mercado Oriental, sector El Calvario, pasillo principal",
            "Ciudad Jardín, frente a la parada de autobuses",
            "Mercado Oriental, de la caimana 1 cuadra abajo",
            "Mercado Oriental, sector de las ferreterías",
            "Ciudad Jardín, contiguo a la Óptica"
        ]
    },
    "Linda Vista": {
        "lat": 12.1495, "lng": -86.2995,
        "addresses": [
            "De los semáforos de Linda Vista 1 cuadra al Norte",
            "Monseñor Lezcano, de la estatua 2 cuadras abajo",
            "Linda Vista, de la gasolinera Puma 1 cuadra al Oeste",
            "Monseñor Lezcano, frente al parque San Sebastián",
            "Linda Vista, contiguo al Supermercado La Colonia",
            "Monseñor Lezcano, de la Iglesia 1 cuadra arriba",
            "Linda Vista, frente al Centro Comercial"
        ]
    },
    "Carretera Norte": {
        "lat": 12.1415, "lng": -86.1985,
        "addresses": [
            "Carretera Norte Km 5.5, frente a Pepsi",
            "Sector La Subasta, de los semáforos 1 cuadra al Este",
            "Carretera Norte, del paso a desnivel de Portezuelo 150 vrs abajo",
            "Km 6 Carretera Norte, contiguo a la parada de buses La Subasta",
            "Carretera Norte, frente a las bodegas de Aduana",
            "Km 7 Carretera Norte, frente a Taller El Pro",
            "Carretera Norte, del muelle de aduanas 2 cuadras arriba"
        ]
    },
    "Villa Fontana": {
        "lat": 12.1150, "lng": -86.2700,
        "addresses": [
            "De la rotonda Universitaria 1 cuadra al Este, frente a la UNAN",
            "Villa Fontana, frente a la entrada principal del Club Terraza",
            "De los semáforos del Club Terraza 100 vrs al Sur",
            "Sector Centroamérica, frente a la Iglesia Fátima",
            "Villa Fontana, contiguo al edificio de Invercasa",
            "De la UNAN-Managua 2 cuadras al Oeste, sector universitario",
            "Villa Fontana, frente a Plaza Familias"
        ]
    }
}

# Rich templates for Unsplash category images
PRODUCT_TEMPLATES = {
    "Comida": {
        "bases": ["Nacatamal", "Vigorón", "Quesillo", "Asado de Res", "Pollo al Carbón", "Filete de Pescado", "Guapote Frito", "Sopa de Mariscos", "Pizza Personal", "Hamburguesa Especial", "Alitas Picantes", "Fresco de Cacao", "Tiste Helado", "Café Negro", "Tres Leches", "Tarta de Chocolate", "Tacos de Pollo", "Enchilada", "Tajadas con Cerdo", "Chancho con Yuca", "Quesadilla", "Sándwich de Pollo", "Hot Dog", "Papas Fritas", "Sopa de Gallina"],
        "modifiers": ["Tradicional", "Gigante", "Familiar", "con Doble Queso", "Especial", "con Papas", "Managua Style", "al Ajillo", "a la Parrilla", "Casero", "Mixto", "Crujiente", "Picante", "Mediano", "Grande"],
        "min_price": 45.0, "max_price": 490.0, "moneda": "C$",
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
        "bases": ["Bujía de Auto", "Filtro de Aceite", "Llanta Deportiva", "Pastillas de Freno", "Batería de Carro", "Amortiguador Hidráulico", "Aceite Castrol 20W50", "Llanta de Moto", "Cadena de Moto", "Faro Delantero", "Espejo Retrovisor", "Cables de Bujía", "Carburador de Moto", "Disco de Embrague", "Filtro de Aire", "Kit de Herramientas", "Radiador de Aluminio", "Bocina de Aire", "Luces LED Auxiliares", "Anticongelante"],
        "modifiers": ["Bosch", "Suzuki", "Toyota", "Yamaha", "Universal", "Heavy Duty", "Mobil 1", "Denso", "Premium", "Genuino", "Japan Quality", "Sport", "Duramax", "R17", "R15"],
        "min_price": 100.0, "max_price": 4500.0, "moneda": "C$",
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

# Lists of typical names for businesses in Managua per category
BUSINESS_NAMES_DATA = {
    "Comida": [
        "Fritanga La Gran Managua", "Pupusería El Pipiripao", "La Casa del Café Altamira", "Pizza Valentis", "Asados El Primo", 
        "Fritanga Doña Julia", "El Kiosko de los Antojitos", "Restaurante El Novillo", "Nica Burgers", "Sabor de mi Tierra",
        "El Rincón del Asado", "Fritanga Monseñor Lezcano", "La Esquina del Sabor", "Pupusería La Bendición", "Bufet El Granero",
        "Asados Bello Horizonte", "Refresquería El Nectar", "La Fritanga del Negro", "Sopa de Mondongo El Patrón", "Sopas El Chino",
        "Tacos El Güegüense", "Pizzería Nápoles Linda Vista", "Cafetín Universitario", "Quesillos El Pipe", "El Portal de las Carnes"
    ],
    "Repuestos": [
        "Repuestos El Chele", "Multirepuestos Managua", "Auto Repuestos Altamira", "Lubricantes El Veloz", "Taller de Motos El Colocho",
        "Repuestos El Chino", "Doctor Moto Carretera Norte", "Lubricentro La Subasta", "Accesorios Automotrices León", "Taller Náutico Solentiname",
        "Baterías y Frenos El Rey", "Eléctricos Managua", "Super Repuestos Linda Vista", "Taller de Motos Express", "Clutch y Frenos del Norte"
    ],
    "Ferretería": [
        "Ferretería El Halcón", "Ferretería La Grapadora", "Ferretería Oriental", "Ferretería Linda Vista", "Ferretería El Tornillo Bello Horizonte",
        "Ferretería El Constructor", "Ferretería La Solución", "Ferretería San José", "Materiales de Construcción El Sólido", "Ferretería El Puente Carretera Norte",
        "Tornillos y Más Oriental", "Ferretería La Económica", "Ferretería La Ganga", "Ferretería El Martillazo", "Ferretería Multitools"
    ],
    "Ropa": [
        "Variedades El Oriental", "Boutique Divas Altamira", "Tienda El Regalo", "Boutique El Estilo", "Variedades La Quince",
        "Boutique Las Américas", "Novedades Managua", "Boutique Masculina El Elegante", "Tienda de Moda Estilos", "Variedades J&M",
        "Moda Nica", "Variedades La Bendición", "Boutique Esencia", "Moda Casual La Fe", "Novedades Linda Vista"
    ],
    "Calzado": [
        "Calzado Masaya Ciudad Jardín", "Calzado El Éxito", "Zapatería El Cuero", "Zapatería San Jerónimo", "Calzado Comfort",
        "Zapatería La Masatepeña", "Calzado Masaya Oriental", "Zapatería La Elegancia", "Calzado Deportivo El Gol", "Zapatería Sandalias y Más"
    ],
    "Deportes": [
        "Tienda Deportiva El Gol", "Deportes Managua", "Todo Deportes Altamira", "El Campéon Sports", "Deportes Universitaria",
        "Accesorios Deportivos Nica", "El Estadio Store", "Sport Center Bello Horizonte", "Deportes Linda Vista", "Mundo Deportivo"
    ],
    "Farmacia": [
        "Farmacia El Descuento", "Farmacia San Jerónimo", "Farmacia Santa Lucía", "Farmacia La Salud", "Farmacia Divina Misericordia",
        "Farmacia La Gracia", "Farmacia El Ahorro Managua", "Farmacia Santa María", "Farmacia La Fe", "Farmacia San Rafael"
    ],
    "Celulares": [
        "Doctor Celular", "Tecno Cell Oriental", "Ciber Conéctate", "Servicio Técnico El Pro", "Variedades Tecnológicas Managua",
        "Accesorios El Puerto", "Celulares y Más", "El Palacio del Celular", "Mundo Móvil Altamira", "Smart Choice Nicaragua"
    ],
    "Super": [
        "Distribuidora La Estación", "Pulpería La Esquina", "Abarrotes El Chino", "Distribuidora El Diamante", "Pulpería La Esperanza",
        "Minisuper El Centro", "Abarrotes y Granos El Oriental", "Distribuidora La Favorita", "Pulpería San Antonio", "Distribuidora El Éxito"
    ]
}

# Map categories to their config mapping
CATEGORY_MAP = {
    "Comida": "comida",
    "Repuestos": "repuestos",
    "Ferretería": "ferretería",
    "Ropa": "ropa",
    "Calzado": "calzado",
    "Deportes": "deportes",
    "Farmacia": "farmacia",
    "Celulares": "celulares",
    "Super": "super"
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

def create_store(base_url, token, store_info):
    headers = {"Authorization": f"Bearer {token}"}
    clean_name = store_info["nombre"].lower().replace(" ", "").replace("'", "")
    
    # Dynamic real cover/logo depending on category
    category_key = store_info["categoria"]
    img_conf = STORE_IMAGES.get(category_key, STORE_IMAGES["Super"])
    logo_url = img_conf["logo"]
    cover_url = img_conf["cover"]

    payload = {
        "NombreTienda": store_info["nombre"],
        "DescripcionTienda": store_info["descripcion"],
        "TelefonoTienda": store_info["whatsapp"],
        "WhatsAppTienda": store_info["whatsapp"],
        "EmailTienda": f"contacto@{clean_name}.com",
        "DireccionTienda": store_info["direccion"],
        "Latitud": store_info["lat"],
        "Longitud": store_info["lng"],
        "Ciudad": "Managua",
        "Departamento": "Managua",
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
            "Descripcion": f"{prod_name} de alta calidad. Ideal para el día a día. Garantía y soporte directo en Managua.",
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
            "Descripcion": f"Producto premium {prod_name}. Disponible en nuestro catálogo de Managua.",
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

def save_store_markdown(outdir, store, tienda_id, username, password, live_url, products):
    clean_name = re.sub(r'[^a-zA-Z0-9]', '_', store['nombre']).lower()
    filename = os.path.join(outdir, f"{clean_name}.md")
    
    # Generate WhatsApp message link
    message = (
        f"Hola, un gusto saludarles. 👋 Soy del equipo de BuscaYa de Cowib.\n\n"
        f"Hemos lanzado la plataforma BuscaYa (buscaya.encuentrame.org) para digitalizar e impulsar los comercios de Managua.\n\n"
        f"Viendo el éxito de su negocio, nos tomamos la libertad de crearles un demo gratuito con un catálogo inicial para que vea cómo sus clientes pueden buscar sus productos y ordenarles directo a su WhatsApp:\n\n"
        f"👉 {live_url}\n\n"
        f"Les entregamos el acceso administrativo de forma 100% gratuita y sin compromisos para que puedan actualizar precios, subir fotos o agregar nuevos productos. ¿Les interesaría coordinar una llamada breve para explicarles cómo funciona?"
    )
    encoded_message = urllib.parse.quote(message)
    whatsapp_link = f"https://wa.me/{store['whatsapp']}?text={encoded_message}"

    with open(filename, "w", encoding="utf-8") as f:
        f.write(f"# 🏪 Credenciales de Acceso - {store['nombre']}\n\n")
        f.write(f"Este archivo contiene la información administrativa, accesos y la propuesta digital creada para **{store['nombre']}** en Managua.\n\n")
        
        f.write("## 📌 Datos Comerciales\n")
        f.write(f"- **Nombre Comercial:** {store['nombre']}\n")
        f.write(f"- **Categoría:** {store['categoria']}\n")
        f.write(f"- **WhatsApp:** `+{store['whatsapp']}`\n")
        f.write(f"- **Dirección Física:** {store['direccion']}\n")
        f.write(f"- **Ubicación Geográfica:** Latitud `{store['lat']}`, Longitud `{store['lng']}` ([Ver en Google Maps](https://www.google.com/maps/search/?api=1&query={store['lat']},{store['lng']}))\n\n")
        
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

def generate_managua_100_list():
    stores = []
    
    # Tienda real Telcmax solicitada con datos exactos de Managua
    telcmax_store = {
        "nombre": "Telcmax",
        "categoria": "Celulares",
        "descripcion": "Tienda líder de tecnología en Nicaragua. Dispositivos móviles, smartwatches, audio, televisores y accesorios del ecosistema tecnológico con facilidades de pago Banpro Cuotas.",
        "whatsapp": "50583397888",
        "lat": 12.126400,
        "lng": -86.262500,
        "direccion": "Carretera a Masaya, Km 4.5, Costado Norte de Casino Pharaohs, Managua"
    }
    stores.append(telcmax_store)
    
    # We will generate exactly 99 more stores distributed across the 6 zones of Managua
    # using our config and lists of names
    zones = list(ZONES_CONFIG.keys())
    
    random.seed(42) # Ensure reproducible list on multiple runs
    
    phone_prefix = "505881"
    
    # Generate 99 businesses
    for i in range(99):
        # Select zone in round-robin fashion
        zone_name = zones[i % len(zones)]
        zone_conf = ZONES_CONFIG[zone_name]
        
        # Select category based on what categories usually appear in this zone
        # Altamira/Villa Fontana: high end, Comida, Celulares, Deportes
        # Oriental: Ropa, Calzado, Super, Repuestos, Ferreteria
        # Linda Vista/BH: mixed
        if zone_name in ["Altamira", "Villa Fontana"]:
            categories_pool = ["Comida", "Ropa", "Celulares", "Deportes", "Farmacia"]
        elif zone_name == "Mercado Oriental":
            categories_pool = ["Ropa", "Calzado", "Super", "Repuestos", "Ferretería"]
        elif zone_name == "Carretera Norte":
            categories_pool = ["Repuestos", "Ferretería", "Super"]
        else: # Bello Horizonte, Linda Vista
            categories_pool = ["Comida", "Ropa", "Celulares", "Farmacia", "Ferretería", "Calzado", "Repuestos"]
            
        category = random.choice(categories_pool)
        
        # Pull typical business name
        names_pool = BUSINESS_NAMES_DATA[category]
        base_name = names_pool[i % len(names_pool)]
        
        # Add index modifier to avoid duplicate store names
        suffix = ""
        if (i // len(names_pool)) > 0:
            suffix = f" {zone_name}"
            
        store_name = f"{base_name}{suffix}"
        
        # Generate address and coordinates
        address_template = random.choice(zone_conf["addresses"])
        address = f"{address_template}, Managua"
        
        offset_lat = random.uniform(-0.0012, 0.0012)
        offset_lng = random.uniform(-0.0012, 0.0012)
        
        lat = round(zone_conf["lat"] + offset_lat, 6)
        lng = round(zone_conf["lng"] + offset_lng, 6)
        
        # WhatsApp phone number
        whatsapp = f"{phone_prefix}{i:05d}"
        
        # Description
        desc_templates = {
            "Comida": "Exquisitos platillos locales, asados al carbón y la mejor sazón nicaragüense directo a tu mesa.",
            "Repuestos": "Venta de repuestos mecánicos, lubricantes de calidad y accesorios automotores multimarca.",
            "Ferretería": "Materiales eléctricos, herramientas manuales, tornillería y artículos de construcción general.",
            "Ropa": "Moda casual y formal para toda la familia, mochilas y accesorios de vestir de última tendencia.",
            "Calzado": "Calzado de cuero nacional e importado, zapatillas deportivas y sandalias cómodas de diario.",
            "Deportes": "Implementos deportivos de alta calidad, balones de fútbol, camisas de equipos y calzado especial.",
            "Farmacia": "Atención profesional, venta de medicamentos genéricos y de marcas líderes, cuidado infantil y de salud.",
            "Celulares": "Venta de accesorios, protectores de pantalla, cargadores rápidos y repuestos técnicos para celulares.",
            "Super": "Abarrotes generales, granos básicos, productos de limpieza y todo para el abastecimiento familiar."
        }
        descripcion = f"{desc_templates[category]} Calidad y servicio garantizado en {zone_name}, Managua."
        
        stores.append({
            "nombre": store_name,
            "categoria": category,
            "descripcion": descripcion,
            "whatsapp": whatsapp,
            "lat": lat,
            "lng": lng,
            "direccion": address
        })
        
    return stores

def main():
    args = parse_args()
    base_url = args.url.rstrip('/')
    front_url = args.front_url.rstrip('/')
    
    print("=" * 70)
    print("      🚀 INICIANDO SIEMBRA MASIVA - 100 TIENDAS MANAGUA x 50 PRODUCTOS 🚀")
    print("=" * 70)
    print(f"[*] Backend: {base_url}")
    print(f"[*] Frontend: {front_url}")
    
    # Generate the 100 businesses
    managua_stores = generate_managua_100_list()
    print(f"[*] Total Tiendas Managua a crear: {len(managua_stores)}")
    print(f"[*] Total Productos a crear: {len(managua_stores) * 50} (5,000 en total)\n")
    
    # Create output directory for individual md credentials
    outdir = "credenciales_outreach_managua"
    os.makedirs(outdir, exist_ok=True)
    print(f"[*] Directorio de credenciales individuales creado: {outdir}/")
    
    categories = fetch_categories(base_url)
    
    results = []
    
    for idx, store in enumerate(managua_stores):
        store_num = idx + 1
        print("-" * 60)
        print(f"[{store_num}/{len(managua_stores)}] Tienda Managua: {store['nombre']} ({store['categoria']})")
        
        clean_name = re.sub(r'[^a-zA-Z0-9]', '', store['nombre']).lower()
        username = f"owner_mga_{clean_name}"
        password = f"MGA{store_num:03d}2026!"
        email = f"{username}@buscaya.app"
        
        print(f"  [*] Registrando/Logueando usuario '{username}'...")
        token, user_id = register_user(base_url, username, password, f"Dueño de {store['nombre']}", email, store['whatsapp'])
        
        if not token:
            print(f"  [!] Fallo de autenticación para {store['nombre']}. Omitiendo.")
            continue
            
        print(f"  [+] Usuario OK (ID: {user_id}). Creando tienda...")
        tienda_id = create_store(base_url, token, store)
        
        if not tienda_id:
            print(f"  [!] Fallo al crear/obtener tienda. Omitiendo.")
            continue
            
        print(f"  [+] Tienda creada con ID: {tienda_id}")
        
        # Clean existing products to make it idempotent and avoid double population
        clear_store_products(base_url, token)
        
        cat_key = store["categoria"].lower()
        # Map to database category IDs
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
        
        # Save individual store md file
        save_store_markdown(outdir, store, tienda_id, username, password, f"{front_url}/tienda/{tienda_id}", products)
        print(f"  [+] Archivo de credenciales individuales creado: {outdir}/{clean_name}.md")
        
        results.append({
            "nombre": store["nombre"],
            "whatsapp": store["whatsapp"],
            "tienda_id": tienda_id,
            "username": username,
            "password": password,
            "live_url": f"{front_url}/tienda/{tienda_id}",
            "productos_count": inserted,
            "direccion": store["direccion"],
            "clean_name": clean_name
        })

    generate_report(args.output, results, front_url)

def generate_report(output_file, results, front_url):
    print("\n" + "=" * 60)
    print(f"[*] Generando Reporte de Outreach de Managua en {output_file}...")
    print("=" * 60)
    
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# 🚀 Reporte de Outreach y Siembra de Negocios en Managua\n\n")
        total_p = sum(r["productos_count"] for r in results)
        f.write(f"Se ha completado la carga masiva de **{len(results)} tiendas** en Managua, sumando un total de **{total_p} productos**.\n\n")
        
        f.write(f"**Resumen Técnico:**\n")
        f.write(f"- **Total Comercios Creados:** {len(results)}\n")
        f.write(f"- **Total Productos Sembrados:** {total_p} items\n")
        f.write(f"- **Ubicación:** Zonas Comerciales de Managua (Altamira, Bello Horizonte, Mercado Oriental, Linda Vista, Carretera Norte, Villa Fontana)\n")
        f.write(f"- **Fichas Individuales:** Almacenadas en la carpeta `credenciales_outreach_managua/` para entrega ordenada.\n\n")
        
        f.write("## 🏪 Directorio de Comercios Digitalizados (Managua)\n\n")
        f.write("| # | Negocio | Dirección | Productos | ID Tienda | Enlace de Prueba |\n")
        f.write("| --- | --- | --- | :---: | :---: | --- |\n")
        for idx, r in enumerate(results):
            f.write(f"| {idx+1} | **{r['nombre']}** | {r['direccion']} | {r['productos_count']} | `{r['tienda_id']}` | [Ver Tienda]({r['live_url']}) |\n")
            
        f.write("\n---\n\n")
        f.write("## 🔑 Credenciales para Entrega Administrativa\n")
        f.write("Cuando contactes a un negocio y desees cederle el control de su inventario, facilítale estos accesos:\n\n")
        
        for r in results:
            f.write(f"### 🏪 {r['nombre']}\n")
            f.write(f"- **Usuario:** `{r['username']}`\n")
            f.write(f"- **Contraseña Temporal:** `{r['password']}`\n")
            f.write(f"- **Ficha Completa:** [Ver Ficha](./credenciales_outreach_managua/{r['clean_name']}.md)\n")
            f.write(f"- **Enlace en BuscaYa:** [{r['live_url']}]({r['live_url']})\n\n")
            
        f.write("---\n\n")
        f.write("## 💬 Enlaces Rápidos de Prospección por WhatsApp (Managua)\n")
        f.write("Haz clic en los enlaces para abrir WhatsApp con un mensaje personalizado:\n\n")
        
        for r in results:
            message = (
                f"Hola, un gusto saludarles. 👋 Soy del equipo de BuscaYa de Cowib.\n\n"
                f"Hemos lanzado la plataforma BuscaYa (buscaya.encuentrame.org) para digitalizar e impulsar los comercios de Managua.\n\n"
                f"Viendo el éxito de su negocio, nos tomamos la libertad de crearles un demo gratuito con un catálogo inicial para que vea cómo sus clientes pueden buscar sus productos y ordenarles directo a su WhatsApp:\n\n"
                f"👉 {r['live_url']}\n\n"
                f"Les entregamos el acceso administrativo de forma 100% gratuita y sin compromisos para que puedan actualizar precios, subir fotos o agregar nuevos productos. ¿Les interesaría coordinar una llamada breve para explicarles cómo funciona?"
            )
            
            encoded_message = urllib.parse.quote(message)
            whatsapp_link = f"https://wa.me/{r['whatsapp']}?text={encoded_message}"
            
            f.write(f"### 📨 Outreach: **{r['nombre']}**\n")
            f.write(f"- **Contacto:** `+{r['whatsapp']}`\n")
            f.write(f"- **👉 [Enviar Mensaje por WhatsApp]({whatsapp_link})**\n\n")
            
    print(f"[+] Reporte completado con éxito en '{output_file}'.")

if __name__ == "__main__":
    main()
