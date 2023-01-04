# Fhi.ReverseProxyMiddleware
Handles reverse proxy if you just need to route api-calls through without doing anything more about them

## Setup example

Create a new asp.net web minimal api project

`dotnet new web -o TodoApi `

Add a httpClient in `program.cs`

```
builder.Services.AddHttpClient("httpClientName", c =>
{
    c.BaseAddress = new Uri("urlToApi");
});
```
    
Add this package

`dotnet add package fhi.reverseproxymiddleware`

Add configuration in appsettings.json. TargetPath will be what you want to reverse proxy; ie if your site is hosted on ```https://localhost```, with the configuration below, all calls to `https://localhost/api` will be forwarded to the url setup in the httpClient the specified name.

```
"ReverseProxyOptions": {
    "HttpClientName": "httpClientName",
    "TargetPath": "api" 
  } 
```

Add options and middleware into program.cs

```
var reverseProxyOptions = builder.Configuration.GetSection("ReverseProxyOptions").Get<ReverseProxyOptions>();
...
app.UseMiddleware<ReverseProxyMiddleware>(Options.Create(reverseProxyOptions));
```
