# Federation Metadata Validator

Standalone command-line validator for federation-metadata retrieval and validation.

## Validation performed

- Accepts one absolute HTTPS URL or a local folder of XML files. Plain HTTP is invalid.
- Uses normal platform TLS certificate validation.
- By default, rejects 3xx responses and final-response URI changes.
- Limits response headers to 4 KiB and the response body to 512 KiB.
- Uses 10-second request and stream-read timeouts.
- Retries request timeouts and HTTP 502, 503, and 504 twice with exponential backoff.
- Parses XML with DTDs prohibited and external entity resolution disabled.
- By default, requires exactly one XMLDSIG signature with exactly one reference.
- Requires the signature reference to cover the metadata root, resolving the first nonempty
  `ID`, `Id`, or `id` attribute in that order (or using an empty reference URI).
- Requires the enveloped-signature transform and allows only standard inclusive/exclusive
  canonicalization transforms.
- Requires the XML signature integrity check to pass.
- Extracts public X.509 signing certificates from the WS-Federation
  `SecurityTokenServiceType` role descriptor.
- Ignores malformed certificate candidates, but requires at least one valid public certificate
  for the metadata to be reported as valid.

The XML signature check validates document integrity against key material embedded in the
signature. It does not independently establish a CA trust chain for the signer.

The tool reports metadata validity. Certificate authorization and application-specific trust
policies are not evaluated.

## Build and test

```powershell
dotnet test .\FederationMetadataValidator.slnx
dotnet publish .\src\FederationMetadataValidator\FederationMetadataValidator.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
  -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\publish
```

## Usage

```powershell
.\artifacts\publish\FederationMetadataValidator.exe `
  https://sts.example.com/federationmetadata/2007-06/federationmetadata.xml
```

Both options accept `enforce` (the default), `what-if`, or `disabled`, independently:

```powershell
.\artifacts\publish\FederationMetadataValidator.exe `
  https://sts.example.com/federationmetadata/2007-06/federationmetadata.xml `
  --redirect-validation what-if --signature-validation disabled
```

`enforce` rejects a failing rule. `what-if` evaluates and displays `[WHAT-IF FAIL]` without
rejecting the document or blocking signing-certificate extraction. `disabled` skips the rule
and displays `[SKIP]`. Redirects are followed using the platform's bounded automatic redirect
behavior unless redirect enforcement is selected. Final URI comparison uses normal `Uri` equality.
An unfollowed 3xx response is rejected only in enforcement mode; other HTTP errors remain fatal.
HTTPS input, TLS validation, resource limits, XML parsing, and valid signing-certificate extraction
remain required in all modes.

`VALID` means the document passed all enforced checks. In what-if or disabled mode it does not
necessarily mean the signature is valid or the URL did not redirect.

### Local XML files

```powershell
.\artifacts\publish\FederationMetadataValidator.exe --folder 'C:\metadata'
```

`--folder` validates all `*.xml` files directly in the folder (not subfolders), in filename order,
and prints each file's checks followed by a total/valid/invalid summary. It is mutually exclusive
with a URL. Local files use the same 512 KiB limit, safe XML parsing, XML signature checks, and
signing-certificate extraction as downloaded metadata. `--signature-validation` applies as usual;
HTTPS, TLS, HTTP headers, and redirects are not evaluated for local files.

The tool continues after an unreadable or invalid file and exits `1` if any file fails an enforced
check. An inaccessible folder or a folder containing no XML files also exits `1`.

Exit codes:

| Code | Meaning |
| ---: | --- |
| `0` | All enforced checks passed (what-if failures do not change this code) |
| `1` | An enforced check failed or metadata could not be retrieved |
| `2` | Incorrect command-line usage |
