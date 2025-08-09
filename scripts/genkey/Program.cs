using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// This can be used to generate temporary self-signed certificates for the identity server.

var name = "anaidentitykey";

// Generate a new key pair
using var rsa = RSA.Create(keySizeInBits: 2048);

// Create a certificate request
var request = new CertificateRequest(
    subjectName: $"CN={name}",
    rsa,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1
);

// Self-sign the certificate
var certificate = request.CreateSelfSigned(
    DateTimeOffset.Now,
    DateTimeOffset.Now.AddYears(10)
);

// Export the certificate to a PFX file
var pfxBytes = certificate.Export(
    // TODO: pick a format
    X509ContentType.Pfx,
    // TODO: change the password
    password: (string)null
);
File.WriteAllBytes($"{name}.pfx", pfxBytes);
Console.Write(certificate);
Console.WriteLine("Self-signed certificate created successfully.");
Console.WriteLine($"Certificate saved to {name}.pfx");