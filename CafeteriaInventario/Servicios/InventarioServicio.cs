using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CafeteriaInventario.Modelos; // <--- Agregar esta línea

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
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT sku, nombre, precio_base, stock_actual, stock_minimo FROM productos;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- CATÁLOGO DE PRODUCTOS ---");
                    while (reader.Read())
                    {
                        string sku = reader.GetString(0);
                        string nombre = reader.GetString(1);
                        decimal precio = reader.GetDecimal(2);
                        decimal stock = reader.GetDecimal(3);
                        decimal minimo = reader.GetDecimal(4);

                        string alerta = stock <= minimo ? $" [BAJO STOCK - Mín: {minimo}]" : "";
                        Console.WriteLine($"[{sku}] {nombre} - ${precio:F2} | Stock: {stock}{alerta}");
                    }
                }
            }
        }

        public void ListarInsumosBarra()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT nombre, stock_actual, unidad_medida, stock_minimo FROM insumos;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- INSUMOS EN BARRA (PERSISTENTES) ---");
                    while (reader.Read())
                    {
                        string nombre = reader.GetString(0);
                        decimal stock = reader.GetDecimal(1);
                        string unidad = reader.GetString(2);
                        decimal minimo = reader.GetDecimal(3);

                        string alerta = stock <= minimo ? $" [BAJO STOCK - Mín: {minimo}{unidad}]" : "";
                        Console.WriteLine($"- {nombre}: {stock:F1} {unidad} restantes{alerta}");
                    }
                }
            }
        }

        public bool RegistrarVenta(string sku, decimal cantidad)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string queryCheck = "SELECT stock_actual, nombre FROM productos WHERE sku = @sku;";
                decimal stockActual = 0;
                string nombre = "";

                using (var cmd = new SqliteCommand(queryCheck, con))
                {
                    cmd.Parameters.AddWithValue("@sku", sku);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stockActual = reader.GetDecimal(0);
                            nombre = reader.GetString(1);
                        }
                        else
                        {
                            Console.WriteLine("Producto no encontrado.");
                            return false;
                        }
                    }
                }

                if (stockActual < cantidad)
                {
                    Console.WriteLine($"Stock insuficiente. Solo quedan {stockActual} piezas.");
                    return false;
                }

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string update = "UPDATE productos SET stock_actual = stock_actual - @cant WHERE sku = @sku;";
                        using (var cmd = new SqliteCommand(update, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@cant", cantidad);
                            cmd.Parameters.AddWithValue("@sku", sku);
                            cmd.ExecuteNonQuery();
                        }

                        string mov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Venta', @cant, 'Venta de mostrador', datetime('now'), 'Abierto');
                        ";
                        using (var cmd = new SqliteCommand(mov, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@sku", sku);
                            cmd.Parameters.AddWithValue("@cant", cantidad);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"Venta realizada: {cantidad} de {nombre}.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("Error al procesar la venta: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public bool VenderProductoElaborado(string sku, decimal porciones)
        {
            using (var con = _bd.ObtenerConexion())
            {
                // 1. Verificar insumos necesarios y validar existencias
                string queryReceta = @"
                    SELECT r.insumo_id, i.nombre, (r.cantidad_requerida * @porciones) AS requerida, i.stock_actual, i.unidad_medida
                    FROM recetas r
                    INNER JOIN insumos i ON r.insumo_id = i.id
                    WHERE r.producto_sku = @sku;
                ";

                using (var cmd = new SqliteCommand(queryReceta, con))
                {
                    cmd.Parameters.AddWithValue("@porciones", porciones);
                    cmd.Parameters.AddWithValue("@sku", sku);

                    using (var reader = cmd.ExecuteReader())
                    {
                        bool hayReceta = false;
                        while (reader.Read())
                        {
                            hayReceta = true;
                            string nombreInsumo = reader.GetString(1);
                            decimal requerida = reader.GetDecimal(2);
                            decimal stock = reader.GetDecimal(3);
                            string unidad = reader.GetString(4);

                            if (stock < requerida)
                            {
                                Console.WriteLine($"Faltan insumos: {nombreInsumo} requiere {requerida}{unidad}, pero solo hay {stock}{unidad}.");
                                return false;
                            }
                        }

                        if (!hayReceta)
                        {
                            Console.WriteLine("Este producto no tiene una receta registrada.");
                            return false;
                        }
                    }
                }

                // 2. Si hay suficiente materia prima, descontamos de la tabla insumos y auditamos
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string updateInsumo = @"
                            UPDATE insumos 
                            SET stock_actual = stock_actual - (
                                SELECT r.cantidad_requerida * @porciones 
                                FROM recetas r 
                                WHERE r.insumo_id = insumos.id AND r.producto_sku = @sku
                            )
                            WHERE id IN (SELECT insumo_id FROM recetas WHERE producto_sku = @sku);
                        ";

                        using (var cmd = new SqliteCommand(updateInsumo, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@porciones", porciones);
                            cmd.Parameters.AddWithValue("@sku", sku);
                            cmd.ExecuteNonQuery();
                        }

                        string mov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Venta', @cant, 'Venta de producto elaborado', datetime('now'), 'Abierto');
                        ";
                        using (var cmd = new SqliteCommand(mov, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@sku", sku);
                            cmd.Parameters.AddWithValue("@cant", porciones);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"Venta completada: {porciones} porcion(es) preparada(s) y descontada(s) de barra.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("Error al procesar la receta: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public bool ReabastecerStock(string sku, decimal cantidad)
        {
            using (var con = _bd.ObtenerConexion())
            {
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string update = "UPDATE productos SET stock_actual = stock_actual + @cant WHERE sku = @sku;";
                        using (var cmd = new SqliteCommand(update, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@cant", cantidad);
                            cmd.Parameters.AddWithValue("@sku", sku);
                            int filas = cmd.ExecuteNonQuery();
                            if (filas == 0)
                            {
                                Console.WriteLine("SKU no encontrado.");
                                tx.Rollback();
                                return false;
                            }
                        }

                        string mov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Entrada', @cant, 'Reabastecimiento de mostrador', datetime('now'), 'Abierto');
                        ";
                        using (var cmd = new SqliteCommand(mov, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@sku", sku);
                            cmd.Parameters.AddWithValue("@cant", cantidad);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"Stock reabastecido (+{cantidad}).");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("Error al reabastecer: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public void VerHistorialMovimientos()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT producto_sku, tipo, cantidad, motivo, fecha, estado FROM movimientos ORDER BY id DESC LIMIT 10;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- ÚLTIMOS 10 MOVIMIENTOS EN BITÁCORA ---");
                    while (reader.Read())
                    {
                        Console.WriteLine($"[{reader.GetString(4)}] SKU: {reader.GetString(0)} | {reader.GetString(1)}: {reader.GetDecimal(2)} | Motivo: {reader.GetString(3)} | Turno: {reader.GetString(5)}");
                    }
                }
            }
        }
        // Búsqueda polimórfica: SKU exacto, código numérico o coincidencia parcial de nombre
        public ProductoTerminado? BuscarProductoUniversal(string criterio)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT id, sku, nombre, precio_base, costo_precio, stock_actual, stock_minimo 
                    FROM productos 
                    WHERE sku = @criterio OR nombre LIKE @nombreLike COLLATE NOCASE;
                ";

                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@criterio", criterio.Trim());
                    cmd.Parameters.AddWithValue("@nombreLike", $"%{criterio.Trim()}%");

                    var coincidencias = new System.Collections.Generic.List<ProductoTerminado>();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                    {
                        long id = reader.GetInt64(0);
                        string sku = reader.GetString(1);
                        string nombre = reader.GetString(2);
                        decimal precio = reader.GetDecimal(3);
                        decimal costo = reader.GetDecimal(4);
                        decimal stock = reader.GetDecimal(5);
                        decimal minimo = reader.GetDecimal(6);

                        // Se envían los 6 parámetros requeridos por el constructor
                        var prod = new ProductoTerminado(id, sku, nombre, precio, costo, stock);
                        prod.Existencias = stock;
                        prod.StockMinimo = minimo;
                        coincidencias.Add(prod);
                    }

                    if (coincidencias.Count == 0)
                    {
                        Console.WriteLine("-> No se encontraron productos con ese criterio.");
                        return null;
                    }

                    if (coincidencias.Count == 1)
                    {
                        return coincidencias[0];
                    }

                    Console.WriteLine("\nCoincidencias encontradas:");
                    for (int i = 0; i < coincidencias.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. [{coincidencias[i].Sku}] {coincidencias[i].Nombre} - ${coincidencias[i].Precio:F2} (Stock: {coincidencias[i].Existencias})");
                    }

                    Console.Write("Seleccione el numero del producto deseado: ");
                    if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion >= 1 && seleccion <= coincidencias.Count)
                    {
                        return coincidencias[seleccion - 1];
                    }

                    Console.WriteLine("Seleccion invalida.");
                    return null;
                }
            }
        }    
    }
// Alta dinámica de producto directo de venta
        public bool RegistrarNuevoProducto(string sku, string nombre, decimal precio, decimal costo, decimal stockInicial, decimal stockMinimo)
        {
            using (var con = _bd.ObtenerConexion())
            {
                // Validar si el SKU ya existe
                string checkQuery = "SELECT COUNT(*) FROM productos WHERE sku = @sku;";
                using (var checkCmd = new SqliteCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@sku", sku.Trim());
                    long existe = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                    if (existe > 0)
                    {
                        Console.WriteLine($"-> Error: El SKU '{sku}' ya se encuentra registrado.");
                        return false;
                    }
                }

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string queryInsert = @"
                            INSERT INTO productos (sku, nombre, precio_base, costo_precio, stock_actual, stock_minimo)
                            VALUES (@sku, @nombre, @precio, @costo, @stock, @minimo);
                        ";

                        using (var cmd = new SqliteCommand(queryInsert, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@sku", sku.Trim());
                            cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                            cmd.Parameters.AddWithValue("@precio", precio);
                            cmd.Parameters.AddWithValue("@costo", costo);
                            cmd.Parameters.AddWithValue("@stock", stockInicial);
                            cmd.Parameters.AddWithValue("@minimo", stockMinimo);
                            cmd.ExecuteNonQuery();
                        }

                        if (stockInicial > 0)
                        {
                            string movInsert = @"
                                INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                                VALUES (@sku, 'Entrada', @stock, 'Inventario inicial de registro', datetime('now'), 'Abierto');
                            ";
                            using (var movCmd = new SqliteCommand(movInsert, con, tx))
                            {
                                movCmd.Parameters.AddWithValue("@sku", sku.Trim());
                                movCmd.Parameters.AddWithValue("@stock", stockInicial);
                                movCmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        Console.WriteLine($"-> Producto '{nombre}' registrado exitosamente con alerta en stock <= {stockMinimo}.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("-> Error al guardar producto: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        // Alta dinámica de insumos para recetas de barra
        public bool RegistrarNuevoInsumo(string nombre, string unidadMedida, decimal stockInicial, decimal stockMinimo)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string queryInsert = @"
                    INSERT INTO insumos (nombre, unidad_medida, stock_actual, stock_minimo)
                    VALUES (@nombre, @unidad, @stock, @minimo);
                ";

                using (var cmd = new SqliteCommand(queryInsert, con))
                {
                    cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@unidad", unidadMedida.Trim());
                    cmd.Parameters.AddWithValue("@stock", stockInicial);
                    cmd.Parameters.AddWithValue("@minimo", stockMinimo);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"-> Insumo '{nombre}' ({unidadMedida}) registrado con exito.");
                return true;
            }
        }
// Eliminación de producto con validación de dependencias
        public bool EliminarProducto(string sku)
        {
            using (var con = _bd.ObtenerConexion())
            {
                // 1. Verificar si el producto existe
                string checkQuery = "SELECT nombre FROM productos WHERE sku = @sku;";
                string nombre = "";

                using (var checkCmd = new SqliteCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@sku", sku.Trim());
                    var result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        Console.WriteLine("-> Error: El producto no existe.");
                        return false;
                    }
                    nombre = result.ToString() ?? "";
                }

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        // 2. Si está en alguna receta como producto elaborado, limpiar su receta
                        string delRecetas = "DELETE FROM recetas WHERE producto_sku = @sku;";
                        using (var cmdRecetas = new SqliteCommand(delRecetas, con, tx))
                        {
                            cmdRecetas.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdRecetas.ExecuteNonQuery();
                        }

                        // 3. Eliminar el producto de la tabla principal
                        string queryDelete = "DELETE FROM productos WHERE sku = @sku;";
                        using (var cmdDel = new SqliteCommand(queryDelete, con, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdDel.ExecuteNonQuery();
                        }

                        // 4. Registrar en la bitácora la baja administrativa
                        string movBaja = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Baja', 0, 'Eliminación manual de catálogo', datetime('now'), 'Abierto');
                        ";
                        using (var cmdMov = new SqliteCommand(movBaja, con, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdMov.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"-> Producto '{nombre}' [{sku}] eliminado con éxito.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("-> Error al eliminar producto: " + ex.Message);
                        return false;
                    }
                }
            }
        }
}
}