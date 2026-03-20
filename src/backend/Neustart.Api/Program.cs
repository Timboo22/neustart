using Neustart.Api.DTOs;
using Neustart.Api.Services;
using Neustart.Api.Services.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IRegisterService, RegisterService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/v1/sign-in", (RegisterDTOs register, RegisterService _service) =>
{
    //Logic for Register
});

app.UseHttpsRedirection();

app.Run();
