function productoDetail(config) {
    return {
        storeId: config.storeId,
        prodId: config.prodId,
        prodName: config.prodName,
        prodPrice: parseFloat(config.prodPrice) || 0,
        prodMoneda: config.prodMoneda || 'C$',
        prodFoto: config.prodFoto,
        imagenes: config.imagenes || [],
        activeImage: config.activeImage || config.prodFoto,
        qtySelected: 1,
        toastShow: false,

        init() {
            setTimeout(() => {
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }, 100);
        },

        agregarAlCarrito() {
            const cartKey = `buscaya_cart_${this.storeId}`;
            let cart = [];
            const savedCart = localStorage.getItem(cartKey);
            if (savedCart) {
                try {
                    cart = JSON.parse(savedCart);
                } catch (e) {
                    cart = [];
                }
            }

            const existing = cart.find(item => item.id == this.prodId);
            if (existing) {
                existing.qty += this.qtySelected;
            } else {
                cart.push({
                    id: parseInt(this.prodId),
                    nombre: this.prodName,
                    precio: this.prodPrice,
                    moneda: this.prodMoneda,
                    fotoUrl: this.prodFoto,
                    qty: this.qtySelected
                });
            }

            localStorage.setItem(cartKey, JSON.stringify(cart));

            // Mostrar Toast
            this.toastShow = true;
            setTimeout(() => {
                this.toastShow = false;
            }, 4000);
        }
    }
}
