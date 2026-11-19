# Federation Metadata Validator

Standalone command-line validator for federation-metadata retrieval and validation.

## Validation performed

- Accepts one absolute HTTPS URL or a local folder of XML files. Plain HTTP is invalid.
- Uses normal platform TLS certificate validation.
- Rejects 3xx responses and final-response URI changes.
- Limits response headers to 4 KiB and the response body to 512 KiB.
- Uses 10-second request and stream-read timeouts.
- Retries request timeouts and HTTP 502, 503, and 504 twice with exponential backoff.
- Parses XML with DTDs prohibited and external entity resolution disabled.
- Requires exactly one XMLDSIG signature with exactly one reference.
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

All validation checks are mandatory. Redirects are not followed, and final URI comparison uses
normal `Uri` equality. Signature failures stop validation before signing-certificate extraction.
`VALID` means the document passed all checks.

### Local XML files

```powershell
.\artifacts\publish\FederationMetadataValidator.exe --folder 'C:\metadata'
```

`--folder` validates all `*.xml` files directly in the folder (not subfolders), in filename order,
and prints each file's checks followed by a total/valid/invalid summary. It is mutually exclusive
with a URL. Local files use the same 512 KiB limit, safe XML parsing, XML signature checks, and
signing-certificate extraction as downloaded metadata;
HTTPS, TLS, HTTP headers, and redirects are not evaluated for local files.

The tool continues after an unreadable or invalid file and exits `1` if any file fails a
check. An inaccessible folder or a folder containing no XML files also exits `1`.

Exit codes:

| Code | Meaning |
| ---: | --- |
| `0` | All checks passed |
| `1` | A check failed or metadata could not be retrieved |
| `2` | Incorrect command-line usage |
