using System;
using CafeteriaInventario.Servicios;

namespace CafeteriaInventario
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InventarioServicio servicio = new InventarioServicio();
            bool salir = false;

            do
            {
                Console.WriteLine("\n=================================");
                Console.WriteLine("     CAFETERIA - CONTROL STOCK   ");
                Console.WriteLine("=================================");
                Console.WriteLine("1. Ver lista de productos y existencias");
                Console.WriteLine("2. Registrar una venta");
                Console.WriteLine("3. Reabastecer producto (Entrada)");
                Console.WriteLine("4. Salir");
                Console.Write("Seleccione una opcion: ");

                string opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        servicio.ListarProductos();
                        break;

                    case "2":
                        Console.Write("Ingrese el SKU del producto: ");
                        string skuVenta = Console.ReadLine();
                        Console.Write("Ingrese la cantidad vendida: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal cantVenta))
                        {
                            servicio.RegistrarVenta(skuVenta, cantVenta);
                        }
                        else
                        {
                            Console.WriteLine("Cantidad no valida.");
                        }
                        break;

                    case "3":
                        Console.Write("Ingrese el SKU del producto: ");
                        string skuEntrada = Console.ReadLine();
                        Console.Write("Ingrese la cantidad a ingresar: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal cantEntrada))
                        {
                            servicio.ReabastecerStock(skuEntrada, cantEntrada);
                        }
                        else
                        {
                            Console.WriteLine("Cantidad no valida.");
                        }
                        break;

                    case "4":
                        Console.WriteLine("Cerrando sistema...");
                        salir = true;
                        break;

                    default:
                        Console.WriteLine("Opcion no valida. Intente de nuevo.");
                        break;
                }

            } while (!salir);
        }
    }
}