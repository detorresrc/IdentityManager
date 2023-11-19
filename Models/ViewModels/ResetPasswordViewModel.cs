using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models.ViewModels
{
  public class ResetPasswordViewModel
  {
    [Required]
    public string Code { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
  }
}