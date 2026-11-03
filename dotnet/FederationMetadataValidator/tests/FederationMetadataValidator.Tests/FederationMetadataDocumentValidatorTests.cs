using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace FederationMetadataValidator.Tests;

public sealed class FederationMetadataDocumentValidatorTests : IDisposable
{
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly X509Certificate2 signingCertificate;
    private readonly FederationMetadataDocumentValidator validator = new();

    public FederationMetadataDocumentValidatorTests()
    {
        CertificateRequest request = new(
            "CN=Federation Metadata Test",
            signingKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        signingCertificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_AcceptsValidRootSignedMetadataInEveryMode(ValidationMode mode)
    {
        byte[] metadata = CreateSignedMetadata(Convert.ToBase64String(signingCertificate.RawData));

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(metadata);

        Assert.All(result.Checks, check => Assert.True(check.Passed, check.Detail));
        SigningCertificateInfo certificate = Assert.Single(result.Certificates);
        Assert.Equal(signingCertificate.Thumbprint, certificate.Thumbprint);
        Assert.Contains(result.Checks, check => check.Mode == mode);
    }

    [Fact]
    public void Validate_AcceptsEmptyReferenceUriForRoot()
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        AddSignature(document, document.DocumentElement!, string.Empty);

        DocumentValidationResult result = validator.Validate(Serialize(document));

        Assert.All(result.Checks, check => Assert.True(check.Passed, check.Detail));
        Assert.Single(result.Certificates);
    }

    [Theory]
    [InlineData("ID")]
    [InlineData("Id")]
    [InlineData("id")]
    public void Validate_AcceptsSupportedRootIdAttributeNames(string attributeName)
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        document.DocumentElement!.RemoveAttribute("ID");
        document.DocumentElement.SetAttribute(attributeName, "_root");
        AddSignature(document, document.DocumentElement, "#_root");

        DocumentValidationResult result = validator.Validate(Serialize(document));

        Assert.All(result.Checks, check => Assert.True(check.Passed, check.Detail));
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_HandlesUnsignedMetadataAccordingToMode(ValidationMode mode)
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Serialize(document));

        AssertSignatureFailure(result, "XML signature count", mode);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_HandlesTamperedMetadataAccordingToMode(ValidationMode mode)
    {
        XmlDocument document = Load(CreateSignedMetadata(Convert.ToBase64String(signingCertificate.RawData)));
        document.DocumentElement!.SetAttribute("tampered", "true");

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Serialize(document));

