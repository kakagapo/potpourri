using System.Net;

namespace FederationMetadataValidator;

public sealed class FederationMetadataDownloader : IDisposable
{
    public const int MaximumResponseBytes = 512 * 1024;
    public const int MaximumResponseHeadersKilobytes = 4;
    public const int MaximumRetries = 2;

    private const int BufferSize = 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);

    private readonly HttpClient httpClient;

    public FederationMetadataDownloader(HttpMessageHandler? handler = null)
    {
        handler ??= new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = RequestTimeout,
            MaxResponseHeadersLength = MaximumResponseHeadersKilobytes,
        };

        httpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FederationMetadataValidator/1.0");
    }

    public async Task<DownloadResult> DownloadAsync(Uri requestedUri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendWithRetriesAsync(requestedUri, cancellationToken);

        Uri? responseUri = response.RequestMessage?.RequestUri;
        bool redirected = IsRedirect(response.StatusCode) ||
            (responseUri is not null && responseUri != requestedUri);
        if (redirected)
        {
            throw new Exception(
                $"Redirect detected: HTTP {(int)response.StatusCode}; requested '{requestedUri}', final '{responseUri}'.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"The endpoint returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
        {
            throw new Exception(
                $"The declared response size exceeds {MaximumResponseBytes} bytes.");
        }

        byte[] content = await ReadBoundedContentAsync(response.Content, cancellationToken);
        return new DownloadResult(
            response.StatusCode,
            responseUri,
            content,
            MaximumResponseHeadersKilobytes,
            MaximumResponseBytes,
            new ValidationCheck("Redirects", true, "No HTTP or final-URI redirect occurred."));
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private async Task<HttpResponseMessage> SendWithRetriesAsync(
        Uri requestedUri,
        CancellationToken cancellationToken)
    {
        TimeSpan retryDelay = InitialRetryDelay;

        for (int attempt = 0; attempt <= MaximumRetries; attempt++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestedUri);
            using CancellationTokenSource requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            requestTimeout.CancelAfter(RequestTimeout);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    requestTimeout.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == MaximumRetries)
                {
                    throw new Exception(
                        $"The request timed out after {MaximumRetries + 1} attempts.",
                        ex);
                }

                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
                continue;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(ex.Message, ex);
            }

            if (IsRetriable(response.StatusCode) && attempt < MaximumRetries)
            {
                response.Dispose();
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
                continue;
            }

            return response;
        }

        throw new InvalidOperationException("The retry loop completed without a response.");
    }

    private static async Task<byte[]> ReadBoundedContentAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using Stream responseStream = await content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream buffer = new();
        byte[] readBuffer = new byte[BufferSize];

        while (true)
        {
            using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            readTimeout.CancelAfter(ReadTimeout);

            int bytesRead;
            try
            {
                bytesRead = await responseStream.ReadAsync(readBuffer, readTimeout.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new Exception(
                    $"Reading the response timed out after {ReadTimeout.TotalSeconds:0} seconds.",
                    ex);
            }
            catch (IOException ex)
            {
                throw new Exception(ex.Message, ex);
            }

            if (bytesRead == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + bytesRead > MaximumResponseBytes)
            {
                throw new Exception(
                    $"The response exceeds {MaximumResponseBytes} bytes.");
            }

            buffer.Write(readBuffer, 0, bytesRead);
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        int numericStatus = (int)statusCode;
        return numericStatus >= 300 && numericStatus <= 399;
    }

    private static bool IsRetriable(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}

public sealed record DownloadResult(
    HttpStatusCode StatusCode,
    Uri? ResponseUri,
    byte[] Content,
    int MaximumResponseHeadersKilobytes,
    int MaximumResponseBytes,
    ValidationCheck RedirectCheck);
