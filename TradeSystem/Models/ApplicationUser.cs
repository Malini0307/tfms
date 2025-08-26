using Microsoft.AspNetCore.Identity;

namespace TradeSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
