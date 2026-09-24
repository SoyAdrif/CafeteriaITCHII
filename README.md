# CAFETERÍA - CONTROL TOTAL | Sistema Administrativo y Operativo

> Sistema de Punto de Venta (POS) y Control de Inventarios desarrollado para la Cafetería del Instituto Tecnológico de Chihuahua II (ITCH II).

---

## 1. Descripción General del Proyecto
Este software provee una solución integral de escritorio para la administración operativa del punto de venta en cafetería. Diseñado con una arquitectura modular por capas orientada a objetos, permite la captura ágil de ventas, control de inventario de materias primas y productos terminados con recetas, corte de turnos de caja en tiempo real y persistencia en base de datos local SQLite.

La interfaz de usuario implementa un esquema visual institucional de alto rendimiento, optimizado para equipos de cómputo de recursos moderados en el punto de cobro.

---

## 2. Pila Tecnológica (Tech Stack)
* **Lenguaje:** C# (.NET 10.0)
* **Framework Gráfico:** Windows Forms (WinForms / GDI+)
* **Base de Datos:** SQLite 3 (vía `Microsoft.Data.Sqlite`)
* **Control de Versiones:** Git & GitHub

---

## 3. Arquitectura y Diseño Orientado a Objetos

El sistema aplica una separación clara de responsabilidades:

```text
CafeteriaInventario/
├── Modelos/             # Entidades del dominio (ProductoTerminado, ItemVentaTemporal, CorteTurno)
├── Servicios/           # Capa de lógica de negocio y persistencia (InventarioServicio, CajaServicio, BaseDatosServicio)
├── FormPrincipal.cs     # Interfaz principal de ventas y comanda
└── FormCatalogo.cs      # Ventana modal para exploración interactiva de productos y existencias