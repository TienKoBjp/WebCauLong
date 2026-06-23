using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetMvcApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMvcApp.Services;

public class ChatbotService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private List<ProductVector> _productVectors = new();

    public ChatbotService(IConfiguration config, IServiceProvider serviceProvider)
    {
        _config = config;
        _httpClient = new HttpClient();
        _serviceProvider = serviceProvider;
    }

    public void Initialize()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var products = context.Products.ToList();
        
        foreach(var p in products)
        {
            var text = $"{p.Name}. {p.Description}. Giá: {p.Price} VNĐ.";
            _productVectors.Add(new ProductVector { ProductId = p.Id, Text = text });
        }
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var apiKey = _config["AI:GeminiKey"] ?? "";
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("placeholder"))
        {
            // Simple keyword search fallback if no valid API key is set
            var found = _productVectors.Where(p => 
                userMessage.Contains(p.Text.Split('.')[0], StringComparison.OrdinalIgnoreCase) ||
                p.Text.Contains(userMessage, StringComparison.OrdinalIgnoreCase)
            ).FirstOrDefault();

            if (found != null) 
                return $"Dựa vào dữ liệu cửa hàng, tôi tìm thấy sản phẩm này có thể phù hợp với bạn: {found.Text.Split('.')[0]} (Giá: {found.Text.Split("Giá: ")[1]}). Bạn có thể tìm tên sản phẩm trên thanh tìm kiếm nhé!";
            
            return "Hệ thống AI đang chạy ở chế độ Offline (chưa có API Key). Tôi chưa thể suy luận phức tạp. Vui lòng thêm Gemini API Key vào appsettings.json!";
        }

        // RAG Context retrieval
        var userMsgLower = userMessage.ToLower();
        var contextDocs = _productVectors
            .Where(p => userMsgLower.Contains(p.Text.Split('.')[0].ToLower()) || p.Text.ToLower().Contains(userMsgLower))
            .Select(p => p.Text)
            .ToList();

        if(!contextDocs.Any()) 
            contextDocs = _productVectors.Take(5).Select(p => p.Text).ToList(); // Default context
        
        var contextStr = string.Join("\n- ", contextDocs);

        var prompt = $"Bạn là nhân viên tư vấn nhiệt tình của cửa hàng thể thao Shop Yonex. Trả lời ngắn gọn, lịch sự.\nDữ liệu cửa hàng hiện có:\n- {contextStr}\n\nKhách hỏi: {userMessage}\nNhân viên:";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var reply = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    return reply ?? "Xin lỗi, tôi không thể trả lời lúc này.";
                }
                
                var errJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Gemini Error] Attempt {attempt + 1}/{maxRetries} - Status: {response.StatusCode}, Body: {errJson}");
                
                // Parse error message from Gemini API response
                var statusCode = (int)response.StatusCode;
                string apiErrorMsg = "";
                try
                {
                    using var errDoc = JsonDocument.Parse(errJson);
                    apiErrorMsg = errDoc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";
                }
                catch { }

                // Handle specific error cases
                if (statusCode == 400 && apiErrorMsg.Contains("API key", StringComparison.OrdinalIgnoreCase))
                    return "⚠️ API Key Gemini đã hết hạn hoặc không hợp lệ. Vui lòng tạo key mới tại https://aistudio.google.com/apikey và cập nhật vào appsettings.json";

                // Retry on 503 ServiceUnavailable or 429 TooManyRequests
                if ((statusCode == 503 || statusCode == 429) && attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 1s, 2s, 4s
                    continue;
                }

                // If still failing after retries on transient errors
                if (statusCode == 503 || statusCode == 429)
                {
                    if (apiErrorMsg.Contains("quota", StringComparison.OrdinalIgnoreCase))
                        return "⚠️ Đã hết quota miễn phí của Gemini API hôm nay. Vui lòng thử lại sau hoặc nâng cấp plan tại https://aistudio.google.com";
                    return "Hệ thống AI đang quá tải, vui lòng thử lại sau vài giây nhé! 🙏";
                }
                
                // Fallback to local search if API fails
                return GetOfflineFallback(userMessage) + $"\n\n(Ghi chú: API AI gặp lỗi {statusCode}. {(string.IsNullOrEmpty(apiErrorMsg) ? "Vui lòng kiểm tra lại API Key" : apiErrorMsg)})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gemini Exception] Attempt {attempt + 1}/{maxRetries} - {ex.Message}");
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    continue;
                }
                return GetOfflineFallback(userMessage) + $"\n\n(Lỗi AI: {ex.Message})";
            }
        }

        return GetOfflineFallback(userMessage);
    }

    private string GetOfflineFallback(string userMessage)
    {
        var found = _productVectors.Where(p => 
            userMessage.Contains(p.Text.Split('.')[0], StringComparison.OrdinalIgnoreCase) ||
            p.Text.Contains(userMessage, StringComparison.OrdinalIgnoreCase)
        ).FirstOrDefault();

        if (found != null) 
            return $"Dựa vào dữ liệu cửa hàng, tôi tìm thấy sản phẩm này có thể phù hợp với bạn: {found.Text.Split('.')[0]} (Giá: {found.Text.Split("Giá: ")[1]}). Bạn có thể tìm tên sản phẩm trên thanh tìm kiếm nhé!";
        
        return "Hệ thống AI đang chạy ở chế độ Offline. Vui lòng thêm Gemini API Key hợp lệ vào appsettings.json để sử dụng AI!";
    }

    private class ProductVector
    {
        public int ProductId { get; set; }
        public string Text { get; set; } = "";
    }
}
