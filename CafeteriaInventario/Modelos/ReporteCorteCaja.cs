using System;

namespace CafeteriaInventario.Modelos
{
    public class ReporteCorteCaja
    {
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public int TotalTransaccionesVenta { get; set; }
        public decimal TotalUnidadesVendidas { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalGananciaEstimada { get; set; }
    }
}