using System;
using System.Collections.Generic;

namespace CafeteriaInventario.Modelos
{
    // Representa un producto que requiere insumos para su preparación
    public class ProductoElaborado : ItemInventarioBase
    {
        // Relación de composición: contiene una lista de insumos necesarios por porción
        public Dictionary<Ingrediente, decimal> RecetaPorcion { get; set; } = new Dictionary<Ingrediente, decimal>();

        public ProductoElaborado(long id, string sku, string nombre, decimal precio, decimal costo)
            : base(id, sku, nombre, precio, costo)
        {
        }

        public void AgregarInsumoAReceta(Ingrediente ingrediente, decimal cantidadRequerida)
        {
            RecetaPorcion[ingrediente] = cantidadRequerida;
        }

        // Sobrescritura (overriding): descuenta insumos en lugar de stock propio
        public override bool DescontarExistencias(decimal cantidadPorciones)
        {
            if (cantidadPorciones <= 0) return false;

            // Verificar si alcanzan todos los ingredientes
            foreach (var item in RecetaPorcion)
            {
                decimal totalNecesario = item.Value * cantidadPorciones;
                if (item.Key.StockGramosOMl < totalNecesario)
                {
                    Console.WriteLine($"Insumo insuficiente: {item.Key.Nombre} (Faltan insumos).");
                    return false;
                }
            }

            // Descontar cada insumo si la validación fue exitosa
            foreach (var item in RecetaPorcion)
            {
                item.Key.StockGramosOMl -= (item.Value * cantidadPorciones);
            }

            return true;
        }
    }
}