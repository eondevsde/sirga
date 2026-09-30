using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[Route("account")]
public class AccountController : Controller
{
  private readonly SignInManager<IdentityUser> _signInManager;
  private readonly UserManager<IdentityUser> _userManager;
  private readonly AccountingStudioService _studioService;
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly ILogger<AccountController> _logger;

  public AccountController(
      SignInManager<IdentityUser> signInManager,
      UserManager<IdentityUser> userManager,
      RoleManager<IdentityRole> roleManager,
      AccountingStudioService studioService,
      ILogger<AccountController> logger)
  {
    _signInManager = signInManager;
    _userManager = userManager;
    _roleManager = roleManager;
    _studioService = studioService;
    _logger = logger;
  }

  [HttpPost("login")]
  [IgnoreAntiforgeryToken]
  public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
  {
    returnUrl ??= Url.Content("~/");

    var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    if (result.Succeeded)
    {
      var user = await _userManager.FindByEmailAsync(email);
      if (user != null)
      {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("EstudioContable"))
        {
          var studio = await _studioService.GetByUsernameAsync(email);
          if (studio != null && !studio.IsApproved)
          {
            await _signInManager.SignOutAsync();
            return Redirect($"/login?error=NotApproved");
          }
        }
      }
      return LocalRedirect(returnUrl);
    }

    // Si falla, redirigimos de vuelta a login con un parámetro de error
    return Redirect($"/login?error=InvalidCredentials&email={email}");
  }

  [HttpPost("register")]
  [IgnoreAntiforgeryToken]
  public async Task<IActionResult> Register(
      string email,
      string password,
      string role,
      string? studioName = null,
      string? phone = null,
      string? cellPhone = null,
      string? address = null,
      int? cityId = null,
      string? referencePerson = null,
      string? referencePhone = null,
      string? returnUrl = null)
  {
    returnUrl ??= Url.Content("~/");

    var user = new IdentityUser { UserName = email, Email = email };
    var result = await _userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
      // Asegurar que el rol existe y asignarlo
      if (!await _roleManager.RoleExistsAsync(role))
      {
        await _roleManager.CreateAsync(new IdentityRole(role));
      }
      await _userManager.AddToRoleAsync(user, role);

      // Si es un estudio contable, registrarlo en la tabla de entidades con aprobación pendiente
      if (role == "EstudioContable" && !string.IsNullOrEmpty(studioName))
      {
        try
        {
          await _studioService.RegisterStudioAsync(studioName, email, email, phone, cellPhone, address, cityId, referencePerson, referencePhone);
        }
        catch (Exception ex)
        {
          // Si falla la creación del estudio, no dejar el usuario huérfano: revertir y avisar
          _logger.LogError(ex, "No se pudo registrar el estudio contable para {Email}", email);
          await _userManager.DeleteAsync(user);
          return Redirect($"/register?error={Uri.EscapeDataString("No se pudo completar el registro del estudio. Intente nuevamente.")}&email={Uri.EscapeDataString(email)}");
        }
      }

      await _signInManager.SignInAsync(user, isPersistent: false);
      return LocalRedirect(returnUrl);
    }

    var errors = string.Join("|", result.Errors.Select(e => e.Description));
    return Redirect($"/register?error={errors}&email={email}");
  }

  [HttpGet("logout")]
  [HttpPost("logout")]
  public async Task<IActionResult> Logout()
  {
    await _signInManager.SignOutAsync();
    return Redirect("/login");
  }
}