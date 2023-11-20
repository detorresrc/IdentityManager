using System.Text.Encodings.Web;
using IdentityManager.Models;
using IdentityManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IdentityManager.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly UrlEncoder _urlEncoder;
        private ILogger<AccountController> _logger;

        public AccountController
        (
            ILogger<AccountController> logger,
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            UrlEncoder urlEncoder
        ){
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _urlEncoder = urlEncoder;
            _signInManager = signInManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login([FromQuery]string? returnUrl=null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet("auth/register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromQuery]string? returnUrl=null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            RegisterViewModel model = new()
            {
                RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
            return View(model);
        }

        [HttpGet("registration-confirmation")]
        [AllowAnonymous]
        public IActionResult RegistrationConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet("reset-password")]
        [AllowAnonymous]
        public IActionResult ResetPassword([FromQuery]string code = null)
        {
            return code == null ? View("Error") : View();
        }

        [HttpGet("reset-password-confirmation")]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery]string code, [FromQuery]string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if(user == null)
            {
                return View("Error");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if(result.Succeeded == false)
            {
                return View("Error");
            }

            return View();
        }

        [HttpGet("authenticator-confirmation")]
        [AllowAnonymous]
        public IActionResult AuthenticatorConfirmation()
        {
            return View();
        }

        [HttpGet("auth/verify-authenticator")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticator
        (
            bool rememberMe,
            string returnUrl = null
        )
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if(user == null)
            {
                return View("Error");
            }
            ViewData["ReturnUrl"] = returnUrl;
            
            return View(new VerifyAuthenticatorViewModel{RememberMe = rememberMe, ReturnUrl = returnUrl});
        }

        [HttpPost("auth/verify-authenticator")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticator(VerifyAuthenticatorViewModel model)
        {
            model.ReturnUrl = model.ReturnUrl ?? Url.Content("~/");
            if(!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, rememberClient: false);
            if(result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }
            if(result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View(model);
            }
        }


        [HttpGet("auth/enable-authenticator")]
        public async Task<IActionResult> EnableAuthenticator()
        {
            string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
            var user = await _userManager.GetUserAsync(User);
            await _userManager.ResetAuthenticatorKeyAsync(user!);
            var token = await _userManager.GetAuthenticatorKeyAsync(user!);
            
            string AuthUri = string.Format
            (
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Identity Manager"),
                _urlEncoder.Encode(user.Email),
                token
            );
            var model = new TwoFactorAuthenticationViewModel
            {
                Token = token!,
                QRCodeUrl = AuthUri
            };

            return View(model);
        }

        [HttpGet("auth/remove-authenticator")]
        public async Task<IActionResult> RemoveAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return View("Error");
            }
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _userManager.SetTwoFactorEnabledAsync(user, false);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost("auth/enable-authenticator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(TwoFactorAuthenticationViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var succeeded = await _userManager.VerifyTwoFactorTokenAsync
                (
                    user!,
                    _userManager.Options.Tokens.AuthenticatorTokenProvider,
                    model.Code 
                );
                if(succeeded)
                {
                    await _userManager.SetTwoFactorEnabledAsync(user, true);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Your two factor auth code could not validated.");
                    return View(model);
                }

                return RedirectToAction(nameof(AuthenticatorConfirmation));
            }

            return View("Error");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, [FromQuery]string? returnUrl =null)
        {
            returnUrl ??= Url.Content("~/");
            
            if(ModelState.IsValid)
            {
                var user = await _signInManager.PasswordSignInAsync
                (
                    model.Email, 
                    model.Password, 
                    model.RememberMe, 
                    true
                );
                if(user.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                else if(user.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(VerifyAuthenticator), new{ returnUrl = returnUrl, rememberMe = model.RememberMe});
                }
                else if(user.IsLockedOut)
                {
                    return View("Lockout");
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");

            }
            return View(model);
        }

        [HttpPost("auth/register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, [FromQuery]string? returnUrl =null)
        {
            returnUrl ??= Url.Content("~/");

            if(ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if(result.Succeeded)
                {
                    if(model.RoleSelected!=null && model.RoleSelected.Length>0)
                    {
                        await _userManager.AddToRoleAsync(user, model.RoleSelected);
                    }

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action
                    (
                        "ConfirmEmail", 
                        "Account", 
                        new {userId = user.Id, code = code}, 
                        protocol: HttpContext.Request.Scheme
                    );

                    try
                    {
                        await _emailSender.SendEmailAsync
                        (
                            model.Email, 
                            "Confirm Email", 
                            $"Please confirm your email by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
                        );
                        _logger.LogInformation("Email sent");
                        _logger.LogDebug($"callbackUrl: '{callbackUrl}'");
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e.Message);
                    }

                    return RedirectToAction(nameof(RegistrationConfirmation));
                }
                AddErrors(result);
            }

            model.RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
            {
                Text = i,
                Value = i
            });

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action
                (
                    "ResetPassword", 
                    "Account", 
                    new {userId = user.Id, code = code}, 
                    protocol: HttpContext.Request.Scheme
                );

                try
                {
                    await _emailSender.SendEmailAsync
                    (
                        model.Email, 
                        "Reset Password", 
                        $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
                    );
                    _logger.LogInformation("Email sent");
                    _logger.LogDebug($"callbackUrl: '{callbackUrl}'");
                }
                catch(Exception e)
                {
                    _logger.LogError(e.Message);
                }
                

                return RedirectToAction("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            _logger.LogDebug($"model.Code: '{model.Code}'");
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if(result.Succeeded)
                {
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }
                AddErrors(result);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home"); 
        }
    }
}