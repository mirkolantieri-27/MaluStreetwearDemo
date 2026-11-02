using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MaluStreetwearDemo
{
    /// <summary>
    /// Formulario principal de la demo: integra en tiempo real el inventario del
    /// Sistema de Gestión ZOO LOGIC con el canal de venta online (Instagram).
    /// </summary>
    public partial class Form1 : Form
    {
        // ---------------------------------------------------------------
        // Paleta de colores para el estado de stock crítico
        // ---------------------------------------------------------------
        private static readonly Color ColorCritico = Color.FromArgb(255, 205, 210);          // rosado suave
        private static readonly Color ColorCriticoSeleccion = Color.FromArgb(229, 115, 115); // rojo suave (fila seleccionada)
        private static readonly Color ColorTextoCritico = Color.FromArgb(183, 28, 28);       // rojo oscuro (texto)

        // ---------------------------------------------------------------
        // Estado en memoria (simula la base de datos de ZOO LOGIC)
        // ---------------------------------------------------------------
        private readonly BindingList<ProductoStock> _inventario = new BindingList<ProductoStock>();

        // Dos BindingSource independientes sobre la MISMA lista:
        // ambos canales ven los mismos datos, pero cada uno tiene su propia selección.
        private readonly BindingSource _bsZooLogic = new BindingSource();
        private readonly BindingSource _bsInstagram = new BindingSource();

        private Font _fuenteNegrita;
        private int _numeroPedido;

        public Form1()
        {
            InitializeComponent();
        }

        // ===============================================================
        // EVENTOS DEL FORMULARIO
        // ===============================================================

        private void Form1_Load(object sender, EventArgs e)
        {
            _fuenteNegrita = new Font(dgvStock.Font, FontStyle.Bold);

            CargarInventarioInicial();
            ConfigurarEnlaces();
            ActualizarDetalleProducto();
            ActualizarIndicadorSincronizacion();

            RegistrarEvento("Sistema iniciado. Sincronización ZOO LOGIC <-> Instagram activa.");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_fuenteNegrita != null)
            {
                _fuenteNegrita.Dispose();
                _fuenteNegrita = null;
            }
        }

        // ===============================================================
        // INICIALIZACIÓN
        // ===============================================================

        /// <summary>
        /// Carga el inventario de ejemplo: algunos productos con stock de sobra
        /// y otros ya en nivel crítico (stock actual menor o igual al mínimo).
        /// </summary>
        private void CargarInventarioInicial()
        {
            _inventario.Add(new ProductoStock("MAL-001", "Buzo Oversize Malú", "L", "Negro", 45000m, 15, 2));
            _inventario.Add(new ProductoStock("MAL-002", "Remera Boxy Fit", "M", "Blanco", 18500m, 8, 2));
            _inventario.Add(new ProductoStock("MAL-003", "Jogger Cargo", "M", "Verde Militar", 38900m, 2, 2));
            _inventario.Add(new ProductoStock("MAL-004", "Campera Puffer", "XL", "Negro", 89900m, 1, 2));
            _inventario.Add(new ProductoStock("MAL-005", "Gorra Trucker", "Único", "Beige", 15000m, 6, 2));
        }

        /// <summary>
        /// Enlaza la grilla de ZOO LOGIC y el combo de Instagram a la misma fuente de datos.
        /// </summary>
        private void ConfigurarEnlaces()
        {
            // Sistema de Gestión (ZOO LOGIC)
            dgvStock.AutoGenerateColumns = false; // usamos las columnas definidas en el Designer
            _bsZooLogic.DataSource = _inventario;
            dgvStock.DataSource = _bsZooLogic;

            // Canal Online (Instagram)
            _bsInstagram.DataSource = _inventario;
            cboProductosOnline.DisplayMember = "DescripcionOnline";
            cboProductosOnline.ValueMember = "Codigo";
            cboProductosOnline.DataSource = _bsInstagram;
        }

        // ===============================================================
        // VENTA ONLINE
        // ===============================================================

        private void btnVenderOnline_Click(object sender, EventArgs e)
        {
            ProductoStock producto = cboProductosOnline.SelectedItem as ProductoStock;
            if (producto == null)
            {
                MessageBox.Show("Seleccione un producto del catálogo online.",
                    "Instagram - Malú Streetwear", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int cantidad = (int)nudCantidad.Value;
            if (cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser mayor a cero.",
                    "Instagram - Malú Streetwear", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // a) Validación de stock contra ZOO LOGIC
            if (!producto.TieneStockSuficiente(cantidad))
            {
                RegistrarEvento(string.Format("RECHAZADA: {0} x{1} (disponible: {2})",
                    producto.Codigo, cantidad, producto.StockActual));

                MessageBox.Show(
                    "QUIEBRE DE STOCK\n\n" +
                    "Producto: " + producto.DescripcionOnline + "\n" +
                    "Cantidad solicitada: " + cantidad + " u.\n" +
                    "Disponible en ZOO LOGIC: " + producto.StockActual + " u.\n\n" +
                    "La venta online fue rechazada.",
                    "ZOO LOGIC - Error de stock",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // b) Descuento de stock en tiempo real y refresco inmediato de la grilla
            producto.DescontarStock(cantidad);
            _numeroPedido++;
            string nroPedido = string.Format("IG-{0:0000}", _numeroPedido);

            SeleccionarFilaProducto(producto);
            dgvStock.Refresh();              // repinta la grilla antes de cualquier MessageBox
            ActualizarDetalleProducto();
            ActualizarIndicadorSincronizacion();

            RegistrarEvento(string.Format("{0} OK: {1} x{2} -> stock restante: {3}",
                nroPedido, producto.Codigo, cantidad, producto.StockActual));

            // c) Evaluación de stock mínimo y alerta automática de reposición
            if (producto.EsStockCritico)
            {
                RegistrarEvento(string.Format("ALERTA: {0} requiere pedido al proveedor.", producto.Codigo));

                MessageBox.Show(
                    "Venta " + nroPedido + " registrada correctamente.\n\n" +
                    "ALERTA DE REPOSICIÓN\n" +
                    "Producto: " + producto.DescripcionOnline + "\n" +
                    "Stock actual: " + producto.StockActual + " u.  |  Stock mínimo: " + producto.StockMinimo + " u.\n\n" +
                    "Se requiere generar un pedido al proveedor.",
                    "ZOO LOGIC - Stock crítico",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(
                    "Venta " + nroPedido + " registrada y sincronizada con ZOO LOGIC.\n\n" +
                    "Producto: " + producto.DescripcionOnline + "\n" +
                    "Unidades vendidas: " + cantidad + "\n" +
                    "Stock restante: " + producto.StockActual + " u.",
                    "Instagram - Venta confirmada",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            nudCantidad.Value = nudCantidad.Minimum;
        }

        private void cboProductosOnline_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarDetalleProducto();
        }

        // ===============================================================
        // PRESENTACIÓN DE LA GRILLA (coloreo de filas críticas)
        // ===============================================================

        private void dgvStock_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvStock.Rows.Count)
                return;

            ProductoStock producto = dgvStock.Rows[e.RowIndex].DataBoundItem as ProductoStock;
            if (producto == null || !producto.EsStockCritico)
                return; // las filas normales usan el estilo por defecto

            e.CellStyle.BackColor = ColorCritico;
            e.CellStyle.SelectionBackColor = ColorCriticoSeleccion;
            e.CellStyle.ForeColor = ColorTextoCritico;
            e.CellStyle.SelectionForeColor = Color.White;

            string columna = dgvStock.Columns[e.ColumnIndex].Name;
            if ((columna == colStockActual.Name || columna == colEstado.Name) && _fuenteNegrita != null)
            {
                e.CellStyle.Font = _fuenteNegrita;
            }
        }

        private void dgvStock_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Al cargar, no dejamos ninguna fila seleccionada para que se vean los colores de estado.
            if (e.ListChangedType == ListChangedType.Reset)
                dgvStock.ClearSelection();
        }

        // ===============================================================
        // MÉTODOS AUXILIARES
        // ===============================================================

        private void SeleccionarFilaProducto(ProductoStock producto)
        {
            int indice = _inventario.IndexOf(producto);
            if (indice < 0 || indice >= dgvStock.Rows.Count)
                return;

            dgvStock.ClearSelection();
            dgvStock.Rows[indice].Selected = true;
        }

        private void ActualizarDetalleProducto()
        {
            ProductoStock producto = cboProductosOnline.SelectedItem as ProductoStock;
            if (producto == null)
            {
                lblDetalleProducto.Text = "Seleccione un producto del catálogo.";
                lblDetalleProducto.ForeColor = Color.DimGray;
                return;
            }

            string aviso = string.Empty;
            if (producto.StockActual == 0)
                aviso = "  (SIN STOCK)";
            else if (producto.EsStockCritico)
                aviso = "  (¡Últimas unidades!)";

            lblDetalleProducto.Text = string.Format("Precio: {0:C2}\nDisponible: {1} u.{2}",
                producto.Precio, producto.StockActual, aviso);
            lblDetalleProducto.ForeColor = producto.EsStockCritico ? ColorTextoCritico : Color.DimGray;
        }

        private void ActualizarIndicadorSincronizacion()
        {
            lblSincronizacion.Text = "● Sincronizado " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void RegistrarEvento(string mensaje)
        {
            lstHistorial.Items.Insert(0, string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, mensaje));
        }

        private void grpInstagram_Enter(object sender, EventArgs e)
        {

        }
    }

    // ===================================================================
    // MODELO
    // ===================================================================

    /// <summary>
    /// Representa una prenda del inventario de ZOO LOGIC.
    /// Implementa INotifyPropertyChanged para que cualquier cambio de stock
    /// se refleje automáticamente en todos los controles enlazados.
    /// </summary>
    public class ProductoStock : INotifyPropertyChanged
    {
        private int _stockActual;

        public ProductoStock(string codigo, string descripcion, string talle, string colorPrenda,
                             decimal precio, int stockActual, int stockMinimo)
        {
            Codigo = codigo;
            Descripcion = descripcion;
            Talle = talle;
            ColorPrenda = colorPrenda;
            Precio = precio;
            StockMinimo = stockMinimo;
            _stockActual = stockActual;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Codigo { get; private set; }
        public string Descripcion { get; private set; }
        public string Talle { get; private set; }
        public string ColorPrenda { get; private set; }
        public decimal Precio { get; private set; }
        public int StockMinimo { get; private set; }

        public int StockActual
        {
            get { return _stockActual; }
            private set
            {
                if (_stockActual == value)
                    return;

                _stockActual = value;
                OnPropertyChanged(nameof(StockActual));
                OnPropertyChanged(nameof(EsStockCritico));
                OnPropertyChanged(nameof(Estado));
            }
        }

        /// <summary>Verdadero cuando el stock actual es menor o igual al stock mínimo.</summary>
        public bool EsStockCritico
        {
            get { return StockActual <= StockMinimo; }
        }

        public string Estado
        {
            get
            {
                if (StockActual == 0) return "SIN STOCK";
                return EsStockCritico ? "CRÍTICO - Reponer" : "OK";
            }
        }

        /// <summary>Texto que ve el cliente en el catálogo de Instagram.</summary>
        public string DescripcionOnline
        {
            get { return string.Format("{0} - {1} - Talle {2}", Descripcion, ColorPrenda, Talle); }
        }

        public bool TieneStockSuficiente(int cantidad)
        {
            return cantidad > 0 && cantidad <= StockActual;
        }

        public void DescontarStock(int cantidad)
        {
            if (!TieneStockSuficiente(cantidad))
                throw new InvalidOperationException("Stock insuficiente para el producto " + Codigo + ".");

            StockActual -= cantidad;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
