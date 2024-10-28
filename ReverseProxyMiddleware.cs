using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Fhi.ReverseProxyMiddleware;

/// <summary>
/// Initializes a new instance of the <see cref="ReverseProxyMiddleware"/> class.
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="nextMiddleware"></param>
/// <param name="httpClientFactory"></param>
/// <param name="reverseProxyOptions"></param>
public class ReverseProxyMiddleware(RequestDelegate nextMiddleware, IHttpClientFactory httpClientFactory, IOptions<ReverseProxyOptions> reverseProxyOptions)
{
    private HttpClient? _httpClient;
    private readonly ReverseProxyOptions _reverseProxyOptions = reverseProxyOptions.Value;

    /// <summary>
    /// Runs whenever an api-call is made
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        _httpClient ??= httpClientFactory.CreateClient(_reverseProxyOptions.HttpClientName);

        var targetUri = BuildTargetUri(context.Request);
        var allowedHttpMethods = _reverseProxyOptions.AllowedHttpMethods.Split(';');

        if (!string.IsNullOrEmpty(targetUri) &&
            allowedHttpMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            var targetRequestMessage = CreateTargetMessage(context, targetUri);

            using var responseMessage = await _httpClient.SendAsync(targetRequestMessage);
            context.Response.StatusCode = (int)responseMessage.StatusCode;

            await ProcessResponseContent(context, responseMessage);
            return;
        }

        await nextMiddleware(context);
    }

    private string BuildTargetUri(HttpRequest request)
    {
        if (_reverseProxyOptions.IncludeTargetPath)
        {
            var targetPath = _reverseProxyOptions.IncludeTargetPath ? _reverseProxyOptions.TargetPath : string.Empty;
            return request.Path.StartsWithSegments("/" + _reverseProxyOptions.TargetPath, out var remainingPath)
                ? $"{targetPath}{remainingPath}{request.QueryString}"
                : string.Empty;
        }
        else
        {
            var targetPath = request.Path.HasValue ? request.Path.Value : string.Empty;

            if (targetPath.Length > 0 && targetPath[0].Equals('/'))
            {
                targetPath = targetPath[1..];
            }
            return $"{targetPath}{request.QueryString}";
        }
    }

    private static async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
    {
        var contentHeaders = responseMessage.Content.Headers;

        // Copy the response content headers only after ensuring they are complete.
        // We ask for Content-Length first because HttpContent lazily computes this
        // and only afterwards writes the value into the content headers.
        var _ = contentHeaders.ContentLength;

        foreach (var header in contentHeaders.Where(x => WhiteListedHeaders.Contains(x.Key)))
        {
            context.Response.Headers.Append(header.Key, header.Value.ToArray());
        }

        var content = await responseMessage.Content.ReadAsByteArrayAsync();
        await context.Response.Body.WriteAsync(content);
    }

    private static readonly string[] WhiteListedHeaders =
[
            "Content-Length",
            "Content-Type",
            "Content-Disposition",
            "Cache-Control",
            "Access-Control-Expose-Headers",
        ];

    private static HttpRequestMessage CreateTargetMessage(HttpContext context, string targetUri)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUri);
        CopyFromOriginalRequestContentAndHeaders(context, requestMessage);
        requestMessage.Method = GetMethod(context.Request.Method);
        return requestMessage;
    }

    private static void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        var requestMethod = context.Request.Method;

        if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
        {
            var streamContent = new StreamContent(context.Request.Body);
            requestMessage.Content = streamContent;
        }

        foreach (var header in context.Request.Headers)
        {
            requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private static HttpMethod GetMethod(string method)
    {
        if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
        if (HttpMethods.IsGet(method)) return HttpMethod.Get;
        if (HttpMethods.IsHead(method)) return HttpMethod.Head;
        if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
        if (HttpMethods.IsPost(method)) return HttpMethod.Post;
        if (HttpMethods.IsPut(method)) return HttpMethod.Put;
        if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;

        return new HttpMethod(method);
    }
}
