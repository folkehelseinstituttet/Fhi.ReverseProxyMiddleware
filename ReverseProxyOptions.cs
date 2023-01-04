namespace Fhi.ReverseProxyMiddleware;

/// <summary>
/// Initializes a new instance of the <see cref="ReverseProxyOptions"/> class.
/// </summary>
public class ReverseProxyOptions
{
    /// <summary>
    /// The name of the HttpClient to use for the proxy.
    /// </summary>
    public string HttpClientName { get; set; }

    /// <summary>
    /// Target path that the middleware will intercept; ie 'api'
    /// </summary>
    public string TargetPath { get; set; }

    /// <summary>
    /// List of allowed http methods separated by ';' (ie 'get;post')
    /// By default all of them are allowed
    /// </summary>
    public string AllowedHttpMethods { get; set; }

    /// <summary>
    /// Constuctor
    /// </summary>
    public ReverseProxyOptions()
    {
        HttpClientName = string.Empty;
        TargetPath = string.Empty;
        AllowedHttpMethods = "connect;delete;get;head;options;patch;post;put;trace";
    }
}
