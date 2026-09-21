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

            bool salir = false;

            do
            {
                Console.WriteLine("\n=================================");
                Console.WriteLine("    CAFETERIA - CONTROL TOTAL    ");
                Console.WriteLine("=================================");
                Console.WriteLine("1. Ver inventario general (SQLite)");
                Console.WriteLine("2. Registrar venta de mostrador (Smart Search)");
                Console.WriteLine("3. Reabastecer stock (Smart Search)");
                Console.WriteLine("4. Registrar nuevo producto / insumo");
                Console.WriteLine("5. Eliminar producto del catalogo (Smart Search)");
                Console.WriteLine("6. Vender Cappuccino elaborado (Receta en SQLite)");
                Console.WriteLine("7. Ver existencias de insumos de barra (SQLite)");
                Console.WriteLine("8. Ver bitacora de movimientos (SQLite)");
                Console.WriteLine("9. Realizar corte de caja y cierre de turno");
                Console.WriteLine("10. Salir");
                Console.Write("Seleccione una opcion: ");

                string opcion = Console.ReadLine() ?? "";

                switch (opcion)
                {
                    case "1":
                        inventarioBD.ListarProductos();
                        break;

                    case "2":
                        Console.Write("Escanee código de barras, clave rápida o escriba el nombre: ");
                        string criterioVenta = Console.ReadLine() ?? "";
                        ProductoTerminado? prodVenta = inventarioBD.BuscarProductoUniversal(criterioVenta);

                        if (prodVenta != null)
                        {
                            Console.WriteLine($"-> Seleccionado: [{prodVenta.Sku}] {prodVenta.Nombre} (Stock actual: {prodVenta.Existencias})");
                            Console.Write("Ingrese la cantidad a vender: ");
                            if (decimal.TryParse(Console.ReadLine(), out decimal cantVenta) && cantVenta > 0)
                            {
                                inventarioBD.RegistrarVenta(prodVenta.Sku, cantVenta);
                            }
                            else
                            {
                                Console.WriteLine("Cantidad invalida.");
                            }
                        }
                        break;

                    case "3":
                        Console.Write("Escanee código de barras, clave rápida o escriba el nombre: ");
                        string criterioEntrada = Console.ReadLine() ?? "";
                        ProductoTerminado? prodEntrada = inventarioBD.BuscarProductoUniversal(criterioEntrada);

                        if (prodEntrada != null)
                        {
                            Console.WriteLine($"-> Seleccionado: [{prodEntrada.Sku}] {prodEntrada.Nombre} (Stock actual: {prodEntrada.Existencias})");
                            Console.Write("Ingrese la cantidad a ingresar: ");
                            if (decimal.TryParse(Console.ReadLine(), out decimal cantEntrada) && cantEntrada > 0)
                            {
                                inventarioBD.ReabastecerStock(prodEntrada.Sku, cantEntrada);
                            }
                            else
                            {
                                Console.WriteLine("Cantidad invalida.");
                            }
                        }
                        break;

                    case "4":
                        Console.WriteLine("\n--- REGISTRO DE NUEVO ARTÍCULO ---");
                        Console.WriteLine("a. Producto de venta directa (embotellado, pan, etc.)");
                        Console.WriteLine("b. Insumo para barra (grano, jarabe, leche, etc.)");
                        Console.Write("Elija el tipo (a/b): ");
                        string subOpcion = (Console.ReadLine() ?? "").Trim().ToLower();

                        if (subOpcion == "a")
                        {
                            Console.Write("Ingrese SKU o escanee código de barras (ej. 104 o 7501...): ");
                            string nuevoSku = Console.ReadLine() ?? "";

                            Console.Write("Nombre del producto: ");
                            string nuevoNombre = Console.ReadLine() ?? "";

                            Console.Write("Precio de venta ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal precio);

                            Console.Write("Costo de adquisición ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal costo);

                            Console.Write("Stock inicial en existencias: ");
                            decimal.TryParse(Console.ReadLine(), out decimal stock);

                            Console.Write("Umbral de bajo stock mínimo para alerta: ");
                            decimal.TryParse(Console.ReadLine(), out decimal minimo);

                            inventarioBD.RegistrarNuevoProducto(nuevoSku, nuevoNombre, precio, costo, stock, minimo);
                        }
                        else if (subOpcion == "b")
                        {
                            Console.Write("Nombre del insumo (ej. Jarabe Vainilla): ");
                            string nombreInsumo = Console.ReadLine() ?? "";

                            Console.Write("Unidad de medida (g, ml, pza): ");
                            string unidad = Console.ReadLine() ?? "";

                            Console.Write("Stock inicial disponible: ");
                            decimal.TryParse(Console.ReadLine(), out decimal stockInsumo);

                            Console.Write("Umbral mínimo para alerta: ");
                            decimal.TryParse(Console.ReadLine(), out decimal minimoInsumo);

                            inventarioBD.RegistrarNuevoInsumo(nombreInsumo, unidad, stockInsumo, minimoInsumo);
                        }
                        else
                        {
                            Console.WriteLine("Opción no válida.");
                        }
                        break;

                    case "5":
                        Console.Write("Ingrese SKU, clave o nombre del producto a eliminar: ");
                        string criterioEliminar = Console.ReadLine() ?? "";
                        ProductoTerminado? prodEliminar = inventarioBD.BuscarProductoUniversal(criterioEliminar);

                        if (prodEliminar != null)
                        {
                            Console.Write($"¿Está seguro de eliminar '{prodEliminar.Nombre}' [{prodEliminar.Sku}] del inventario? (s/n): ");
                            string confirmacion = (Console.ReadLine() ?? "").Trim().ToLower();
                            if (confirmacion == "s")
                            {
                                inventarioBD.EliminarProducto(prodEliminar.Sku);
                            }
                            else
                            {
                                Console.WriteLine("Operación cancelada.");
                            }
                        }
                        break;

                    case "6":
                        Console.Write("¿Cuantas tazas de Cappuccino desea preparar?: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal tazas) && tazas > 0)
                        {
                            inventarioBD.VenderProductoElaborado("BEB-CAP", tazas);
                        }
                        else
                        {
                            Console.WriteLine("Numero de tazas invalido.");
                        }
                        break;

                    case "7":
                        inventarioBD.ListarInsumosBarra();
                        break;

                    case "8":
                        inventarioBD.VerHistorialMovimientos();
                        break;

                    case "9":
                        var corte = cajaBD.GenerarCorteTurno();
                        Console.WriteLine("\n=================================");
                        Console.WriteLine("        CORTE DE TURNO ACTUAL    ");
                        Console.WriteLine("=================================");
                        Console.WriteLine($"Fecha: {corte.FechaGeneracion:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Total de Ventas:      {corte.TotalTransaccionesVenta}");
                        Console.WriteLine($"Unidades Despachadas: {corte.TotalUnidadesVendidas}");
                        Console.WriteLine($"Ingresos Brutos:     ${corte.TotalIngresos:F2}");
                        Console.WriteLine($"Utilidad Estimada:   ${corte.TotalGananciaEstimada:F2}");
                        Console.WriteLine("=================================");

                        Console.Write("¿Desea cerrar el turno actual y reiniciar el contador a 0? (s/n): ");
                        string respuesta = Console.ReadLine() ?? "";
                        if (respuesta.Trim().ToLower() == "s")
                        {
                            cajaBD.CerrarTurnoCaja();
                            Console.WriteLine("-> Turno cerrado exitosamente. El contador de caja ahora esta en 0.");
                        }
                        break;

                    case "10":
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