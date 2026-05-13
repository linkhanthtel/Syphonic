using Microsoft.AspNetCore.Identity;

namespace Syphonic.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
