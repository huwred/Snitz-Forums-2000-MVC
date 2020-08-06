using System.ComponentModel.DataAnnotations;
using SnitzDataModel.Validation;

namespace WWW.ViewModels
{
    public class SetupViewModel
    {
        public bool IntegratedSecurity { get; set; }

        [RequiredIf("IntegratedSecurity", false, "SQL Username Required")]
        public string SqlUser { get; set; }
        [RequiredIf("IntegratedSecurity", false, "SQL Password Required")]
        public string SqlPwd { get; set; }

        [Required]
        public string AdminUsername { get; set; }
        [Required(ErrorMessage = "Email Address Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string AdminEmail { get; set; }
        [Required(ErrorMessage = "Please provide an Admin password")]
        [StringLength(25, MinimumLength = 6, ErrorMessage = "Password should be between 6 and 25 characters long")]
        public string AdminPwd { get; set; }
        [Required(ErrorMessage = "Passwords do not match")]
        [Compare("AdminPwd")]
        public string AdminPwdConfirm { get; set; }
        public string DbsFile { get; set; }
    }

    public class UpdateViewModel
    {
        public string DbsFile { get; set; }
    }
}