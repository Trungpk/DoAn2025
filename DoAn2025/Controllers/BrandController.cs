using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Controllers
{

	public class BrandController : Controller
	{
		private readonly DataContext _dataContext;
		public BrandController(DataContext context)
		{
			_dataContext = context;
		}
		public async Task<IActionResult> Index(string Slug = "")
		{
			BrandModel brand = _dataContext.BrandModels.Where(c => c.Slug == Slug).FirstOrDefault();

			if (brand == null) return RedirectToAction("Index");

			var productsByBrand = _dataContext.Products.Where(p => p.CategoryId == brand.Id);


			return View(await productsByBrand.OrderByDescending(p => p.Id).ToListAsync());

		}
	}
}
