using System;
using System.Configuration;
using System.Net.Mail;
using System.Web.UI;
using FacturacionLegacy.WebForms.Datos;

namespace FacturacionLegacy.WebForms
{
    public partial class Clientes : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarClientes();
            }
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            Page.Validate("AltaCliente");
            if (!Page.IsValid)
            {
                lblResultado.Text = "Completá los campos obligatorios.";
                return;
            }

            // Validación y coordinación del negocio acopladas a los controles de la página.
            string nombre = txtNombre.Text.Trim();
            string email = txtEmail.Text.Trim();
            if (nombre.Length == 0 || nombre.Length > 100 || email.Length == 0 || email.Length > 254)
            {
                lblResultado.Text = "Ingresá un nombre (hasta 100 caracteres) y un email (hasta 254 caracteres).";
                return;
            }

            try
            {
                var direccion = new MailAddress(email);
                if (!string.Equals(direccion.Address, email, StringComparison.OrdinalIgnoreCase))
                {
                    lblResultado.Text = "Ingresá un email válido, sin nombre para mostrar.";
                    return;
                }
            }
            catch (FormatException)
            {
                lblResultado.Text = "Ingresá un email válido.";
                return;
            }

            if (!ClientesEnMemoria.Registrar(nombre, email))
            {
                lblResultado.Text = "Ya existe un cliente con ese email.";
                return;
            }

            lblResultado.Text = Server.HtmlEncode(ConfigurationManager.AppSettings["Clientes.MensajeAlta"]);
            txtNombre.Text = string.Empty;
            txtEmail.Text = string.Empty;
            CargarClientes();
        }

        private void CargarClientes()
        {
            grdClientes.DataSource = ClientesEnMemoria.Listar();
            grdClientes.DataBind();
        }
    }
}
