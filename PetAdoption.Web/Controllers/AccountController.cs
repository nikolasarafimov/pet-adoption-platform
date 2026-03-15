using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Account;

namespace PetAdoption.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly IShelterService _shelters;
        private readonly IAdopterService _adopters;

        public AccountController(
            SignInManager<ApplicationUser> signIn,
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles,
            IShelterService shelters,
            IAdopterService adopters)
        {
            _signIn = signIn;
            _users = users;
            _roles = roles;
            _shelters = shelters;
            _adopters = adopters;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
            => View(new RegisterVm { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (vm.IsShelter)
            {
                if (string.IsNullOrWhiteSpace(vm.ShelterName))
                    ModelState.AddModelError(nameof(vm.ShelterName), "Shelter name is required.");

                if (string.IsNullOrWhiteSpace(vm.ShelterAddress))
                    ModelState.AddModelError(nameof(vm.ShelterAddress), "Address is required.");

                if (string.IsNullOrWhiteSpace(vm.ShelterCity))
                    ModelState.AddModelError(nameof(vm.ShelterCity), "City is required.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.FullName))
                    ModelState.AddModelError(nameof(vm.FullName), "Full name is required.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            foreach (var r in new[] { "Admin", "Shelter", "Adopter" })
            {
                if (!await _roles.RoleExistsAsync(r))
                    await _roles.CreateAsync(new IdentityRole(r));
            }

            var displayName = vm.IsShelter
                ? (string.IsNullOrWhiteSpace(vm.FullName) ? vm.ShelterName!.Trim() : vm.FullName.Trim())
                : vm.FullName!.Trim();

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true,
                FullName = displayName
            };

            var createUserResult = await _users.CreateAsync(user, vm.Password!);
            if (!createUserResult.Succeeded)
            {
                foreach (var e in createUserResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                return View(vm);
            }

            try
            {
                await _users.AddClaimAsync(user, new Claim("display_name", displayName));

                if (vm.IsShelter)
                {
                    var shelterId = await _shelters.CreateAsync(
                        vm.ShelterName!.Trim(),
                        vm.ShelterAddress!.Trim(),
                        vm.ShelterCity!.Trim(),
                        vm.ShelterPhone,
                        vm.ShelterEmail);

                    user.ShelterId = shelterId;
                    await _users.UpdateAsync(user);
                    await _users.AddToRoleAsync(user, "Shelter");
                }
                else
                {
                    var adopterId = await _adopters.CreateAsync(
                        vm.FullName!.Trim(),
                        vm.Email,
                        null,
                        null,
                        null);

                    user.AdopterId = adopterId;
                    await _users.UpdateAsync(user);
                    await _users.AddToRoleAsync(user, "Adopter");
                }

                await _signIn.SignInAsync(user, isPersistent: false);
                return LocalRedirect(vm.ReturnUrl ?? "/");
            }
            catch (Exception ex)
            {
                await _users.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Registration failed: {ex.Message}");
                return View(vm);
            }
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginVm { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var res = await _signIn.PasswordSignInAsync(
                vm.Email!,
                vm.Password!,
                vm.RememberMe,
                lockoutOnFailure: true);

            if (res.Succeeded)
                return LocalRedirect(vm.ReturnUrl ?? "/");

            ModelState.AddModelError(string.Empty, res.IsLockedOut ? "Account locked." : "Invalid email or password.");
            return View(vm);
        }

        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Welcome", "Home");
        }
    }
}