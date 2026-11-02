using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace MaluStreetwearDemo
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Cultura argentina: precios con formato "$ 45.000,00"
            CultureInfo cultura = new CultureInfo("es-AR");
            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
