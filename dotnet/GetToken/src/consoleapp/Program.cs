using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
{
    Console.WriteLine("Enter the tenant ID :");
    string tenantId = Console.ReadLine();
    Console.WriteLine("Enter the client ID :");
    string clientId = Console.ReadLine();
    Console.WriteLine("Enter the client cert thumbprint:");
    string certThumbprint = Console.ReadLine();

    store.Open(OpenFlags.ReadOnly);

    var certColection = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false);
    if (certColection == null || certColection.Count <= 0)
    {
        throw new Exception($"Could not find cert with thumbprint {args[0]}");
    }

    X509Certificate2 signingCert = certColection.First();

    // audience
    string aud = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

    // aud and iss are the only required claims.
    var claims = 
        new Dictionary<string, object>()
        {
            { "aud", aud },
            { "iss", clientId },
            { "jti", Guid.NewGuid().ToString() }, // JWT ID, a unique identifier
            { "sub", clientId }
        };

    var now = DateTime.UtcNow;
    var securityTokenDescriptor = new SecurityTokenDescriptor
    {
        Claims = claims,
        NotBefore = now,
        Expires = now.AddMinutes(5),
        SigningCredentials = new X509SigningCredentials(signingCert)
    };

    var handler = new JsonWebTokenHandler();
    var token = handler.CreateToken(securityTokenDescriptor);
    Console.WriteLine("Token : " + token);
}