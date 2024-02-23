using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Net.Mqtt.Server.Hosting;

/// <summary>
/// Represents abstract policy to be used for remote authentication certificates validation.
/// </summary>
public interface IRemoteCertificateValidationPolicy
{
    /// <summary>
    /// Verifies the remote SSL certificate.
    /// </summary>
    /// <param name="certificate">The certificate provided by the remote party.</param>
    /// <param name="chain">The chain of certificate authorities associated with certificate.</param>
    /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
    /// <returns>
    /// <see langword="true" /> if this certificate is valid and should be used 
    /// for authentication, otherwise <see langword="false"/>
    /// </returns>
    bool Verify(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

    /// <summary>
    /// Gets a value that specifies whether the client is asked for a certificate for authentication.
    /// </summary>
    bool Required { get; }
}