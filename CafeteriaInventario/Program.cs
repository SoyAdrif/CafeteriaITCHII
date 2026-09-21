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
                Console.WriteLine("2. Registrar venta regular (Smart Search)");
                Console.WriteLine("3. Registrar venta con PROMOCIÓN / COMBO (Sobrecarga POO)");
                Console.WriteLine("4. Reabastecer stock (Productos directos o Insumos)");
                Console.WriteLine("5. Registrar artículos y recetas");
                Console.WriteLine("6. Eliminar producto del catálogo (Smart Search)");
                Console.WriteLine("7. Ver existencias de insumos de barra (SQLite)");
                Console.WriteLine("8. Ver bitácora de movimientos (SQLite)");
                Console.WriteLine("9. Realizar corte de caja y cierre de turno");
                Console.WriteLine("10. Salir");
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
                        Console.WriteLine("\n--- MODULO DE PROMOCIONES Y COMBOS ---");
                        Console.WriteLine("a. Descuento porcentual directo a un producto/bebida");
                        Console.WriteLine("b. Combo Desayuno (Bebida + Pan/Repostería con 15% desc.)");
                        Console.WriteLine("c. Volver al menú principal");
                        Console.Write("Seleccione tipo de promoción (a/b/c): ");
                        string subPromo = (Console.ReadLine() ?? "").Trim().ToLower();

                        if (subPromo == "a")
                        {
                            Console.Write("Escanee código de barras o escriba nombre del producto: ");
                            string crit = Console.ReadLine() ?? "";
                            ProductoTerminado? prod = inventarioBD.BuscarProductoUniversal(crit);

                            if (prod != null)
                            {
                                Console.Write("Ingrese porcentaje de descuento (ej. 10 para 10%, 20 para 20%): ");
                                if (decimal.TryParse(Console.ReadLine(), out decimal desc) && desc >= 0 && desc <= 100)
                                {
                                    Console.Write("Cantidad a despachar: ");
                                    if (decimal.TryParse(Console.ReadLine(), out decimal cant) && cant > 0)
                                    {
                                        if (inventarioBD.TieneReceta(prod.Sku))
                                        {
                                            inventarioBD.VenderProductoElaborado(prod.Sku, cant, desc);
                                        }
                                        else
                                        {
                                            inventarioBD.RegistrarVenta(prod.Sku, cant, desc);
                                        }
                                    }
                                }
                            }
                        }
                        else if (subPromo == "b")
                        {
                            Console.WriteLine("\n[CONFIGURANDO COMBO DESAYUNO - 15% DESC]");
                            Console.Write("1. Seleccione la bebida (código, clave rápida o nombre): ");
                            string critBeb = Console.ReadLine() ?? "";
                            ProductoTerminado? beb = inventarioBD.BuscarProductoUniversal(critBeb);

                            if (beb == null)
                            {
                                Console.WriteLine("-> No se pudo agregar la bebida. Combo cancelado.");
                                break;
                            }
                            Console.WriteLine($"   Bebida agregada: [{beb.Sku}] {beb.Nombre} (${beb.Precio:F2})");

                            Console.Write("\n2. Seleccione el acompañamiento (código, clave rápida o nombre): ");
                            string critPan = Console.ReadLine() ?? "";
                            ProductoTerminado? pan = inventarioBD.BuscarProductoUniversal(critPan);

                            if (pan == null)
                            {
                                Console.WriteLine("-> No se pudo agregar el acompañamiento. Combo cancelado.");
                                break;
                            }
                            Console.WriteLine($"   Acompañamiento agregado: [{pan.Sku}] {pan.Nombre} (${pan.Precio:F2})");

                            decimal totalRegular = beb.Precio + pan.Precio;
                            decimal totalConDesc = totalRegular * 0.85m;

                            Console.WriteLine($"\n-> Combo configurado: [{beb.Nombre}] + [{pan.Nombre}]");
                            Console.WriteLine($"-> Precio regular: ${totalRegular:F2} | Total con 15% OFF: ${totalConDesc:F2}");
                            Console.Write("¿Desea confirmar el despacho del combo? (s/n): ");
                            string confirmar = (Console.ReadLine() ?? "").Trim().ToLower();

                            if (confirmar == "s")
                            {
                                // Despacho de bebida
                                if (inventarioBD.TieneReceta(beb.Sku))
                                    inventarioBD.VenderProductoElaborado(beb.Sku, 1, 15);
                                else
                                    inventarioBD.RegistrarVenta(beb.Sku, 1, 15);

                                // Despacho de alimento/acompañamiento
                                if (inventarioBD.TieneReceta(pan.Sku))
                                    inventarioBD.VenderProductoElaborado(pan.Sku, 1, 15);
                                else
                                    inventarioBD.RegistrarVenta(pan.Sku, 1, 15);

                                Console.WriteLine("-> Combo despachado e ingresado a caja exitosamente.");
                            }
                            else
                            {
                                Console.WriteLine("-> Operación cancelada.");
                            }
                        }
                        else if (subPromo == "c")
                        {
                            Console.WriteLine("-> Regresando al menú principal...");
                        }
                        break;

                    case "4":
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
                            }
                        }
                        break;

                    case "5":
                        Console.WriteLine("\n--- REGISTRO Y CONFIGURACIÓN ---");
                        Console.WriteLine("a. Producto de venta directa (embotellado, pan, etc.)");
                        Console.WriteLine("b. Insumo de barra (leche, café en grano, jarabes, etc.)");
                        Console.WriteLine("c. Registrar producto preparado y armar su receta");
                        Console.WriteLine("d. Volver al menú principal");
                        Console.Write("Elija el tipo (a/b/c/d): ");
                        string subAlta = (Console.ReadLine() ?? "").Trim().ToLower();

                        if (subAlta == "a")
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
                        else if (subAlta == "b")
                        {
                            Console.Write("Nombre del insumo: ");
                            string nomInsumo = Console.ReadLine() ?? "";
                            Console.Write("Unidad de medida (g, ml, pza): ");
                            string unidad = Console.ReadLine() ?? "";
                            Console.Write("Stock inicial en barra: ");
                            decimal.TryParse(Console.ReadLine(), out decimal stkInsumo);
                            Console.Write("Umbral mínimo para alerta: ");
                            decimal.TryParse(Console.ReadLine(), out decimal minInsumo);

                            inventarioBD.RegistrarNuevoInsumo(nomInsumo, unidad, stkInsumo, minInsumo);
                        }
                        else if (subAlta == "c")
                        {
                            Console.Write("SKU para preparado: ");
                            string skuRec = (Console.ReadLine() ?? "").Trim();
                            Console.Write("Nombre: ");
                            string nomRec = Console.ReadLine() ?? "";
                            Console.Write("Precio ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal precioRec);
                            Console.Write("Costo estimado ($): ");
                            decimal.TryParse(Console.ReadLine(), out decimal costoRec);

                            if (inventarioBD.RegistrarProductoElaborado(skuRec, nomRec, precioRec, costoRec))
                            {
                                inventarioBD.ListarInsumosConId();
                                bool mas = true;
                                while (mas)
                                {
                                    Console.Write("\nID de insumo: ");
                                    if (long.TryParse(Console.ReadLine(), out long idIns))
                                    {
                                        Console.Write("Cantidad requerida por porción: ");
                                        if (decimal.TryParse(Console.ReadLine(), out decimal cantReq) && cantReq > 0)
                                        {
                                            inventarioBD.AgregarIngredienteAReceta(skuRec, idIns, cantReq);
                                        }
                                    }
                                    Console.Write("¿Agregar otro ingrediente? (s/n): ");
                                    if ((Console.ReadLine() ?? "").Trim().ToLower() != "s") mas = false;
                                }
                            }
                        }
                        break;

                    case "6":
                        Console.Write("Ingrese SKU o nombre a eliminar: ");
                        ProductoTerminado? pElim = inventarioBD.BuscarProductoUniversal(Console.ReadLine() ?? "");
                        if (pElim != null)
                        {
                            Console.Write($"¿Confirmar eliminación de '{pElim.Nombre}' [{pElim.Sku}]? (s/n): ");
                            if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                                inventarioBD.EliminarProducto(pElim.Sku);
                        }
                        break;

                    case "7":
                        inventarioBD.ListarInsumosBarra();
                        break;

                    case "8":
                        Console.WriteLine("\n--- CONSULTA DE AUDITORÍA Y BITÁCORA ---");
                        Console.WriteLine("1. Ver movimientos del turno actual (Pendientes de corte / Abiertos)");
                        Console.WriteLine("2. Ver últimos 25 movimientos generales");
                        Console.WriteLine("3. Filtrar por fecha específica (YYYY-MM-DD)");
                        Console.WriteLine("4. Volver al menú principal");
                        Console.Write("Seleccione una opción: ");
                        string opcBitacora = (Console.ReadLine() ?? "").Trim();

                        switch (opcBitacora)
                        {
                            case "1":
                                inventarioBD.VerHistorialMovimientos("TURNO_ACTUAL");
                                break;

                            case "2":
                                inventarioBD.VerHistorialMovimientos("RECIENTES");
                                break;

                            case "3":
                                Console.Write("Ingrese la fecha a consultar (ej. 2026-09-21): ");
                                string fechaFiltro = (Console.ReadLine() ?? "").Trim();
                                if (!string.IsNullOrEmpty(fechaFiltro))
                                {
                                    inventarioBD.VerHistorialMovimientos("FECHA", fechaFiltro);
                                }
                                else
                                {
                                    Console.WriteLine("Fecha inválida.");
                                }
                                break;

                            case "4":
                                Console.WriteLine("-> Regresando al menú principal...");
                                break;

                            default:
                                Console.WriteLine("Opción no válida.");
                                break;
                        }
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

                        Console.Write("¿Cerrar el turno actual y reiniciar contador a 0? (s/n): ");
                        if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                        {
                            cajaBD.CerrarTurnoCaja();
                            Console.WriteLine("-> Turno cerrado exitosamente.");
                        }
                        break;

                    case "10":
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