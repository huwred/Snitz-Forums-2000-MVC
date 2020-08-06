using SnitzCore.Filters;

namespace WWW.ViewModels
{
    public class AdminCreateUserViewModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Compare("Password")]
        public string Confirm { get; set; }

    }
}