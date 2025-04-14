using Microsoft.AspNetCore.Mvc;
using DoAn2025.Repository;
using DoAn2025.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAn2025.Controllers
{
    public class CategoryController : Controller
    {
		private readonly DataContext _dataContext;
        public CategoryController(DataContext context) 
        { 
            _dataContext = context;
        }
		public async Task<IActionResult> Index(string Slug= "")
        {
            CategoryModel category = _dataContext.Categories.Where(c => c.Slug == Slug).FirstOrDefault();

            if (category == null) return RedirectToAction("Index");

            var productsByCategory = _dataContext.Products.Where(p => p.CategoryId == category.Id);

             
			return View(await productsByCategory.OrderByDescending(p => p.Id).ToListAsync());

        }
    }
}
