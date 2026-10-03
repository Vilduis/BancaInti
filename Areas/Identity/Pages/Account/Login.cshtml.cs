using BancaInti.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Areas.Identity.Pages.Account
{
    // Reemplaza el login de Identity UI: en español y sin registro ni proveedores externos.
    [AllowAnonymous]
    public class LoginModel(SignInManager<IdentityUser> signInManager) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Display(Name = "Correo"), Required(ErrorMessage = Mensajes.Requerido), EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
            public string Email { get; set; } = "";

            [Display(Name = "Contraseña"), Required(ErrorMessage = Mensajes.Requerido), DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return Page();

            var result = await signInManager.PasswordSignInAsync(Input.Email.Trim(), Input.Password, Input.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "~/");

            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "La cuenta está bloqueada temporalmente por varios intentos fallidos. Intente en unos minutos."
                : "Correo o contraseña incorrectos.");
            return Page();
        }
    }
}
