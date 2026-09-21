namespace CafeteriaInventario.Modelos
{
    public abstract class ItemInventarioBase
    {
        public long Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public decimal Existencias { get; set; }
        public decimal StockMinimo { get; set; } = 5;

        // Constructor por defecto
        public ItemInventarioBase() { }

        // Constructor con 5 argumentos (con Id como long)
        public ItemInventarioBase(long id, string sku, string nombre, decimal precio, decimal costo)
        {
            Id = id;
            Sku = sku;
            Nombre = nombre;
            Precio = precio;
            Costo = costo;
            Existencias = 0;
            StockMinimo = 5;
        }

        public bool TieneBajoStock => Existencias <= StockMinimo;

        public abstract bool DescontarExistencias(decimal cantidad);
    }
}