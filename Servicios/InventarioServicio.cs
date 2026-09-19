using System;
using Microsoft.Data.Sqlite;

namespace CafeteriaInventario.Servicios
{
    public class InventarioServicio
    {
        private readonly BaseDatosServicio _bd;

        public InventarioServicio()
        {
            _bd = new BaseDatosServicio();
        }

        public void ListarProductos()
        {
            Console.WriteLine("\n--- INVENTARIO ACTUAL ---");
            Console.WriteLine("{0,-8} {1,-25} {2,-10} {3,-10} {4,-10}", "SKU", "Nombre", "Precio", "Stock", "Alerta");
            Console.WriteLine(new string('-', 68));

            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT p.sku, p.nombre, p.precio_base, i.cantidad_disponible, i.stock_minimo 
                    FROM productos p
                    INNER JOIN inventario i ON p.id = i.producto_id;
                ";

                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sku = reader.GetString(0);
                        string nombre = reader.GetString(1);
                        decimal precio = reader.GetDecimal(2);
                        decimal stock = reader.GetDecimal(3);
                        decimal minimo = reader.GetDecimal(4);
                        string alerta = stock <= minimo ? "[BAJO STOCK]" : "OK";

                        Console.WriteLine("{0,-8} {1,-25} ${2,-9:F2} {3,-10} {4,-10}", sku, nombre, precio, stock, alerta);
                    }
                }
            }
        }

        public bool RegistrarVenta(string sku, decimal cantidad)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string checkQuery = @"
                    SELECT p.id, p.nombre, p.precio_base, i.cantidad_disponible 
                    FROM productos p
                    INNER JOIN inventario i ON p.id = i.producto_id
                    WHERE p.sku = @sku;
                ";

                long prodId = 0;
                string nombre = "";
                decimal precio = 0;
                decimal stock = 0;

                using (var checkCmd = new SqliteCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@sku", sku);
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine("Error: Producto no encontrado.");
                            return false;
                        }
                        prodId = reader.GetInt64(0);
                        nombre = reader.GetString(1);
                        precio = reader.GetDecimal(2);
                        stock = reader.GetDecimal(3);
                    }
                }

                if (stock < cantidad)
                {
                    Console.WriteLine($"Error: Stock insuficiente. Solo hay {stock} disponible(s).");
                    return false;
                }

                // Descontar stock
                string updateQuery = "UPDATE inventario SET cantidad_disponible = cantidad_disponible - @cant WHERE producto_id = @id;";
                using (var updateCmd = new SqliteCommand(updateQuery, con))
                {
                    updateCmd.Parameters.AddWithValue("@cant", cantidad);
                    updateCmd.Parameters.AddWithValue("@id", prodId);
                    updateCmd.ExecuteNonQuery();
                }

                // Registrar auditoría en tabla movimientos
                RegistrarMovimiento(con, sku, "Venta", cantidad, "Venta en caja");

                Console.WriteLine($"Venta registrada: {cantidad}x {nombre}. Total: ${(precio * cantidad):F2}");
                return true;
            }
        }

        public bool ReabastecerStock(string sku, decimal cantidad)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string updateQuery = @"
                    UPDATE inventario 
                    SET cantidad_disponible = cantidad_disponible + @cant 
                    WHERE producto_id = (SELECT id FROM productos WHERE sku = @sku);
                ";

                using (var cmd = new SqliteCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@cant", cantidad);
                    cmd.Parameters.AddWithValue("@sku", sku);
                    int filas = cmd.ExecuteNonQuery();

                    if (filas > 0)
                    {
                        RegistrarMovimiento(con, sku, "Entrada", cantidad, "Reabastecimiento de proveedor");
                        Console.WriteLine("Stock actualizado exitosamente.");
                        return true;
                    }
                }
            }

            Console.WriteLine("Error: Producto no encontrado.");
            return false;
        }

        private void RegistrarMovimiento(SqliteConnection con, string sku, string tipo, decimal cantidad, string motivo)
        {
            string query = @"
                INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha) 
                VALUES (@sku, @tipo, @cantidad, @motivo, @fecha);
            ";
            using (var cmd = new SqliteCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@sku", sku);
                cmd.Parameters.AddWithValue("@tipo", tipo);
                cmd.Parameters.AddWithValue("@cantidad", cantidad);
                cmd.Parameters.AddWithValue("@motivo", motivo);
                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
        }

        public void VerHistorialMovimientos()
        {
            Console.WriteLine("\n--- BITACORA DE MOVIMIENTOS ---");
            Console.WriteLine("{0,-20} {1,-10} {2,-10} {3,-10} {4,-25}", "Fecha", "SKU", "Tipo", "Cantidad", "Motivo");
            Console.WriteLine(new string('-', 78));

            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT fecha, producto_sku, tipo, cantidad, motivo FROM movimientos ORDER BY id DESC LIMIT 10;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("{0,-20} {1,-10} {2,-10} {3,-10} {4,-25}",
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetDecimal(3),
                            reader.GetString(4));
                    }
                }
            }
        }
    }
}