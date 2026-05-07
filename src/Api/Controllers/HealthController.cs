using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/health")]
[Authorize(Roles = "Admin,Supervisor")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
