using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        Assert.False(report.Checks.Single(check => check.Name == "Redirects").Passed);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce, ValidationMode.Enforce)]
    [InlineData(ValidationMode.Enforce, ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Enforce, ValidationMode.Disabled)]
    [InlineData(ValidationMode.WhatIf, ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf, ValidationMode.WhatIf)]
    [InlineData(ValidationMode.WhatIf, ValidationMode.Disabled)]
    [InlineData(ValidationMode.Disabled, ValidationMode.Enforce)]
    [InlineData(ValidationMode.Disabled, ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled, ValidationMode.Disabled)]
    public async Task ValidateAsync_AppliesRedirectAndSignatureModesIndependently(
        ValidationMode redirectMode,
        ValidationMode signatureMode)
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new("CN=Test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        string xml = $"""
            <EntityDescriptor xmlns:fed="http://docs.oasis-open.org/wsfed/federation/200706"
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <RoleDescriptor xsi:type="fed:SecurityTokenServiceType">
                <KeyDescriptor use="signing">
                  <KeyInfo><X509Data><X509Certificate>{Convert.ToBase64String(certificate.RawData)}</X509Certificate></X509Data></KeyInfo>
                </KeyDescriptor>
              </RoleDescriptor>
            </EntityDescriptor>
            """;
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://redirected.example/metadata.xml"),
                Content = new StringContent(xml, Encoding.UTF8, "application/xml"),
            });
        using FederationMetadataDownloader downloader = new(handler, redirectMode);
        using FederationMetadataValidationService service = new(downloader, signatureMode: signatureMode);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        bool expectedValid = redirectMode != ValidationMode.Enforce && signatureMode != ValidationMode.Enforce;
        Assert.Equal(expectedValid, report.IsValid);
        ValidationCheck redirect = Assert.Single(report.Checks, check => check.Name == "Redirects");
        Assert.Equal(redirectMode, redirect.Mode);
        Assert.Equal(redirectMode == ValidationMode.Disabled, redirect.Passed);
        if (expectedValid)
        {
            Assert.Equal(certificate.Thumbprint, Assert.Single(report.Certificates).Thumbprint);
        }
        else
        {
            Assert.Empty(report.Certificates);
        }
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public async Task DownloadAsync_HandlesUnfollowedRedirectAccordingToMode(ValidationMode mode)
    {
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Content = new StringContent("<metadata />"),
            });
        using FederationMetadataDownloader downloader = new(handler, mode);
        if (mode == ValidationMode.Enforce)
        {
            using FederationMetadataValidationService service = new(downloader);
            ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");
            Assert.False(report.IsValid);
            Assert.False(report.Checks.Single(check => check.Name == "Redirects").Passed);
        }
        else
        {
            DownloadResult result = await downloader.DownloadAsync(
                new Uri("https://example.test/metadata.xml"), CancellationToken.None);
            Assert.Equal(mode, result.RedirectCheck.Mode);
            Assert.Equal(mode == ValidationMode.Disabled, result.RedirectCheck.Passed);
            Assert.Equal("<metadata />", Encoding.UTF8.GetString(result.Content));
        }
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
        Assert.False(report.Checks.Single(check => check.Name == "Redirects").Passed);
    }

    [Theory]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public async Task ValidateAsync_RejectsHttpErrorsInNonEnforcingModes(ValidationMode mode)
    {
        using StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using FederationMetadataDownloader downloader = new(handler, mode);
        using FederationMetadataValidationService service = new(downloader, signatureMode: mode);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        ValidationCheck failed = Assert.Single(report.Checks, check => check.Name == "HTTP retrieval");
        Assert.False(failed.Passed);
        Assert.Equal(ValidationMode.Enforce, failed.Mode);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public async Task ValidateAsync_RejectsOversizedResponseInEveryMode(ValidationMode mode)
    {
        byte[] oversized = new byte[FederationMetadataDownloader.MaximumResponseBytes + 1];
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(oversized),
            });
        using FederationMetadataDownloader downloader = new(handler, mode);
        using FederationMetadataValidationService service = new(downloader, signatureMode: mode);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        Assert.False(report.Checks.Single(check => check.Name == "Response size").Passed);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public async Task ValidateAsync_RejectsMalformedXmlInEveryMode(ValidationMode mode)
    {
        using StubHttpMessageHandler handler = new(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<not-closed>", Encoding.UTF8, "application/xml"),
            });
        using FederationMetadataDownloader downloader = new(handler, mode);
        using FederationMetadataValidationService service = new(downloader, signatureMode: mode);

        ValidationReport report = await service.ValidateAsync("https://example.test/metadata.xml");

        Assert.False(report.IsValid);
        Assert.False(report.Checks.Single(check => check.Name == "XML parsing").Passed);
    }

    [Theory]
    [InlineData("oversized", ValidationMode.Enforce)]
    [InlineData("oversized", ValidationMode.WhatIf)]
    [InlineData("oversized", ValidationMode.Disabled)]
    [InlineData("malformed", ValidationMode.Enforce)]
    [InlineData("malformed", ValidationMode.WhatIf)]
    [InlineData("malformed", ValidationMode.Disabled)]
    [InlineData("dtd", ValidationMode.Enforce)]
    [InlineData("dtd", ValidationMode.WhatIf)]
    [InlineData("dtd", ValidationMode.Disabled)]
    public async Task ValidateFileAsync_RejectsInvalidFilesInEveryMode(string scenario, ValidationMode mode)
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
            using FederationMetadataValidationService service = new(signatureMode: mode);

            ValidationReport report = await service.ValidateFileAsync(path);

            Assert.False(report.IsValid);
            string failedName = scenario == "oversized" ? "File size" : "XML parsing";
            ValidationCheck failed = Assert.Single(report.Checks, check => check.Name == failedName);
            Assert.False(failed.Passed);
            Assert.Equal(ValidationMode.Enforce, failed.Mode);
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
