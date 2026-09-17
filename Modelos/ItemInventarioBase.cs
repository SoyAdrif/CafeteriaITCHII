using System;

namespace CafeteriaInventario.Modelos
{
    // Clase abstracta: no se puede instanciar directamente, solo heredar de ella
    public abstract class ItemInventarioBase
    {
        public long Id { get; set; }
        public string Sku { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }

        protected ItemInventarioBase(long id, string sku, string nombre, decimal precio, decimal costo)
        {
            Id = id;
            Sku = sku;
            Nombre = nombre;
            Precio = precio;
            Costo = costo;
        }

        // Método abstracto que obliga a cada clase hija a implementar su forma de descontar existencias
        public abstract bool DescontarExistencias(decimal cantidad);
    }
}