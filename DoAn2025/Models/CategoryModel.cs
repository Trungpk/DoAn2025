using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace DoAn2025.Models
{
    public class CategoryModel
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Name required")]
        public string Name { get; set; }
		[Required(ErrorMessage = "Request to enter Category Description")]
		public string Description { get; set; }

		public string Slug { get; set; } 
        public int Status { get; set; }
    }
}
