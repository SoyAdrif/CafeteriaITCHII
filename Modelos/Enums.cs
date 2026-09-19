namespace CafeteriaInventario.Modelos
{
    public enum TipoProducto
    {
        Finished,
        Ingredient,
        Combo,
        Service
    }

    public enum AreaPreparacion
    {
        Kitchen,
        Bar,
        Both,
        None
    }

    public enum TipoMovimiento
    {
        Purchase,
        Sale,
        Adjustment,
        Waste,
        TransferIn,
        TransferOut,
        Return,
        Production
    }
}