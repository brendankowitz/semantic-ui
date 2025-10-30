using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticUI.Api.Data;
using SemanticUI.Api.Hubs;
using SemanticUI.Api.Security;
using SemanticUI.Api.Services;
using SemanticUI.Core.Interfaces;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback to in-memory database for development
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("SemanticUI"));
}

// Redis for caching and SignalR (optional in development)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });

    // SignalR with Redis backplane
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisConnection);
}
else
{
    // SignalR without Redis for development
    builder.Services.AddSignalR();
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[] { "http://localhost:5173", "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Authentication (JWT)
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true
            };

            // For SignalR - extract token from query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
}

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });
});

// Semantic Kernel
var openAiKey = builder.Configuration["OpenAI:ApiKey"];
var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4";

if (!string.IsNullOrEmpty(openAiKey) && openAiKey != "your-openai-api-key-here")
{
    builder.Services.AddSingleton(sp =>
    {
        var kernelBuilder = Kernel.CreateBuilder();

        // Add OpenAI Chat Completion
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: openAiModel,
            apiKey: openAiKey);

        // Add FHIR plugins
        kernelBuilder.Plugins.AddFromType<FhirUIGenerationPlugin>("FhirUI");
        kernelBuilder.Plugins.AddFromType<FhirDataPlugin>("FhirData");

        return kernelBuilder.Build();
    });

    builder.Services.AddSingleton(sp =>
        sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
}
else
{
    // Mock services for development without OpenAI key
    builder.Services.AddSingleton<Kernel>(sp =>
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Plugins.AddFromType<FhirUIGenerationPlugin>("FhirUI");
        kernelBuilder.Plugins.AddFromType<FhirDataPlugin>("FhirData");
        return kernelBuilder.Build();
    });

    builder.Services.AddSingleton<IChatCompletionService>(sp =>
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured. Please set OpenAI:ApiKey in appsettings.json");
    });
}

// Application services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<ICodeValidationService, CodeValidationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: https:; " +
              "connect-src 'self' ws: wss:; " +
              "frame-src 'self' https://sandpack-bundler.codesandbox.io;";
    context.Response.Headers.Append("Content-Security-Policy", csp);

    await next();
});

app.UseHttpsRedirection();

app.UseCors();

if (!string.IsNullOrEmpty(jwtKey))
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

// Ensure database is created (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.EnsureCreated();
    }
}

app.Run();
