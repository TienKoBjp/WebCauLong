using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AspNetMvcApp.Services;

namespace AspNetMvcApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatbotService _chatbotService;

    public ChatController(ChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest();

        var response = await _chatbotService.GetResponseAsync(request.Message);
        return Ok(new { reply = response });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = "";
}
