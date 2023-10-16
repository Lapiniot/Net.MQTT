using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Mqtt.Server.Identity.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mqtt.Server.Web;

internal static partial class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Pages/Account directory of this project.
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapPost(String, Delegate)")]
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

        manageGroup.MapPost("/DownloadPersonalData", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
        {
            var user = await userManager.GetUserAsync(context.User).ConfigureAwait(false);
            if (user is null)
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");

            var userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);
            LogUserAskedPersonalData(downloadLogger, userId);

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user).ConfigureAwait(false);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            personalData.Add($"Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false))!);
            var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

            context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
            return Results.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
        });

        return accountGroup;
    }

    [LoggerMessage(LogLevel.Information, "User with ID '{userId}' asked for their personal data.")]
    private static partial void LogUserAskedPersonalData(ILogger logger, string userId);
}