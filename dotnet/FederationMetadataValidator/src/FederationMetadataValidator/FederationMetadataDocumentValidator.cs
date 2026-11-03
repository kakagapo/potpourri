using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace FederationMetadataValidator;

public sealed class FederationMetadataDocumentValidator
{
    private const string SigningCertificateXPath =
        "//*[local-name()='RoleDescriptor' and " +
        "'fed:SecurityTokenServiceType'=@*[local-name()='type']]" +
        "/*[local-name()='KeyDescriptor' and @use='signing']" +
        "/*[local-name()='KeyInfo']" +
        "/*[local-name()='X509Data']" +
        "/*[local-name()='X509Certificate']";

    private static readonly HashSet<string> AllowedReferenceTransforms = new(StringComparer.Ordinal)
    {
        SignedXml.XmlDsigEnvelopedSignatureTransformUrl,
        SignedXml.XmlDsigC14NTransformUrl,
        SignedXml.XmlDsigC14NWithCommentsTransformUrl,
        SignedXml.XmlDsigExcC14NTransformUrl,
        SignedXml.XmlDsigExcC14NWithCommentsTransformUrl,
    };

    private readonly ValidationMode signatureMode;

    public FederationMetadataDocumentValidator(ValidationMode signatureMode = ValidationMode.Enforce)
    {
        if (!Enum.IsDefined(signatureMode))
        {
            throw new ArgumentOutOfRangeException(nameof(signatureMode));
        }

        this.signatureMode = signatureMode;
    }

    public DocumentValidationResult Validate(byte[] content)
    {
        List<ValidationCheck> checks = [];
        List<SigningCertificateInfo> certificates = [];

        XmlDocument document;
        try
        {
            document = LoadDocument(content);
            checks.Add(new ValidationCheck(
                "XML parsing",
                true,
                "The document is well-formed; DTDs and external entity resolution are disabled."));
        }
        catch (XmlException ex)
        {
            checks.Add(new ValidationCheck("XML parsing", false, ex.Message));
            return new DocumentValidationResult(checks, certificates);
        }

        XmlElement? root = document.DocumentElement;
        if (root is null)
        {
            checks.Add(new ValidationCheck("Metadata root", false, "The XML document has no root element."));
            return new DocumentValidationResult(checks, certificates);
        }

        checks.Add(new ValidationCheck("Metadata root", true, $"Root element: {root.LocalName}."));

        if (signatureMode == ValidationMode.Disabled)
        {
            checks.Add(new ValidationCheck(
                "XML signature validation",
                true,
                "Disabled; XML signature integrity was not evaluated.",
                signatureMode));
        }
        else
        {
            List<ValidationCheck> signatureChecks = [];
            ValidateSignature(document, root, signatureChecks);
            checks.AddRange(signatureChecks.Select(check => check with { Mode = signatureMode }));
            if (signatureMode == ValidationMode.Enforce && signatureChecks.Any(check => !check.Passed))
            {
                return new DocumentValidationResult(checks, certificates);
            }
        }

        ExtractSigningCertificates(document, certificates, out int candidateCount, out int invalidCount);
        if (certificates.Count == 0)
        {
            checks.Add(new ValidationCheck(
                "Signing certificates",
                false,
                $"Found {candidateCount} candidate entries, but none was a valid public X.509 certificate."));
            return new DocumentValidationResult(checks, certificates);
        }

        checks.Add(new ValidationCheck(
            "Signing certificates",
            true,
            $"Found {certificates.Count} valid signing certificate(s); ignored {invalidCount} invalid candidate(s)."));

        return new DocumentValidationResult(checks, certificates);
    }

    private static void ValidateSignature(XmlDocument document, XmlElement root, List<ValidationCheck> checks)
    {
        XmlNodeList signatureNodes = document.GetElementsByTagName(
            "Signature",
            SignedXml.XmlDsigNamespaceUrl);
        if (signatureNodes.Count != 1 || signatureNodes[0] is not XmlElement signatureElement)
        {
            checks.Add(new ValidationCheck(
                "XML signature count",
                false,
                $"Expected exactly one XMLDSIG Signature element; found {signatureNodes.Count}."));
            return;
        }

        checks.Add(new ValidationCheck(
            "XML signature count",
            true,
            "Exactly one XMLDSIG Signature element is present."));

        SignedXml signedXml = new(document);
        try
        {
            signedXml.LoadXml(signatureElement);
        }
        catch (CryptographicException ex)
        {
            checks.Add(new ValidationCheck("XML signature format", false, ex.Message));
            return;
        }

        if (signedXml.SignedInfo?.References.Count != 1 ||
            signedXml.SignedInfo.References[0] is not Reference reference)
        {
            checks.Add(new ValidationCheck(
                "Signature reference count",
                false,
                $"Expected exactly one signature reference; found {signedXml.SignedInfo?.References.Count ?? 0}."));
            return;
        }

        checks.Add(new ValidationCheck(
            "Signature reference count",
            true,
            "The signature contains exactly one reference."));

        if (!ReferenceCoversRoot(reference.Uri, root))
        {
            checks.Add(new ValidationCheck(
                "Signature scope",
                false,
                $"Reference URI '{reference.Uri}' does not cover the metadata root."));
            return;
        }

        checks.Add(new ValidationCheck(
            "Signature scope",
            true,
            $"Reference URI '{reference.Uri}' covers the metadata root."));

        if (!HasSafeReferenceTransforms(reference, out string transformDetail))
        {
            checks.Add(new ValidationCheck("Signature transforms", false, transformDetail));
            return;
        }

        checks.Add(new ValidationCheck("Signature transforms", true, transformDetail));

        try
        {
            if (!signedXml.CheckSignature())
            {
                checks.Add(new ValidationCheck(
                    "Signature integrity",
                    false,
                    "The cryptographic XML signature check failed."));
                return;
            }
        }
        catch (CryptographicException ex)
        {
            checks.Add(new ValidationCheck("Signature integrity", false, ex.Message));
            return;
        }

        checks.Add(new ValidationCheck(
            "Signature integrity",
            true,
            "The XML signature is cryptographically valid."));
    }

