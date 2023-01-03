﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Fhi.ReverseProxyMiddleware;

/// <summary>
/// Initializes a new instance of the <see cref="ReverseProxyMiddleware"/> class.
/// </summary>
public class ReverseProxyMiddleware
{
    private readonly HttpClient _httpClient;
    private readonly RequestDelegate _nextMiddleware;
    private readonly ReverseProxyOptions _reverseProxyOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="nextMiddleware"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="reverseProxyOptions"></param>
    public ReverseProxyMiddleware(RequestDelegate nextMiddleware, IHttpClientFactory httpClientFactory, IOptions<ReverseProxyOptions> reverseProxyOptions)
    {
        _nextMiddleware = nextMiddleware;
        _reverseProxyOptions = reverseProxyOptions.Value;
        _httpClient = httpClientFactory.CreateClient(_reverseProxyOptions.HttpClientName);
    }

    /// <summary>
    /// Runs whenever an api-call is made
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var targetUri = BuildTargetUri(context.Request);

        if (!string.IsNullOrEmpty(targetUri))
        {
            var targetRequestMessage = CreateTargetMessage(context, targetUri);

            using var responseMessage = await _httpClient.SendAsync(targetRequestMessage);
            context.Response.StatusCode = (int)responseMessage.StatusCode;

            await ProcessResponseContent(context, responseMessage);
            return;
        }

        await _nextMiddleware(context);
    }

    private string BuildTargetUri(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/" + _reverseProxyOptions.TargetPath, out var remainingPath)
            ? $"{_reverseProxyOptions.TargetPath}{remainingPath}{request.QueryString}"
            : string.Empty;
    }

    private static async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
    {
        var content = await responseMessage.Content.ReadAsByteArrayAsync();
        await context.Response.Body.WriteAsync(content);
    }

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
