using Dairy.Api.Extensions;
using Dairy.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDairy(builder.Configuration);

var app = builder.Build();

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");

app.UseHttpLogging();
app.UseCors(Dairy.Api.Extensions.ServiceCollectionExtensions.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dairy Management API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
if (swaggerEnabled)
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.Run();