    private static XmlDocument LoadDocument(byte[] content)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        using MemoryStream stream = new(content, writable: false);
        using XmlReader reader = XmlReader.Create(stream, settings);
        XmlDocument document = new()
        {
            PreserveWhitespace = true,
            XmlResolver = null,
        };
        document.Load(reader);
        return document;
    }

    private static bool ReferenceCoversRoot(string? referenceUri, XmlElement root)
    {
        if (string.IsNullOrEmpty(referenceUri))
        {
            return true;
        }

        if (!referenceUri.StartsWith('#') || referenceUri.Length == 1)
        {
            return false;
        }

        string rootId = root.GetAttribute("ID");
        if (string.IsNullOrEmpty(rootId))
        {
            rootId = root.GetAttribute("Id");
        }

        if (string.IsNullOrEmpty(rootId))
        {
            rootId = root.GetAttribute("id");
        }

        return string.Equals(referenceUri[1..], rootId, StringComparison.Ordinal);
    }

    private static bool HasSafeReferenceTransforms(Reference reference, out string detail)
    {
        bool hasEnvelopedSignatureTransform = false;
        List<string> algorithms = [];

        foreach (Transform transform in reference.TransformChain)
        {
            string algorithm = transform.Algorithm ?? string.Empty;
            algorithms.Add(algorithm);

            if (!AllowedReferenceTransforms.Contains(algorithm))
            {
                detail = $"Transform '{algorithm}' is not allowed.";
                return false;
            }

            if (string.Equals(
                    algorithm,
                    SignedXml.XmlDsigEnvelopedSignatureTransformUrl,
                    StringComparison.Ordinal))
            {
                hasEnvelopedSignatureTransform = true;
            }
        }

        if (!hasEnvelopedSignatureTransform)
        {
            detail = "The reference transform chain does not contain the enveloped-signature transform.";
            return false;
        }

        detail = $"Allowed transform chain: {string.Join(", ", algorithms)}.";
        return true;
    }

    private static void ExtractSigningCertificates(
        XmlDocument document,
        List<SigningCertificateInfo> certificates,
        out int candidateCount,
        out int invalidCount)
    {
        XmlNodeList? certificateNodes = document.SelectNodes(SigningCertificateXPath);
        candidateCount = certificateNodes?.Count ?? 0;
        invalidCount = 0;
        HashSet<string> seenThumbprints = new(StringComparer.OrdinalIgnoreCase);

        if (certificateNodes is null)
        {
            return;
        }

        foreach (XmlNode certificateNode in certificateNodes)
        {
            try
            {
                byte[] certificateBytes = Convert.FromBase64String(certificateNode.InnerText);
                if (X509Certificate2.GetCertContentType(certificateBytes) != X509ContentType.Cert)
                {
                    invalidCount++;
                    continue;
                }

                using X509Certificate2 certificate = new(certificateBytes);
                if (certificate.HasPrivateKey || !seenThumbprints.Add(certificate.Thumbprint))
                {
                    if (certificate.HasPrivateKey)
                    {
                        invalidCount++;
                    }

                    continue;
                }

                certificates.Add(new SigningCertificateInfo(
                    certificate.Thumbprint,
                    certificate.Subject,
                    certificate.NotBefore.ToUniversalTime(),
                    certificate.NotAfter.ToUniversalTime()));
            }
            catch (FormatException)
            {
                invalidCount++;
            }
            catch (CryptographicException)
            {
                invalidCount++;
            }
        }
    }
}

public sealed record DocumentValidationResult(
    IReadOnlyList<ValidationCheck> Checks,
    IReadOnlyList<SigningCertificateInfo> Certificates);
