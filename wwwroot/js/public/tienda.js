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

        products: config.products || [],

        init() {
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
