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
                Console.WriteLine("2. Registrar venta (Smart Search: Directo o Receta)");
                Console.WriteLine("3. Reabastecer stock (Productos directos o Insumos)");
                Console.WriteLine("4. Registrar artículos y recetas");
                Console.WriteLine("5. Eliminar producto del catálogo (Smart Search)");
                Console.WriteLine("6. Ver existencias de insumos de barra (SQLite)");
                Console.WriteLine("7. Ver bitácora de movimientos (SQLite)");
                Console.WriteLine("8. Realizar corte de caja y cierre de turno");
                Console.WriteLine("9. Salir");
                Console.Write("Seleccione una opción: ");

                string opcion = Console.ReadLine() ?? "";

                switch (opcion)
                {
                    case "1":
                        inventarioBD.ListarProductos();
                        break;

                    case "2":
                        Console.Write("Escanee código de barras, clave rápida o nombre: ");
                        string criterioVenta = Console.ReadLine() ?? "";
                        ProductoTerminado? prodVenta = inventarioBD.BuscarProductoUniversal(criterioVenta);

                        if (prodVenta != null)
                        {
                            bool esReceta = inventarioBD.TieneReceta(prodVenta.Sku);

                            if (esReceta)
                            {
                                Console.WriteLine($"-> Producto de preparación: [{prodVenta.Sku}] {prodVenta.Nombre}");
                                Console.Write("Ingrese la cantidad de porciones/vasos a preparar: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal cantElab) && cantElab > 0)
                                {
                                    inventarioBD.VenderProductoElaborado(prodVenta.Sku, cantElab);
                                }
                                else
                                {
                                    Console.WriteLine("Cantidad inválida.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"-> Producto físico: [{prodVenta.Sku}] {prodVenta.Nombre} (Stock: {prodVenta.Existencias})");
                                Console.Write("Ingrese la cantidad a vender: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal cantVenta) && cantVenta > 0)
                                {
                                    inventarioBD.RegistrarVenta(prodVenta.Sku, cantVenta);
                                }
                                else
                                {
                                    Console.WriteLine("Cantidad inválida.");
                                }
                            }
                        }
                        break;

                    case "3":
                        Console.WriteLine("\n--- REABASTECIMIENTO DE INVENTARIO ---");
                        Console.WriteLine("a. Reabastecer producto directo (Smart Search)");
                        Console.WriteLine("b. Reabastecer materia prima / insumo de barra");
                        Console.WriteLine("c. Volver al menú principal");
                        Console.Write("Elija una opción (a/b/c): ");
                        string subReab = (Console.ReadLine() ?? "").Trim().ToLower();

                        if (subReab == "a")
                        {
                            Console.Write("Escanee código de barras, clave rápida o nombre: ");
                            string criterioEntrada = Console.ReadLine() ?? "";
                            ProductoTerminado? prodEntrada = inventarioBD.BuscarProductoUniversal(criterioEntrada);

                            if (prodEntrada != null)
                            {
                                if (inventarioBD.TieneReceta(prodEntrada.Sku))
                                {
                                    Console.WriteLine("-> Este artículo se elabora por receta; reabastezca sus materias primas en la opción b.");
                                    break;
                                }

                                Console.WriteLine($"-> Seleccionado: [{prodEntrada.Sku}] {prodEntrada.Nombre} (Stock actual: {prodEntrada.Existencias})");
                                Console.Write("Ingrese la cantidad a ingresar: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal cantEntrada) && cantEntrada > 0)
                                {
                                    inventarioBD.ReabastecerStock(prodEntrada.Sku, cantEntrada);
                                }
                                else
                                {
                                    Console.WriteLine("Cantidad inválida.");
                                }
                            }
                        }
                        else if (subReab == "b")
                        {
                            inventarioBD.ListarInsumosConId();
                            Console.Write("\nIngrese el ID del insumo a reabastecer: ");
                            if (long.TryParse(Console.ReadLine(), out long idInsumo))
                            {
                                Console.Write("Cantidad a ingresar en almacén: ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal cantInsumo) && cantInsumo > 0)
                                {
                                    inventarioBD.ReabastecerInsumo(idInsumo, cantInsumo);
                                }
                                else
                                {
                                    Console.WriteLine("Cantidad inválida.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("ID inválido.");
                            }
                        }
                        else if (subReab == "c")
                        {
                            Console.WriteLine("-> Regresando al menú principal...");
                        }
                        else
                        {
                            Console.WriteLine("Opción no válida.");
                        }
                        break;

                    case "4":
                        Console.WriteLine("\n--- REGISTRO Y CONFIGURACIÓN ---");
                        Console.WriteLine("a. Producto de venta directa (embotellado, pan, etc.)");
                        Console.WriteLine("b. Insumo de barra (leche, café en grano, jarabes, etc.)");
                        Console.WriteLine("c. Registrar producto preparado y armar su receta");
                        Console.WriteLine("d. Volver al menú principal");
                        Console.Write("Elija el tipo (a/b/c/d): ");
                        string subOpcion = (Console.ReadLine() ?? "").Trim().ToLower();

                        if (subOpcion == "a")
                        {
                            Console.Write("SKU o código de barras: ");
                            string sku = Console.ReadLine() ?? "";
                            Console.Write("Nombre: ");
                            string nom = Console.ReadLine() ?? "";
                            Console.Write("Precio venta ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal pre);
                            Console.Write("Costo ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal cos);
                            Console.Write("Stock inicial: ");
                            decimal.TryParse(Console.ReadLine(), out decimal stk);
                            Console.Write("Umbral de stock mínimo para alerta: ");
                            decimal.TryParse(Console.ReadLine(), out decimal min);

                            inventarioBD.RegistrarNuevoProducto(sku, nom, pre, cos, stk, min);
                        }
                        else if (subOpcion == "b")
                        {
                            Console.Write("Nombre del insumo (ej. Jarabe, Leche): ");
                            string nomInsumo = Console.ReadLine() ?? "";
                            Console.Write("Unidad de medida (g, ml, pza): ");
                            string unidad = Console.ReadLine() ?? "";
                            Console.Write("Stock inicial en barra: ");
                            decimal.TryParse(Console.ReadLine(), out decimal stkInsumo);
                            Console.Write("Umbral mínimo para alerta: ");
                            decimal.TryParse(Console.ReadLine(), out decimal minInsumo);

                            inventarioBD.RegistrarNuevoInsumo(nomInsumo, unidad, stkInsumo, minInsumo);
                        }
                        else if (subOpcion == "c")
                        {
                            Console.WriteLine("\n[1/2] Datos generales de la bebida o producto preparado");
                            Console.Write("SKU para este preparado (ej. BEB-CHO o clave rápida 205): ");
                            string skuRec = (Console.ReadLine() ?? "").Trim();
                            Console.Write("Nombre (ej. Leche con Chocolate): ");
                            string nomRec = Console.ReadLine() ?? "";
                            Console.Write("Precio de venta al cliente ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal precioRec);
                            Console.Write("Costo estimado de ingredientes ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal costoRec);

                            if (inventarioBD.RegistrarProductoElaborado(skuRec, nomRec, precioRec, costoRec))
                            {
                                Console.WriteLine("\n[2/2] Insumos requeridos para 1 porción");
                                inventarioBD.ListarInsumosConId();

                                bool mas = true;
                                while (mas)
                                {
                                    Console.Write("\nIngrese el ID del insumo a incluir: ");
                                    if (long.TryParse(Console.ReadLine(), out long idIns))
                                    {
                                        Console.Write("Cantidad requerida por porción (ej. 250 para ml o 25 para g): ");
                                        if (decimal.TryParse(Console.ReadLine(), out decimal cantReq) && cantReq > 0)
                                        {
                                            inventarioBD.AgregarIngredienteAReceta(skuRec, idIns, cantReq);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Cantidad inválida.");
                                        }
                                    }

                                    Console.Write("¿Desea agregar otro ingrediente a esta preparación? (s/n): ");
                                    if ((Console.ReadLine() ?? "").Trim().ToLower() != "s") mas = false;
                                }
                                Console.WriteLine($"-> Receta para '{nomRec}' guardada y lista para vender en mostrador.");
                            }
                        }
                        else if (subOpcion == "d")
                        {
                            Console.WriteLine("-> Regresando al menú principal...");
                        }
                        else
                        {
                            Console.WriteLine("Opción no válida.");
                        }
                        break;

                    case "5":
                        Console.Write("Ingrese SKU o nombre del producto a eliminar: ");
                        string critElim = Console.ReadLine() ?? "";
                        ProductoTerminado? pElim = inventarioBD.BuscarProductoUniversal(critElim);
                        if (pElim != null)
                        {
                            Console.Write($"¿Confirmar eliminación de '{pElim.Nombre}' [{pElim.Sku}]? (s/n): ");
                            if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                            {
                                inventarioBD.EliminarProducto(pElim.Sku);
                            }
                        }
                        break;

                    case "6":
                        inventarioBD.ListarInsumosBarra();
                        break;

                    case "7":
                        inventarioBD.VerHistorialMovimientos();
                        break;

                    case "8":
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

                        Console.Write("¿Cerrar el turno actual y reiniciar contador a 0? (s/n): ");
                        if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                        {
                            cajaBD.CerrarTurnoCaja();
                            Console.WriteLine("-> Turno cerrado exitosamente.");
                        }
                        break;

                    case "9":
                        Console.WriteLine("Cerrando sistema...");
                        salir = true;
                        break;

                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }

            } while (!salir);
        }
    }
}