using Neustart.Api.DTOs;

namespace Neustart.Tests;


public class RegististierungsTests
{
    [Fact]
    public void RegistrationDataIsBeingAcceptedPromptly()
    {
        var neueRegistrierung = new RegisterDTOs()
        {
            Email = "testmail@testmail.com",
            Passwort = "passwort",
            Geburstdatum = new DateOnly(2007, 10, 10)
        };
    }
}