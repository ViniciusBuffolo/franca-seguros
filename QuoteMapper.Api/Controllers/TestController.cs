using Microsoft.AspNetCore.Mvc;

namespace QuoteMapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                message = "API is running successfully.",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok(new
            {
                success = true,
                message = "Hello Vinny!"
            });
        }
    }
}