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
                // Unimos con la tabla recetas para saber si el artículo es una preparación
                string query = @"
                    SELECT p.sku, p.nombre, p.precio_base, p.stock_actual, p.stock_minimo,
                           COUNT(r.id) AS total_ingredientes
                    FROM productos p
                    LEFT JOIN recetas r ON p.sku = r.producto_sku
                    GROUP BY p.sku;
                ";

                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n=================================");
                    Console.WriteLine("       INVENTARIO GENERAL        ");
                    Console.WriteLine("=================================");

                    while (reader.Read())
                    {
                        string sku = reader.GetString(0);
                        string nombre = reader.GetString(1);
                        decimal precio = reader.GetDecimal(2);
                        decimal stock = reader.GetDecimal(3);
                        decimal stockMinimo = reader.GetDecimal(4);
                        long totalIngredientes = reader.GetInt64(5);

                        // Si tiene ingredientes vinculados, se cataloga como Receta
                        if (totalIngredientes > 0)
                        {
                            Console.WriteLine($"[{sku}] {nombre} - ${precio:F2} | [Receta / Preparación en barra]");
                        }
                        else
                        {
                            string alertaStock = stock <= stockMinimo 
                                ? $" [BAJO STOCK - Mín: {stockMinimo}]" 
                                : "";

                            Console.WriteLine($"[{sku}] {nombre} - ${precio:F2} | Stock: {stock}{alertaStock}");
                        }
                    }
                    Console.WriteLine("=================================");
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

// Consulta avanzada de bitácora con soporte para filtros de auditoría
        public void VerHistorialMovimientos(string filtro = "RECIENTES", string valorFiltro = "")
        {
            using (var con = _bd.ObtenerConexion())
            {
                string queryBase = "SELECT id, producto_sku, tipo, cantidad, motivo, fecha, estado FROM movimientos ";
                string titulo = "";

                if (filtro == "TURNO_ACTUAL")
                {
                    queryBase += "WHERE estado = 'Abierto' ORDER BY id DESC;";
                    titulo = "--- MOVIMIENTOS DEL TURNO ACTUAL (ESTADO: ABIERTO) ---";
                }
                else if (filtro == "FECHA")
                {
                    queryBase += "WHERE strftime('%Y-%m-%d', fecha) = @fecha ORDER BY id DESC;";
                    titulo = $"--- MOVIMIENTOS REGISTRADOS EN LA FECHA [{valorFiltro}] ---";
                }
                else // RECIENTES
                {
                    queryBase += "ORDER BY id DESC LIMIT 25;";
                    titulo = "--- ÚLTIMOS 25 MOVIMIENTOS REGISTRADOS ---";
                }

                using (var cmd = new SqliteCommand(queryBase, con))
                {
                    if (filtro == "FECHA")
                    {
                        cmd.Parameters.AddWithValue("@fecha", valorFiltro.Trim());
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"\n=================================");
                        Console.WriteLine(titulo);
                        Console.WriteLine("=================================");

                        bool hayRegistros = false;
                        while (reader.Read())
                        {
                            hayRegistros = true;
                            long id = reader.GetInt64(0);
                            string sku = reader.GetString(1);
                            string tipo = reader.GetString(2);
                            decimal cant = reader.GetDecimal(3);
                            string motivo = reader.GetString(4);
                            string fecha = reader.GetString(5);
                            string estado = reader.GetString(6);

                            Console.WriteLine($"#{id} | [{fecha}] | {tipo.ToUpper()} | Cant: {cant} | Ref: {sku} | Turno: {estado}");
                            Console.WriteLine($"   Detalle: {motivo}");
                        }

                        if (!hayRegistros)
                        {
                            Console.WriteLine("-> No se encontraron registros con el criterio seleccionado.");
                        }

                        Console.WriteLine("=================================");
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
// Listar insumos con ID para facilitar la selección al armar recetas
        public void ListarInsumosConId()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT id, nombre, unidad_medida, stock_actual FROM insumos;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n--- CATÁLOGO DE INSUMOS DISPONIBLES ---");
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader.GetInt64(0)} | {reader.GetString(1)} ({reader.GetString(2)}) - Stock actual: {reader.GetDecimal(3):F1}");
                    }
                }
            }
        }

        // Asignar o agregar un ingrediente a la receta de un producto
