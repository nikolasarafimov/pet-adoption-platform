using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IShelterService shelterService,
            IAdopterService adopterService,
            ILogger<AccountController> logger
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _shelterService = shelterService;
            _adopterService = adopterService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            return View(
                new RegisterVm
                {
                    ReturnUrl = returnUrl
                }
            );
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
            var roleName = vm.IsShelter
                ? "Shelter"
                : "Adopter";

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

            var displayName = GetDisplayName(vm);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                FullName = displayName
            };

            var createResult =
                await _userManager.CreateAsync(
                    user,
                    vm.Password!
                );

            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return View(vm);
            }

            int? createdShelterId = null;
            int? createdAdopterId = null;

            try
            {
                var claimResult =
                    await _userManager.AddClaimAsync(
                        user,
                        new Claim(
                            "display_name",
                            displayName
                        )
                    );

                EnsureSucceeded(
                    claimResult,
                    "Unable to add the display name claim."
                );

                if (vm.IsShelter)
                {
                    createdShelterId =
                        await _shelterService.CreateAsync(
                            vm.ShelterName!.Trim(),
                            vm.ShelterAddress!.Trim(),
                            vm.ShelterCity!.Trim(),
                            vm.ShelterPhone,
                            vm.ShelterEmail
                        );

                    user.ShelterId =
                        createdShelterId;
                }
                else
                {
                    createdAdopterId =
                        await _adopterService.CreateAsync(
                            vm.FullName!.Trim(),
                            email,
                            null,
                            null,
                            null
                        );

                    user.AdopterId =
                        createdAdopterId;
                }

                var updateResult =
                    await _userManager.UpdateAsync(user);

                EnsureSucceeded(
                    updateResult,
                    "Unable to link the account to its profile."
                );

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        roleName
                    );

                EnsureSucceeded(
                    roleResult,
                    "Unable to assign the account role."
                );

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false
                );

                return LocalRedirect(
                    vm.ReturnUrl ?? "/"
                );
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Registration failed for {Email}.",
                    email
                );

                await CleanupFailedRegistrationAsync(
                    user,
                    createdShelterId,
                    createdAdopterId
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Registration failed. Please try again."
                );

                return View(vm);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(
            string? returnUrl = null
        )
        {
            return View(
                new LoginVm
                {
                    ReturnUrl = returnUrl
                }
            );
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

            var result =
                await _signInManager.PasswordSignInAsync(
                    vm.Email!.Trim(),
                    vm.Password!,
                    vm.RememberMe,
                    lockoutOnFailure: true
                );

            if (result.Succeeded)
            {
                return LocalRedirect(
                    vm.ReturnUrl ?? "/"
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

            return RedirectToAction(
                "Welcome",
                "Home"
            );
        }

        private void ValidateRegistrationModel(
            RegisterVm vm
        )
        {
            if (vm.IsShelter)
            {
                if (string.IsNullOrWhiteSpace(
                    vm.ShelterName
                ))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterName),
                        "Shelter name is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(
                    vm.ShelterAddress
                ))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterAddress),
                        "Address is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(
                    vm.ShelterCity
                ))
                {
                    ModelState.AddModelError(
                        nameof(vm.ShelterCity),
                        "City is required."
                    );
                }
            }
            else if (string.IsNullOrWhiteSpace(
                vm.FullName
            ))
            {
                ModelState.AddModelError(
                    nameof(vm.FullName),
                    "Full name is required."
                );
            }
        }

        private static string GetDisplayName(
            RegisterVm vm
        )
        {
            if (!vm.IsShelter)
            {
                return vm.FullName!.Trim();
            }

            return !string.IsNullOrWhiteSpace(
                vm.FullName
            )
                ? vm.FullName.Trim()
                : vm.ShelterName!.Trim();
        }

        private async Task EnsureRoleExistsAsync(
            string roleName
        )
        {
            if (await _roleManager.RoleExistsAsync(
                roleName
            ))
            {
                return;
            }

            var result =
                await _roleManager.CreateAsync(
                    new IdentityRole(roleName)
                );

            if (!result.Succeeded
                && !await _roleManager.RoleExistsAsync(
                    roleName
                ))
            {
                EnsureSucceeded(
                    result,
                    $"Unable to create role '{roleName}'."
                );
            }
        }

        private void AddIdentityErrors(
            IdentityResult result
        )
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
                result.Errors.Select(
                    error => error.Description
                )
            );

            throw new InvalidOperationException(
                $"{message} {details}"
            );
        }

        private async Task CleanupFailedRegistrationAsync(
            ApplicationUser user,
            int? shelterId,
            int? adopterId
        )
        {
            try
            {
                if (adopterId.HasValue)
                {
                    await _adopterService.DeleteAsync(
                        adopterId.Value
                    );
                }

                if (shelterId.HasValue)
                {
                    await _shelterService.DeleteAsync(
                        shelterId.Value
                    );
                }

                var existingUser =
                    await _userManager.FindByIdAsync(
                        user.Id
                    );

                if (existingUser != null)
                {
                    await _userManager.DeleteAsync(
                        existingUser
                    );
                }
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to completely clean up registration for {UserId}.",
                    user.Id
                );
            }
        }
    }
}