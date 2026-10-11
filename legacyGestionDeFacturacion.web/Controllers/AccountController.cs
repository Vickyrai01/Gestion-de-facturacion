using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FacturacionLegacy.Datos;
using legacyGestionDeFacturacion.web.Models;
using legacyGestionDeFacturacion.web.Seguridad;

namespace legacyGestionDeFacturacion.web.Controllers
{
    public class AccountController : Controller
    {
        private static readonly string HashInexistente = PasswordSeguridad.CrearHash(Guid.NewGuid().ToString("N"));
        [AllowAnonymous, HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginModel());
        }
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);
            string nombre = model.NombreUsuario.Trim().ToLowerInvariant();
            if (!IntentosLogin.Admitir(nombre))
            {
                ModelState.AddModelError("", "No se pudo iniciar sesión. Esperá 15 minutos antes de volver a intentar.");
                return View(model);
            }
            using (var db = new FacturacionDbContext())
            {
                var usuario = db.Usuarios.SingleOrDefault(u => u.NombreUsuario == nombre);
                bool valido = PasswordSeguridad.Verificar(model.Password, usuario == null ? HashInexistente : usuario.PasswordHash);
                if (!valido || usuario == null || !usuario.Activo)
                {
                    ModelState.AddModelError("", "Usuario o contraseña inválidos.");
                    return View(model);
                }
                IntentosLogin.Limpiar(nombre);
                Session.Clear();
                Session.Abandon();
                FormsAuthentication.SetAuthCookie(usuario.NombreUsuario, false);
            }
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
    }
}
