﻿using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ExpressLocalizationSampleProject.LocalizationResources;
using LazZiya.ExpressLocalization;
using LazZiya.ExpressLocalization.DataAnnotations;
using LazZiya.ExpressLocalization.Messages;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ExpressLocalizationSampleProject.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ISharedCultureLocalizer _loc;

        private readonly string _culture;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ISharedCultureLocalizer loc)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _loc = loc;

            _culture = CultureInfo.CurrentCulture.Name;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [ExRequired]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [ExRequired]
            [ExStringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [ExCompare("Password")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content($"~/{_culture}");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content($"~/{_culture}");
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Page(
                        $"/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userId = user.Id, code = code, culture = _culture },
                        protocol: Request.Scheme);

                    var str = _loc.GetLocalizedString(LocalizedBackendMessages.VerificationEmailBody, args: HtmlEncoder.Default.Encode(callbackUrl));

                    await _emailSender.SendEmailAsync(Input.Email,
                        _loc.GetLocalizedString(LocalizedBackendMessages.VerificationEmailTitle),
                        str);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    //ModelState.AddModelError(string.Empty, error.Description);
                    TempData.Danger(error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
