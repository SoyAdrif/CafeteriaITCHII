using System;

namespace CafeteriaInventario.Modelos
{
    public class ItemInventario
    {
        public long Id { get; set; }
        public long SucursalId { get; set; }
        public long? ProductoId { get; set; }
        public long? IngredienteId { get; set; }
        public decimal CantidadDisponible { get; private set; }
        public decimal CantidadReservada { get; private set; }
        public decimal CostoPromedio { get; set; }

        public ItemInventario(long id, long sucursalId, long? productoId, decimal cantidadInicial, decimal costoPromedio)
        {
            Id = id;
            SucursalId = sucursalId;
            ProductoId = productoId;
            CantidadDisponible = cantidadInicial;
            CostoPromedio = costoPromedio;
        }

        // Métodos de control de existencias protegidos contra stock negativo
        public bool DescontarStock(decimal cantidad)
        {
            if (cantidad <= 0) return false;
            if (CantidadDisponible < cantidad) return false;

            CantidadDisponible -= cantidad;
            return true;
        }

        public void AgregarStock(decimal cantidad)
        {
            if (cantidad > 0)
            {
                CantidadDisponible += cantidad;
            }
        }
    }
}