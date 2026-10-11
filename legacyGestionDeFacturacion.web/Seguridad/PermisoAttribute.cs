using System.Web.Mvc;

namespace legacyGestionDeFacturacion.web.Seguridad
{
    public class PermisoAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAuthenticated)
                filterContext.Result = new HttpStatusCodeResult(403, "El rol no permite esta operación.");
            else base.HandleUnauthorizedRequest(filterContext);
        }
    }
}
