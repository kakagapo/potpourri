namespace FederationMetadataValidator;

public sealed class FederationMetadataValidationService : IDisposable
{
    private readonly FederationMetadataDownloader downloader;
    private readonly FederationMetadataDocumentValidator documentValidator;

    public FederationMetadataValidationService(
        FederationMetadataDownloader? downloader = null,
        FederationMetadataDocumentValidator? documentValidator = null,
        ValidationMode redirectMode = ValidationMode.Enforce,
        ValidationMode signatureMode = ValidationMode.Enforce)
    {
        this.downloader = downloader ?? new FederationMetadataDownloader(redirectMode: redirectMode);
        this.documentValidator = documentValidator ?? new FederationMetadataDocumentValidator(signatureMode);
    }

    public async Task<ValidationReport> ValidateAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        List<ValidationCheck> checks = [];

        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri))
        {
            checks.Add(new ValidationCheck("URL", false, "The input is not an absolute URL."));
            return new ValidationReport(input, checks, []);
        }

        checks.Add(new ValidationCheck("URL", true, "The input is an absolute URL."));

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            checks.Add(new ValidationCheck(
                "HTTPS",
                false,
                $"Scheme '{uri.Scheme}' is not allowed; federation metadata must use HTTPS."));
            return new ValidationReport(input, checks, []);
        }

        checks.Add(new ValidationCheck("HTTPS", true, "The URL uses HTTPS."));

        DownloadResult download;
        try
        {
            download = await downloader.DownloadAsync(uri, cancellationToken);
        }
        catch (MetadataValidationException ex)
        {
            checks.Add(new ValidationCheck(ex.CheckName, false, ex.Message));
            return new ValidationReport(input, checks, []);
        }

        checks.Add(new ValidationCheck(
            "HTTP retrieval",
            true,
            $"Received HTTP {(int)download.StatusCode} ({download.StatusCode}); response headers were limited to " +
            $"{download.MaximumResponseHeadersKilobytes} KiB."));
        checks.Add(download.RedirectCheck);
        checks.Add(new ValidationCheck(
            "Response size",
            true,
            $"Read {download.Content.Length} bytes within the {download.MaximumResponseBytes}-byte limit."));

        DocumentValidationResult documentResult = documentValidator.Validate(download.Content);
        checks.AddRange(documentResult.Checks);
        return new ValidationReport(input, checks, documentResult.Certificates);
    }

    public void Dispose()
    {
        downloader.Dispose();
    }

    public async Task<ValidationReport> ValidateFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        List<ValidationCheck> checks = [];
        byte[] content;
        try
        {
            await using FileStream stream = new(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] buffer = new byte[FederationMetadataDownloader.MaximumResponseBytes + 1];
            int count = 0;
            while (count < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(count), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                count += read;
            }

            if (count > FederationMetadataDownloader.MaximumResponseBytes)
            {
                checks.Add(new ValidationCheck(
                    "File size",
                    false,
                    $"The file exceeds {FederationMetadataDownloader.MaximumResponseBytes} bytes."));
                return new ValidationReport(path, checks, []);
            }

            content = buffer[..count];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            checks.Add(new ValidationCheck("File access", false, ex.Message));
            return new ValidationReport(path, checks, []);
        }

        checks.Add(new ValidationCheck("File access", true, "Read the local XML file; no HTTP request was made."));
        checks.Add(new ValidationCheck(
            "File size",
            true,
            $"Read {content.Length} bytes within the {FederationMetadataDownloader.MaximumResponseBytes}-byte limit."));
        DocumentValidationResult documentResult = documentValidator.Validate(content);
        checks.AddRange(documentResult.Checks);
        return new ValidationReport(path, checks, documentResult.Certificates);
    }
}