// Asignar o agregar un ingrediente a la receta de un producto con validación de clave foránea
        public bool AgregarIngredienteAReceta(string sku, long insumoId, decimal cantidadRequerida)
        {
            using (var con = _bd.ObtenerConexion())
            {
                // 1. Validar que el producto exista antes de intentar vincular
                string checkProducto = "SELECT COUNT(*) FROM productos WHERE sku = @sku;";
                using (var cmdProd = new SqliteCommand(checkProducto, con))
                {
                    cmdProd.Parameters.AddWithValue("@sku", sku.Trim());
                    long totalProd = Convert.ToInt64(cmdProd.ExecuteScalar() ?? 0);
                    if (totalProd == 0)
                    {
                        Console.WriteLine($"-> Error: El producto con SKU '{sku}' no existe en el catálogo.");
                        Console.WriteLine("-> Debe registrar el producto primero (opción a) antes de asignarle una receta.");
                        return false;
                    }
                }

                // 2. Validar que el insumo exista
                string checkInsumo = "SELECT COUNT(*) FROM insumos WHERE id = @id;";
                using (var cmdInsumo = new SqliteCommand(checkInsumo, con))
                {
                    cmdInsumo.Parameters.AddWithValue("@id", insumoId);
                    long totalInsumo = Convert.ToInt64(cmdInsumo.ExecuteScalar() ?? 0);
                    if (totalInsumo == 0)
                    {
                        Console.WriteLine($"-> Error: El insumo con ID {insumoId} no existe.");
                        return false;
                    }
                }

                // 3. Inserción protegida en la tabla recetas
                try
                {
                    string queryInsert = @"
                        INSERT INTO recetas (producto_sku, insumo_id, cantidad_requerida)
                        VALUES (@sku, @insumoId, @cant);
                    ";

                    using (var cmd = new SqliteCommand(queryInsert, con))
                    {
                        cmd.Parameters.AddWithValue("@sku", sku.Trim());
                        cmd.Parameters.AddWithValue("@insumoId", insumoId);
                        cmd.Parameters.AddWithValue("@cant", cantidadRequerida);
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("-> Materia prima vinculada exitosamente a la receta.");
                    return true;
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine("-> Error de integridad en base de datos: " + ex.Message);
                    return false;
                }
            }
        }
        // Registrar un producto elaborado (receta) con SKU, nombre, precio y costo
        public bool RegistrarProductoElaborado(string sku, string nombre, decimal precio, decimal costo)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string checkQuery = "SELECT COUNT(*) FROM productos WHERE sku = @sku;";
                using (var checkCmd = new SqliteCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@sku", sku.Trim());
                    long existe = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                    if (existe > 0)
                    {
                        Console.WriteLine($"-> Error: El SKU '{sku}' ya existe.");
                        return false;
                    }
                }

                // Se registra con stock 0 y stock_minimo 0 porque sus existencias dependen de los insumos
                string queryInsert = @"
                    INSERT INTO productos (sku, nombre, precio_base, costo_precio, stock_actual, stock_minimo)
                    VALUES (@sku, @nombre, @precio, @costo, 0, 0);
                ";
                using (var cmd = new SqliteCommand(queryInsert, con))
                {
                    cmd.Parameters.AddWithValue("@sku", sku.Trim());
                    cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@precio", precio);
                    cmd.Parameters.AddWithValue("@costo", costo);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"-> Producto elaborado '{nombre}' registrado. Proceda a agregar sus ingredientes.");
                return true;
            }
        }

        // Saber si un producto tiene receta asociada
        public bool TieneReceta(string sku)
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT COUNT(*) FROM recetas WHERE producto_sku = @sku;";
                using (var cmd = new SqliteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@sku", sku.Trim());
                    return Convert.ToInt64(cmd.ExecuteScalar() ?? 0) > 0;
                }
            }
        }
