using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Thekdar.Models;

namespace Thekdar.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HandleStatusCode(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            
            _logger.LogWarning($"Status code {statusCode} occurred. Original path: {statusCodeResult?.OriginalPath}");

            switch (statusCode)
            {
                case 404:
                    return View("NotFound");
                case 403:
                    return View("AccessDenied");
                case 500:
                    return View("ServerError");
                default:
                    return View("Error", new ErrorViewModel 
                    { 
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                    });
            }
        }

        [AllowAnonymous]
        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Access denied for user {User}. Requested path: {Path}", User.Identity?.Name ?? "Anonymous", HttpContext.Request.Path);
            return View("AccessDenied");
        }

        [Route("Error")]
        public IActionResult HandleError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            
            _logger.LogError(exception, $"Unhandled exception at {exceptionHandlerPathFeature?.Path}");

            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Exception = exception,
                Path = exceptionHandlerPathFeature?.Path
            };
            
            return View("Error", errorViewModel);
        }
    }
}
