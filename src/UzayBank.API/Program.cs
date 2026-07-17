using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UzayBank.Application.Interfaces;
using UzayBank.Application.Services;
using UzayBank.Domain.Interfaces;
using UzayBank.Infrastructure.ExternalServices;
using UzayBank.Infrastructure.Persistence;
using UzayBank.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

// Veritabanı
builder.Services.AddDbContext<UzayBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var vakifBankCookies = new System.Net.CookieContainer();

builder.Services.AddHttpClient<VakifBankAuthService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        UseCookies = true,
        CookieContainer = vakifBankCookies,
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

builder.Services.AddHttpClient<VakifBankAccountService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false,
        UseCookies = true,
        CookieContainer = vakifBankCookies,
        AutomaticDecompression = System.Net.DecompressionMethods.All,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
//rest json 
// Repository kayıtları
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();

// Service kayıtları
builder.Services.AddScoped<IAccountService, VakifBankAccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBranchService, VakifBankBranchService>();
builder.Services.AddScoped<IMarketService, VakifBankMarketService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Çıkış yapılmış token'ların kara listesi
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<UzayBank.Application.Mappings.MappingProfile>();
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT token girin: Bearer {token}"
        
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Token imzası geçerli olsa bile, çıkış yapılmışsa reddet
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();

                var jti = context.Principal?
                    .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                if (jti != null && blacklist.IsRevoked(jti))
                    context.Fail("Token iptal edilmiş (çıkış yapılmış).");

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UzayBankDbContext>();
    try
    {
        await DataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed hatası: {ex.Message}");
    }
}
app.Run();