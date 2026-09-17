using System;
using CafeteriaInventario.Modelos;

namespace CafeteriaInventario
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sistema de Inventario de Cafeteria ===");

            // Prueba de instanciación básica
            Producto cafe = new Producto(1, "CAF-01", "Cafe Americano", 35.00m, 12.00m);
            ItemInventario stockCafe = new ItemInventario(1, 1, cafe.Id, 50, 12.00m);

            Console.WriteLine($"Producto: {cafe.Nombre}");
            Console.WriteLine($"Stock disponible: {stockCafe.CantidadDisponible}");

            // Prueba de venta/descuento
            if (stockCafe.DescontarStock(2))
            {
                Console.WriteLine("Venta realizada. Nuevo stock: " + stockCafe.CantidadDisponible);
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}