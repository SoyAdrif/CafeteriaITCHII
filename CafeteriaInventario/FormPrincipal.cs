using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CafeteriaInventario.Modelos;
using CafeteriaInventario.Servicios;

namespace CafeteriaInventario
{
    public class FormPrincipal : Form
    {
        private readonly InventarioServicio _inventario;
        private readonly CajaServicio _caja;
        private readonly List<ItemVentaTemporal> _carrito;

        // Controles de interfaz
// Controles de interfaz inicializados en InicializarComponentes()
    private TextBox txtEntrada = null!;
    private NumericUpDown numCantidad = null!;
    private Button btnAgregar = null!;
    private DataGridView dgvTicket = null!;
    private Label lblTotal = null!;
    private Button btnCobrar = null!;
    private Button btnLimpiar = null!;
    private Button btnCorte = null!;

        public FormPrincipal()
        {
            _inventario = new InventarioServicio();
            _caja = new CajaServicio();
            _carrito = new List<ItemVentaTemporal>();

            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            // Propiedades de la ventana
            Text = "Punto de Venta - Cafetería ITCH II";
            Size = new Size(880, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 11);

            // Panel superior de captura
            Panel pnlSuperior = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(10) };

            Label lblBuscar = new Label { Text = "Código / Producto:", AutoSize = true, Location = new Point(15, 12) };
            txtEntrada = new TextBox { Location = new Point(15, 35), Width = 300 };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;

            Label lblCant = new Label { Text = "Cantidad:", AutoSize = true, Location = new Point(330, 12) };
            numCantidad = new NumericUpDown { Location = new Point(330, 35), Width = 80, Minimum = 1, Maximum = 100, Value = 1 };

            btnAgregar = new Button { Text = "Agregar (+)", Location = new Point(425, 33), Width = 110, Height = 32 };
            btnAgregar.Click += (s, e) => AgregarProducto();

            pnlSuperior.Controls.AddRange(new Control[] { lblBuscar, txtEntrada, lblCant, numCantidad, btnAgregar });

            // Panel lateral de cobro y acciones
            Panel pnlLateral = new Panel { Dock = DockStyle.Right, Width = 260, Padding = new Padding(15) };

            Label lblTextoTotal = new Label { Text = "TOTAL A PAGAR:", AutoSize = true, Location = new Point(15, 20), ForeColor = Color.Gray };
            lblTotal = new Label { Text = "$0.00", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.DarkGreen, Location = new Point(15, 45), AutoSize = true };

            btnCobrar = new Button { Text = "COBRAR TICKET", Location = new Point(15, 120), Width = 225, Height = 55, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            btnCobrar.Click += (s, e) => CobrarTicket();

            btnLimpiar = new Button { Text = "Cancelar Orden", Location = new Point(15, 190), Width = 225, Height = 35 };
            btnLimpiar.Click += (s, e) => LimpiarCarrito();

            btnCorte = new Button { Text = "Corte de Turno", Location = new Point(15, 240), Width = 225, Height = 35 };
            btnCorte.Click += (s, e) => MostrarCorte();

            pnlLateral.Controls.AddRange(new Control[] { lblTextoTotal, lblTotal, btnCobrar, btnLimpiar, btnCorte });

            // Tabla central de artículos del ticket
            dgvTicket = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };

            dgvTicket.Columns.Add("Sku", "SKU");
            dgvTicket.Columns.Add("Nombre", "Descripción");
            dgvTicket.Columns.Add("Precio", "P. Unitario");
            dgvTicket.Columns.Add("Cantidad", "Cant.");
            dgvTicket.Columns.Add("Subtotal", "Subtotal");

            // Agregar a la ventana
            Controls.Add(dgvTicket);
            Controls.Add(pnlLateral);
            Controls.Add(pnlSuperior);
        }

        private void TxtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evitar sonido beep
                AgregarProducto();
            }
        }

        private void AgregarProducto()
        {
            string texto = txtEntrada.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            var prod = _inventario.BuscarProductoUniversal(texto);
            if (prod == null)
            {
                MessageBox.Show("Producto no encontrado en el catálogo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntrada.SelectAll();
                return;
            }

            decimal cant = numCantidad.Value;
            bool esReceta = _inventario.TieneReceta(prod.Sku);

            var itemExistente = _carrito.FirstOrDefault(x => x.Producto.Sku == prod.Sku);
            if (itemExistente != null)
            {
                itemExistente.Cantidad += cant;
            }
            else
            {
                _carrito.Add(new ItemVentaTemporal(prod, cant, esReceta));
            }

            txtEntrada.Clear();
            numCantidad.Value = 1;
            txtEntrada.Focus();
            ActualizarTabla();
        }

        private void ActualizarTabla()
        {
            dgvTicket.Rows.Clear();
            decimal total = 0;

            foreach (var it in _carrito)
            {
                dgvTicket.Rows.Add(it.Producto.Sku, it.Producto.Nombre, $"${it.Producto.Precio:F2}", it.Cantidad, $"${it.Subtotal:F2}");
                total += it.Subtotal;
            }

            lblTotal.Text = $"${total:F2}";
        }

        private void CobrarTicket()
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("No hay artículos en la orden para cobrar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dialogResult = MessageBox.Show($"¿Desea confirmar el cobro por {lblTotal.Text}?", "Confirmar Venta", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                bool exito = _inventario.ProcesarTicketVenta(_carrito);
                if (exito)
                {
                    MessageBox.Show("Venta completada con éxito. Inventario actualizado.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarCarrito();
                }
            }
        }

        private void LimpiarCarrito()
        {
            _carrito.Clear();
            ActualizarTabla();
            txtEntrada.Focus();
        }

        private void MostrarCorte()
        {
            var corte = _caja.GenerarCorteTurno();
            string msg = $"--- CORTE DE TURNO ACTUAL ---\n\n" +
                         $"Transacciones de Venta: {corte.TotalTransaccionesVenta}\n" +
                         $"Unidades Despachadas:   {corte.TotalUnidadesVendidas}\n" +
                         $"Total Cobrado:          ${corte.TotalIngresos:F2}\n" +
                         $"Utilidad Estimada:      ${corte.TotalGananciaEstimada:F2}";

            MessageBox.Show(msg, "Resumen Financiero", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}