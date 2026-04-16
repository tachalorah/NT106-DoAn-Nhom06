using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SecureChat.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public abstract class BaseController : ControllerBase
	{
		protected string NewID() => Guid.NewGuid().ToString("N")[..8];
	}
}
