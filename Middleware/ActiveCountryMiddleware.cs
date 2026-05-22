using WebApplication1.Services;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// Resolves the active country for the current request (via
    /// <see cref="ICountryService"/>) and pins it to <c>HttpContext.Items</c>
    /// so views and controllers can read it without re-querying the DB.
    ///
    /// Use in views like:
    ///   <c>var country = Context.Items[CountryKeys.HttpContextItemKey] as Country;</c>
    /// </summary>
    public class ActiveCountryMiddleware
    {
        private readonly RequestDelegate _next;

        public ActiveCountryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICountryService countries)
        {
            // Don't bother running this for static files / framework endpoints.
            // Cheap heuristic — skip if the path has a file extension we don't own.
            var path = context.Request.Path.Value ?? "";
            if (path.Contains('.') && !path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var active = await countries.ResolveActiveCountryAsync(context);
            context.Items[CountryKeys.HttpContextItemKey] = active;

            await _next(context);
        }
    }

    public static class ActiveCountryMiddlewareExtensions
    {
        public static IApplicationBuilder UseActiveCountry(this IApplicationBuilder app) =>
            app.UseMiddleware<ActiveCountryMiddleware>();
    }
}
