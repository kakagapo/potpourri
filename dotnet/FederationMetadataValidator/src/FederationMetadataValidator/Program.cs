using FederationMetadataValidator;

ValidationMode redirectMode = ValidationMode.Enforce;
ValidationMode signatureMode = ValidationMode.Enforce;
string? input = null;
string? folder = null;
HashSet<string> suppliedOptions = new(StringComparer.Ordinal);
for (int index = 0; index < args.Length; index++)
{
    string argument = args[index];
    if (argument == "--folder")
    {
        if (!suppliedOptions.Add(argument) || ++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
        {
            return UsageError("Option '--folder' must be supplied at most once with a folder path.");
        }

        folder = args[index];
    }
    else if (argument is "--redirect-validation" or "--signature-validation")
    {
        if (!suppliedOptions.Add(argument) || ++index >= args.Length)
        {
            return UsageError($"Option '{argument}' must be supplied at most once with a mode.");
        }

        ValidationMode? mode = args[index] switch
        {
            "enforce" => ValidationMode.Enforce,
            "what-if" => ValidationMode.WhatIf,
            "disabled" => ValidationMode.Disabled,
            _ => null,
        };
        if (mode is null)
        {
            return UsageError($"Unknown validation mode '{args[index]}'.");
        }

        if (argument == "--redirect-validation")
        {
            redirectMode = mode.Value;
        }
        else
        {
            signatureMode = mode.Value;
        }
    }
    else if (argument.StartsWith("--", StringComparison.Ordinal) || input is not null)
    {
        return UsageError($"Unexpected argument '{argument}'.");
    }
    else
    {
        input = argument;
    }
}

if ((input is null) == (folder is null))
{
    return UsageError("Supply either one HTTPS URL or --folder <path>, but not both.");
}

using FederationMetadataValidationService service = new(redirectMode: redirectMode, signatureMode: signatureMode);
if (folder is not null)
{
    string[] files;
    try
    {
        files = Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
    {
        Console.Error.WriteLine($"Cannot inspect folder '{folder}': {ex.Message}");
        return 1;
    }

    if (files.Length == 0)
    {
        Console.Error.WriteLine($"No XML files found in folder '{folder}'.");
        return 1;
    }

    int valid = 0;
    foreach (string file in files)
    {
        ValidationReport fileReport = await service.ValidateFileAsync(file);
        WriteReport(fileReport, localFile: true);
        if (fileReport.IsValid)
        {
            valid++;
        }
    }

    Console.WriteLine($"Folder validation summary: {files.Length} total, {valid} valid, {files.Length - valid} invalid.");
    return valid == files.Length ? 0 : 1;
}

if (input is null)
{
    return UsageError("One HTTPS URL is required.");
}

ValidationReport report = await service.ValidateAsync(input);
WriteReport(report, localFile: false);
return report.IsValid ? 0 : 1;

void WriteReport(ValidationReport report, bool localFile)
{
    Console.WriteLine($"Federation metadata validation: {(report.IsValid ? "VALID" : "INVALID")}");
    Console.WriteLine($"{(localFile ? "File" : "URL")}: {report.Input}");
    Console.WriteLine(
        $"Validation modes: redirects={(localFile ? "N/A (local file)" : redirectMode.ToString())}, signature={signatureMode}.");
    Console.WriteLine();

    foreach (ValidationCheck check in report.Checks)
    {
        string status = check.Mode switch
        {
            ValidationMode.Disabled => "SKIP",
            ValidationMode.WhatIf => check.Passed ? "WHAT-IF PASS" : "WHAT-IF FAIL",
            _ => check.Passed ? "PASS" : "FAIL",
        };
        Console.WriteLine($"[{status}] {check.Name}: {check.Detail}");
    }

    if (report.Certificates.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Signing certificates:");

        foreach (SigningCertificateInfo certificate in report.Certificates)
        {
            Console.WriteLine(
                $"- {certificate.Thumbprint} | {certificate.Subject} | " +
                $"{certificate.NotBeforeUtc:O} - {certificate.NotAfterUtc:O}");
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "Successful signature validation proves document integrity using embedded signing key material; " +
        "it does not independently establish CA trust.");
    Console.WriteLine(
        "Certificate authorization and application-specific trust policies are not evaluated.");
    Console.WriteLine();
}

static int UsageError(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine(
        "Usage: FederationMetadataValidator (<https-url> | --folder <path>) " +
        "[--redirect-validation enforce|what-if|disabled] " +
        "[--signature-validation enforce|what-if|disabled]");
    return 2;
}
