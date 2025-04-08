using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn2025.Models
{
	public class ProductModel
	{
		[Key]
		public int Id { get; set; }
		[Required(ErrorMessage = "Required to enter Product Name")]
		public string Name { get; set; }

		public string Slug { get; set; }
		[Required, MinLength(4, ErrorMessage = "Product Description Required")]
		public string Description { get; set; }
		[Required(ErrorMessage = "Product Price Entry Request")]
		[Range(0.01, double.MaxValue)]
		[Column(TypeName = "decimal(8, 2)")]
		public decimal Price { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Select a brand")]
        public int BrandId { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Select a category")]

        public int CategoryId { get; set; }
		public CategoryModel Category { get; set; }
		public BrandModel Brand { get; set; }

		public string Image { get; set; } = "noimage.jpg";

		[NotMapped]
		public IFormFile ImageUpload { get; set; }
	}
}
