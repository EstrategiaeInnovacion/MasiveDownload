using System.Windows;

namespace VucemDownloader
{
    public partial class NewExpedienteWindow : Window
    {
        public string Nombre => txtNombre.Text.Trim();
        public string Rfc => txtRfc.Text.Trim().ToUpper();
        public string Aduana => txtAduana.Text.Trim().ToUpper();

        public NewExpedienteWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            txtNombre.Focus();
        }

        private void btnCrear_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese un nombre para el expediente", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRfc.Text))
            {
                MessageBox.Show("Ingrese el RFC", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRfc.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}