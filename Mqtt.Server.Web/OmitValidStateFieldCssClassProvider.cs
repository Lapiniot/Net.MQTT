using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Mqtt.Server.Web;

public class OmitValidStateFieldCssClassProvider : FieldCssClassProvider
{
    public static readonly OmitValidStateFieldCssClassProvider Instance = new();

    public override string GetFieldCssClass([NotNull] EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var invalid = !editContext.IsValid(fieldIdentifier);
        var modified = editContext.IsModified(fieldIdentifier);
        return modified
            ? invalid
                ? "modified invalid"
                : "modified"
            : invalid
                ? "invalid"
                : "";
    }
}