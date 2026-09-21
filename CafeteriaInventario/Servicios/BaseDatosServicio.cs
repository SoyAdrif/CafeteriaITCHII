using System;
using Microsoft.Data.Sqlite;

namespace CafeteriaInventario.Servicios
{
    public class BaseDatosServicio
    {
        private const string CadenaConexion = "Data Source=cafeteria.db";

        public BaseDatosServicio()
        {
            InicializarBaseDatos();
        }

        public SqliteConnection ObtenerConexion()
        {
            var conexion = new SqliteConnection(CadenaConexion);
            conexion.Open();
            return conexion;
        }

        private void InicializarBaseDatos()
        {
            using (var con = ObtenerConexion())
            {
                // 1. Tabla de Productos con stock_minimo configurable
                string crearTablaProductos = @"
                    CREATE TABLE IF NOT EXISTS productos (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        sku TEXT NOT NULL UNIQUE,
                        nombre TEXT NOT NULL,
                        precio_base DECIMAL NOT NULL,
                        costo_precio DECIMAL NOT NULL,
                        stock_actual DECIMAL NOT NULL,
                        stock_minimo DECIMAL NOT NULL DEFAULT 5
                    );
                ";

                // 2. Tabla de Insumos físicos de barra
                string crearTablaInsumos = @"
                    CREATE TABLE IF NOT EXISTS insumos (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        nombre TEXT NOT NULL,
                        unidad_medida TEXT NOT NULL,
                        stock_actual DECIMAL NOT NULL,
                        stock_minimo DECIMAL NOT NULL DEFAULT 100
                    );
                ";

                // 3. Tabla intermedia de Recetas (Composición)
                string crearTablaRecetas = @"
                    CREATE TABLE IF NOT EXISTS recetas (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        producto_sku TEXT NOT NULL,
                        insumo_id INTEGER NOT NULL,
                        cantidad_requerida DECIMAL NOT NULL,
                        FOREIGN KEY(producto_sku) REFERENCES productos(sku),
                        FOREIGN KEY(insumo_id) REFERENCES insumos(id)
                    );
                ";

                // 4. Tabla de Movimientos y Auditoría con control de turno
                string crearTablaMovimientos = @"
                    CREATE TABLE IF NOT EXISTS movimientos (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        producto_sku TEXT NOT NULL,
                        tipo TEXT NOT NULL,
                        cantidad DECIMAL NOT NULL,
                        motivo TEXT NOT NULL,
                        fecha TEXT NOT NULL,
                        estado TEXT NOT NULL DEFAULT 'Abierto'
                    );
                ";

                using (var cmd = new SqliteCommand(crearTablaProductos, con)) cmd.ExecuteNonQuery();
                using (var cmd = new SqliteCommand(crearTablaInsumos, con)) cmd.ExecuteNonQuery();
                using (var cmd = new SqliteCommand(crearTablaRecetas, con)) cmd.ExecuteNonQuery();
                using (var cmd = new SqliteCommand(crearTablaMovimientos, con)) cmd.ExecuteNonQuery();

                // Poblar productos base si está vacía
                string checkProd = "SELECT COUNT(*) FROM productos;";
                using (var checkCmd = new SqliteCommand(checkProd, con))
                {
                    long total = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                    if (total == 0)
                    {
                        string insertarProductos = @"
                            INSERT INTO productos (sku, nombre, precio_base, costo_precio, stock_actual, stock_minimo) VALUES
                            ('BEB-AME', 'Cafe Americano 12oz', 35.00, 8.50, 20, 5),
                            ('REP-DON', 'Dona Glaseada', 22.00, 10.00, 15, 3),
                            ('BEB-CAP', 'Cappuccino Preparado', 50.00, 16.00, 0, 0);
                        ";
                        using (var insCmd = new SqliteCommand(insertarProductos, con)) insCmd.ExecuteNonQuery();
                    }
                }

                // Poblar insumos y receta base si está vacía
                string checkInsumos = "SELECT COUNT(*) FROM insumos;";
                using (var checkCmd = new SqliteCommand(checkInsumos, con))
                {
                    long total = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);
                    if (total == 0)
                    {
                        string insertarInsumos = @"
                            INSERT INTO insumos (nombre, unidad_medida, stock_actual, stock_minimo) VALUES
                            ('Grano de Cafe', 'g', 1000.0, 250.0),
                            ('Leche Entera', 'ml', 2000.0, 500.0);

                            -- Composición de receta: 1 Cappuccino requiere 18g cafe (id 1) y 150ml leche (id 2)
                            INSERT INTO recetas (producto_sku, insumo_id, cantidad_requerida) VALUES
                            ('BEB-CAP', 1, 18.0),
                            ('BEB-CAP', 2, 150.0);
                        ";
                        using (var insCmd = new SqliteCommand(insertarInsumos, con)) insCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}