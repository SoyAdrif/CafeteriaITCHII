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
                try
                {
                    Console.WriteLine("\n=================================");
                    Console.WriteLine("    CAFETERIA - CONTROL TOTAL    ");
                    Console.WriteLine("=================================");
                    Console.WriteLine("1. Ver inventario general");
                    Console.WriteLine("2. Cobrar / Vender (Buscar por código o nombre)");
                    Console.WriteLine("3. Promociones y combos");
                    Console.WriteLine("4. Reabastecer existencias o insumos");
                    Console.WriteLine("5. Dar de alta productos y recetas");
                    Console.WriteLine("6. Dar de baja un producto");
                    Console.WriteLine("7. Ver existencias de barra (leche, café, etc.)");
                    Console.WriteLine("8. Bitácora de movimientos y ventas");
                    Console.WriteLine("9. Centro de reportes y corte de caja");
                    Console.WriteLine("10. Salir");
                    Console.Write("Elija el número de opción: ");

                    string opcion = (Console.ReadLine() ?? "").Trim();

                    switch (opcion)
                    {
                        case "1":
                            inventarioBD.ListarProductos();
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "2":
                            Console.WriteLine("\n--- VENTA EN MOSTRADOR ---");
                            string criterioVenta = ConsolaHelper.LeerTextoNoVacio("Pase el código de barras o escriba el nombre del producto: ");
                            ProductoTerminado? prodVenta = inventarioBD.BuscarProductoUniversal(criterioVenta);

                            if (prodVenta != null)
                            {
                                bool esReceta = inventarioBD.TieneReceta(prodVenta.Sku);

                                if (esReceta)
                                {
                                    Console.WriteLine($"-> Bebida/Preparación: [{prodVenta.Sku}] {prodVenta.Nombre}");
                                    decimal cantElab = ConsolaHelper.LeerDecimalPositivo("¿Cuántas tazas o porciones desea preparar?: ");
                                    inventarioBD.VenderProductoElaborado(prodVenta.Sku, cantElab);
                                }
                                else
                                {
                                    Console.WriteLine($"-> Producto físico: [{prodVenta.Sku}] {prodVenta.Nombre} (Disponibles: {prodVenta.Existencias})");
                                    decimal cantVenta = ConsolaHelper.LeerDecimalPositivo("¿Cuántas piezas va a vender?: ");
                                    inventarioBD.RegistrarVenta(prodVenta.Sku, cantVenta);
                                }
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "3":
                            Console.WriteLine("\n--- PROMOCIONES Y COMBOS ---");
                            Console.WriteLine("a. Descuento directo en un producto");
                            Console.WriteLine("b. Combo Desayuno (Bebida + Alimento con 15% desc.)");
                            Console.WriteLine("c. Volver");
                            Console.Write("Seleccione una letra (a/b/c): ");
                            string subPromo = (Console.ReadLine() ?? "").Trim().ToLower();

                            if (subPromo == "a")
                            {
                                string crit = ConsolaHelper.LeerTextoNoVacio("Escriba el nombre o pase el código del producto: ");
                                ProductoTerminado? prod = inventarioBD.BuscarProductoUniversal(crit);

                                if (prod != null)
                                {
                                    decimal desc = ConsolaHelper.LeerDecimalPositivo("Porcentaje de descuento (ejemplo: 10 o 20): ", true);
                                    if (desc <= 100)
                                    {
                                        decimal cant = ConsolaHelper.LeerDecimalPositivo("Cantidad de piezas a vender: ");
                                        if (inventarioBD.TieneReceta(prod.Sku))
                                            inventarioBD.VenderProductoElaborado(prod.Sku, cant, desc);
                                        else
                                            inventarioBD.RegistrarVenta(prod.Sku, cant, desc);
                                    }
                                    else
                                    {
                                        Console.WriteLine("-> El descuento no puede superar el 100%. Operación cancelada.");
                                    }
                                }
                            }
                            else if (subPromo == "b")
                            {
                                Console.WriteLine("\n[CONFIGURANDO COMBO DESAYUNO - 15% DESC]");
                                string critBeb = ConsolaHelper.LeerTextoNoVacio("1. Seleccione la bebida (nombre o código): ");
                                ProductoTerminado? beb = inventarioBD.BuscarProductoUniversal(critBeb);

                                if (beb == null)
                                {
                                    Console.WriteLine("-> Bebida no encontrada. Combo cancelado.");
                                    ConsolaHelper.PausaContinuar();
                                    break;
                                }

                                string critPan = ConsolaHelper.LeerTextoNoVacio("2. Seleccione el acompañamiento (nombre o código): ");
                                ProductoTerminado? pan = inventarioBD.BuscarProductoUniversal(critPan);

                                if (pan == null)
                                {
                                    Console.WriteLine("-> Acompañamiento no encontrado. Combo cancelado.");
                                    ConsolaHelper.PausaContinuar();
                                    break;
                                }

                                decimal totalRegular = beb.Precio + pan.Precio;
                                decimal totalDesc = totalRegular * 0.85m;
                                Console.WriteLine($"\n-> Combo: [{beb.Nombre}] + [{pan.Nombre}]");
                                Console.WriteLine($"-> Precio normal: ${totalRegular:F2} | Con 15% descuento: ${totalDesc:F2}");
                                Console.Write("¿Desea confirmar el cobro? (s/n): ");
                                if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                                {
                                    if (inventarioBD.TieneReceta(beb.Sku))
                                        inventarioBD.VenderProductoElaborado(beb.Sku, 1, 15);
                                    else
                                        inventarioBD.RegistrarVenta(beb.Sku, 1, 15);

                                    if (inventarioBD.TieneReceta(pan.Sku))
                                        inventarioBD.VenderProductoElaborado(pan.Sku, 1, 15);
                                    else
                                        inventarioBD.RegistrarVenta(pan.Sku, 1, 15);
                                }
                                else
                                {
                                    Console.WriteLine("-> Operación cancelada.");
                                }
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "4":
                            Console.WriteLine("\n--- REABASTECIMIENTO ---");
                            Console.WriteLine("a. Reabastecer producto directo (embotellados, paquetes)");
                            Console.WriteLine("b. Reabastecer insumos de barra (leche, café, etc.)");
                            Console.WriteLine("c. Volver");
                            Console.Write("Seleccione opción (a/b/c): ");
                            string subReab = (Console.ReadLine() ?? "").Trim().ToLower();

                            if (subReab == "a")
                            {
                                string critEntrada = ConsolaHelper.LeerTextoNoVacio("Pase el código o nombre del producto: ");
                                ProductoTerminado? prodEntrada = inventarioBD.BuscarProductoUniversal(critEntrada);

                                if (prodEntrada != null)
                                {
                                    if (inventarioBD.TieneReceta(prodEntrada.Sku))
                                    {
                                        Console.WriteLine("-> Este artículo se prepara con receta; no tiene existencias directas. Use la opción 'b'.");
                                    }
                                    else
                                    {
                                        decimal cant = ConsolaHelper.LeerDecimalPositivo("Cantidad de piezas que ingresan: ");
                                        inventarioBD.ReabastecerStock(prodEntrada.Sku, cant);
                                    }
                                }
                            }
                            else if (subReab == "b")
                            {
                                inventarioBD.ListarInsumosConId();
                                long idInsumo = ConsolaHelper.LeerEnteroPositivo("\nEscriba el número ID del insumo a reabastecer: ");
                                decimal cantInsumo = ConsolaHelper.LeerDecimalPositivo("Cantidad a ingresar en almacén: ");
                                inventarioBD.ReabastecerInsumo(idInsumo, cantInsumo);
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "5":
                            Console.WriteLine("\n--- ALTA DE PRODUCTOS Y RECETAS ---");
                            Console.WriteLine("a. Producto de venta directa");
                            Console.WriteLine("b. Materia prima / Insumo para barra");
                            Console.WriteLine("c. Bebida o producto preparado (con receta)");
                            Console.WriteLine("d. Volver");
                            Console.Write("Seleccione opción (a/b/c/d): ");
                            string subAlta = (Console.ReadLine() ?? "").Trim().ToLower();

                            if (subAlta == "a")
                            {
                                string sku = ConsolaHelper.LeerTextoNoVacio("Código o SKU: ");
                                string nom = ConsolaHelper.LeerTextoNoVacio("Nombre: ");
                                decimal pre = ConsolaHelper.LeerDecimalPositivo("Precio al cliente ($): ");
                                decimal cos = ConsolaHelper.LeerDecimalPositivo("Costo de compra ($): ", true);
                                decimal stk = ConsolaHelper.LeerDecimalPositivo("Stock inicial: ", true);
                                decimal min = ConsolaHelper.LeerDecimalPositivo("Mínimo para alerta de poco stock: ", true);

                                inventarioBD.RegistrarNuevoProducto(sku, nom, pre, cos, stk, min);
                            }
                            else if (subAlta == "b")
                            {
                                string nomInsumo = ConsolaHelper.LeerTextoNoVacio("Nombre del insumo: ");
                                string unidad = ConsolaHelper.LeerTextoNoVacio("Unidad de medida (g, ml, pza): ");
                                decimal stkInsumo = ConsolaHelper.LeerDecimalPositivo("Stock inicial: ", true);
                                decimal minInsumo = ConsolaHelper.LeerDecimalPositivo("Mínimo para alerta: ", true);

                                inventarioBD.RegistrarNuevoInsumo(nomInsumo, unidad, stkInsumo, minInsumo);
                            }
                            else if (subAlta == "c")
                            {
                                string skuRec = ConsolaHelper.LeerTextoNoVacio("Código o SKU para este preparado: ");
                                string nomRec = ConsolaHelper.LeerTextoNoVacio("Nombre de la preparación: ");
                                decimal precioRec = ConsolaHelper.LeerDecimalPositivo("Precio al cliente ($): ");
                                decimal costoRec = ConsolaHelper.LeerDecimalPositivo("Costo aproximado ($): ", true);

                                if (inventarioBD.RegistrarProductoElaborado(skuRec, nomRec, precioRec, costoRec))
                                {
                                    inventarioBD.ListarInsumosConId();
                                    bool mas = true;
                                    while (mas)
                                    {
                                        long idIns = ConsolaHelper.LeerEnteroPositivo("\nEscriba el número ID del insumo a incluir: ");
                                        decimal cantReq = ConsolaHelper.LeerDecimalPositivo("Cantidad requerida por taza/porción: ");
                                        inventarioBD.AgregarIngredienteAReceta(skuRec, idIns, cantReq);

                                        Console.Write("¿Desea agregar otro ingrediente a esta preparación? (s/n): ");
                                        if ((Console.ReadLine() ?? "").Trim().ToLower() != "s") mas = false;
                                    }
                                }
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "6":
                            string critElim = ConsolaHelper.LeerTextoNoVacio("Escriba el código o nombre del producto a dar de baja: ");
                            ProductoTerminado? pElim = inventarioBD.BuscarProductoUniversal(critElim);
                            if (pElim != null)
                            {
                                Console.Write($"¿Está seguro de eliminar '{pElim.Nombre}' [{pElim.Sku}]? (s/n): ");
                                if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                                    inventarioBD.EliminarProducto(pElim.Sku);
                                else
                                    Console.WriteLine("-> Operación cancelada.");
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "7":
                            inventarioBD.ListarInsumosBarra();
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "8":
                            Console.WriteLine("\n--- CONSULTA DE AUDITORÍA Y BITÁCORA ---");
                            Console.WriteLine("1. Ver ventas y movimientos del turno actual");
                            Console.WriteLine("2. Ver últimos 25 movimientos generales");
                            Console.WriteLine("3. Filtrar por fecha (Año-Mes-Día)");
                            Console.WriteLine("4. Volver");
                            Console.Write("Seleccione opción: ");
                            string opcBit = (Console.ReadLine() ?? "").Trim();

                            switch (opcBit)
                            {
                                case "1":
                                    inventarioBD.VerHistorialMovimientos("TURNO_ACTUAL");
                                    break;
                                case "2":
                                    inventarioBD.VerHistorialMovimientos("RECIENTES");
                                    break;
                                case "3":
                                    string fecha = ConsolaHelper.LeerTextoNoVacio("Ingrese la fecha (ejemplo: 2026-09-21): ");
                                    inventarioBD.VerHistorialMovimientos("FECHA", fecha);
                                    break;
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "9":
                            Console.WriteLine("\n--- CENTRO DE REPORTES Y CORTE DE CAJA ---");
                            Console.WriteLine("1. Ver corte de dinero del turno actual");
                            Console.WriteLine("2. Ver los 10 productos más vendidos");
                            Console.WriteLine("3. Ver bajas y mermas del turno");
                            Console.WriteLine("4. Cerrar turno actual (Reinicia balance a 0)");
                            Console.WriteLine("5. Volver");
                            Console.Write("Seleccione opción: ");
                            string opcRep = (Console.ReadLine() ?? "").Trim();

                            switch (opcRep)
                            {
                                case "1":
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
                                    break;

                                case "2":
                                    cajaBD.MostrarTopProductosVendidos(10);
                                    break;

                                case "3":
                                    cajaBD.MostrarResumenBajasTurno();
                                    break;

                                case "4":
                                    Console.Write("¿Está seguro de cerrar el turno y poner el balance en 0? (s/n): ");
                                    if ((Console.ReadLine() ?? "").Trim().ToLower() == "s")
                                    {
                                        cajaBD.CerrarTurnoCaja();
                                        Console.WriteLine("-> Turno cerrado exitosamente.");
                                    }
                                    break;
                            }
                            ConsolaHelper.PausaContinuar();
                            break;

                        case "10":
                            Console.WriteLine("Cerrando el sistema de forma segura...");
                            salir = true;
                            break;

                        default:
                            Console.WriteLine("-> Opción no reconocida. Por favor elija un número de la lista.");
                            ConsolaHelper.PausaContinuar();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Blindaje total: cualquier error imprevisto se notifica amablemente sin que el programa se caiga
                    Console.WriteLine("\n-> Nota: Ocurrió un inconveniente momentáneo al procesar la información.");
                    Console.WriteLine($"-> Detalle: {ex.Message}");
                    Console.WriteLine("-> El sistema continuará funcionando normalmente.");
                    ConsolaHelper.PausaContinuar();
                }

            } while (!salir);
        }
    }
}