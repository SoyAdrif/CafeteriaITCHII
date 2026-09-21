# Sistema de Gestión de Inventario y Punto de Venta (POS) - Cafetería ITCH II

Sistema modular para el control de inventario, recetas de barra, venta en mostrador y cortes de caja desarrollado en **C# (.NET 10)** y persistencia relacional con **SQLite**. Diseñado bajo los principios de la **Programación Orientada a Objetos (POO)** y la metodología de desarrollo de software del **Tecnológico Nacional de México Campus Chihuahua II**.

---

## 1. Metodología de Desarrollo (TecNM II)

El proyecto se estructuró siguiendo las etapas formales de ingeniería de software:
1. **Análisis del problema y requerimientos:** Identificación de entradas (SKU alfanumérico, códigos de barra EAN-13, teclado rápido, nombres), restricciones de stock y salidas deseadas (bitácora, deducciones y corte financiero).
2. **Diseño y estructuración de clases:** 
   - Abstracción de entidades base (`ItemInventarioBase`).
   - Jerarquía de generalización/especialización (Herencia) para productos directos y preparados.
   - Relación de composición estricta para el modelado de recetas e insumos.
3. **Implementación y desacoplamiento:** Separación en capas de **Modelos** y **Servicios** con persistencia atómica mediante transacciones SQL.
4. **Prueba y verificación final:** Validación con conjuntos de datos reales (ventas unitarias, recetas compuestas, prevención de sobreventa y cierres de turno).
5. **Mantenimiento y actualización:** Incorporación de búsqueda universal polimórfica (*Smart Search*) y umbrales de stock mínimo configurables por producto.

---

## 2. Diagrama de Clases UML (Estructura y Relaciones)

El siguiente diagrama modela la arquitectura completa del backend, reflejando generalización, composición, encapsulamiento y dependencias de servicio:

```mermaid
classDiagram
    direction TB

    %% Jerarquía de Herencia (Generalización)
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

    %% Servicios del Sistema
    class BaseDatosServicio {
        -string CadenaConexion
        +ObtenerConexion() SqliteConnection
        -InicializarBaseDatos() void
    }

    class InventarioServicio {
        -BaseDatosServicio _bd
        +ListarProductos() void
        +ListarInsumosBarra() void
        +RegistrarVenta(string sku, decimal cantidad) bool
        +VenderProductoElaborado(string sku, decimal porciones) bool
        +ReabastecerStock(string sku, decimal cantidad) bool
        +BuscarProductoUniversal(string criterio) ProductoTerminado
        +VerHistorialMovimientos() void
    }

    class CajaServicio {
        -BaseDatosServicio _bd
        +GenerarCorteTurno() ReporteCorteCaja
        +CerrarTurnoCaja() void
    }

    %% Relaciones UML
    ItemInventarioBase <|-- ProductoTerminado : Herencia (Generalización)
    ItemInventarioBase <|-- ProductoElaborado : Herencia (Generalización)
    ProductoElaborado *-- Ingrediente : Composición (1..*)
    
    InventarioServicio ..> BaseDatosServicio : Usa
    CajaServicio ..> BaseDatosServicio : Usa
    InventarioServicio ..> ProductoTerminado : Retorna / Manipula
    CajaServicio ..> ReporteCorteCaja : Genera
    InventarioServicio ..> MovimientoInventario : Registra
```

---

## 3. Diagrama de Secuencia: Venta con Smart Search y Descuento Atómico

Representa el flujo de control, llamadas y persistencia al procesar una venta desde la consola:

```mermaid
sequenceDiagram
    autonumber
    actor Cajero
    participant Program as Vista / Consola
    participant Inventario as InventarioServicio
    participant BD as SQLite (cafeteria.db)

    Cajero->>Program: Ingresa texto, código rápido o escaneo de código de barras
    Program->>Inventario: BuscarProductoUniversal(criterio)
    Inventario->>BD: SELECT con WHERE sku = @criterio OR nombre LIKE @criterio
    BD-->>Inventario: Registros coincidentes
    alt Coincidencia única
        Inventario-->>Program: Instancia de ProductoTerminado
    else Coincidencias múltiples
        Inventario->>Program: Despliega menú de opciones numeradas
        Cajero->>Program: Selecciona número de opción
        Program-->>Inventario: Retorna selección
        Inventario-->>Program: ProductoTerminado elegido
    end

    Program->>Cajero: Solicita cantidad a despachar
    Cajero->>Program: Ingresa unidades (ej. 2)
    Program->>Inventario: RegistrarVenta(sku, cantidad)
    Inventario->>BD: BeginTransaction()
    Inventario->>BD: UPDATE productos SET stock = stock - cantidad
    Inventario->>BD: INSERT INTO movimientos (Auditoría de venta)
    Inventario->>BD: CommitTransaction()
    BD-->>Inventario: Operación confirmada
    Inventario-->>Program: true (Venta exitosa)
    Program-->>Cajero: Muestra confirmación de venta y saldo restante
```

---

## 4. Ejecución del Proyecto

1. Posicionarse en la carpeta del ejecutable:
   ```bash
   cd /workspaces/CafeteriaITCHII/CafeteriaInventario
   ```
2. Ejecutar la aplicación:
   ```bash
   dotnet run
   ```