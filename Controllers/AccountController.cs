using System.Text.Encodings.Web;
using IdentityManager.Models;
using IdentityManager.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManager.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private ILogger<AccountController> _logger;

        public AccountController
        (
            ILogger<AccountController> logger,
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender
        ){
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login([FromQuery]string? returnUrl=null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register([FromQuery]string? returnUrl=null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet("registration-confirmation")]
        public IActionResult RegistrationConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword([FromQuery]string code = null)
        {
            return code == null ? View("Error") : View();
        }

        [HttpGet("reset-password-confirmation")]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet("confirm-email")]
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

        [HttpPost]
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
                else if(user.IsLockedOut)
                {
                    return View("Lockout");
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");

            }
            return View(model);
        }

        [HttpPost]
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
            return View(model);
        }

        [HttpPost]
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