using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace INSY7315_ElevateDigitalStudios_POE.Helper
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint != null && endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                return;
            }

            var userEmail = context.HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//