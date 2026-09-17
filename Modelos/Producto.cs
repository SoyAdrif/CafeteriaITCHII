using System;

namespace CafeteriaInventario.Modelos
{
    public class Producto
    {
        // Atributos / Propiedades
        public long Id { get; set; }
        public long? CategoriaId { get; set; }
        public string Sku { get; set; }
        public string CodigoBarras { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal CostoPrecio { get; set; }
        public TipoProducto Tipo { get; set; } = TipoProducto.Finished;
        public AreaPreparacion Area { get; set; } = AreaPreparacion.Bar;
        public bool ControlaStock { get; set; } = true;
        public bool Activo { get; set; } = true;

        // Constructor
        public Producto(long id, string sku, string nombre, decimal precioBase, decimal costoPrecio)
        {
            Id = id;
            Sku = sku;
            Nombre = nombre;
            PrecioBase = precioBase;
            CostoPrecio = costoPrecio;
        }

        // Método de negocio (ejemplo de encapsulación de comportamiento)
        public decimal CalcularGanancia()
        {
            return PrecioBase - CostoPrecio;
        }
    }
}