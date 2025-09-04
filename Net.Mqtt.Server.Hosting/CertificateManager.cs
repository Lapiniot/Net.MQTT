using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace Net.Mqtt.Server.Hosting;

public static class CertificateManager
{
    private const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";

    public static X509Certificate2 LoadFromPkcs12File(string path, string? password)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12FromFile(path, password);
#else
        return new X509Certificate2(path, password);
#endif
    }

    public static X509Certificate2 LoadFromPemFile(string certPemFilePath, string? keyPemFilePath = null, string? password = null)
    {
        return password is { }
            ? X509Certificate2.CreateFromEncryptedPemFile(certPemFilePath, password, keyPemFilePath)
            : X509Certificate2.CreateFromPemFile(certPemFilePath, keyPemFilePath);
    }

    public static X509Certificate2? LoadFromStore(StoreName storeName, StoreLocation storeLocation, string subject, bool allowInvalid)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        try
        {
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, subject, !allowInvalid);
            var matchingCert = certificates is [{ } first, ..] ? first : null;
            DisposeCertificates(certificates, except: matchingCert);
            return matchingCert;
        }
        finally
        {
            store.Close();
        }
    }

    public static X509Certificate2? LoadDevCertFromStore()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        try
        {
            var certificates = store.Certificates.Find(X509FindType.FindByExtension, findValue: AspNetHttpsOid, validOnly: true);
            var matchingCert = certificates.OrderByDescending(GetVersion).FirstOrDefault();
            DisposeCertificates(certificates, except: matchingCert);
            return matchingCert;
        }
        finally
        {
            store.Close();
        }

        static int GetVersion(X509Certificate2 certificate)
        {
            var data = certificate.Extensions.Single(x => x.Oid?.Value == AspNetHttpsOid).RawData;
            return data is { Length: 42 } and [(byte)'A', ..] or [] ? 0 : data[0];
        }
    }

    public static X509Certificate2? LoadDevCertFromAppDataPkcs12File(string applicationName, string password)
    {
        if (GetDevCertPath(applicationName) is { } path && File.Exists(path))
        {
            try
            {
                var cert = LoadFromPkcs12File(path, password);
                if (IsDevelopmentCertificate(cert))
                {
                    return cert;
                }
                else
                {
                    cert.Dispose();
                }
            }
            catch (CryptographicException)
            {
            }
        }

        return null;

        static string? GetDevCertPath(string applicationName)
        {
            // See https://github.com/aspnet/Hosting/issues/1294
            var basePath = Environment.GetEnvironmentVariable("APPDATA") is { } appData
                ? Path.Combine(appData, "ASP.NET", "https")
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) is { } home
                    ? Path.Combine(home, ".aspnet", "https")
                    : null;
            return basePath is not null ? Path.Combine(basePath, $"{applicationName}.pfx") : null;
        }
    }

    public static bool IsDevelopmentCertificate(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (string.Equals(certificate.Subject, "CN=localhost", StringComparison.Ordinal))
        {
            foreach (var extension in certificate.Extensions.OfType<X509Extension>())
            {
                if (string.Equals(AspNetHttpsOid, extension.Oid?.Value, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void DisposeCertificates(X509Certificate2Collection certificates, X509Certificate2? except)
    {
        foreach (var cert in certificates)
        {
            if (cert != except)
            {
                cert.Dispose();
            }
        }
    }
}