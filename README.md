# Sistema de Gestión de Inventario y Punto de Venta (POS) - Cafetería ITCH II

Sistema integral para la administración de inventario, recetas de barra, venta de mostrador multilínea (estilo supermercado/ticket atómico), promociones automáticas y auditoría financiera desarrollado en **C# (.NET 10)** con persistencia relacional en **SQLite**. Desarrollado bajo los principios de la **Programación Orientada a Objetos (POO)** y la metodología de ingeniería de software del **Tecnológico Nacional de México Campus Chihuahua II**.

---

## 1. Fundamentos Arquitectónicos y Metodología (TecNM II)

* **Abstracción y Encapsulamiento:** Ocultamiento de la representación interna de datos y exposición de operaciones de negocio mediante contratos de servicio.
* **Herencia (Generalización):** Clase base abstracta `ItemInventarioBase` especializada en `ProductoTerminado` (físico directo) y `ProductoElaborado` (artículo elaborado por receta).
* **Polimorfismo:** Sobrescritura (`override`) de `DescontarExistencias` y sobrecarga (`overload`) de métodos para ventas regulares vs. ventas con descuentos o promociones combinadas.
* **Composición y Agregación:** 
  * `ProductoElaborado` mantiene una relación existencial fuerte de composición con sus `Ingrediente` (1..*).
  * El ticket de compra utiliza agregación temporal con instancias de `ItemVentaTemporal`.
* **Transacciones Atómicas (ACID):** Despacho seguro de órdenes multilínea (`ProcesarTicketVenta`) donde la deducción de ingredientes de recetas y stock físico ocurre dentro de un bloque transaccional (`BeginTransaction` / `Commit` / `Rollback`).
* **Blindaje de Entradas:** Validación centralizada mediante `ConsolaHelper` para mitigar errores de captura sin interrumpir el ciclo de vida de la aplicación.

---

## 2. Diagrama de Clases UML

```mermaid
classDiagram
    direction TB

    class ItemInventarioBase {
        <<abstract>>
        +long Id
        +string Sku
        +string Nombre
        +decimal Precio
        +decimal Costo
        +decimal Existencias
        +decimal StockMinimo
        +bool TieneBajoStock
        +DescontarExistencias(decimal cantidad)* bool
    }

    class ProductoTerminado {
        +ProductoTerminado(long id, string sku, string nombre, decimal precio, decimal costo, decimal stockInicial)
        +DescontarExistencias(decimal cantidad) bool
    }

    class ProductoElaborado {
        -List~Ingrediente~ Receta
        +ProductoElaborado(long id, string sku, string nombre, decimal precio, decimal costo)
        +AgregarIngrediente(Ingrediente ingrediente) void
        +DescontarExistencias(decimal porciones) bool
    }

    class Ingrediente {
        +long Id
        +string Nombre
        +decimal CantidadRequerida
        +string UnidadMedida
        +Ingrediente(long id, string nombre, decimal cantidadRequerida, string unidadMedida)
    }

    class ItemVentaTemporal {
        +ProductoTerminado Producto
        +decimal Cantidad
        +bool EsReceta
        +decimal Subtotal
        +ItemVentaTemporal(ProductoTerminado producto, decimal cantidad, bool esReceta)
    }

    class MovimientoInventario {
        +long Id
        +string ProductoSku
        +TipoMovimiento Tipo
        +decimal Cantidad
        +string Motivo
        +DateTime Fecha
        +string EstadoTurno
    }

    class ReporteCorteCaja {
        +DateTime FechaGeneracion
        +int TotalTransaccionesVenta
        +decimal TotalUnidadesVendidas
        +decimal TotalIngresos
        +decimal TotalGananciaEstimada
    }

    class BaseDatosServicio {
        -string CadenaConexion
        +ObtenerConexion() SqliteConnection
        -InicializarBaseDatos() void
    }

    class InventarioServicio {
        -BaseDatosServicio _bd
        +ListarProductos() void
        +ListarInsumosBarra() void
        +ListarInsumosConId() void
        +BuscarProductoUniversal(string criterio) ProductoTerminado
        +TieneReceta(string sku) bool
        +RegistrarVenta(string sku, decimal cantidad) bool
        +RegistrarVenta(string sku, decimal cantidad, decimal porcentajeDescuento) bool
        +VenderProductoElaborado(string sku, decimal porciones) bool
        +VenderProductoElaborado(string sku, decimal porciones, decimal porcentajeDescuento) bool
        +ProcesarTicketVenta(List~ItemVentaTemporal~ items) bool
        +ReabastecerStock(string sku, decimal cantidad) bool
        +ReabastecerInsumo(long insumoId, decimal cantidad) bool
        +RegistrarNuevoProducto(string sku, string nombre, decimal precio, decimal costo, decimal stock, decimal minimo) bool
        +RegistrarNuevoInsumo(string nombre, string unidad, decimal stock, decimal minimo) bool
        +RegistrarProductoElaborado(string sku, string nombre, decimal precio, decimal costo) bool
        +AgregarIngredienteAReceta(string sku, long insumoId, decimal cantidadRequerida) bool
        +EliminarProducto(string sku) bool
        +VerHistorialMovimientos(string filtro, string valorFiltro) void
    }

    class CajaServicio {
        -BaseDatosServicio _bd
        +GenerarCorteTurno() ReporteCorteCaja
        +MostrarTopProductosVendidos(int top) void
        +MostrarResumenBajasTurno() void
        +CerrarTurnoCaja() void
    }

    class ConsolaHelper {
        <<static>>
        +LeerDecimalPositivo(string mensaje, bool permitirCero) decimal
        +LeerEnteroPositivo(string mensaje) long
        +LeerTextoNoVacio(string mensaje) string
        +PausaContinuar() void
    }

    ItemInventarioBase <|-- ProductoTerminado : Herencia
    ItemInventarioBase <|-- ProductoElaborado : Herencia
    ProductoElaborado *-- Ingrediente : Composición (1..*)
    ItemVentaTemporal o-- ProductoTerminado : Agregación
    
    InventarioServicio ..> BaseDatosServicio : Dependencia
    CajaServicio ..> BaseDatosServicio : Dependencia
    InventarioServicio ..> ItemVentaTemporal : Procesa
    InventarioServicio ..> MovimientoInventario : Registra
    CajaServicio ..> ReporteCorteCaja : Genera
```

---

## 3. Diagrama de Secuencia: Venta Multilínea y Liquidación Atómica

```mermaid
sequenceDiagram
    autonumber
    actor Cajero
    participant Vista as Program (Consola / GUI)
    participant Carrito as List~ItemVentaTemporal~
    participant Servicio as InventarioServicio
    participant BD as SQLite (cafeteria.db)

    loop Captura de Artículos (Estilo Supermercado)
        Cajero->>Vista: Escanea código o ingresa nombre + cantidad
        Vista->>Servicio: BuscarProductoUniversal(criterio)
        Servicio->>BD: SELECT producto / receta
        BD-->>Servicio: Registro de catálogo
        Servicio-->>Vista: Instancia ProductoTerminado
        Vista->>Carrito: Add(ItemVentaTemporal)
        Carrito-->>Vista: Subtotal acumulado en pantalla
    end

    Cajero->>Vista: Comando 'cobrar' / 'pagar'