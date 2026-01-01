using System.Text.Json.Serialization;

namespace DraftSpec.Cli.Configuration;

/// <summary>
/// Configuration for plugin loading and security.
/// </summary>
/// <remarks>
/// <para>
/// <b>Security Model</b>
/// </para>
/// <para>
/// DraftSpec supports two methods for verifying plugin authenticity:
/// </para>
/// <list type="number">
/// <item>
/// <term>Public Key Tokens</term>
/// <description>
/// 8-byte (64-bit) hash of the strong name public key. Fast and simple,
/// but provides weaker security guarantees. No revocation support.
/// </description>
/// </item>
/// <item>
/// <term>Certificate Thumbprints (Recommended)</term>
/// <description>
/// SHA256 hash of the Authenticode signing certificate. Provides stronger
/// verification and can be revoked by removing from the trusted list.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Limitations</b>
/// </para>
/// <list type="bullet">
/// <item>No automatic certificate revocation checking (CRL/OCSP)</item>
/// <item>No certificate chain validation against system trust stores</item>
/// <item>No timestamp verification for expired certificates</item>
/// </list>
/// <para>
/// If a signing key is compromised, you must manually remove it from the
/// trusted list and redeploy configuration to all affected systems.
/// </para>
/// </remarks>
public class PluginsConfig
{
    /// <summary>
    /// When true, only load plugins that are signed with a trusted key or certificate.
    /// Default: false (allow all plugins).
    /// </summary>
    /// <remarks>
    /// When enabled, plugins must have either:
    /// <list type="bullet">
    /// <item>A strong name with a public key token in <see cref="TrustedPublicKeyTokens"/>, OR</item>
    /// <item>An Authenticode signature with a certificate in <see cref="TrustedCertificateThumbprints"/></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("requireSignedPlugins")]
    public bool RequireSignedPlugins { get; set; } = false;

    /// <summary>
    /// List of trusted public key tokens (hex strings, e.g., "b77a5c561934e089").
    /// Plugins must be signed with one of these keys when RequireSignedPlugins is true.
    /// </summary>
    /// <remarks>
    /// Public key tokens are 8 bytes (16 hex characters). They are derived from
    /// the strong name public key and provide basic verification of assembly origin.
    /// Consider using <see cref="TrustedCertificateThumbprints"/> for stronger security.
    /// </remarks>
    [JsonPropertyName("trustedPublicKeyTokens")]
    public List<string>? TrustedPublicKeyTokens { get; set; }

    /// <summary>
    /// List of trusted certificate thumbprints (SHA256 hex strings).
    /// Plugins signed with any of these certificates are trusted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Certificate thumbprints are SHA256 hashes (64 hex characters) of the
    /// Authenticode signing certificate. This provides stronger verification
    /// than public key tokens.
    /// </para>
    /// <para>
    /// To get a certificate thumbprint:
    /// <code>
    /// # Windows PowerShell
    /// (Get-AuthenticodeSignature .\Plugin.dll).SignerCertificate.Thumbprint
    ///
    /// # .NET CLI
    /// dotnet tool run sigcheck -a Plugin.dll
    /// </code>
    /// </para>
    /// </remarks>
    [JsonPropertyName("trustedCertificateThumbprints")]
    public List<string>? TrustedCertificateThumbprints { get; set; }
}
