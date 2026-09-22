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

        public FormCatalogo()
        {
            _inventario = new InventarioServicio();
            InicializarComponentes();
            CargarProductos();
        }

        private void InicializarComponentes()
        {
            Text = "Catálogo de Productos y Existencias";
            Size = new Size(760, 500);
            MinimumSize = new Size(620, 400);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.FromArgb(245, 246, 250);

            // Panel superior con buscador en vivo
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.White
            };

            Label lblBuscar = new Label
            {
                Text = "Filtrar por SKU o Nombre:",
                AutoSize = true,
                Location = new Point(15, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            txtFiltro = new TextBox
            {
                Location = new Point(15, 30),
                Width = 340,
                Font = new Font("Segoe UI", 11f)
            };
            txtFiltro.TextChanged += (s, e) => CargarProductos(txtFiltro.Text.Trim());

            pnlTop.Controls.Add(lblBuscar);
            pnlTop.Controls.Add(txtFiltro);

            // Tabla de productos
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

            Controls.Add(dgvCatalogo);
            Controls.Add(pnlTop);
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