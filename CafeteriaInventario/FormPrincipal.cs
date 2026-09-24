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

        // Paleta de colores
        private readonly Color ColorVinoOscuro = Color.FromArgb(120, 16, 22);
        private readonly Color ColorVinoSidebar = Color.FromArgb(178, 28, 36);
        private readonly Color ColorVinoEncabezadoTabla = Color.FromArgb(125, 20, 25);
        private readonly Color ColorAmarilloActivo = Color.FromArgb(254, 204, 27);
        private readonly Color ColorVerdeBoton = Color.FromArgb(22, 163, 74);
        private readonly Color ColorFondoGrisVentana = Color.FromArgb(240, 244, 248);

        // Controles interactivos
        private TextBox txtEntrada = null!;
        private NumericUpDown numCantidad = null!;
        private Button btnAgregar = null!;
        private DataGridView dgvTicket = null!;
        private Label lblTotal = null!;

        public FormPrincipal()
        {
            _inventario = new InventarioServicio();
            _caja = new CajaServicio();
            _carrito = new List<ItemVentaTemporal>();

            ConfigurarVentana();
            ConstruirInterfaz();
        }

        private void ConfigurarVentana()
        {
            Text = "CAFETERÍA - CONTROL TOTAL | Sistema Administrativo y Operativo";
            Size = new Size(1220, 720);
            MinimumSize = new Size(1100, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorFondoGrisVentana;
            Font = new Font("Segoe UI", 10f);
        }

        private void ConstruirInterfaz()
        {
            // -------------------------------------------------------------
            // 1. SIDEBAR IZQUIERDO
            // -------------------------------------------------------------
            Panel pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ColorVinoSidebar,
                Padding = new Padding(14, 15, 14, 14)
            };

            Label lblLogo = new Label
            {
                Text = "CONTROL TOTAL\nCafetería ITCH II • v2.4",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.TopLeft
            };

            Label lblModuloSec = new Label
            {
                Text = "MÓDULOS DEL SISTEMA",
                ForeColor = Color.FromArgb(250, 210, 210),
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft
            };

            Panel pnlItemsMenu = new Panel { Dock = DockStyle.Fill, AutoScroll = false };

            var items = new (string Icono, string Nombre, bool Activo)[]
            {
                ("📦", "Ver inventario general", false),
                ("🛒", "Cobrar / Vender", true),
                ("🏷️", "Promociones y combos", false),
                ("🚚", "Reabastecer existencias", false),
                ("➕", "Alta productos y recetas", false),
                ("🗑️", "Dar de baja un producto", false),
                ("☕", "Existencias de barra", false),
                ("⏱️", "Bitácora de movimientos", false),
                ("📊", "Reportes y corte de caja", false)
            };

            int posY = 10;
            foreach (var mod in items)
            {
                Button btn = new Button
                {
                    Text = $"  {mod.Icono}  {mod.Nombre}" + (mod.Activo ? "      >" : ""),
                    Location = new Point(0, posY),
                    Width = 232,
                    Height = 38,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.2f, mod.Activo ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = mod.Activo ? Color.FromArgb(60, 20, 0) : Color.White,
                    BackColor = mod.Activo ? ColorAmarilloActivo : Color.Transparent,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;

                if (mod.Nombre.Contains("Reportes"))
                    btn.Click += (s, e) => EjecutarCierreTurno();
                else if (mod.Nombre.Contains("inventario general"))
                    btn.Click += (s, e) => AbrirCatalogo();
                else if (!mod.Activo)
                    btn.Click += (s, e) => MessageBox.Show($"El módulo '{mod.Nombre}' estará disponible próximamente.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

                pnlItemsMenu.Controls.Add(btn);
                posY += 42;
            }

            // Pie de status de caja (dibuja el punto verde sólido con GDI+)
            Panel pnlEstadoCaja = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.FromArgb(115, 15, 20)
            };
            pnlEstadoCaja.Paint += (s, e) =>
            {
                using var brushVerde = new SolidBrush(Color.FromArgb(34, 197, 94));
                e.Graphics.FillEllipse(brushVerde, 18, 22, 16, 16);
            };

            Label lblEstadoTexto = new Label
            {
                Text = "Caja Principal - Abierta\nUsuario: Cajero 01",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Location = new Point(44, 14),
                AutoSize = true
            };
            pnlEstadoCaja.Controls.Add(lblEstadoTexto);

            pnlSidebar.Controls.Add(pnlItemsMenu);
            pnlSidebar.Controls.Add(lblModuloSec);
            pnlSidebar.Controls.Add(lblLogo);
            pnlSidebar.Controls.Add(pnlEstadoCaja);

            // -------------------------------------------------------------
            // 2. PANEL LATERAL DERECHO (LIQUIDACIÓN Y ACCIONES)
            // -------------------------------------------------------------
            Panel pnlDerecho = new Panel
            {
                Dock = DockStyle.Right,
                Width = 275,
                BackColor = Color.White,
                Padding = new Padding(15, 18, 15, 15)
            };

            Panel pnlCardTotal = new Panel
            {
                Location = new Point(15, 15),
                Width = 245,
                Height = 98,
                BackColor = Color.FromArgb(255, 253, 240)
            };
            pnlCardTotal.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(252, 225, 140), 1.6f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlCardTotal.Width - 1, pnlCardTotal.Height - 1);
            };

            Label lblTituloMonto = new Label
            {
                Text = "TOTAL A PAGAR:",
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };
            lblTotal = new Label
            {
                Text = "$0.00",
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                Location = new Point(8, 32),
                AutoSize = true
            };
            pnlCardTotal.Controls.AddRange(new Control[] { lblTituloMonto, lblTotal });

            Button btnCobrar = new Button
            {
                Text = "COBRAR TICKET",
                Location = new Point(15, 130),
                Width = 245,
                Height = 54,
                BackColor = ColorVerdeBoton,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCobrar.FlatAppearance.BorderSize = 0;
            btnCobrar.Click += (s, e) => CobrarTicket();

            Button btnQuitar = CrearBotonAccion("Quitar Artículo Seleccionado", 200, Color.FromArgb(220, 38, 38));
            btnQuitar.Click += (s, e) => QuitarArticuloSeleccionado();

            Button btnCancelar = CrearBotonAccion("Cancelar Orden Completa", 250, Color.FromArgb(220, 38, 38));
            btnCancelar.Click += (s, e) => LimpiarCarrito();

            Button btnCorte = CrearBotonAccion("Corte de Turno Rápido", 318, Color.FromArgb(217, 119, 6));
            btnCorte.Click += (s, e) => MostrarCorte();

            Button btnCatalogo = CrearBotonAccion("Consultar Catálogo Rápido", 368, Color.FromArgb(75, 85, 99));
            btnCatalogo.Click += (s, e) => AbrirCatalogo();

            pnlDerecho.Controls.AddRange(new Control[] { pnlCardTotal, btnCobrar, btnQuitar, btnCancelar, btnCorte, btnCatalogo });

            // -------------------------------------------------------------
            // 3. ÁREA CENTRAL FLOTANTE (ESTILO TARJETAS)
            // -------------------------------------------------------------
            Panel pnlCentro = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 18, 22, 22),
                BackColor = ColorFondoGrisVentana
            };

            Panel pnlCardInputs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10)
            };
            pnlCardInputs.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlCardInputs.Width - 1, pnlCardInputs.Height - 1);
            };

            Label lblCode = new Label { Text = "Código de barras o Nombre:", AutoSize = true, Location = new Point(15, 8), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81) };
            txtEntrada = new TextBox { Location = new Point(15, 32), Width = 370, Font = new Font("Segoe UI", 11.5f) };
            txtEntrada.KeyDown += TxtEntrada_KeyDown;

            Label lblQty = new Label { Text = "Cantidad:", AutoSize = true, Location = new Point(400, 8), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81) };
            numCantidad = new NumericUpDown { Location = new Point(400, 32), Width = 80, Font = new Font("Segoe UI", 11.5f), Minimum = 1, Maximum = 500, Value = 1 };

            btnAgregar = new Button
            {
                Text = "Agregar (+)",
                Location = new Point(495, 30),
                Width = 140,
                Height = 35,
                BackColor = ColorVinoOscuro,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += (s, e) => AgregarProducto();

            pnlCardInputs.Controls.AddRange(new Control[] { lblCode, txtEntrada, lblQty, numCantidad, btnAgregar });

            Panel pnlCardTabla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            pnlCardTabla.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlCardTabla.Width - 1, pnlCardTabla.Height - 1);
            };

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
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 34 }
            };

            dgvTicket.ColumnHeadersDefaultCellStyle.BackColor = ColorVinoEncabezadoTabla;
            dgvTicket.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTicket.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.8f, FontStyle.Bold);
            dgvTicket.ColumnHeadersHeight = 38;

            dgvTicket.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 243, 199);
            dgvTicket.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
            dgvTicket.DefaultCellStyle.Font = new Font("Segoe UI", 10f);

            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", FillWeight = 16 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripción del Producto", FillWeight = 46 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "P. Unitario", FillWeight = 18 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant.", FillWeight = 10 });
            dgvTicket.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", FillWeight = 18 });

            Panel pnlSeparador = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0) };
            pnlCardTabla.Controls.Add(dgvTicket);
            pnlSeparador.Controls.Add(pnlCardTabla);

            pnlCentro.Controls.Add(pnlSeparador);
            pnlCentro.Controls.Add(pnlCardInputs);

            // Montaje al formulario sin franjas extra
            Controls.Add(pnlCentro);
            Controls.Add(pnlDerecho);
            Controls.Add(pnlSidebar);
        }

        private Button CrearBotonAccion(string texto, int y, Color color)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(15, y),
                Width = 245,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = color,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = color;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
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
                MessageBox.Show("Producto no encontrado en el inventario.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void LimpiarCarrito()
        {
            _carrito.Clear();
            ActualizarTabla();
            txtEntrada.Focus();
        }

        private void ActualizarTabla()
        {
            dgvTicket.Rows.Clear();
            decimal total = 0;

            foreach (var it in _carrito)
            {
                int rowIdx = dgvTicket.Rows.Add(it.Producto.Sku, it.Producto.Nombre, $"${it.Producto.Precio:F2}", it.Cantidad, $"${it.Subtotal:F2}");
                dgvTicket.Rows[rowIdx].Cells[0].Style.ForeColor = Color.FromArgb(180, 83, 9);
                dgvTicket.Rows[rowIdx].Cells[0].Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
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

        private void AbrirCatalogo()
        {
            using var form = new FormCatalogo();
            if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(form.SkuSeleccionado))
            {
                txtEntrada.Text = form.SkuSeleccionado;
                AgregarProducto();
            }
        }

        private void MostrarCorte()
        {
            var corte = _caja.GenerarCorteTurno();
            string msg = $"--- CORTE DE TURNO RÁPIDO ---\n\n" +
                         $"Transacciones de Venta: {corte.TotalTransaccionesVenta}\n" +
                         $"Unidades Despachadas:   {corte.TotalUnidadesVendidas}\n" +
                         $"Total Cobrado en Caja:  ${corte.TotalIngresos:F2}\n" +
                         $"Utilidad Estimada:      ${corte.TotalGananciaEstimada:F2}";

            MessageBox.Show(msg, "Resumen Financiero", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EjecutarCierreTurno()
        {
            var confirmacion = MessageBox.Show(
                "¿Está seguro de que desea realizar el CIERRE DE TURNO?\n\nEsta acción calculará los balances finales y cerrará el periodo de caja actual.",
                "Confirmar Cierre de Turno",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmacion == DialogResult.Yes)
            {
                if (_caja.CerrarTurnoCaja(out string reporte))
                {
                    MessageBox.Show(reporte, "Cierre Contable Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarCarrito();
                }
            }
        }
    }
}