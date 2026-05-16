using System.Diagnostics;
using GrievanceRedressal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json; // Required for PostAsJsonAsync

namespace GrievanceRedressal.Controllers
{
    public class HomeController : Controller
    {
        private readonly GrievanceRepository _repo;
        private readonly IConfiguration _config;

        public HomeController(GrievanceRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/" }
            );
            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> GetChatResponse(string message, string? culture)
        {
            if (string.IsNullOrEmpty(message)) return Json(new { reply = "Describe your issue..." });

            // 1. Determine Language (Prioritize explicit parameter, then RequestFeature, then default)
            var rqf = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            string finalCulture = culture ?? rqf?.RequestCulture.UICulture.Name ?? "en";
            string languageName = finalCulture.StartsWith("hi") ? "Hindi" : (finalCulture.StartsWith("or") ? "Odia" : "English");

            // 2. Language-Specific Instructions (Isolation)
            string promptLang = "";
            string examples = "";

            if (languageName == "Hindi")
            {
                promptLang = "STRICT PURE HINDI in Devanagari script. NO English characters (A-Z).";
                examples = "GOOD RESPONSE:\nनमस्ते! आपकी समस्या 'तकनीकी' (Technical) श्रेणी में आती है। कदम:\n1. 'लॉगिन' करें\n2. 'शिकायत दर्ज करें' (Raise Grievance) बटन पर क्लिक करें\n3. 'तकनीकी' श्रेणी चुनें और विवरण भरकर सबमिट करें।";
            }
            else if (languageName == "Odia")
            {
                promptLang = "STRICT PURE ODIA in Odia script. NO English characters (A-Z) or Hindi words.";
                examples = "GOOD RESPONSE:\nନମସ୍କାର! ଆପଣଙ୍କର ସମସ୍ୟା 'ତୈଷୟିକ' (Technical) ବର୍ଗରେ ଆସୁଛି। ପଦକ୍ଷେପ:\n1. ଆପଣଙ୍କ ଆକାଉଣ୍ଟକୁ 'ଲଗଇନ୍' କରନ୍ତୁ\n2. 'ଅଭିଯୋଗ କରନ୍ତୁ' ବଟନ୍ ଉପରେ କ୍ଲିକ୍ କରନ୍ତୁ\n3. 'ତୈଷୟିକ' ବର୍ଗ ଚୟନ କରନ୍ତୁ ଏବଂ ବିବରଣୀ ପ୍ରଦାନ କରି ପ୍ରେରଣ କରନ୍ତୁ।";
            }
            else
            {
                promptLang = "STRICT PROFESSIONAL ENGLISH.";
                examples = "GOOD RESPONSE:\nHello! Your issue is categorized as 'Technical'. Steps to file in ReSolve360:\n1. Log in to your portal.\n2. Click on 'Raise Grievance'.\n3. Select 'Technical' category and submit the form.";
            }

            string systemPrompt = $"You are ReSolve360, a premium AI grievance analyzer. Respond ONLY in {languageName}. " +
                                  $"LANGUAGE RULE: {promptLang}\n" +
                                  $"EXAMPLES:\n{examples}\n\n" +
                                  "CRITICAL RULES:\n" +
                                  "1. IDENTIFY CATEGORY: Technical, Hostel, Academic, or Security.\n" +
                                  "2. PORTAL STEPS: Provide EXACTLY 3 steps to file the complaint WITHIN the ReSolve360 website. Do NOT give troubleshooting advice like 'restart'.\n" +
                                  "3. PURITY: No mixing of languages. 0% cross-over.";

            string apiKey = _config["AiConfig:GroqApiKey"];
            string model = "llama-3.1-8b-instant"; 
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "PASTE_YOUR_NEW_GROQ_KEY_HERE")
            {
                return Json(new { reply = "AI Configuration Error: Please provide a valid Groq API Key in appsettings.json." });
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var requestBody = new
                {
                    model = model,
                    messages = new[] {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    },
                    temperature = 0.5
                };

                try
                {
                    var response = await client.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", requestBody);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
                        
                        string reply = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? "No response content.";

                        return Json(new { reply = reply });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return Json(new { reply = $"AI Core Error: {error}" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { reply = $"I'm having trouble connecting to my AI core ({ex.Message}). Please try again later." });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> LogChatbot(string sessionId, string message, string response)
        {
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int id)) userId = id;
            }

            // Simple map for SuggestedCategory based on keywords in response
            string suggested = "General";
            string low = response.ToLower();
            if (low.Contains("technical") || low.Contains("system")) suggested = "Technical";
            else if (low.Contains("hostel") || low.Contains("mess")) suggested = "Hostel";
            else if (low.Contains("academic") || low.Contains("fees")) suggested = "Academic";
            else if (low.Contains("security") || low.Contains("safe")) suggested = "Security";

            await _repo.AddChatbotLogAsync(sessionId, userId, message, suggested);
            return Json(new { success = true });
        }
    }
}
