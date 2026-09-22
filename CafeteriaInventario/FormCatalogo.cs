using System;
using System.Drawing;
using System.Windows.Forms;
using CafeteriaInventario.Servicios;

namespace CafeteriaInventario
{
    public class FormCatalogo : Form
    {
        private readonly InventarioServicio _inventario;
        private DataGridView dgvCatalogo = null!;
        private TextBox txtFiltro = null!;
        private Button btnSeleccionar = null!;

        // Propiedad pública para transferir el SKU seleccionado al FormPrincipal
        public string? SkuSeleccionado { get; private set; }

        public FormCatalogo()
        {
            _inventario = new InventarioServicio();
            InicializarComponentes();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            Text = "Catálogo de Productos - Doble clic para agregar al ticket";
            Size = new Size(760, 500);
            MinimumSize = new Size(620, 400);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.FromArgb(245, 246, 250);

            // Panel superior
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };

            Label lblBuscar = new Label { Text = "Filtrar por SKU o Nombre:", AutoSize = true, Location = new Point(15, 10), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtFiltro = new TextBox { Location = new Point(15, 30), Width = 340, Font = new Font("Segoe UI", 11f) };
            txtFiltro.TextChanged += (s, e) => CargarProductos(txtFiltro.Text.Trim());

            btnSeleccionar = new Button
            {
                Text = "Cargar al Ticket (Enter)",
                Location = new Point(370, 28),
                Width = 190,
                Height = 32,
                BackColor = Color.FromArgb(220, 240, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSeleccionar.Click += (s, e) => ConfirmarSeleccion();

            pnlTop.Controls.AddRange(new Control[] { lblBuscar, txtFiltro, btnSeleccionar });

            // Tabla DataGridView
            dgvCatalogo = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 30 }
            };

            dgvCatalogo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", FillWeight = 20 });
            dgvCatalogo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripción", FillWeight = 45 });
            dgvCatalogo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", FillWeight = 15 });
            dgvCatalogo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock / Tipo", FillWeight = 20 });

            // Evento: Al hacer doble clic sobre un renglón, se selecciona y se cierra
            dgvCatalogo.CellDoubleClick += (s, e) => ConfirmarSeleccion();

            Controls.Add(dgvCatalogo);
            Controls.Add(pnlTop);
        }

        private void ConfirmarSeleccion()
        {
            if (dgvCatalogo.SelectedRows.Count > 0)
            {
                SkuSeleccionado = dgvCatalogo.SelectedRows[0].Cells[0].Value?.ToString();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void CargarProductos(string filtro = "")
        {
            dgvCatalogo.Rows.Clear();
            var lista = _inventario.ObtenerCatalogoCompleto();

            foreach (var prod in lista)
            {
                if (!string.IsNullOrEmpty(filtro) &&
                    !prod.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) &&
                    !prod.Sku.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dgvCatalogo.Rows.Add(prod.Sku, prod.Nombre, $"${prod.Precio:F2}", prod.Stock);
            }
        }
    }
}