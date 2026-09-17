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
            Console.WriteLine("\n--- INVENTARIO ACTUAL (Base de Datos Local: SQLite) ---");
            Console.WriteLine("{0,-8} {1,-25} {2,-10} {3,-10}", "SKU", "Nombre", "Precio", "Stock");
            Console.WriteLine(new string('-', 55));

            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT p.sku, p.nombre, p.precio_base, i.cantidad_disponible 
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

                        Console.WriteLine("{0,-8} {1,-25} ${2,-9:F2} {3,-10}", sku, nombre, precio, stock);
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

                string updateQuery = "UPDATE inventario SET cantidad_disponible = cantidad_disponible - @cant WHERE producto_id = @id;";
                using (var updateCmd = new SqliteCommand(updateQuery, con))
                {
                    updateCmd.Parameters.AddWithValue("@cant", cantidad);
                    updateCmd.Parameters.AddWithValue("@id", prodId);
                    updateCmd.ExecuteNonQuery();
                }

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
                        Console.WriteLine("Stock actualizado exitosamente en la base de datos local.");
                        return true;
                    }
                }
            }

            Console.WriteLine("Error: Producto no encontrado.");
            return false;
        }
    }
}