// Consulta del inventario de materias primas en barra con alerta de stock mínimo
        public void ListarInsumosBarra()
        {
            using (var con = _bd.ObtenerConexion())
            {
                string query = "SELECT id, nombre, unidad_medida, stock_actual, stock_minimo FROM insumos;";
                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n=================================");
                    Console.WriteLine("    EXISTENCIAS EN BARRA (INSUMOS) ");
                    Console.WriteLine("=================================");

                    while (reader.Read())
                    {
                        long id = reader.GetInt64(0);
                        string nombre = reader.GetString(1);
                        string unidad = reader.GetString(2);
                        decimal stock = reader.GetDecimal(3);
                        decimal stockMinimo = reader.GetDecimal(4);

                        string alerta = stock <= stockMinimo 
                            ? $" [BAJO STOCK - Mín: {stockMinimo} {unidad}]" 
                            : "";

                        Console.WriteLine($"ID {id} | {nombre}: {stock:F1} {unidad}{alerta}");
                    }
                    Console.WriteLine("=================================");
                }
            }
        } 
// Reabastecimiento de materias primas en barra con auditoría
        public bool ReabastecerInsumo(long insumoId, decimal cantidad)
        {
            if (cantidad <= 0)
            {
                Console.WriteLine("-> La cantidad a reabastecer debe ser mayor a 0.");
                return false;
            }

            using (var con = _bd.ObtenerConexion())
            {
                // 1. Obtener datos actuales del insumo
                string querySelect = "SELECT nombre, unidad_medida, stock_actual FROM insumos WHERE id = @id;";
                string nombre = "";
                string unidad = "";
                decimal stockActual = 0;

                using (var cmdSelect = new SqliteCommand(querySelect, con))
                {
                    cmdSelect.Parameters.AddWithValue("@id", insumoId);
                    using (var reader = cmdSelect.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine($"-> Error: Insumo con ID {insumoId} no encontrado.");
                            return false;
                        }
                        nombre = reader.GetString(0);
                        unidad = reader.GetString(1);
                        stockActual = reader.GetDecimal(2);
                    }
                }

                // 2. Transacción de actualización y registro
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string queryUpdate = "UPDATE insumos SET stock_actual = stock_actual + @cant WHERE id = @id;";
                        using (var cmdUpdate = new SqliteCommand(queryUpdate, con, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("@cant", cantidad);
                            cmdUpdate.Parameters.AddWithValue("@id", insumoId);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        string movInsert = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Entrada', @cant, @motivo, datetime('now'), 'Abierto');
                        ";
                        using (var cmdMov = new SqliteCommand(movInsert, con, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@sku", $"INS-{insumoId}");
                            cmdMov.Parameters.AddWithValue("@cant", cantidad);
                            cmdMov.Parameters.AddWithValue("@motivo", $"Reabastecimiento de insumo: {nombre}");
                            cmdMov.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"-> Insumo '{nombre}' actualizado: {stockActual:F1} {unidad} -> {(stockActual + cantidad):F1} {unidad}.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("-> Error al reabastecer insumo: " + ex.Message);
                        return false;
                    }
                }
            }
        } 
// Sobrecarga de RegistrarVenta para aplicar porcentaje de descuento (Polimorfismo por Sobrecarga)
        public bool RegistrarVenta(string sku, decimal cantidad, decimal porcentajeDescuento)
        {
            if (porcentajeDescuento < 0 || porcentajeDescuento > 100)
            {
                Console.WriteLine("-> Porcentaje de descuento inválido (debe ser entre 0 y 100).");
                return false;
            }

            using (var con = _bd.ObtenerConexion())
            {
                string queryProd = "SELECT nombre, precio_base, stock_actual FROM productos WHERE sku = @sku;";
                string nombre = "";
                decimal precioBase = 0;
                decimal stock = 0;

                using (var cmd = new SqliteCommand(queryProd, con))
                {
                    cmd.Parameters.AddWithValue("@sku", sku.Trim());
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine("-> Producto no encontrado.");
                            return false;
                        }
                        nombre = reader.GetString(0);
                        precioBase = reader.GetDecimal(1);
                        stock = reader.GetDecimal(2);
                    }
                }

                if (stock < cantidad)
                {
                    Console.WriteLine($"-> Stock insuficiente para '{nombre}'. Disponible: {stock}.");
                    return false;
                }

                decimal factor = 1 - (porcentajeDescuento / 100m);
                decimal precioFinal = precioBase * factor;
                decimal subtotal = precioFinal * cantidad;

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string updateStock = "UPDATE productos SET stock_actual = stock_actual - @cant WHERE sku = @sku;";
                        using (var cmdUp = new SqliteCommand(updateStock, con, tx))
                        {
                            cmdUp.Parameters.AddWithValue("@cant", cantidad);
                            cmdUp.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdUp.ExecuteNonQuery();
                        }

                        string insertMov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Salida', @cant, @motivo, datetime('now'), 'Abierto');
                        ";
                        using (var cmdMov = new SqliteCommand(insertMov, con, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdMov.Parameters.AddWithValue("@cant", cantidad);
                            cmdMov.Parameters.AddWithValue("@motivo", $"Venta con promo ({porcentajeDescuento:F0}% desc.) - Subtotal: ${subtotal:F2}");
                            cmdMov.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"-> Venta procesada con {porcentajeDescuento:F0}% desc: {cantidad}x '{nombre}' | Total cobrado: ${subtotal:F2}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("-> Error en transacción: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        // Sobrecarga de VenderProductoElaborado para preparaciones con descuento
        public bool VenderProductoElaborado(string sku, decimal porciones, decimal porcentajeDescuento)
        {
            if (porcentajeDescuento < 0 || porcentajeDescuento > 100)
            {
                Console.WriteLine("-> Descuento inválido.");
                return false;
            }

            using (var con = _bd.ObtenerConexion())
            {
                string infoProd = "SELECT nombre, precio_base FROM productos WHERE sku = @sku;";
                string nombreProd = "";
                decimal precioBase = 0;

                using (var cmdP = new SqliteCommand(infoProd, con))
                {
                    cmdP.Parameters.AddWithValue("@sku", sku.Trim());
                    using (var reader = cmdP.ExecuteReader())
                    {
                        if (!reader.Read()) return false;
                        nombreProd = reader.GetString(0);
                        precioBase = reader.GetDecimal(1);
                    }
                }

                // Obtener ingredientes y verificar existencias
                string queryReceta = @"
                    SELECT r.insumo_id, i.nombre, r.cantidad_requerida, i.stock_actual, i.unidad_medida
                    FROM recetas r
                    JOIN insumos i ON r.insumo_id = i.id
                    WHERE r.producto_sku = @sku;
                ";

                var lista = new List<(long id, string nombre, decimal dosis, decimal stock, string unidad)>();

                using (var cmdRec = new SqliteCommand(queryReceta, con))
                {
                    cmdRec.Parameters.AddWithValue("@sku", sku.Trim());
                    using (var reader = cmdRec.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add((
                                reader.GetInt64(0),
                                reader.GetString(1),
                                reader.GetDecimal(2),
                                reader.GetDecimal(3),
                                reader.GetString(4)
                            ));
                        }
                    }
                }

                if (lista.Count == 0)
                {
                    Console.WriteLine("-> Este artículo no cuenta con receta de preparación.");
                    return false;
                }

                foreach (var item in lista)
                {
                    decimal totalNecesario = item.dosis * porciones;
                    if (item.stock < totalNecesario)
                    {
                        Console.WriteLine($"-> Insumo insuficiente: '{item.nombre}'. Requiere {totalNecesario} {item.unidad}, stock actual: {item.stock} {item.unidad}.");
                        return false;
                    }
                }

                decimal factor = 1 - (porcentajeDescuento / 100m);
                decimal subtotal = (precioBase * factor) * porciones;

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in lista)
                        {
                            decimal totalNecesario = item.dosis * porciones;
                            string updateInsumo = "UPDATE insumos SET stock_actual = stock_actual - @cant WHERE id = @id;";
                            using (var cmdUp = new SqliteCommand(updateInsumo, con, tx))
                            {
                                cmdUp.Parameters.AddWithValue("@cant", totalNecesario);
                                cmdUp.Parameters.AddWithValue("@id", item.id);
                                cmdUp.ExecuteNonQuery();
                            }
                        }

                        string insertMov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Salida', @cant, @motivo, datetime('now'), 'Abierto');
                        ";
                        using (var cmdMov = new SqliteCommand(insertMov, con, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@sku", sku.Trim());
                            cmdMov.Parameters.AddWithValue("@cant", porciones);
                            cmdMov.Parameters.AddWithValue("@motivo", $"Preparación elaborada promo ({porcentajeDescuento:F0}% desc.) - Subtotal: ${subtotal:F2}");
                            cmdMov.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Console.WriteLine($"-> Venta elaborada con {porcentajeDescuento:F0}% desc: {porciones}x '{nombreProd}' | Total cobrado: ${subtotal:F2}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("-> Error al despachar receta: " + ex.Message);
                        return false;
                    }
                }
            }
        }  
// Procesa la venta de múltiples artículos en un solo ticket garantizando atomicidad
        public bool ProcesarTicketVenta(List<Modelos.ItemVentaTemporal> items)
        {
            if (items == null || items.Count == 0)
                return false;

            using (var con = _bd.ObtenerConexion())
            using (var tx = con.BeginTransaction())
            {
                try
                {
                    decimal totalTicket = 0;
                    int totalPiezas = 0;

                    foreach (var item in items)
                    {
                        if (item.EsReceta)
                        {
                            // Verificar insumos de la receta
                            string queryReceta = @"
                                SELECT r.insumo_id, i.nombre, r.cantidad_requerida, i.stock_actual, i.unidad_medida
                                FROM recetas r
                                JOIN insumos i ON r.insumo_id = i.id
                                WHERE r.producto_sku = @sku;
                            ";

                            using (var cmdRec = new SqliteCommand(queryReceta, con, tx))
                            {
                                cmdRec.Parameters.AddWithValue("@sku", item.Producto.Sku);
                                using (var reader = cmdRec.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        long insId = reader.GetInt64(0);
                                        string insNom = reader.GetString(1);
                                        decimal dosis = reader.GetDecimal(2);
                                        decimal stockIns = reader.GetDecimal(3);
                                        string unidad = reader.GetString(4);

                                        decimal totalRequerido = dosis * item.Cantidad;
                                        if (stockIns < totalRequerido)
                                        {
                                            tx.Rollback();
                                            Console.WriteLine($"\n-> Cancelado: Falta insumo '{insNom}' para preparar [{item.Producto.Nombre}].");
                                            Console.WriteLine($"   Se requerían {totalRequerido} {unidad}, existencias: {stockIns} {unidad}.");
                                            return false;
                                        }
                                    }
                                }
                            }

                            // Descontar materias primas
                            string updateInsumos = @"
                                UPDATE insumos 
                                SET stock_actual = stock_actual - (r.cantidad_requerida * @cant)
                                FROM recetas r
                                WHERE insumos.id = r.insumo_id AND r.producto_sku = @sku;
                            ";
                            using (var cmdUpIns = new SqliteCommand(updateInsumos, con, tx))
                            {
                                cmdUpIns.Parameters.AddWithValue("@cant", item.Cantidad);
                                cmdUpIns.Parameters.AddWithValue("@sku", item.Producto.Sku);
                                cmdUpIns.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Verificar stock físico de producto directo
                            string queryStock = "SELECT stock_actual FROM productos WHERE sku = @sku;";
                            decimal stockActual = 0;
                            using (var cmdStock = new SqliteCommand(queryStock, con, tx))
                            {
                                cmdStock.Parameters.AddWithValue("@sku", item.Producto.Sku);
                                stockActual = Convert.ToDecimal(cmdStock.ExecuteScalar() ?? 0);
                            }

                            if (stockActual < item.Cantidad)
                            {
                                tx.Rollback();
                                Console.WriteLine($"\n-> Cancelado: Stock insuficiente para [{item.Producto.Nombre}]. Disponible: {stockActual}.");
                                return false;
                            }

                            // Descontar producto físico
                            string updateProd = "UPDATE productos SET stock_actual = stock_actual - @cant WHERE sku = @sku;";
                            using (var cmdUpProd = new SqliteCommand(updateProd, con, tx))
                            {
                                cmdUpProd.Parameters.AddWithValue("@cant", item.Cantidad);
                                cmdUpProd.Parameters.AddWithValue("@sku", item.Producto.Sku);
                                cmdUpProd.ExecuteNonQuery();
                            }
                        }

                        // Registrar movimiento individual para auditoría
                        string insertMov = @"
                            INSERT INTO movimientos (producto_sku, tipo, cantidad, motivo, fecha, estado)
                            VALUES (@sku, 'Salida', @cant, @motivo, datetime('now'), 'Abierto');
                        ";
                        using (var cmdMov = new SqliteCommand(insertMov, con, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@sku", item.Producto.Sku);
                            cmdMov.Parameters.AddWithValue("@cant", item.Cantidad);
                            cmdMov.Parameters.AddWithValue("@motivo", $"Venta mostrador: {item.Producto.Nombre} (${item.Subtotal:F2})");
                            cmdMov.ExecuteNonQuery();
                        }

                        totalTicket += item.Subtotal;
                        totalPiezas += (int)item.Cantidad;
                    }

                    tx.Commit();
                    Console.WriteLine("\n=================================");
                    Console.WriteLine("       TICKET COBRADO CON ÉXITO   ");
                    Console.WriteLine("=================================");
                    Console.WriteLine($"Artículos procesados: {items.Count} ({totalPiezas} piezas en total)");
                    Console.WriteLine($"TOTAL COBRADO:        ${totalTicket:F2}");
                    Console.WriteLine("=================================");
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Console.WriteLine("-> Error al liquidar orden: " + ex.Message);
                    return false;
                }
                        }            }
 
        public List<(string Sku, string Nombre, decimal Precio, string Stock)> ObtenerCatalogoCompleto()
        {
            var catalogo = new List<(string Sku, string Nombre, decimal Precio, string Stock)>();

            using (var con = _bd.ObtenerConexion())
            {
                string query = @"
                    SELECT p.sku, p.nombre, p.precio_base, p.stock_actual,
                           COUNT(r.id) AS total_ingredientes
                    FROM productos p
                    LEFT JOIN recetas r ON p.sku = r.producto_sku
                    GROUP BY p.sku
                    ORDER BY p.nombre ASC;
                ";

                using (var cmd = new SqliteCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sku = reader.GetString(0);
                        string nombre = reader.GetString(1);
                        decimal precio = reader.GetDecimal(2);
                        decimal stockActual = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                        long totalIngredientes = reader.IsDBNull(4) ? 0 : reader.GetInt64(4);

                        string stockDisplay = totalIngredientes > 0 ? "Preparado" : stockActual.ToString("0.##");

                        catalogo.Add((sku, nombre, precio, stockDisplay));
                    }
                }
            }

            return catalogo;
        }
    }
}