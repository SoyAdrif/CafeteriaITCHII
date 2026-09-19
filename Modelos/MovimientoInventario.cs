using System;

namespace CafeteriaInventario.Modelos
{
    public class MovimientoInventario
    {
        public long Id { get; set; }
        public long SucursalId { get; set; }
        public long ItemInventarioId { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaHora { get; set; }

        public MovimientoInventario(long id, long sucursalId, long itemInventarioId, TipoMovimiento tipo, decimal cantidad, string motivo)
        {
            Id = id;
            SucursalId = sucursalId;
            ItemInventarioId = itemInventarioId;
            Tipo = tipo;
            Cantidad = cantidad;
            Motivo = motivo;
            FechaHora = DateTime.Now;
        }
    }
}