using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace Web.Controllers
{
	public class ChatbotController : Controller
	{
		private readonly DataContext _context;
		private readonly IWebHostEnvironment _env;
		private readonly HttpClient _client = new();
		private readonly string _apiKey = ""; 
		private readonly Dictionary<string, string> _responses = new()
		{
			{"vi_welcome", "Chào bạn! Mình là trợ lý siêu vui! 😊 Tìm gì nào?"},
			{"en_welcome", "Hi! I'm your cheerful assistant! 😊 What's up?"},
			{"vi_no_product", "Hơi tiếc! 😔 Chưa có sản phẩm phù hợp. Thử lại nhé?"},
			{"en_no_product", "Tiny bummer! 😔 No products found. Try again?"},
			{"vi_found", "Tìm thấy rồi! 😍 Xem nhé:\n"},
			{"en_found", "Found it! 😍 Check these out:\n"},
			{"vi_product_link", "Wao, sản phẩm đã có trong shop mình nè! Nhấp vào đây để mua nha 😜"},
			{"en_product_link", "Wow, we’ve got this in our shop! Click here to grab it 😜"}
		};

		public ChatbotController(DataContext context, IWebHostEnvironment env)
		{
			_context = context;
			_env = env;
			_client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
		}

		[HttpPost]
		public async Task<JsonResult> GetChatbotResponse(string message, string language = null)
		{
			language = language ?? (message.Any(c => c >= 'À' && c <= 'ỹ') ? "vi" : "en");
			var history = HttpContext.Session.GetString("ChatHistory") ?? "";
			var (reply, productUrl) = await ProcessQuery(message.ToLower(), language, message);

			history = (history.Length > 5000 ? history.Substring(history.Length - 4000) : history) +
				$"<div><b>Bạn:</b> {message}</div><div><b>Bot:</b> {reply}</div>";
			HttpContext.Session.SetString("ChatHistory", history);
			return Json(new { reply, productUrl });
		}

		[HttpGet]
		public JsonResult GetChatHistory()
		{
			return Json(new { history = HttpContext.Session.GetString("ChatHistory") ?? "" });
		}

		private async Task<(string Reply, string ProductUrl)> ProcessQuery(string lowerMessage, string language, string originalMessage)
		{
			var budget = ExtractBudget(lowerMessage);
			var (minPrice, maxPrice) = ExtractPriceRange(lowerMessage);
			var products = new List<ProductModel>();
			string reply, productUrl = null;

			if (string.IsNullOrEmpty(HttpContext.Session.GetString("ChatHistory")))
			{
				return (_responses[$"{language}_welcome"], null);
			}

			if (minPrice.HasValue && maxPrice.HasValue)
			{
				products = await FindProducts(lowerMessage, budgetRange: (minPrice.Value, maxPrice.Value));
			}
			else if (budget.HasValue && IsCategoryQuery(lowerMessage))
			{
				products = await FindProducts(lowerMessage, budget: budget);
			}
			else if (budget.HasValue)
			{
				products = await FindProducts(lowerMessage, budget: budget);
			}
			else if (IsCategoryAndBrandQuery(lowerMessage))
			{
				products = await FindProducts(lowerMessage, byCategoryAndBrand: true);
			}
			else if (IsCategoryQuery(lowerMessage))
			{
				products = await FindProducts(lowerMessage, byCategory: true);
			}
			else if (IsBrandQuery(lowerMessage))
			{
				products = await FindProducts(lowerMessage, byBrand: true);
			}
			else if (IsProductRelatedQuery(lowerMessage))
			{
				var product = await FindProduct(lowerMessage);
				var openAiReply = await CallOpenAI($"Thông tin sản phẩm: {originalMessage}", language);
				reply = openAiReply;

				if (product != null)
				{
					productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
					reply += $"\n{_responses[$"{language}_found"]}<img src='{GetImagePath(product)}' alt='{product.Name}' width='100' /> {product.Name} - ${product.Price}\n{_responses[$"{language}_product_link"]} [Mua ngay]({productUrl})";
				}
				else
				{
					reply += $"\n{_responses[$"{language}_no_product"]}";
					// Ghi log để debug
					System.Diagnostics.Debug.WriteLine($"Không tìm thấy sản phẩm cho tin nhắn: {lowerMessage}");
				}

				return (reply, productUrl);
			}
			else
			{
				reply = await CallOpenAI(originalMessage, language);
				return (reply, null);
			}

			reply = products.Any()
				? _responses[$"{language}_found"] + string.Join("\n", products.Select(p =>
				{
					productUrl = Url.Action("DetailsWithSlug", "Product", new { id = p.Id, slug = p.Slug }, Request.Scheme);
					return $"<img src='{GetImagePath(p)}' alt='{p.Name}' width='100' /> {p.Name} - ${p.Price} - [{_responses[$"{language}_product_link"]}]({productUrl})";
				}))
				: _responses[$"{language}_no_product"];

			return (reply, productUrl);
		}

		private async Task<List<ProductModel>> FindProducts(string message, bool byCategory = false, bool byBrand = false, bool byCategoryAndBrand = false, decimal? budget = null, (decimal min, decimal max)? budgetRange = null)
		{
			var query = _context.Products
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.AsQueryable();

			if (byCategory || byCategoryAndBrand)
			{
				var category = await _context.Categories.FirstOrDefaultAsync(c => message.Contains(c.Name.ToLower()));
				if (category != null) query = query.Where(p => p.CategoryId == category.Id);
			}

			if (byBrand || byCategoryAndBrand)
			{
				var brand = await _context.BrandModels.FirstOrDefaultAsync(b => message.Contains(b.Name.ToLower()));
				if (brand != null) query = query.Where(p => p.BrandId == brand.Id);
			}

			if (budget.HasValue)
			{
				query = query.Where(p => p.Price <= budget.Value);
			}

			if (budgetRange.HasValue)
			{
				query = query.Where(p => p.Price >= budgetRange.Value.min && p.Price <= budgetRange.Value.max);
			}

			return await query.Take(5).ToListAsync();
		}

		private async Task<ProductModel> FindProduct(string message)
		{
			var query = _context.Products
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.AsQueryable();

			// Chuẩn hóa tin nhắn
			var normalizedMessage = message.ToLower().Trim();
			// Loại bỏ các từ không liên quan
			var stopWords = new[] { "thông", "tin", "chi", "tiết", "về", "information", "details", "about" };
			foreach (var stopWord in stopWords)
			{
				normalizedMessage = normalizedMessage.Replace(stopWord, " ");
			}
			normalizedMessage = Regex.Replace(normalizedMessage, @"\s+", " ").Trim();

			// Tìm danh mục (tùy chọn, không bắt buộc)
			var category = await _context.Categories.FirstOrDefaultAsync(c => normalizedMessage.Contains(c.Name.ToLower()));
			if (category != null)
			{
				query = query.Where(p => p.CategoryId == category.Id);
			}

			// Tách từ khóa
			var words = normalizedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Where(w => w.Length > 2)
				.ToList();

			// Xử lý từ đồng nghĩa
			var synonyms = new Dictionary<string, string[]>
			{
				{ "gram", new[] { "lg gram", "gram 17", "gram 16" } },
				{ "iphone", new[] { "apple iphone", "iphone 13", "iphone 14" } }
			};

			var expandedWords = new List<string>(words);
			foreach (var word in words)
			{
				if (synonyms.ContainsKey(word))
				{
					expandedWords.AddRange(synonyms[word]);
				}
			}

			// Tìm sản phẩm khớp với ít nhất một từ khóa
			if (expandedWords.Any())
			{
				query = query.Where(p => expandedWords.Any(w =>
					p.Name.ToLower().Contains(w) ||
					(p.Description != null && p.Description.ToLower().Contains(w))));
			}

			// Debug: Ghi log truy vấn
			System.Diagnostics.Debug.WriteLine($"Từ khóa tìm kiếm: {string.Join(", ", expandedWords)}");

			// Trả về sản phẩm phù hợp nhất
			return await query.OrderByDescending(p => p.Price).FirstOrDefaultAsync();
		}

		private async Task<string> CallOpenAI(string prompt, string language)
		{
			try
			{
				var request = new
				{
					model = "gpt-3.5-turbo",
					messages = new[] { new { role = "user", content = language == "vi" ? prompt : $"Respond in English: {prompt}" } },
					max_tokens = 500
				};

				var response = await _client.PostAsync("https://api.openai.com/v1/chat/completions",
					new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));
				response.EnsureSuccessStatusCode();

				var data = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
				return data.choices[0].message.content.ToString();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Lỗi OpenAI: {ex.Message}");
				return _responses[$"{language}_no_product"];
			}
		}

		private decimal? ExtractBudget(string message)
		{
			var match = Regex.Match(message, @"(?:tôi có |với |I have )?(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase);
			return match.Success && decimal.TryParse(match.Groups[1].Value, out var budget) ? budget : null;
		}

		private (decimal? minPrice, decimal? maxPrice) ExtractPriceRange(string message)
		{
			var match = Regex.Match(message, @"(?:từ |from )?(\d+(?:\.\d+)?)\s*(?:đến |to )\s*(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase);
			return match.Success && decimal.TryParse(match.Groups[1].Value, out var min) && decimal.TryParse(match.Groups[2].Value, out var max) ? (min, max) : (null, null);
		}

		private bool IsCategoryQuery(string message) => _context.Categories.Any(c => message.Contains(c.Name.ToLower()));
		private bool IsBrandQuery(string message) => _context.BrandModels.Any(b => message.Contains(b.Name.ToLower()));
		private bool IsCategoryAndBrandQuery(string message) => IsCategoryQuery(message) && IsBrandQuery(message);
		private bool IsProductRelatedQuery(string message) => new[] { "thông tin", "sản phẩm", "giá", "xem", "information", "product", "price", "details" }.Any(message.Contains);

		private string GetImagePath(ProductModel product)
		{
			var defaultImage = "/media/products/noimage.jpg";
			if (string.IsNullOrEmpty(product.Image) || product.Image == "noimage.jpg") return defaultImage;
			var path = Path.Combine(_env.WebRootPath, "media/products", product.Image);
			return System.IO.File.Exists(path) ? $"/media/products/{product.Image}" : defaultImage;
		}
	}
}