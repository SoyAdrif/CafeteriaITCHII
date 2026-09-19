namespace CafeteriaInventario.Modelos
{
    public class Ingrediente
    {
        public string Nombre { get; set; }
        public decimal StockGramosOMl { get; set; }

        public Ingrediente(string nombre, decimal stock)
        {
            Nombre = nombre;
            StockGramosOMl = stock;
        }
    }
}