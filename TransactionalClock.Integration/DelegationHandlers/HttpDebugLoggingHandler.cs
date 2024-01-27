using System.Diagnostics;
using System.Net.Http.Headers;

namespace TransactionalClock.Integration.DelegationHandlers;

public class HttpDebugLoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) 
    { 
        var id = Guid.NewGuid().ToString();
        var msg = $"[{id} -   Request]";

        Debug.WriteLine($"{msg}========Start==========");
        Debug.WriteLine($"{msg} {request.Method} {request.RequestUri?.PathAndQuery} {request.RequestUri?.Scheme}/{request.Version}");
        Debug.WriteLine($"{msg} Host: {request.RequestUri?.Scheme}://{request.RequestUri?.Host}");

        foreach (var header in request.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
                Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

            if (request.Content is StringContent || IsTextBasedContentType(request.Headers) || IsTextBasedContentType(request.Content.Headers))
            {   
                var result = await request.Content.ReadAsStringAsync(cancellationToken);

                Debug.WriteLine($"{msg} Content:");
                Debug.WriteLine($"{msg} {string.Join("", result.Take(255))}...");

            }
        }

        var start = DateTime.Now;

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var end = DateTime.Now;

        Debug.WriteLine($"{msg} Duration: {end - start}");
        Debug.WriteLine($"{msg}==========End==========");

        msg = $"[{id} - Response]";
        Debug.WriteLine($"{msg}=========Start=========");

        var resp = response;

        Debug.WriteLine($"{msg} {request.RequestUri?.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

        foreach (var header in resp.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        foreach (var header in resp.Content.Headers)
            Debug.WriteLine($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

        if (resp.Content is StringContent || IsTextBasedContentType(resp.Headers) || IsTextBasedContentType(resp.Content.Headers))
        {
            start = DateTime.Now;
            var result = await resp.Content.ReadAsStringAsync(cancellationToken);
            end = DateTime.Now;

            Debug.WriteLine($"{msg} Content:");
            Debug.WriteLine($"{msg} {string.Join("", result.Take(255))}...");
            Debug.WriteLine($"{msg} Duration: {end - start}");
        }

        Debug.WriteLine($"{msg}==========End==========");
        return response;
    }

    private static readonly string[] Types = { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

    private static bool IsTextBasedContentType(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Content-Type", out var values))
            return false;
        var header = string.Join(" ", values).ToLowerInvariant();

        return Types.Any(t => header.Contains(t));
    }
}