using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoAn2025.Repository.Components
{
	public class CategoriesViewComponent : ViewComponent
	{
		private readonly DataContext _dataContext;
		public CategoriesViewComponent(DataContext context)
		{
			_dataContext = context;
		}

		public async Task<IViewComponentResult> InvokeAsync() => View(await _dataContext.Categories.ToListAsync());
	}
}
 