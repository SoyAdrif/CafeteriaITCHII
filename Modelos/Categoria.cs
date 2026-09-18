namespace CafeteriaInventario.Modelos
{
    public class Categoria
    {
        public long Id { get; set; }
        public long? CategoriaPadreId { get; set; }
        public string Nombre { get; set; }
        public string Slug { get; set; }
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;

        public Categoria(long id, string nombre, string slug)
        {
            Id = id;
            Nombre = nombre;
            Slug = slug;
        }
    }
}