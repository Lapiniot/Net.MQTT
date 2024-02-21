using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server.Hosting;

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
        switch (path)
        {
            case not null when keyPath is not null:
                return X509Certificate2.CreateFromPemFile(path, keyPath);
            default:
                ArgumentNullException.ThrowIfNull(path);
                return new(path, password);
        }
    }
}