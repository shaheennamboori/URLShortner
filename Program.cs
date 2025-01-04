using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using URLShortner.Models;
using URLShortner.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddScoped<UrlShorteningService>();

// Configure DbContext with PostgreSQL connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("url-shortner")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("shorten", async (string url, UrlShorteningService urlService, ApplicationDbContext dbContext,
    HttpContext httpContext) =>
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
    {
        return Results.BadRequest("Invalid Url format");
    }

    var shortCode = await urlService.GenerateUniqueCode();

    var request = httpContext.Request;

    var shortenedUrl = new ShortenedUrl
    {
        Id = Guid.NewGuid(),
        LongUrl = url,
        Code = shortCode,
        ShortUrl = $"{request.Scheme}://{request.Host}/{shortCode}",
        CreatedOnUtc = DateTime.UtcNow
    };

    dbContext.ShortenedUrls.Add(shortenedUrl);

    await dbContext.SaveChangesAsync();

    return Results.Ok(shortenedUrl.ShortUrl);
});

app.MapGet("{shortCode}", async (string shortCode, UrlShorteningService urlService, ApplicationDbContext applicationDbContext) =>
{
    var originalUrl = await applicationDbContext.ShortenedUrls.SingleOrDefaultAsync(x => x.Code == shortCode);

    return originalUrl is null ? Results.NotFound() : Results.Redirect(originalUrl.LongUrl);
});

app.UseHttpsRedirection();

app.Run();
