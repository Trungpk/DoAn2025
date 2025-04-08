 using System.ComponentModel.DataAnnotations;

namespace DoAn2025.Models
{
    public class BrandModel
    {
		[Key]
		public int Id { get; set; }
		[Required(ErrorMessage = "Brand Name Required")]
		public string Name { get; set; }
		[Required(ErrorMessage = "Brand Description Required")]
		public string Description { get; set; }

		public string Slug { get; set; }
		public int Status { get; set; }
	}
}
