using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mqtt.Server.Hosting;

public static class CertificateLoader
{
    public static X509Certificate2 LoadFromStore(StoreName storeName, StoreLocation storeLocation, string subject, bool allowInvalid)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);
        var collection = store.Certificates.Find(X509FindType.FindBySubjectName, subject, !allowInvalid);
        return collection.Capacity > 0 ? collection[0] : null;
    }

    public static X509Certificate2 LoadFromFile(string path, string keyPath, string password)
    {
        return path is not null && keyPath is not null
            ? X509Certificate2.CreateFromPemFile(path, keyPath)
            : new X509Certificate2(path, password);
    }
}