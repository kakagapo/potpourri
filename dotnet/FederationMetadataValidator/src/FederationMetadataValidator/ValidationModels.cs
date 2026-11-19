namespace FederationMetadataValidator;

public sealed record ValidationCheck(
    string Name,
    bool Passed,
    string Detail);

public sealed record SigningCertificateInfo(
    string Thumbprint,
    string Subject,
    DateTime NotBeforeUtc,
    DateTime NotAfterUtc);

public sealed record ValidationReport(
    string Input,
    IReadOnlyList<ValidationCheck> Checks,
    IReadOnlyList<SigningCertificateInfo> Certificates)
{
    public bool IsValid => Checks.Count > 0 && Checks.All(check => check.Passed);
}
