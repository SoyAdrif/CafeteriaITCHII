using System;

namespace CafeteriaInventario.Modelos
{
    // Hereda de ItemInventarioBase y gestiona stock directo por unidades
    public class ProductoTerminado : ItemInventarioBase
    {
        public decimal StockDisponible { get; private set; }

        public ProductoTerminado(long id, string sku, string nombre, decimal precio, decimal costo, decimal stockInicial)
            : base(id, sku, nombre, precio, costo)
        {
            StockDisponible = stockInicial;
        }

        // Sobrescritura (overriding) de la lógica base
        public override bool DescontarExistencias(decimal cantidad)
        {
            if (cantidad <= 0 || StockDisponible < cantidad)
            {
                return false;
            }

            StockDisponible -= cantidad;
            return true;
        }

        public void AgregarStock(decimal cantidad)
        {
            if (cantidad > 0) StockDisponible += cantidad;
        }
    }
}