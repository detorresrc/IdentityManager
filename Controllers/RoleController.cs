using IdentityManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManager.Controllers
{
    [Route("role")]
    [Authorize]
    public class RoleController : BaseController
    {
        private readonly ILogger<UserController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleController
        (
            ILogger<UserController> logger,
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager
        )
        {
            _logger = logger;
            _db = db;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var roles = _db.Roles.ToList();
            return View(roles);
        }

        [HttpGet("upsert")]
        public IActionResult Upsert(string roleId=null)
        {
            if(String.IsNullOrEmpty(roleId))
            {
                return View();
            }
            else
            {
                var role = _db.Roles.FirstOrDefault(r => r.Id == roleId);
                if(role==null)
                {
                    TempData["error"] = "Role not found!";
                    return RedirectToAction(nameof(Index));
                }
                return View(role);
            }
        }

        [HttpPost("upsert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(IdentityRole role, string roleId=null)
        {
            if(String.IsNullOrEmpty(role.Id))
            {
                await _roleManager.CreateAsync(new IdentityRole
                {
                    Name = role.Name,
                    NormalizedName = role.Name.ToUpper()
                }
                );
                TempData["success"] = "Role created successfully!";
            }
            else
            {
                var _role = await _roleManager.FindByIdAsync(roleId);
                if(_role == null)
                {
                    TempData["error"] = "Role not found!";
                    return RedirectToAction(nameof(Index));
                }
                _role.Name = role.Name;
                _role.NormalizedName = role.Name!.ToUpper();
                var result = await _roleManager.UpdateAsync(_role);
                TempData["success"] = "Role updated successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromQuery]string roleId)
        {
            var role = _db.Roles.FirstOrDefault(r => r.Id == roleId);
            if(role==null)
                return NotFound("Role not found!");

            var userRolesFirThisRole = _db.UserRoles.Count(ur => ur.RoleId == roleId);
            if( userRolesFirThisRole > 0 )
            {
                TempData["error"] = "Role cannot be deleted!";
                return RedirectToAction(nameof(Index));
            }

            await _roleManager.DeleteAsync(role);
            TempData["success"] = "Role deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}