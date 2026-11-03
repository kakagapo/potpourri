namespace FederationMetadataValidator;

public enum ValidationMode
{
    Enforce,
    WhatIf,
    Disabled,
}

public sealed record ValidationCheck(
    string Name,
    bool Passed,
    string Detail,
    ValidationMode Mode = ValidationMode.Enforce);

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
    public bool IsValid => Checks.Count > 0 &&
        Checks.All(check => check.Passed || check.Mode != ValidationMode.Enforce);
}

internal sealed class MetadataValidationException : Exception
{
    public MetadataValidationException(string checkName, string message)
        : base(message)
    {
        CheckName = checkName;
    }

    public MetadataValidationException(string checkName, string message, Exception innerException)
        : base(message, innerException)
    {
        CheckName = checkName;
    }

    public string CheckName { get; }
}
