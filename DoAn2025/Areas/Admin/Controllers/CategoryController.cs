using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Route("Admin/Category")]
	[Authorize(Roles = "Admin,Sale")]
	public class CategoryController : Controller
	{
		private readonly DataContext _dataContext;

		public CategoryController(DataContext context)
		{
			_dataContext = context;
		}

		[Route("Index")]
		public async Task<IActionResult> Index()
		{
			return View(await _dataContext.Categories.OrderByDescending(p => p.Id).ToListAsync());
		}

		[Route("Edit")]
		public async Task<IActionResult> Edit(int id)
		{
			CategoryModel category = await _dataContext.Categories.FindAsync(id);
			return View(category);
		}

		[Route("Edit")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(CategoryModel category)
		{
			if (ModelState.IsValid)
			{
				category.Slug = category.Name.Replace(" ", "-");
				var slug = await _dataContext.Categories.FirstOrDefaultAsync(p => p.Slug == category.Slug);
				if (slug != null)
				{
					ModelState.AddModelError("", "Category already exists.");
					return View(category);
				}

				_dataContext.Update(category);
				await _dataContext.SaveChangesAsync();
				TempData["success"] = "Category updated successfully.";
				return RedirectToAction("Index");
			}
			else
			{
				TempData["error"] = "Model has some errors.";
				List<string> errors = new List<string>();
				foreach (var value in ModelState.Values)
				{
					foreach (var error in value.Errors)
					{
						errors.Add(error.ErrorMessage);
					}
				}
				string errorMessage = string.Join("\n", errors);
				return BadRequest(errorMessage);
			}
		}

		[Route("Create")]
		public IActionResult Create()
		{
			return View();
		}

		[Route("Create")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CategoryModel category)
		{
			if (ModelState.IsValid)
			{
				category.Slug = category.Name.Replace(" ", "-");
				var slug = await _dataContext.Categories.FirstOrDefaultAsync(p => p.Slug == category.Slug);
				if (slug != null)
				{
					ModelState.AddModelError("", "Category already exists.");
					return View(category);
				}

				_dataContext.Add(category);
				await _dataContext.SaveChangesAsync();
				TempData["success"] = "Category added successfully.";
				return RedirectToAction("Index");
			}
			else
			{
				TempData["error"] = "Model has some errors.";
				List<string> errors = new List<string>();
				foreach (var value in ModelState.Values)
				{
					foreach (var error in value.Errors)
					{
						errors.Add(error.ErrorMessage);
					}
				}
				string errorMessage = string.Join("\n", errors);
				return BadRequest(errorMessage);
			}
		}

		[Route("Delete")]
		public async Task<IActionResult> Delete(int id)
		{
			CategoryModel category = await _dataContext.Categories.FindAsync(id);
			_dataContext.Categories.Remove(category);
			await _dataContext.SaveChangesAsync();
			TempData["success"] = "Category deleted successfully.";
			return RedirectToAction("Index");
		}
	}
}