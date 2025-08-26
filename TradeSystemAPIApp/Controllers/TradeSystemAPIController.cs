using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TradeSystemAPIApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeSystemAPIController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCalculatedRiskScore(decimal score)
        {
            var normalized_score = Math.Round(Math.Min((score / 165m) * 100m, 100m),3);
            return Ok(normalized_score);
        }
    }
}
