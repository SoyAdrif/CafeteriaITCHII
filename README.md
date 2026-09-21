# Sistema de Gestión de Inventario y Punto de Venta (POS) - Cafetería ITCH II

Sistema integral para la administración de inventario, recetas de barra, reabastecimiento, venta en mostrador y auditoría financiera desarrollado en **C# (.NET 10)** con persistencia relacional en **SQLite**. Desarrollado bajo los principios de la **Programación Orientada a Objetos (POO)** y la metodología de ingeniería de software del **Tecnológico Nacional de México Campus Chihuahua II**.

---

## 1. Fundamentos Arquitectónicos y Metodología (TecNM II)

El sistema se diseñó cubriendo las etapas de análisis, diseño, codificación y verificación:
* **Abstracción y Encapsulamiento:** Clases base con atributos protegidos/públicos y operaciones de negocio controladas.
* **Herencia (Generalización):** Clase abstracta `ItemInventarioBase` como superclase de `ProductoTerminado` y `ProductoElaborado`.
* **Polimorfismo:** Implementación del método abstracto `DescontarExistencias(decimal)` redefinido (`override`) según el comportamiento de la entidad.
* **Composición:** Relación existencial fuerte entre un producto preparado y su lista de insumos (`Ingrediente`).
* **Búsqueda Universal (Smart Search):** Algoritmo de resolución polimórfica que interpreta códigos de barra (EAN-13), códigos numéricos de teclado rápido o cadenas de texto parciales.

---

## 2. Diagrama de Clases UML

```mermaid
classDiagram
    direction TB

    %% Jerarquía de Herencia
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

    %% Capa de Servicios
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
        +VenderProductoElaborado(string sku, decimal porciones) bool
        +ReabastecerStock(string sku, decimal cantidad) bool
        +ReabastecerInsumo(long insumoId, decimal cantidad) bool
        +RegistrarNuevoProducto(string sku, string nombre, decimal precio, decimal costo, decimal stock, decimal minimo) bool
        +RegistrarNuevoInsumo(string nombre, string unidad, decimal stock, decimal minimo) bool
        +RegistrarProductoElaborado(string sku, string nombre, decimal precio, decimal costo) bool
        +AgregarIngredienteAReceta(string sku, long insumoId, decimal cantidadRequerida) bool
        +EliminarProducto(string sku) bool
        +VerHistorialMovimientos() void
    }

    class CajaServicio {
        -BaseDatosServicio _bd
        +GenerarCorteTurno() ReporteCorteCaja
        +CerrarTurnoCaja() void
    }

    %% Relaciones
    ItemInventarioBase <|-- ProductoTerminado : Herencia
    ItemInventarioBase <|-- ProductoElaborado : Herencia
    ProductoElaborado *-- Ingrediente : Composición (1..*)
    
    InventarioServicio ..> BaseDatosServicio : Dependencia
    CajaServicio ..> BaseDatosServicio : Dependencia
    InventarioServicio ..> ProductoTerminado : Manipula
    InventarioServicio ..> MovimientoInventario : Registra
    CajaServicio ..> ReporteCorteCaja : Genera
```

---

## 3. Diagrama de Secuencia: Despacho Polimórfico (Directo vs Receta)

```mermaid
sequenceDiagram
    autonumber
    actor Cajero
    participant Vista as Program (Consola)
    participant Servicio as InventarioServicio
    participant BD as SQLite (cafeteria.db)

    Cajero->>Vista: Ingresa código, escaneo de barras o nombre
    Vista->>Servicio: BuscarProductoUniversal(criterio)
    Servicio->>BD: SELECT con coincidencia exacta de SKU o parcial por nombre
    BD-->>Servicio: Registro encontrado
    Servicio-->>Vista: Instancia de ProductoTerminado

    Vista->>Servicio: TieneReceta(sku)
    Servicio->>BD: SELECT COUNT(*) FROM recetas WHERE producto_sku = @sku
    BD-->>Servicio: Conteo de recetas

    alt El artículo es una Receta / Preparado
        Servicio-->>Vista: true
        Vista->>Cajero: Solicita porciones / vasos a preparar
        Cajero->>Vista: Ingresa cantidad (ej. 2)
        Vista->>Servicio: VenderProductoElaborado(sku, porciones)
        Servicio->>BD: BeginTransaction()
        Servicio->>BD: Verificar existencias de materias primas
        Servicio->>BD: UPDATE insumos (Deducción por porción)
        Servicio->>BD: INSERT INTO movimientos (Auditoría de venta)
        Servicio->>BD: CommitTransaction()
    else El artículo es un Producto Directo
        Servicio-->>Vista: false
        Vista->>Cajero: Solicita piezas a vender
        Cajero->>Vista: Ingresa cantidad (ej. 1)
        Vista->>Servicio: RegistrarVenta(sku, cantidad)
        Servicio->>BD: BeginTransaction()
        Servicio->>BD: UPDATE productos (Deducción de stock directo)
        Servicio->>BD: INSERT INTO movimientos (Auditoría de venta)
        Servicio->>BD: CommitTransaction()
    end

    BD-->>Servicio: Confirmación de persistencia
    Servicio-->>Vista: Operación exitosa
    Vista-->>Cajero: Mensaje de confirmación en pantalla
```

---

## 4. Instrucciones de Ejecución

1. Abrir terminal en el directorio del proyecto:
   ```bash
   cd /workspaces/CafeteriaITCHII/CafeteriaInventario
   ```
2. Ejecutar la solución:
   ```bash
   dotnet run
   ```