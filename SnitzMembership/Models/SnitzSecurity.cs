using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PetaPoco;

namespace SnitzMembership.Models
{
    [Table("webpages_Membership")]
    public class SnitzSecurity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [System.ComponentModel.DataAnnotations.Schema.Column("UserId")]
        public int UserId { get; set; }

        public DateTime? CreateDate { get; set; }

        public string ConfirmationToken { get; set; }

        public bool IsConfirmed { get; set; }

        public DateTime? LastPasswordFailureDate { get; set; }

        public int PasswordFailuresSinceLastSuccess { get; set; }

        public string Password { get; set; }

        public DateTime? PasswordChangedDate { get; set; }

        public string PasswordSalt { get; set; }

        public string PasswordVerificationToken { get; set; }

        public DateTime? PasswordVerificationTokenExpirationDate { get; set; }
    }

    [Table("webpages_UsersInRoles")]
    public class UserRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [System.ComponentModel.DataAnnotations.Schema.Column(Order = 1)]

        public int UserId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [System.ComponentModel.DataAnnotations.Schema.Column(Order = 2)]
        public int RoleId { get; set; }

        [ResultColumn]
        public String Username { get; set; }
        [ResultColumn]
        public String Rolename { get; set; }
    }

    [Table("webpages_Roles")]
    public class DefRole
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
