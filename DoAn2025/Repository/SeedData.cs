using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;

namespace DoAn2025.Repository
{
	public class SeedData
	{
		public static void SeedingData(DataContext _context)
		{
            _context.Database.Migrate();
			if (!_context.Products.Any())
			{
				CategoryModel macbook = new CategoryModel { Name = "Macbook", Slug = "macbook", Description = "Macbook la hang lon nhat", Status = 1 };
				CategoryModel pc = new CategoryModel { Name = "Pc", Slug = "pc", Description = "pc la hang lon nhat", Status = 1 };
				BrandModel apple = new BrandModel { Name = "apple", Slug = "apple", Description = "apple la hang lon nhat", Status = 1 };
				BrandModel samsung = new BrandModel { Name = "Samsung", Slug = "samsung", Description = "Samsung la hang lon nhat", Status = 1 };

				_context.Products.AddRange(
					new ProductModel { Name = "Macbook", Slug = "Macbook", Description = "Macbook la tot nhat", Image = "1.jpg", Category = macbook, Brand = apple, Price = 1234 },
					new ProductModel { Name = "Pc", Slug = "pc", Description = "pc la tot nhat", Image = "1.jpg", Category = pc, Brand = samsung, Price = 1235 }

				);
				_context.SaveChanges();
			}
		}
	}
}
