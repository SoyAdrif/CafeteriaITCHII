using System;
using CafeteriaInventario.Modelos;
using CafeteriaInventario.Servicios;

namespace CafeteriaInventario
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InventarioServicio inventarioBD = new InventarioServicio();
            CajaServicio cajaBD = new CajaServicio();

            Ingrediente cafeGrano = new Ingrediente("Grano de Cafe (g)", 1000);
            Ingrediente leche = new Ingrediente("Leche (ml)", 2000);

            ProductoElaborado cappuccino = new ProductoElaborado(99, "CAF-CAP", "Cappuccino Preparado", 50.00m, 16.00m);
            cappuccino.AgregarInsumoAReceta(cafeGrano, 18);
            cappuccino.AgregarInsumoAReceta(leche, 150);

            bool salir = false;

            do
            {
                Console.WriteLine("\n=================================");
                Console.WriteLine("    CAFETERIA - CONTROL TOTAL    ");
                Console.WriteLine("=================================");
                Console.WriteLine("1. Ver inventario general (SQLite)");
                Console.WriteLine("2. Registrar venta de mostrador (SQLite)");
                Console.WriteLine("3. Reabastecer stock (SQLite)");
                Console.WriteLine("4. Vender Cappuccino elaborado (Receta)");
                Console.WriteLine("5. Ver existencias de insumos de barra");
                Console.WriteLine("6. Ver bitacora de movimientos (SQLite)");
                Console.WriteLine("7. Realizar corte de caja / finanzas");
                Console.WriteLine("8. Salir");
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
                                Console.WriteLine($"Venta completada: {tazas} Cappuccino(s). Total: ${(cappuccino.Precio * tazas):F2}");
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
                        inventarioBD.VerHistorialMovimientos();
                        break;

                    case "7":
                        var corte = cajaBD.GenerarCorteTurno();
                        Console.WriteLine("\n=================================");
                        Console.WriteLine("        CORTE DE CAJA ACTUAL     ");
                        Console.WriteLine("=================================");
                        Console.WriteLine($"Fecha: {corte.FechaGeneracion:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Total de Ventas:      {corte.TotalTransaccionesVenta}");
                        Console.WriteLine($"Unidades Despachadas: {corte.TotalUnidadesVendidas}");
                        Console.WriteLine($"Ingresos Brutos:     ${corte.TotalIngresos:F2}");
                        Console.WriteLine($"Utilidad Estimada:   ${corte.TotalGananciaEstimada:F2}");
                        Console.WriteLine("=================================");
                        break;

                    case "8":
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