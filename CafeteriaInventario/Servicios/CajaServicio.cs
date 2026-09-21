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
// Genera el ranking de productos más vendidos en el turno actual
        public void MostrarTopProductosVendidos(int top = 3)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT m.producto_sku, p.nombre, SUM(m.cantidad) AS total_unidades
                    FROM movimientos m
                    JOIN productos p ON m.producto_sku = p.sku
                    WHERE m.tipo = 'Salida' AND m.estado = 'Abierto'
                    GROUP BY m.producto_sku, p.nombre
                    ORDER BY total_unidades DESC
                    LIMIT @top;
                ";

                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("\n=================================");
                        Console.WriteLine($"    TOP {top} PRODUCTOS MÁS VENDIDOS (TURNO)");
                        Console.WriteLine("=================================");

                        int posicion = 1;
                        bool hayDatos = false;
                        while (reader.Read())
                        {
                            hayDatos = true;
                            string sku = reader.GetString(0);
                            string nombre = reader.GetString(1);
                            decimal total = reader.GetDecimal(2);

                            Console.WriteLine($"{posicion}°. [{sku}] {nombre} - {total:F0} unidades despachadas");
                            posicion++;
                        }

                        if (!hayDatos)
                        {
                            Console.WriteLine("-> No se han registrado ventas en el turno actual.");
                        }

                        Console.WriteLine("=================================");
                    }
                }
            }
        }

        // Resumen de bajas o mermas administrativas del turno
        public void MostrarResumenBajasTurno()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT producto_sku, motivo, fecha
                    FROM movimientos
                    WHERE tipo = 'Baja' AND estado = 'Abierto'
                    ORDER BY id DESC;
                ";

                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n=================================");
                    Console.WriteLine("    RESUMEN DE BAJAS Y MERMAS (TURNO)");
                    Console.WriteLine("=================================");

                    bool hayBajas = false;
                    while (reader.Read())
                    {
                        hayBajas = true;
                        string sku = reader.GetString(0);
                        string motivo = reader.GetString(1);
                        string fecha = reader.GetString(2);

                        Console.WriteLine($"[{fecha}] SKU: {sku} | {motivo}");
                    }

                    if (!hayBajas)
                    {
                        Console.WriteLine("-> No hay bajas administrativas registradas en el turno actual.");
                    }

                    Console.WriteLine("=================================");
                }
            }
        }        
    }
}