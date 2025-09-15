using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Mqtt.Server.Identity.Data;

public sealed class ApplicationDbContext(DbContextOptions options) : IdentityDbContext<ApplicationUser>(options)
{
}