using System;
using Microsoft.Data.Sqlite;
using CafeteriaInventario.Modelos;

namespace CafeteriaInventario.Servicios
{
    public class CajaServicio
    {
        private readonly BaseDatosServicio _bd;

        public CajaServicio()
        {
            _bd = new BaseDatosServicio();
        }

        // Consulta únicamente las ventas del turno abierto actual
        public ReporteCorteCaja GenerarCorteTurno()
        {
            var reporte = new ReporteCorteCaja();

            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT 
                        COUNT(m.id) AS total_ventas,
                        COALESCE(SUM(m.cantidad), 0) AS unidades_vendidas,
                        COALESCE(SUM(m.cantidad * p.precio_base), 0) AS ingresos_totales,
                        COALESCE(SUM(m.cantidad * (p.precio_base - p.costo_precio)), 0) AS ganancia_total
                    FROM movimientos m
                    INNER JOIN productos p ON m.producto_sku = p.sku
                    WHERE m.tipo = 'Venta' AND m.estado = 'Abierto';
                ";

                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        reporte.TotalTransaccionesVenta = reader.GetInt32(0);
                        reporte.TotalUnidadesVendidas = reader.GetDecimal(1);
                        reporte.TotalIngresos = reader.GetDecimal(2);
                        reporte.TotalGananciaEstimada = reader.GetDecimal(3);
                    }
                }
            }

            return reporte;
        }

        // Cierra el turno actual: no borra datos, pero resetea el contador del nuevo turno a 0
        public bool CerrarTurnoCaja()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "UPDATE movimientos SET estado = 'Cerrado' WHERE estado = 'Abierto';";
                using (var cmd = new SqliteCommand(query, con))
                {
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas >= 0;
                }
            }
        }
    }
}