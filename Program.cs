using System;
using System.Collections.Generic;
using CafeteriaInventario.Modelos;
using CafeteriaInventario.Servicios;

namespace CafeteriaInventario
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InventarioServicio inventarioBD = new InventarioServicio();

            // Insumos y recetas para productos preparados (POO: Composición y Clases Abstractas)
            Ingrediente cafeGrano = new Ingrediente("Grano de Cafe (g)", 1000);
            Ingrediente leche = new Ingrediente("Leche (ml)", 2000);

            ProductoElaborado cappuccino = new ProductoElaborado(99, "CAF-CAP", "Cappuccino Preparado", 50.00m, 16.00m);
            cappuccino.AgregarInsumoAReceta(cafeGrano, 18); // 18g de café
            cappuccino.AgregarInsumoAReceta(leche, 150);    // 150ml de leche

            bool salir = false;

            do
            {
                Console.WriteLine("\n=================================");
                Console.WriteLine("    CAFETERIA - CONTROL TOTAL    ");
                Console.WriteLine("=================================");
                Console.WriteLine("1. Ver inventario general (SQLite)");
                Console.WriteLine("2. Registrar venta de mostrador (SQLite)");
                Console.WriteLine("3. Reabastecer stock (SQLite)");
                Console.WriteLine("4. Vender Cappuccino elaborado (Receta / Insumos)");
                Console.WriteLine("5. Ver existencias de insumos de barra");
                Console.WriteLine("6. Salir");
                Console.Write("Seleccione una opcion: ");

                string opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        inventarioBD.ListarProductos();
                        break;

                    case "2":
                        Console.Write("Ingrese el SKU del producto: ");
                        string sku = Console.ReadLine();
                        Console.Write("Ingrese la cantidad a vender: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal cantVenta))
                        {
                            inventarioBD.RegistrarVenta(sku, cantVenta);
                        }
                        else
                        {
                            Console.WriteLine("Cantidad invalida.");
                        }
                        break;

                    case "3":
                        Console.Write("Ingrese el SKU del producto: ");
                        string skuEntrada = Console.ReadLine();
                        Console.Write("Ingrese la cantidad a ingresar: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal cantEntrada))
                        {
                            inventarioBD.ReabastecerStock(skuEntrada, cantEntrada);
                        }
                        else
                        {
                            Console.WriteLine("Cantidad invalida.");
                        }
                        break;

                    case "4":
                        Console.Write("¿Cuantas tazas de Cappuccino desea preparar?: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal tazas))
                        {
                            if (cappuccino.DescontarExistencias(tazas))
                            {
                                Console.WriteLine($"Venta completada: {tazas} Cappuccino(s) preparados. Total: ${(cappuccino.Precio * tazas):F2}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Numero de tazas invalido.");
                        }
                        break;

                    case "5":
                        Console.WriteLine("\n--- INSUMOS EN BARRA ---");
                        Console.WriteLine($"- {cafeGrano.Nombre}: {cafeGrano.StockGramosOMl} g restantes");
                        Console.WriteLine($"- {leche.Nombre}: {leche.StockGramosOMl} ml restantes");
                        break;

                    case "6":
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