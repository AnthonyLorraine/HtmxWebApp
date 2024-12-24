using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HtmxWebApp.Web.Engine;

public class HtmxComponent : IActionResult
{
    private readonly string _componentName;
    private readonly object? _model;

    public HtmxComponent(string componentName, object? model=null)
    {
        _componentName = componentName;
        _model = model;
    }

    private ViewEngineResult TryGetViewPath(ActionContext context, string viewPath)
    {
        const string viewExtension = ".cshtml";
        if (viewPath.EndsWith(viewExtension))
        {
            viewPath = viewPath.Replace(viewExtension, string.Empty);
        }

        var viewEngine = context.HttpContext.RequestServices
            .GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        
        ArgumentNullException.ThrowIfNull(viewEngine);
        const string basePath = "Views";
        const string componentsBasePath = "Components";
        var currentController = context.RouteData.Values["controller"]?.ToString() ?? "Home";
        string[] pathsToTry = [
            viewPath,
            basePath + $"/{currentController}/" + viewPath + viewExtension,
            componentsBasePath + $"/{currentController}/" + viewPath + viewExtension,
            basePath + "/Shared/" + viewPath + viewExtension,
            componentsBasePath  + "/Shared/" + viewPath + viewExtension,
            basePath + "/" + viewPath + viewExtension,
            componentsBasePath + "/" + viewPath + viewExtension
        ];

        foreach (var path in pathsToTry)
        {
            var result = viewEngine.GetView(null, path, false);

            if (!result.Success) continue;
            
            return result;
        }
        
        throw new FileNotFoundException("\nCould not find component: " + _componentName + $"\nPaths checked:\n{string.Join("\n", pathsToTry)}");
    }
    
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var viewResult = TryGetViewPath(context, _componentName);
        
        var metadataProvider = context.HttpContext.RequestServices
            .GetService(typeof(IModelMetadataProvider)) as IModelMetadataProvider;

        ArgumentNullException.ThrowIfNull(metadataProvider);

        var viewData = new ViewDataDictionary<object?>(metadataProvider, context.ModelState)
        {
            Model = _model
        };

        var tempDataFactory = context.HttpContext.RequestServices
            .GetService(typeof(ITempDataDictionaryFactory)) as ITempDataDictionaryFactory;
        var tempDataProvider = context.HttpContext.RequestServices
            .GetService(typeof(ITempDataProvider)) as ITempDataProvider;
        
        ArgumentNullException.ThrowIfNull(tempDataProvider);
        
        var tempData = tempDataFactory?.GetTempData(context.HttpContext) ??
                       new TempDataDictionary(context.HttpContext, tempDataProvider);
        
        
        await using var writer = new StringWriter();
        var viewContext = new ViewContext(
            context,
            viewResult.View!,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions()
            );
        
        await viewResult.View!.RenderAsync(viewContext);
        var renderedHtml = writer.ToString();
        
        context.HttpContext.Response.ContentType = "text/html";
        await context.HttpContext.Response.WriteAsync(renderedHtml);
    }
}