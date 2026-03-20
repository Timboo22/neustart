namespace Neustart.Api.DTOs;

public record RegisterDTOs
{
    public required string Email { get; set; }
    public required string Passwort { get; set; } 
    public required DateOnly Geburstdatum { get; set; } 
};