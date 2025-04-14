using Whats.Webhook.Models;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;

namespace Whats.Webhook.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult MessagesList()
        {
            return PartialView();
        }

        [HttpPost]
        public IActionResult ClearHistory()
        {
            Messages.MessagesListStatic = new List<Message>();
            Messages.OpenAIConversationHistory = new List<ChatMessage>();
            return RedirectToAction("Index");
        }
    }
}