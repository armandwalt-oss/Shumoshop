namespace WebApplication1.Middleware
{
    /// <summary>
    /// Adds basic security response headers to every HTTP response.
    ///
    /// What each header does:
    ///   • X-Content-Type-Options: nosniff
    ///       Browsers won't second-guess our declared Content-Type. Stops a
    ///       resource served as text from being executed as a script if the
    ///       MIME sniffer thinks it looks like JS.
    ///   • X-Frame-Options: SAMEORIGIN
    ///       Browsers refuse to embed our pages in an &lt;iframe&gt; on another
    ///       origin. Mitigates clickjacking. SAMEORIGIN (not DENY) so our own
    ///       admin previews still work.
    ///   • Referrer-Policy: strict-origin-when-cross-origin
    ///       Outgoing links to other sites get the origin only, not the path
    ///       or query. Don't leak order IDs or user IDs in Referer headers
    ///       to third parties (PayFast, image CDNs, etc.).
    ///   • Permissions-Policy
    ///       Tells the browser to refuse access to powerful APIs we don't
    ///       use. Reduces blast radius if a malicious script ever lands.
    ///   • Content-Security-Policy
    ///       Conservative starter CSP — allows our own origin plus the CDNs
    ///       we already use (FontAwesome, Bootstrap). Open this up later as
    ///       needed; tightening it after the fact is harder.
    ///
    /// Strict-Transport-Security is intentionally NOT set here because the
    /// existing UseHsts() in Program.cs handles that already.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Use indexer assignment so multiple middlewares don't double up.
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(self), interest-cohort=()";

            // Content-Security-Policy: keep it permissive enough to not break
            // existing pages but tight enough to block obvious XSS payloads.
            // 'unsafe-inline' for scripts/styles is included because the
            // project uses inline <script> blocks and inline styles in views.
            // Tightening this further would require moving those to external
            // files and using nonces / hashes.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "img-src 'self' data: https:; " +
                "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
                "font-src 'self' https://cdnjs.cloudflare.com data:; " +
                "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://www.googletagmanager.com https://www.google-analytics.com https://www.clarity.ms https://*.clarity.ms; " +
                "connect-src 'self' https://api.portal.thecourierguy.co.za https://api.shiplogic.com https://sandbox.payfast.co.za https://www.payfast.co.za https://www.google-analytics.com https://*.google-analytics.com https://www.clarity.ms https://*.clarity.ms; " +
                "frame-ancestors 'self'; " +
                "base-uri 'self'; " +
                "form-action 'self' https://sandbox.payfast.co.za https://www.payfast.co.za;";

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
            app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
