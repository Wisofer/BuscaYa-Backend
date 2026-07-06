function catalogoStore(config) {
    return {
        whatsappNum: config.whatsappNum,
        storeName: config.storeName,
        storeId: config.storeId,
        searchQuery: '',
        categoriaSeleccionada: 'all',
        cartOpen: false,
        deliveryAddress: '',
        cart: [],
        categorias: [],
        isLoading: true, // Para el Skeleton Loader

        products: config.products || [],

        init() {
            // Simular carga de red para Skeleton (800ms)
            setTimeout(() => {
                this.isLoading = false;
            }, 800);

            // Inicializar iconos
            setTimeout(() => {
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }, 100);

            // Extraer categorías reales del negocio
            const cats = new Set();
            this.products.forEach(p => {
                let cat = p.categoriaNombre || "General";
                p.displayCategory = cat;
                cats.add(cat);
            });
            this.categorias = Array.from(cats);

            // Cargar carrito de localStorage
            const savedCart = localStorage.getItem(`buscaya_cart_${this.storeId}`);
            if (savedCart) {
                try {
                    this.cart = JSON.parse(savedCart);
                } catch (e) {
                    this.cart = [];
                }
            }

            // Escuchar cambios de localStorage para auto-guardar
            this.$watch('cart', value => {
                localStorage.setItem(`buscaya_cart_${this.storeId}`, JSON.stringify(value));
                this.$nextTick(() => {
                    if (typeof lucide !== 'undefined') lucide.createIcons();
                });
            });
        },

        filteredProducts() {
            let items = this.products;

            // Filtrar por categoría
            if (this.categoriaSeleccionada !== 'all') {
                items = items.filter(p => p.displayCategory === this.categoriaSeleccionada);
            }

            // Filtrar por búsqueda
            if (this.searchQuery.trim() !== '') {
                const q = this.searchQuery.toLowerCase();
                items = items.filter(p => p.nombre.toLowerCase().includes(q) || (p.descripcion && p.descripcion.toLowerCase().includes(q)));
            }

            this.$nextTick(() => {
                if (typeof lucide !== 'undefined') lucide.createIcons();
            });
            return items;
        },

        bestSellers() {
            // Simular los más vendidos tomando los primeros 5 productos (como demo)
            return this.products.slice(0, 5);
        },

        dailyOffers() {
            // Tomar productos que tengan un precioAnterior mayor al precio actual (ofertas reales)
            // Si no hay ninguno configurado así, tomamos algunos al azar para la demo
            let ofertas = this.products.filter(p => p.precioAnterior > p.precio);
            if (ofertas.length === 0 && this.products.length > 5) {
                return this.products.slice(5, 8);
            }
            return ofertas;
        },

        animateToCart(event, product) {
            // 1. Añadir al carrito lógicamente
            this.addToCart(product);

            // 2. Ejecutar Animación Física
            const button = event.currentTarget;
            const productCard = button.closest('.product-card-wrapper') || button.parentElement;
            const img = productCard.querySelector('img');
            
            if (!img) return; // Si no hay imagen, no hay animación visual

            // Clonar la imagen
            const clone = img.cloneNode(true);
            const rect = img.getBoundingClientRect();

            // Configurar el clon para que flote encima de todo
            clone.style.position = 'fixed';
            clone.style.top = `${rect.top}px`;
            clone.style.left = `${rect.left}px`;
            clone.style.width = `${rect.width}px`;
            clone.style.height = `${rect.height}px`;
            clone.style.borderRadius = '50%';
            clone.style.objectFit = 'cover';
            clone.style.zIndex = '9999';
            clone.style.transition = 'all 0.6s cubic-bezier(0.175, 0.885, 0.32, 1.275)'; // Efecto elástico
            clone.style.boxShadow = '0 10px 25px rgba(0,0,0,0.2)';
            
            document.body.appendChild(clone);

            // Forzar un reflow para que la transición funcione
            clone.offsetHeight;

            // Encontrar el ícono del carrito de destino (el flotante)
            const cartIcon = document.querySelector('.fixed.bottom-6.right-6 button') || document.querySelector('.fixed.bottom-8 button');
            let targetRect;
            
            if (cartIcon) {
                targetRect = cartIcon.getBoundingClientRect();
            } else {
                // Posición por defecto si el carrito aún no está visible
                targetRect = { top: window.innerHeight - 80, left: window.innerWidth - 80, width: 50, height: 50 };
            }

            // Mover el clon hacia el destino
            clone.style.top = `${targetRect.top + targetRect.height/2 - 20}px`;
            clone.style.left = `${targetRect.left + targetRect.width/2 - 20}px`;
            clone.style.width = '40px';
            clone.style.height = '40px';
            clone.style.opacity = '0.5';
            clone.style.transform = 'scale(0.2)';

            // Destruir el clon después de la animación
            setTimeout(() => {
                clone.remove();
                
                // Efecto "pop" en el icono del carrito
                if (cartIcon) {
                    cartIcon.style.transform = 'scale(1.2)';
                    setTimeout(() => {
                        cartIcon.style.transform = '';
                    }, 200);
                }
            }, 600);
        },

        addToCart(product) {
            const existing = this.cart.find(item => item.id === product.id);
            if (existing) {
                existing.qty++;
            } else {
                this.cart.push({
                    id: product.id,
                    nombre: product.nombre,
                    precio: product.precio,
                    moneda: product.moneda || 'C$',
                    fotoUrl: product.fotoUrl,
                    qty: 1
                });
            }
            this.cartOpen = true;
        },

        updateQuantity(productId, amount) {
            const item = this.cart.find(i => i.id === productId);
            if (item) {
                item.qty += amount;
                if (item.qty <= 0) {
                    this.cart = this.cart.filter(i => i.id !== productId);
                }
            }
        },

        cartTotalCount() {
            return this.cart.reduce((sum, item) => sum + item.qty, 0);
        },

        cartTotalAmount() {
            return this.cart.reduce((sum, item) => sum + (item.precio * item.qty), 0);
        },

        cartCurrency() {
            return this.cart.length > 0 ? this.cart[0].moneda : 'C$';
        },

        sendOrderWhatsApp() {
            if (this.cart.length === 0) return;

            let msg = `*Pedido Nuevo desde tu Catálogo Digital de BuscaYa* 🛍️\n\n`;
            msg += `*Cliente:* Detalle de Pedido sin registro\n`;
            if (this.deliveryAddress.trim() !== '') {
                msg += `*Dirección:* ${this.deliveryAddress.trim()}\n`;
            }
            msg += `-------------------------------------------\n`;

            this.cart.forEach(item => {
                const itemSub = (item.precio * item.qty).toFixed(2);
                msg += `• ${item.qty}x *${item.nombre}* (${item.moneda} ${parseFloat(item.precio).toFixed(2)}) -> ${item.moneda} ${itemSub}\n`;
            });

            msg += `-------------------------------------------\n`;
            msg += `*Total a pagar: ${this.cartCurrency()} ${this.cartTotalAmount().toFixed(2)}*\n\n`;
            msg += `Por favor, confirma mi pedido y el tiempo estimado de entrega. ¡Gracias!`;

            const cleanPhone = this.whatsappNum.replace(/[^0-9]/g, '');
            const encodedMsg = encodeURIComponent(msg);
            const url = `https://wa.me/${cleanPhone}?text=${encodedMsg}`;

            // Limpiar carrito al ordenar
            this.cart = [];
            localStorage.removeItem(`buscaya_cart_${this.storeId}`);
            this.cartOpen = false;

            // Abrir WhatsApp
            window.open(url, '_blank');
        }
    }
}
