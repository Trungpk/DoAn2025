using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace DoAn2025.Models
{
	public class AppUserModel: IdentityUser
	{

        public string Occupation {  get; set; }

		public string RoleId { get; set; }	
	}
}
