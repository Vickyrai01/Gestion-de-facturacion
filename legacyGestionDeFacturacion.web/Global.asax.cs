using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Security.Principal;
using System.Threading;
using FacturacionLegacy.Datos;

namespace legacyGestionDeFacturacion.web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_PostAuthenticateRequest()
        {
            var identity = Context.User == null ? null : Context.User.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated) return;
            using (var db = new FacturacionDbContext())
            {
                var usuario = db.Usuarios.SingleOrDefault(u => u.NombreUsuario == identity.Name && u.Activo);
                if (usuario == null)
                {
                    FormsAuthentication.SignOut();
                    Context.User = new GenericPrincipal(new GenericIdentity(""), new string[0]);
                }
                else Context.User = new GenericPrincipal(identity, new[] { usuario.Rol });
                Thread.CurrentPrincipal = Context.User;
            }
        }
    }
}
