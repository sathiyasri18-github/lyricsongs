using System.Text.Json.Serialization;
using Lyricsongs.Api.Hubs;
using Lyricsongs.Api.Middleware;
using Lyricsongs.Api.Services;
using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Rendering;
using Lyricsongs.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure();
builder.Services.AddSingleton<IRenderProgressPublisher, SignalRRenderProgressPublisher>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddSignalR();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 512L * 1024 * 1024;
});

var app = builder.Build();

// Resolve FFmpeg paths + GPU encoder at startup (probes NVENC/AMF/QSV/MF).
_ = app.Services.GetRequiredService<IOptions<FfmpegOptions>>().Value;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("web");
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RenderProgressHub>("/hubs/render");

app.Run();

public partial class Program;
