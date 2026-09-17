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

        private void InicializarBaseDatos()
        {
            using (var conexion = new SqliteConnection(CadenaConexion))
            {
                conexion.Open();

                string query = @"
                    CREATE TABLE IF NOT EXISTS productos (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        sku TEXT NOT NULL UNIQUE,
                        nombre TEXT NOT NULL,
                        precio_base DECIMAL NOT NULL,
                        costo_precio DECIMAL NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS inventario (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        producto_id INTEGER NOT NULL UNIQUE,
                        cantidad_disponible DECIMAL NOT NULL,
                        FOREIGN KEY (producto_id) REFERENCES productos(id)
                    );
                ";

                using (var comando = new SqliteCommand(query, conexion))
                {
                    comando.ExecuteNonQuery();
                }

                // Cargar datos base si la tabla está vacía
                InsertarDatosIniciales(conexion);
            }
        }

        private void InsertarDatosIniciales(SqliteConnection conexion)
        {
            string checkQuery = "SELECT COUNT(*) FROM productos;";
            using (var checkCmd = new SqliteCommand(checkQuery, conexion))
            {
                long count = (long)checkCmd.ExecuteScalar();
                if (count == 0)
                {
                    string insertQuery = @"
                        INSERT INTO productos (sku, nombre, precio_base, costo_precio) VALUES 
                        ('CAF-001', 'Cafe Americano', 35.00, 12.00),
                        ('PAN-001', 'Croissant de Mantequilla', 45.00, 18.00),
                        ('BEB-001', 'Te Verde', 30.00, 8.00);

                        INSERT INTO inventario (producto_id, cantidad_disponible) VALUES 
                        (1, 20),
                        (2, 15),
                        (3, 10);
                    ";
                    using (var insertCmd = new SqliteCommand(insertQuery, conexion))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public SqliteConnection ObtenerConexion()
        {
            var conexion = new SqliteConnection(CadenaConexion);
            conexion.Open();
            return conexion;
        }
    }
}