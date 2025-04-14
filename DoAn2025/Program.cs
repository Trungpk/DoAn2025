using DoAn2025.Models;
using DoAn2025.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;
using DoAn2025.Repository;

var builder = WebApplication.CreateBuilder(args);

// Kết nối Database
builder.Services.AddDbContext<DataContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectedDb"));
});

// Cấu hình Identity
builder.Services.AddIdentity<AppUserModel, IdentityRole>()
	.AddEntityFrameworkStores<DataContext>()
	.AddDefaultTokenProviders();

// Cấu hình tùy chỉnh cho Identity
builder.Services.Configure<IdentityOptions>(options =>
{
	// Cấu hình mật khẩu
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequiredLength = 4;

	// Cấu hình email duy nhất
	options.User.RequireUniqueEmail = true;
});

// Cấu hình Session (đã có sẵn, không cần thêm lại)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian timeout 30 phút
	options.Cookie.IsEssential = true;              // Cookie thiết yếu cho ứng dụng
});

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode={0}");

// Middleware xử lý Session (đã có sẵn, không cần thêm lại)
app.UseSession();

// Middleware phục vụ file tĩnh
app.UseStaticFiles();

// Xử lý lỗi nếu không phải môi trường Development
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
}

// Cấu hình định tuyến
app.UseRouting();

// Bật xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Định tuyến cho các Areas
app.MapControllerRoute(
	name: "Areas",
	pattern: "{area:exists}/{controller=Product}/{action=Index}/{id?}");

// Định tuyến cho Category
app.MapControllerRoute(
	name: "category",
	pattern: "/category/{Slug?}",
	defaults: new { controller = "Category", action = "Index" });

// Định tuyến cho Brand
app.MapControllerRoute(
	name: "brand",
	pattern: "/brand/{Slug?}",
	defaults: new { controller = "Brand", action = "Index" });

// Định tuyến mặc định
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

// Seeding Data
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<DataContext>();
	SeedData.SeedingData(context);
}

app.Run();