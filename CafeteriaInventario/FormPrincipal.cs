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
        private TextBox txtEntrada = null!;
        private NumericUpDown numCantidad = null!;
        private Button btnAgregar = null!;
        private DataGridView dgvTicket = null!;
        private Label lblTotal = null!;
        private Button btnCobrar = null!;
        private Button btnQuitarItem = null!;
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
            // Propiedades de la ventana principal
            Text = "Punto de Venta - Cafetería ITCH II";
            Size = new Size(950, 600);
            MinimumSize = new Size(880, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10.5f);
            BackColor = Color.FromArgb(245, 246, 250);

            // Panel superior de captura
            Panel pnlSuperior = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };

            Label lblBuscar = new Label { Text = "Código de barras o Nombre:", AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtEntrada = new TextBox { Location = new Point(15, 38), Width = 340, Font = new Font("Segoe UI", 12f) };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;

            Label lblCant = new Label { Text = "Cantidad:", AutoSize = true, Location = new Point(375, 12), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            numCantidad = new NumericUpDown { Location = new Point(375, 38), Width = 90, Font = new Font("Segoe UI", 12f), Minimum = 1, Maximum = 100, Value = 1 };

            btnAgregar = new Button
            {
                Text = "Agregar (+)",
                Location = new Point(480, 36),
                Width = 120,
                Height = 34,
                BackColor = Color.FromArgb(220, 235, 252),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnAgregar.FlatAppearance.BorderColor = Color.FromArgb(180, 205, 235);
            btnAgregar.Click += (s, e) => AgregarProducto();

            pnlSuperior.Controls.AddRange(new Control[] { lblBuscar, txtEntrada, lblCant, numCantidad, btnAgregar });

            // Panel lateral de cobro y acciones
            Panel pnlLateral = new Panel
            {
                Dock = DockStyle.Right,
                Width = 270,
                Padding = new Padding(15),
                BackColor = Color.White
            };

            Label lblTextoTotal = new Label { Text = "TOTAL A PAGAR:", AutoSize = true, Location = new Point(15, 20), ForeColor = Color.FromArgb(100, 110, 120), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            lblTotal = new Label
            {
                Text = "$0.00",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 134, 75),
                Location = new Point(15, 45),
                AutoSize = true
            };

            btnCobrar = new Button
            {
                Text = "COBRAR TICKET",
                Location = new Point(15, 120),
                Width = 235,
                Height = 60,
                BackColor = Color.FromArgb(46, 184, 92),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCobrar.FlatAppearance.BorderSize = 0;
            btnCobrar.Click += (s, e) => CobrarTicket();

            btnQuitarItem = new Button
            {
                Text = "Quitar Artículo Seleccionado",
                Location = new Point(15, 200),
                Width = 235,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            btnQuitarItem.Click += (s, e) => QuitarArticuloSeleccionado();

            btnLimpiar = new Button
            {
                Text = "Cancelar Orden Completa",
                Location = new Point(15, 245),
                Width = 235,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            btnLimpiar.Click += (s, e) => LimpiarCarrito();

            btnCorte = new Button
            {
                Text = "Corte de Turno",
                Location = new Point(15, 300),
                Width = 235,
                Height = 40,
                BackColor = Color.FromArgb(240, 243, 246),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnCorte.Click += (s, e) => MostrarCorte();

            pnlLateral.Controls.AddRange(new Control[] { lblTextoTotal, lblTotal, btnCobrar, btnQuitarItem, btnLimpiar, btnCorte });

            // Tabla central DataGridView
            dgvTicket = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
            };

            // Definición de columnas con anchos proporcionales
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", DataPropertyName = "Sku", FillWeight = 20 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripción del Producto", DataPropertyName = "Nombre", FillWeight = 45 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "P. Unitario", DataPropertyName = "Precio", FillWeight = 18 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant.", DataPropertyName = "Cantidad", FillWeight = 12 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", DataPropertyName = "Subtotal", FillWeight = 18 });

            // Agregar contenedores a la ventana
            Controls.Add(dgvTicket);
            Controls.Add(pnlLateral);
            Controls.Add(pnlSuperior);
        }

        private void TxtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
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

        private void QuitarArticuloSeleccionado()
        {
            if (dgvTicket.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un artículo de la tabla para quitarlo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int index = dgvTicket.SelectedRows[0].Index;
            if (index >= 0 && index < _carrito.Count)
            {
                _carrito.RemoveAt(index);
                ActualizarTabla();
            }
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