        AssertSignatureFailure(result, "Signature integrity", mode);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_HandlesOutOfScopeSignatureAccordingToMode(ValidationMode mode)
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        XmlElement roleDescriptor = (XmlElement)document.GetElementsByTagName("RoleDescriptor")[0]!;
        roleDescriptor.SetAttribute("ID", "_role");
        AddSignature(document, roleDescriptor, "#_role");

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Serialize(document));

        AssertSignatureFailure(result, "Signature scope", mode);
    }

    [Fact]
    public void Validate_UsesFirstNonemptyRootIdAttribute()
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        document.DocumentElement!.SetAttribute("Id", "_other");
        AddSignature(document, document.DocumentElement, "#_other");

        DocumentValidationResult result = validator.Validate(Serialize(document));

        AssertSignatureFailure(result, "Signature scope", ValidationMode.Enforce);
    }

    [Fact]
    public void Validate_RejectsMultipleSignatures()
    {
        XmlDocument document = Load(CreateSignedMetadata(Convert.ToBase64String(signingCertificate.RawData)));
        XmlElement signature = (XmlElement)document.GetElementsByTagName(
            "Signature",
            SignedXml.XmlDsigNamespaceUrl)[0]!;
        document.DocumentElement!.AppendChild(signature.CloneNode(deep: true));

        DocumentValidationResult result = validator.Validate(Serialize(document));

        ValidationCheck check = Assert.Single(result.Checks, item => item.Name == "XML signature count");
        Assert.False(check.Passed);
    }

    [Fact]
    public void Validate_RejectsMissingEnvelopedSignatureTransform()
    {
        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        AddSignature(
            document,
            document.DocumentElement!,
            "#_root",
            includeEnvelopedSignatureTransform: false);

        DocumentValidationResult result = validator.Validate(Serialize(document));

        ValidationCheck check = Assert.Single(result.Checks, item => item.Name == "Signature transforms");
        Assert.False(check.Passed);
        Assert.Contains("does not contain", check.Detail);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_HandlesUnsafeTransformAccordingToMode(ValidationMode mode)
    {
        XmlDocument transformDocument = new();
        transformDocument.LoadXml(
            $"<XPath xmlns=\"{SignedXml.XmlDsigNamespaceUrl}\">ancestor-or-self::*</XPath>");
        XmlDsigXPathTransform unsafeTransform = new();
        unsafeTransform.LoadInnerXml(transformDocument.ChildNodes);

        XmlDocument document = CreateMetadataDocument(Convert.ToBase64String(signingCertificate.RawData));
        AddSignature(document, document.DocumentElement!, "#_root", additionalTransform: unsafeTransform);

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Serialize(document));

        AssertSignatureFailure(result, "Signature transforms", mode);
        if (mode != ValidationMode.Disabled)
        {
            Assert.Contains(
                SignedXml.XmlDsigXPathTransformUrl,
                result.Checks.Single(item => item.Name == "Signature transforms").Detail);
        }
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_RejectsDtdInEveryMode(ValidationMode mode)
    {
        string xml = """
            <!DOCTYPE EntityDescriptor [<!ENTITY example "value">]>
            <EntityDescriptor xmlns="urn:oasis:names:tc:SAML:2.0:metadata">&example;</EntityDescriptor>
            """;

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Encoding.UTF8.GetBytes(xml));

        ValidationCheck check = Assert.Single(result.Checks, item => item.Name == "XML parsing");
        Assert.False(check.Passed);
        Assert.Equal(ValidationMode.Enforce, check.Mode);
    }

    [Fact]
    public void Validate_IgnoresMalformedCertificateWhenAnotherCertificateIsValid()
    {
        string validCertificate = Convert.ToBase64String(signingCertificate.RawData);
        XmlDocument document = CreateMetadataDocument($"not-base64</ds:X509Certificate><ds:X509Certificate>{validCertificate}");
        AddSignature(document, document.DocumentElement!, "#_root");

        DocumentValidationResult result = validator.Validate(Serialize(document));

        Assert.All(result.Checks, check => Assert.True(check.Passed, check.Detail));
        Assert.Single(result.Certificates);
        Assert.Contains("ignored 1 invalid", result.Checks.Single(check => check.Name == "Signing certificates").Detail);
    }

    [Theory]
    [InlineData(ValidationMode.Enforce)]
    [InlineData(ValidationMode.WhatIf)]
    [InlineData(ValidationMode.Disabled)]
    public void Validate_RequiresValidSigningCertificateInEveryMode(ValidationMode mode)
    {
        XmlDocument document = CreateMetadataDocument("not-base64");
        AddSignature(document, document.DocumentElement!, "#_root");

        DocumentValidationResult result = new FederationMetadataDocumentValidator(mode).Validate(Serialize(document));

        ValidationCheck check = Assert.Single(result.Checks, item => item.Name == "Signing certificates");
        Assert.False(check.Passed);
        Assert.Equal(ValidationMode.Enforce, check.Mode);
    }

    [Fact]
    public void Validate_RejectsPfxSigningCertificate()
    {
        string pfx = Convert.ToBase64String(signingCertificate.Export(X509ContentType.Pfx));
        XmlDocument document = CreateMetadataDocument(pfx);
        AddSignature(document, document.DocumentElement!, "#_root");

        DocumentValidationResult result = validator.Validate(Serialize(document));

        ValidationCheck check = Assert.Single(result.Checks, item => item.Name == "Signing certificates");
        Assert.False(check.Passed);
        Assert.Empty(result.Certificates);
    }

    [Fact]
    public void Validate_IgnoresDuplicateSigningCertificates()
    {
        string certificate = Convert.ToBase64String(signingCertificate.RawData);
        XmlDocument document = CreateMetadataDocument(
            $"{certificate}</ds:X509Certificate><ds:X509Certificate>{certificate}");
        AddSignature(document, document.DocumentElement!, "#_root");

        DocumentValidationResult result = validator.Validate(Serialize(document));

        Assert.All(result.Checks, check => Assert.True(check.Passed, check.Detail));
        Assert.Single(result.Certificates);
    }

    [Theory]
    [InlineData(true, ValidationMode.Enforce)]
    [InlineData(true, ValidationMode.WhatIf)]
    [InlineData(true, ValidationMode.Disabled)]
    [InlineData(false, ValidationMode.Enforce)]
    [InlineData(false, ValidationMode.WhatIf)]
    [InlineData(false, ValidationMode.Disabled)]
    public async Task ValidateFileAsync_UsesDocumentValidationAndSignatureMode(bool signed, ValidationMode mode)
    {
        string certificateValue = Convert.ToBase64String(signingCertificate.RawData);
        byte[] metadata = signed
            ? CreateSignedMetadata(certificateValue)
            : Serialize(CreateMetadataDocument(certificateValue));
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, metadata);
            using FederationMetadataValidationService service = new(signatureMode: mode);

            ValidationReport report = await service.ValidateFileAsync(path);

            bool expectedValid = signed || mode != ValidationMode.Enforce;
            Assert.Equal(expectedValid, report.IsValid);
            Assert.Equal(path, report.Input);
            Assert.Contains(report.Checks, check => check.Name == "File access" && check.Passed);
            Assert.DoesNotContain(report.Checks, check => check.Name is "URL" or "HTTPS" or "HTTP retrieval" or "Redirects");
            if (expectedValid)
            {
                Assert.Equal(signingCertificate.Thumbprint, Assert.Single(report.Certificates).Thumbprint);
            }
            else
            {
                Assert.Empty(report.Certificates);
                Assert.False(report.Checks.Single(check => check.Name == "XML signature count").Passed);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    public void Dispose()
    {
        signingCertificate.Dispose();
        signingKey.Dispose();
    }

    private static void AssertSignatureFailure(
        DocumentValidationResult result,
        string checkName,
        ValidationMode mode)
    {
        if (mode == ValidationMode.Disabled)
        {
            ValidationCheck skipped = Assert.Single(result.Checks, check => check.Mode == ValidationMode.Disabled);
            Assert.Equal("XML signature validation", skipped.Name);
            Assert.DoesNotContain(result.Checks, check => check.Name == checkName);
        }
        else
        {
            ValidationCheck failed = Assert.Single(result.Checks, check => check.Name == checkName);
            Assert.False(failed.Passed);
            Assert.Equal(mode, failed.Mode);
        }

        if (mode == ValidationMode.Enforce)
        {
            Assert.Empty(result.Certificates);
        }
        else
        {
            Assert.Single(result.Certificates);
            Assert.True(new ValidationReport("https://example.test", result.Checks, result.Certificates).IsValid);
        }
    }

    private byte[] CreateSignedMetadata(string certificateValue)
    {
        XmlDocument document = CreateMetadataDocument(certificateValue);
        AddSignature(document, document.DocumentElement!, "#_root");
        return Serialize(document);
    }

    private XmlDocument CreateMetadataDocument(string certificateValue)
    {
        string xml = $"""
            <EntityDescriptor
                xmlns="urn:oasis:names:tc:SAML:2.0:metadata"
                xmlns:fed="http://docs.oasis-open.org/wsfed/federation/200706"
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                xmlns:ds="http://www.w3.org/2000/09/xmldsig#"
                ID="_root">
              <RoleDescriptor xsi:type="fed:SecurityTokenServiceType">
                <KeyDescriptor use="signing">
                  <ds:KeyInfo>
                    <ds:X509Data>
                      <ds:X509Certificate>{certificateValue}</ds:X509Certificate>
                    </ds:X509Data>
                  </ds:KeyInfo>
                </KeyDescriptor>
              </RoleDescriptor>
            </EntityDescriptor>
            """;

        return Load(Encoding.UTF8.GetBytes(xml));
    }

    private void AddSignature(
        XmlDocument document,
        XmlElement target,
        string referenceUri,
        bool includeEnvelopedSignatureTransform = true,
        Transform? additionalTransform = null)
    {
        SignedXml signedXml = new(document)
        {
            SigningKey = signingKey,
        };
        Reference reference = new(referenceUri);
        if (includeEnvelopedSignatureTransform)
        {
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        }

        if (additionalTransform is not null)
        {
            reference.AddTransform(additionalTransform);
        }

        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        KeyInfo keyInfo = new();
        keyInfo.AddClause(new KeyInfoX509Data(signingCertificate));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        target.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));
    }

    private static XmlDocument Load(byte[] content)
    {
        XmlDocument document = new()
        {
            PreserveWhitespace = true,
        };
        document.LoadXml(Encoding.UTF8.GetString(content));
        return document;
    }

    private static byte[] Serialize(XmlDocument document)
    {
        using MemoryStream stream = new();
        document.Save(stream);
        return stream.ToArray();
    }
}
