# PowerShell scripts

`PowerShellScripts.sln` contains the scripts in the `Scripts` directory. Building
the solution copies the scripts to `bin\<Configuration>\net8.0` and signs the
copies with a SHA-256 Authenticode signature.

## Scripts

- `CertificateCreate.ps1` creates a self-signed code-signing certificate.
- `Update-DomainFederationConfiguration.ps1` updates the Microsoft Graph
  federation configuration for a verified Microsoft Entra domain.
- `learning.ps1` contains general PowerShell examples.

Run a script with `-Help` to display its parameters and examples.

## Build and signing

Create the default code-signing certificate:

```powershell
.\Scripts\CertificateCreate.ps1 -CertificateName MySelfSignedCert
```

Build and sign the scripts:

```powershell
dotnet build .\PowerShellScripts.sln
```

By default, the project selects the newest valid code-signing certificate with
the subject `CN=MySelfSignedCert` from `Cert:\CurrentUser\My`. To select a
specific certificate, pass its thumbprint:

```powershell
dotnet build .\PowerShellScripts.sln `
    -p:PowerShellSigningCertificateThumbprint=<thumbprint>
```

The signing command uses `pwsh.exe`. Override `PowerShellExecutable` when a
different PowerShell 7 executable is required.
