using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Mqtt.Server.Identity.Data;

public sealed class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}