using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AccountViewModels;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
	public class AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger) : Controller
	{
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILogger<AccountController> _logger = logger;

        #region Login
        public IActionResult Login()
		{
			return View();
		}
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model , CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            // Allow users to login using either their email or user name.
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                user = await _userManager.FindByNameAsync(model.Email);
            }

            if (user is null || string.IsNullOrWhiteSpace(user.UserName))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Use CheckPasswordSignInAsync against the user object to avoid issues with
            // username normalization and to support both email/username inputs.
            var checkResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if (checkResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                _logger.LogInformation("User {UserId} signed in.", user.Id);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            if (checkResult.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} is locked out.", user.Id);
                ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again later.");
            }
            else if (checkResult.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Sign-in is not allowed for this account.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return View(model);
        }

		#endregion

		#region Sign Out

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
        #endregion
        public IActionResult AccessDenied() => View();

    }
}
