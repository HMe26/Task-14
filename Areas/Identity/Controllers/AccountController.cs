using CinemaApp.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Identity.Controllers;

[Area(AreaConstants.IDENTITY_AREA)]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        IRepository<ApplicationUserOTP> applicationUserOTPRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _applicationUserOTPRepository = applicationUserOTPRepository;
    }

    // Decide which area a user should land on based on their role(s).
    private async Task<IActionResult> RedirectToHomeAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        bool isStaff = roles.Contains(RoleConstants.SUPER_ADMIN)
                    || roles.Contains(RoleConstants.ADMIN)
                    || roles.Contains(RoleConstants.EMPLOYEE);

        string area = isStaff ? AreaConstants.ADMIN_AREA : AreaConstants.CUSTOMER_AREA;

        return RedirectToAction(nameof(HomeController.Index), ControllerConstants.HOME_CONTROLLER, new { area });
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
                return await RedirectToHomeAsync(currentUser);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM registerVM)
    {
        if (!ModelState.IsValid)
            return View(registerVM);

        ApplicationUser user = new()
        {
            FirstName = registerVM.FirstName,
            LastName = registerVM.LastName,
            Email = registerVM.Email,
            UserName = registerVM.UserName,
        };

        var result = await _userManager.CreateAsync(user, registerVM.Password);

        if (!result.Succeeded)
        {
            foreach (var item in result.Errors)
            {
                ModelState.AddModelError(string.Empty, item.Description);
            }

            return View(registerVM);
        }

        {
            // Send confirmation mail
            await SendConfirmationEmailAsync(user);
        }

        await _userManager.AddToRoleAsync(user, RoleConstants.EMPLOYEE);

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Create Account Successfully, please verify your account";

        return RedirectToAction(nameof(Login));
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = Url.Action(nameof(Confirm), ControllerConstants.ACCOUNT_CONTROLLER, new { area = AreaConstants.IDENTITY_AREA, user.Id, token }, Request.Scheme);
        var encodedLink = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(link!);
        string body = $"<h1>Please confirm your account by clicking <b><a href='{encodedLink}'>here</a></b></h1>";

        await _emailSender.SendEmailAsync(user.Email!, "Confirm Your Account", body);
    }

    public async Task<IActionResult> Confirm(string id, string token)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
            return NotFound();

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            TempData[NotificationConstants.ERROR_NOTIFICATION] = string.Join(", ", result.Errors.Select(e => e.Description));
        else
            TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Confirm Account successfully, please login";

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> ResendConfirmation()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
                return await RedirectToHomeAsync(currentUser);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResendConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
    {
        if (!ModelState.IsValid)
            return View(resendEmailConfirmationVM);

        var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.EmailOrUserName) ??
                                await _userManager.FindByNameAsync(resendEmailConfirmationVM.EmailOrUserName);

        if (user is null)
        {
            ModelState.AddModelError(nameof(ResendEmailConfirmationVM.EmailOrUserName), "Invalid User Name or Email");

            return View(resendEmailConfirmationVM);
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Your account is already verified, please login";
            return RedirectToAction(nameof(Login));
        }

        await SendConfirmationEmailAsync(user);

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Confirmation email sent, please check your inbox";

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
                return await RedirectToHomeAsync(currentUser);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM loginVM)
    {
        if (!ModelState.IsValid)
            return View(loginVM);

        // 1. Check user name or email
        var user = await _userManager.FindByEmailAsync(loginVM.EmailOrUserName) ??
                                await _userManager.FindByNameAsync(loginVM.EmailOrUserName);

        if (user is null)
        {
            ModelState.AddModelError(nameof(LoginVM.EmailOrUserName), "Invalid User Name or Email");
            ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

            return View(loginVM);
        }

        // 2. Login & check remember me + lockout
        var signInResult = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.Remember, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            ModelState.AddModelError(nameof(LoginVM.EmailOrUserName), "Too many attempts, please try again later");
            return View(loginVM);
        }

        if (signInResult.IsNotAllowed)
        {
            ModelState.AddModelError(nameof(LoginVM.EmailOrUserName), "Please verify your account!");
            return View(loginVM);
        }

        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError(nameof(LoginVM.EmailOrUserName), "Invalid User Name or Email");
            ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

            return View(loginVM);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = $"Welcome Back {user.FirstName} {user.LastName}";

        return await RedirectToHomeAsync(user);
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Logout successfully";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> ForgetPassword()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
                return await RedirectToHomeAsync(currentUser);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(forgetPasswordVM);

        var user = await _userManager.FindByEmailAsync(forgetPasswordVM.EmailOrUserName) ??
                                await _userManager.FindByNameAsync(forgetPasswordVM.EmailOrUserName);

        if (user is null)
        {
            ModelState.AddModelError(nameof(ForgetPasswordVM.EmailOrUserName), "Invalid User Name or Email");

            return View(forgetPasswordVM);
        }

        string otp = new Random().Next(1000, 9999).ToString();

        await _applicationUserOTPRepository.CreateAsync(new()
        {
            ApplicationUserId = user.Id,
            OTP = otp,
        }, ct);
        await _applicationUserOTPRepository.CommitAsync(ct);

        string body = $"<h1>Your otp number is: {otp}. don't share it.</h1>";
        await _emailSender.SendEmailAsync(user.Email!, "Reset your account", body);

        TempData["RedirectToValidateOTP"] = Guid.NewGuid();
        Response.Cookies.Append("userId", user.Id);

        return RedirectToAction(nameof(ValidateOTP));
    }

    [HttpGet]
    public async Task<IActionResult> ValidateOTP()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
                return await RedirectToHomeAsync(currentUser);
        }

        if (TempData["RedirectToValidateOTP"] is null)
            return NotFound();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ValidateOTP(ValidateOTPVM validateOTP)
    {
        if (!ModelState.IsValid)
            return View(validateOTP);

        var userId = Request.Cookies["userId"];
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var otpInDB = _applicationUserOTPRepository
            .Get(e => e.ApplicationUserId == userId && !e.IsUsed && e.ValidTo >= DateTime.UtcNow)
            .OrderBy(e => e.CreateAt)
            .LastOrDefault();

        if (otpInDB is null || validateOTP.OTP != otpInDB.OTP)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Invalid OTP";

            TempData["RedirectToValidateOTP"] = Guid.NewGuid();
            return RedirectToAction(nameof(ValidateOTP));
        }

        otpInDB.IsUsed = true;
        await _applicationUserOTPRepository.CommitAsync();

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Valid OTP, you can now change your password";

        return RedirectToAction(nameof(NewPassword));
    }

    [HttpGet]
    public IActionResult NewPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> NewPassword(NewPasswordVM newPasswordVM)
    {
        if (!ModelState.IsValid)
            return View(newPasswordVM);

        var userId = Request.Cookies["userId"];
        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPasswordVM.Password);

        if (!result.Succeeded)
        {
            foreach (var item in result.Errors)
            {
                ModelState.AddModelError(string.Empty, item.Description);
            }

            return View(newPasswordVM);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Reset Password successfully";
        Response.Cookies.Delete("userId");

        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
