using IdentityManager.Data;
using IdentityManager.Models;
using IdentityManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityManager.Controllers
{
    [Route("user")]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly ILogger<UserController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController
        (
            ILogger<UserController> logger,
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userList = _db.ApplicationUser.ToList();
            var userRole = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach(var user in userList)
            {
                user.Role = await _userManager.GetRolesAsync(user) as List<string>;
            }

            return View(userList);
        }

        [HttpGet("manage-role")]
        public async Task<IActionResult> ManageRole([FromQuery]string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            List<string>? existingUserRoles = await _userManager.GetRolesAsync(user) as List<string>;
            var model = new RolesViewModel()
            {
                User = user
            };
            foreach (var role in _roleManager.Roles)
            {
                RoleSelection roleSelection = new RoleSelection()
                {
                    RoleName = role.Name
                };
                if (existingUserRoles.Any(c => c == role.Name))
                {
                    roleSelection.IsSelected = true;
                }
                model.RolesList.Add(roleSelection);
            }
            return View(model);
        }

        [HttpPost("manage-role")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRole(RolesViewModel model, [FromQuery]string userId)
        {
            var user = await _userManager.FindByIdAsync(model.User.Id);
            if (user == null)
            {
                return NotFound();
            }
            if(user.Id != model.User.Id)
            {
                return BadRequest();
            }
            
            var oldUserRoles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, oldUserRoles);
            if (!result.Succeeded)
            {
                TempData["error"] = "Error removing existing roles";
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(
                user,
                model.RolesList.Where(x => x.IsSelected).Select(y => y.RoleName)
            );
            if (!result.Succeeded)
            {
                TempData["error"] = "Error adding new roles";
                return View(model);
            }
            TempData["success"] = "Roles updated successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("lock-unlock-user")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUnlockUser([FromQuery]string userId)
        {
            var user = await _db.ApplicationUser.FirstOrDefaultAsync(c => c.Id == userId);
            if (user == null)
            {
                return NotFound();
            }
            
            if(user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                user.LockoutEnd = DateTime.Now;
                TempData["success"] = "User unlocked successfully";
            }
            else
            {
                user.LockoutEnd = DateTime.Now.AddYears(100);
                TempData["success"] = "User locked successfully";
            }
            await _db.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromQuery]string userId)
        {
            var user = _db.ApplicationUser.FirstOrDefault(c => c.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            _db.ApplicationUser.Remove(user);
            await _db.SaveChangesAsync();
            
            TempData["success"] = "User deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}