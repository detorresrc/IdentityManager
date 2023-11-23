namespace IdentityManager.Models.ViewModels;

public class RolesViewModel
{
    public List<RoleSelection> RolesList { get; set; } = new();
    public ApplicationUser User { get; set; }
}

public class RoleSelection
{
    public string RoleName { get; set; }
    public bool IsSelected { get; set; } = false;
}