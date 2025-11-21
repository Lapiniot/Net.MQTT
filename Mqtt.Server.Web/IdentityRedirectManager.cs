using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Mqtt.Server.Identity.Data;

namespace Mqtt.Server.Web;

#pragma warning disable CA1812
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

#if !NET10_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
#if !NET10_0_OR_GREATER
        throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
#endif
    }

#if !NET10_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

#if !NET10_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

#if !NET10_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

#if !NET10_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#endif
    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);

    public void RedirectToInvalidUser(UserManager<ApplicationUser> userManager, HttpContext context)
        => RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
}