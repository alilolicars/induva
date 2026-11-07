using System.Diagnostics;
using Induva.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Induva.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var vm = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };

        if (statusCode == 404)
        {
            vm.Title = "Page Not Found";
            vm.Message = "The page or resource you requested could not be found or may have been moved.";
        }
        else if (statusCode == 403)
        {
            vm.Title = "Access Denied";
            vm.Message = "You do not have permission to access or modify this resource.";
        }
        else if (statusCode.HasValue && statusCode.Value >= 500)
        {
            vm.Title = "Server Error";
            vm.Message = "An unexpected internal server error occurred while processing your request.";
        }

        return View(vm);
    }
}
