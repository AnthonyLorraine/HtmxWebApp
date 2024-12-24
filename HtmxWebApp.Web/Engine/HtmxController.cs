using Microsoft.AspNetCore.Mvc;

namespace HtmxWebApp.Web.Engine;

public class HtmxController : Controller
{
    public IActionResult HtmxComponent(string componentName, object? model = null)
    {
        return new HtmxComponent(componentName, model);
    }
}