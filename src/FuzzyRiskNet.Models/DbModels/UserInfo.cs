using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace FuzzyRiskNet.Models
{
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        [InverseProperty("User")]
        public virtual ICollection<Project> Projects { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserSetting> Settings { get; set; }

        public bool IsAdmin()
        {
            return Roles.Any(r => r.RoleId == "Admin");
        }
    }

    public class Log
    {
        public int ID { get; set; }

        public string UserID { get; set; }

        public string Url { get; set; }
        public string IP { get; set; }

        public string Action { get; set; }
        public string Message { get; set; }
    }

    public class UserSetting
    {
        public int ID { get; set; }

        public string UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        public string Path { get; set; }

        public string Value { get; set; }

    }
}
