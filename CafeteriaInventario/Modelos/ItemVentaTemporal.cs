namespace CafeteriaInventario.Modelos
{
    public class ItemVentaTemporal
    {
        public ProductoTerminado Producto { get; set; }
        public decimal Cantidad { get; set; }
        public bool EsReceta { get; set; }
        public decimal Subtotal => Producto.Precio * Cantidad;

        public ItemVentaTemporal(ProductoTerminado producto, decimal cantidad, bool esReceta)
        {
            Producto = producto;
            Cantidad = cantidad;
            EsReceta = esReceta;
        }
    }
}