using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Repository;
using DoAn2025.Models;

namespace DoAn2025.Controllers
{
	public class ProductController : Controller
	{
		private readonly DataContext _dataContext;

		public ProductController(DataContext context)
		{
			_dataContext = context;
		}

		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> Search(string searchTerm)
		{
			var products = await _dataContext.Products
				.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
				.ToListAsync();

			ViewBag.Keyword = searchTerm;

			return View(products);
		}

		public async Task<IActionResult> Details(int Id)
		{
			if (Id == null) return RedirectToAction("Index");

			var productsById = await _dataContext.Products
				.FirstOrDefaultAsync(p => p.Id == Id);

			if (productsById == null)
			{
				return RedirectToAction("Index");
			}

			return View(productsById);
		}

		public async Task<IActionResult> DetailsWithSlug(int id, string slug)
		{
			var product = await _dataContext.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (product == null || product.Slug != slug)
			{
				return NotFound();
			}

			return View("Details", product); // Vẫn dùng view Details.cshtml
		}
	}
}