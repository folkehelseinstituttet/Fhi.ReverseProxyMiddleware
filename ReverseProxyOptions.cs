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
    /// Constuctor
    /// </summary>
    public ReverseProxyOptions()
    {
        HttpClientName = string.Empty;
        TargetPath = string.Empty;
    }
}
