using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using DominoPontaDeQuina.Api.Controllers;

namespace DominoPontaDeQuina.Api;

internal sealed class RejeitarModeloInvalidoFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
            context.Result = context.ActionDescriptor is ControllerActionDescriptor action
                && action.ControllerTypeInfo.AsType() == typeof(IdentidadeController)
                && action.ActionName == nameof(IdentidadeController.SolicitarToken)
                ? new JsonResult(false) { StatusCode = StatusCodes.Status400BadRequest }
                : new StatusCodeResult(StatusCodes.Status400BadRequest);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
