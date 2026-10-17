using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public sealed class TeacherController : Controller
{
    public IActionResult Index() => View();
}
