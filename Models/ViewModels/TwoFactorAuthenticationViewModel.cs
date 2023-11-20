using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models.ViewModels
{
    public class TwoFactorAuthenticationViewModel
    {
        [Required]
        public string Code { get; set; }
        public string? Token { get; set; }
        public string? QRCodeUrl { get; set; }
    }
}