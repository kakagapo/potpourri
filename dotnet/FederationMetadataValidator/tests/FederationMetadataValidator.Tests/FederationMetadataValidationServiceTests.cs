using System.Net;
using System.Text;

namespace FederationMetadataValidator.Tests;

public sealed class FederationMetadataValidationServiceTests
{
    [Theory]
    [InlineData("not-a-url", "URL")]
    [InlineData("http://example.test/metadata.xml", "HTTPS")]
    public async Task ValidateAsync_RejectsInvalidInputBeforeNetworkCall(
        string input,
        string expectedFailedCheck)
    {
        using FederationMetadataValidationService service = new();

        ValidationReport report = await service.ValidateAsync(input);

        Assert.False(report.IsValid);
        Assert.False(report.Checks.Single(check => check.Name == expectedFailedCheck).Passed);
    }

    [Fact]
    public async Task ValidateAsync_RejectsRedirect()
    {
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://redirected.example/metadata.xml") },
            });
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        ValidationCheck failed = Assert.Single(report.Checks, check => !check.Passed);
        Assert.Equal("Redirect detected: HTTP 302; requested 'https://example.test/metadata.xml', " +
            "final 'https://example.test/metadata.xml'.", failed.Detail);
    }

    [Theory]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Redirect)]
    [InlineData(HttpStatusCode.NotModified)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task DownloadAsync_ThrowsExceptionForRedirect(HttpStatusCode statusCode)
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(statusCode));
        using FederationMetadataDownloader downloader = new(handler);
        Uri uri = new("https://example.test/metadata.xml");

        Exception exception = await Assert.ThrowsAsync<Exception>(
            () => downloader.DownloadAsync(uri, CancellationToken.None));

        Assert.Equal(
            $"Redirect detected: HTTP {(int)statusCode}; requested '{uri}', final '{uri}'.",
            exception.Message);
    }

    [Fact]
    public async Task ValidateAsync_RejectsCaseChangedFinalUriPath()
    {
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.test/Metadata.xml"),
            });
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        ValidationCheck failed = Assert.Single(report.Checks, check => !check.Passed);
        Assert.Contains("Redirect detected", failed.Detail);
        Assert.Contains("https://example.test/Metadata.xml", failed.Detail);
    }

    [Fact]
    public async Task ValidateAsync_RejectsHttpErrors()
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        ValidationCheck failed = Assert.Single(report.Checks, check => check.Name == "HTTP retrieval");
        Assert.False(failed.Passed);
        Assert.Equal("The endpoint returned HTTP 404 (NotFound).", failed.Detail);
    }

    [Fact]
    public async Task DownloadAsync_PreservesRequestFailureMessageAndInnerException()
    {
        HttpRequestException failure = new("The connection failed.");
        using StubHttpMessageHandler handler = new(_ => throw failure);
        using FederationMetadataDownloader downloader = new(handler);

        Exception exception = await Assert.ThrowsAsync<Exception>(
            () => downloader.DownloadAsync(new Uri("https://example.test/metadata.xml"), CancellationToken.None));

        Assert.Equal(failure.Message, exception.Message);
        Assert.Same(failure, exception.InnerException);
    }

    [Fact]
    public async Task ValidateAsync_PropagatesCallerCancellation()
    {
        using CancellationTokenSource cancellation = new();
        using StubHttpMessageHandler handler = new(_ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ValidateAsync("https://example.test/metadata.xml", cancellation.Token));
    }

    [Fact]
    public async Task ValidateAsync_RejectsOversizedResponse()
    {
        byte[] oversized = new byte[FederationMetadataDownloader.MaximumResponseBytes + 1];
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(oversized),
            });
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        ValidationCheck failed = Assert.Single(report.Checks, check => !check.Passed);
        Assert.Equal(
            $"The declared response size exceeds {FederationMetadataDownloader.MaximumResponseBytes} bytes.",
            failed.Detail);
    }

    [Fact]
    public async Task ValidateAsync_RejectsMalformedXml()
    {
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<not-closed>", Encoding.UTF8, "application/xml"),
            });
        using FederationMetadataDownloader downloader = new(handler);
        using FederationMetadataValidationService service = new(downloader);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        Assert.False(report.Checks.Single(check => check.Name == "XML parsing").Passed);
    }

    [Theory]
    [InlineData("oversized")]
    [InlineData("malformed")]
    [InlineData("dtd")]
    public async Task ValidateFileAsync_RejectsInvalidFiles(string scenario)
    {
        byte[] content = scenario switch
        {
            "oversized" => new byte[FederationMetadataDownloader.MaximumResponseBytes + 1],
            "malformed" => Encoding.UTF8.GetBytes("<not-closed>"),
            _ => Encoding.UTF8.GetBytes("<!DOCTYPE metadata [<!ENTITY value 'test'>]><metadata>&value;</metadata>"),
        };
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, content);
            using FederationMetadataValidationService service = new();

            ValidationReport report = await service.ValidateFileAsync(path);

            Assert.False(report.IsValid);
            string failedName = scenario == "oversized" ? "File size" : "XML parsing";
            ValidationCheck failed = Assert.Single(report.Checks, check => check.Name == failedName);
            Assert.False(failed.Passed);
            Assert.Empty(report.Certificates);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ValidateFileAsync_ReportsMissingFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
        using FederationMetadataValidationService service = new();

        ValidationReport report = await service.ValidateFileAsync(path);

        Assert.False(report.IsValid);
        Assert.Equal("File access", Assert.Single(report.Checks).Name);
        Assert.Empty(report.Certificates);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = responseFactory(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
    }
}
