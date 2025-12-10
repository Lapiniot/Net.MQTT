using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

#pragma warning disable CA1812

namespace Mqtt.Server.Web;

internal sealed class SmtpEmailSender(IOptions<SmtpSenderOptions> senderOptions) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var options = senderOptions.Value;

        using var body = new TextPart("html") { Text = htmlMessage };
        using var message = new MimeMessage(
            from: [new MailboxAddress(options.From.Name, options.From.Address)],
            to: [new MailboxAddress("", email)],
            subject, body);

        using var cts = new CancellationTokenSource(options.Timeout);
        var token = cts.Token;

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, cancellationToken: token).ConfigureAwait(false);

        if (options is { UserName: { Length: > 0 } userName, Password: { Length: > 0 } password })
        {
            await client.AuthenticateAsync(userName, password, token).ConfigureAwait(false);
        }

        await client.SendAsync(message, token).ConfigureAwait(false);
        await client.DisconnectAsync(true, token).ConfigureAwait(false);
    }
}