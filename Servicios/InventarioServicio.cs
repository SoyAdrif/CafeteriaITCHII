using System;
using System.Collections.Generic;
using System.Linq;
using CafeteriaInventario.Modelos;

namespace CafeteriaInventario.Servicios
{
    public class InventarioServicio
    {
        private List<Producto> _productos = new List<Producto>();
        private List<ItemInventario> _inventario = new List<ItemInventario>();
        private List<MovimientoInventario> _movimientos = new List<MovimientoInventario>();
        private long _siguienteIdMovimiento = 1;

        public InventarioServicio()
        {
            // Datos iniciales de prueba para la cafetería
            RegistrarProducto(1, "CAF-001", "Cafe Americano", 35.00m, 12.00m, 20);
            RegistrarProducto(2, "PAN-001", "Croissant de Mantequilla", 45.00m, 18.00m, 15);
            RegistrarProducto(3, "BEB-001", "Te Verde", 30.00m, 8.00m, 10);
        }

        public void RegistrarProducto(long id, string sku, string nombre, decimal precio, decimal costo, decimal stockInicial)
        {
            Producto nuevo = new Producto(id, sku, nombre, precio, costo);
            _productos.Add(nuevo);

            ItemInventario item = new ItemInventario(id, 1, nuevo.Id, stockInicial, costo);
            _inventario.Add(item);
        }

        public void ListarProductos()
        {
            Console.WriteLine("\n--- INVENTARIO ACTUAL ---");
            Console.WriteLine("{0,-8} {1,-25} {2,-10} {3,-10}", "SKU", "Nombre", "Precio", "Stock");
            Console.WriteLine(new string('-', 55));

            foreach (var prod in _productos)
            {
                var item = _inventario.FirstOrDefault(i => i.ProductoId == prod.Id);
                decimal stock = item != null ? item.CantidadDisponible : 0;
                Console.WriteLine("{0,-8} {1,-25} ${2,-9:F2} {3,-10}", prod.Sku, prod.Nombre, prod.PrecioBase, stock);
            }
        }

        public bool RegistrarVenta(string sku, decimal cantidad)
        {
            var prod = _productos.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            if (prod == null)
            {
                Console.WriteLine("Error: Producto no encontrado.");
                return false;
            }

            var item = _inventario.FirstOrDefault(i => i.ProductoId == prod.Id);
            if (item == null || !item.DescontarStock(cantidad))
            {
                Console.WriteLine("Error: Stock insuficiente para realizar la venta.");
                return false;
            }

            _movimientos.Add(new MovimientoInventario(_siguienteIdMovimiento++, 1, item.Id, TipoMovimiento.Sale, cantidad, "Venta en mostrador"));
            Console.WriteLine($"Venta registrada: {cantidad}x {prod.Nombre}. Total: ${(prod.PrecioBase * cantidad):F2}");
            return true;
        }

        public bool ReabastecerStock(string sku, decimal cantidad)
        {
            var prod = _productos.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            if (prod == null)
            {
                Console.WriteLine("Error: Producto no encontrado.");
                return false;
            }

            var item = _inventario.FirstOrDefault(i => i.ProductoId == prod.Id);
            if (item != null)
            {
                item.AgregarStock(cantidad);
                _movimientos.Add(new MovimientoInventario(_siguienteIdMovimiento++, 1, item.Id, TipoMovimiento.Purchase, cantidad, "Reabastecimiento de producto"));
                Console.WriteLine($"Stock actualizado. Nuevo total disponible: {item.CantidadDisponible}");
                return true;
            }

            return false;
        }
    }
}