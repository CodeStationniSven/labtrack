using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabTrack.Api.Filters;

public sealed class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var firstErrorMessage = errors.Values.FirstOrDefault()?.FirstOrDefault() 
            ?? "One or more validation errors occurred.";

        var apiError = new ApiError(
            "VALIDATION_ERROR",
            firstErrorMessage,
            errors);

        var response = ApiResponse<object>.Fail(apiError);

        context.Result = new BadRequestObjectResult(response);
    }
}