using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = RoleConstants.SUPER_ADMIN)]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? query, int page = 1, int size = 5)
    {
        var users = _userManager.Users.AsQueryable();

        if (query is not null)
            users = users.Where(e => e.UserName!.ToLower().Contains(query.ToLower()) || e.Email!.ToLower().Contains(query.ToLower()));

        var totalPages = Math.Ceiling(users.Count() / (double)size);
        var pagedUsers = await users.Skip((page - 1) * size).Take(size).ToListAsync();

        var rows = new List<UserRowVM>();
        foreach (var user in pagedUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);

            rows.Add(new UserRowVM
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
            });
        }

        return View(new UserWithFilterVM
        {
            Users = rows,
            Query = query ?? "",
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        if (user.UserName == "SuperAdmin")
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Cannot change the SuperAdmin's role.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Update Role Successfully";

        return RedirectToAction(nameof(Index));
    }
}
