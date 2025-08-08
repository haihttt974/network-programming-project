using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace DKyThucTap.Attributes
{
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if user has the required permission
            var hasPermission = context.HttpContext.User.HasClaim("Permission", _permission);
            
            if (!hasPermission)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }
        }
    }

    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if user has any of the required roles
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }
        }
    }
}
