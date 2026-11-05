using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Account;

namespace PetAdoption.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IShelterService _shelterService;
        private readonly IAdopterService _adopterService;
        private readonly AppDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IShelterService shelterService,
            IAdopterService adopterService,
            AppDbContext db,
            ILogger<AccountController> logger
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _shelterService = shelterService;
            _adopterService = adopterService;
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            return View(new RegisterVm
            {
                ReturnUrl = GetSafeReturnUrl(returnUrl)
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            ValidateRegistrationModel(vm);

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var email = vm.Email!.Trim();
            var roleName = vm.IsShelter ? "Shelter" : "Adopter";
            var displayName = GetDisplayName(vm);
            var returnUrl = GetSafeReturnUrl(vm.ReturnUrl);

            try
            {
                await EnsureRoleExistsAsync(roleName);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to ensure role {RoleName} during registration.",
                    roleName
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Registration is temporarily unavailable."
                );

                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                FullName = displayName
            };

            try
            {
                await using var transaction =
                    await _db.Database.BeginTransactionAsync();

                var createResult = await _userManager.CreateAsync(
                    user,
                    vm.Password!
                );

                if (!createResult.Succeeded)
                {
                    AddIdentityErrors(createResult);

                    await transaction.RollbackAsync();

                    return View(vm);
                }

                var claimResult = await _userManager.AddClaimAsync(
                    user,
                    new Claim("display_name", displayName)
                );

                EnsureSucceeded(
                    claimResult,
                    "Unable to add the display name claim."
                );

                if (vm.IsShelter)
                {
                    var shelterId = await _shelterService.CreateAsync(
                        vm.ShelterName!.Trim(),
                        vm.ShelterAddress!.Trim(),
                        vm.ShelterCity!.Trim(),
                        vm.ShelterPhone,
                        vm.ShelterEmail
                    );

                    user.ShelterId = shelterId;
                }
                else
                {
                    var adopterId = await _adopterService.CreateAsync(
                        vm.FullName!.Trim(),
                        email,
                        null,
                        null,
                        null
                    );

                    var adopter = await _db.Adopters.FindAsync(adopterId)
                        ?? throw new InvalidOperationException(
                            "The newly created adopter profile was not found."
                        );

                    adopter.UserId = user.Id;
                    user.AdopterId = adopterId;
                }

                var updateResult = await _userManager.UpdateAsync(user);

                EnsureSucceeded(
                    updateResult,
                    "Unable to link the account to its profile."
                );

                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    roleName
                );

                EnsureSucceeded(
                    roleResult,
                    "Unable to assign the account role."
                );

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Registration failed for user {UserId} with role {RoleName}.",
                    user.Id,
                    roleName
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Registration failed. Please try again."
                );

                return View(vm);
            }

            try
            {
                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false
                );
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Automatic sign-in failed for user {UserId}.",
                    user.Id
                );

                return RedirectToAction(
                    nameof(Login),
                    new { returnUrl }
                );
            }

            return LocalRedirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVm
            {
                ReturnUrl = GetSafeReturnUrl(returnUrl)
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                vm.Email!.Trim(),
                vm.Password!,
                vm.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                return LocalRedirect(
                    GetSafeReturnUrl(vm.ReturnUrl)
                );
            }

            ModelState.AddModelError(
                string.Empty,
                result.IsLockedOut
                    ? "Account locked. Please try again later."
                    : "Invalid email or password."
            );

            return View(vm);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Welcome", "Home");
        }

        private void ValidateRegistrationModel(RegisterVm vm)
        {
            if (vm.IsShelter)
            {
                if (string.IsNullOrWhiteSpace(vm.ShelterName))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterName),
                        "Shelter name is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(vm.ShelterAddress))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterAddress),
                        "Address is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(vm.ShelterCity))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterCity),
                        "City is required."
                    );
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.FullName))
                {
                    ModelState.AddModelError(
                        nameof(vm.FullName),
                        "Full name is required."
                    );
                }

                if (!string.IsNullOrWhiteSpace(vm.Email) &&
                    vm.Email.Trim().Length > 128)
                {
                    ModelState.AddModelError(
                        nameof(vm.Email),
                        "Adopter email cannot exceed 128 characters."
                    );
                }
            }
        }

        private static string GetDisplayName(RegisterVm vm)
        {
            if (!vm.IsShelter)
            {
                return vm.FullName!.Trim();
            }

            return !string.IsNullOrWhiteSpace(vm.FullName)
                ? vm.FullName.Trim()
                : vm.ShelterName!.Trim();
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) &&
                   Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/";
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var result = await _roleManager.CreateAsync(
                new IdentityRole(roleName)
            );

            if (!result.Succeeded &&
                !await _roleManager.RoleExistsAsync(roleName))
            {
                EnsureSucceeded(
                    result,
                    $"Unable to create role '{roleName}'."
                );
            }
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }
        }

        private static void EnsureSucceeded(
            IdentityResult result,
            string message
        )
        {
            if (result.Succeeded)
            {
                return;
            }

            var details = string.Join(
                "; ",
                result.Errors.Select(error => error.Description)
            );

            throw new InvalidOperationException(
                $"{message} {details}"
            );
        }
    }
}