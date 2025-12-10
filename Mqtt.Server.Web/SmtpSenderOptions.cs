using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Mqtt.Server.Web;

public sealed class SmtpSenderOptions
{
    [Required]
    public required string Host { get; set; }

    [Range(1, ushort.MaxValue)]
    public int Port { get; set; } = 25;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    [Required, ValidateObjectMembers]
    public Mailbox From { get; set; } = new(name: "NMQTT Server", address: "noreply@nmqttserver.com");
}

public sealed class Mailbox(string name, string address)
{
    [Required]
    public string Name { get; set; } = name;

    [Required, EmailAddress]
    public string Address { get; set; } = address;
}

[OptionsValidator]
public sealed partial class SmtpSenderOptionsValidator : IValidateOptions<SmtpSenderOptions> { }