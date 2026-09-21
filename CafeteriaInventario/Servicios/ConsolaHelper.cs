using System;

namespace CafeteriaInventario.Servicios
{
    public static class ConsolaHelper
    {
        // Lee un número decimal positivo guiando al usuario si comete un error
        public static decimal LeerDecimalPositivo(string mensaje, bool permitirCero = false)
        {
            while (true)
            {
                Console.Write(mensaje);
                string entrada = (Console.ReadLine() ?? "").Trim();

                if (decimal.TryParse(entrada, out decimal valor))
                {
                    if (permitirCero && valor >= 0)
                        return valor;

                    if (!permitirCero && valor > 0)
                        return valor;

                    Console.WriteLine("-> Por favor, ingrese un número mayor a cero.");
                }
                else
                {
                    Console.WriteLine("-> Entrada no válida. Ingrese solo números (ejemplo: 15 o 25.50).");
                }
            }
        }

        // Lee un número entero positivo para menús y selecciones
        public static long LeerEnteroPositivo(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string entrada = (Console.ReadLine() ?? "").Trim();

                if (long.TryParse(entrada, out long valor) && valor > 0)
                {
                    return valor;
                }

                Console.WriteLine("-> Por favor, escriba únicamente un número entero válido.");
            }
        }

        // Lee un texto asegurando que no se envíe vacío o con solo espacios
        public static string LeerTextoNoVacio(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string entrada = (Console.ReadLine() ?? "").Trim();

                if (!string.IsNullOrEmpty(entrada))
                {
                    return entrada;
                }

                Console.WriteLine("-> Este dato no puede quedarse en blanco. Escriba algo para continuar.");
            }
        }

        // Pausa amigable para que el usuario pueda leer los resultados
        public static void PausaContinuar()
        {
            Console.WriteLine("\n[Presione cualquier tecla para continuar...]");
            Console.ReadKey(true);
        }
    }
}