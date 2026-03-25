using Microsoft.AspNetCore.Identity;

namespace Neustart.Api.Models;

public class User : IdentityUser
{
    public DateOnly Geburtsdatum { get; set; }
}
