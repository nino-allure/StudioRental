using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Добавляем CORS (ВАЖНО!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new global::Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = global::Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = global::Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите JWT токен"
    });

    options.AddSecurityRequirement(new global::Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new global::Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new global::Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = global::Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecureSecretForRentalService";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "StudioRentalAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "StudioRentalWeb";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Server=localhost;Database=StudioRental;User=root;Password=;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<JwtService>();

builder.Services.AddScoped<ExcelExportService>();

var app = builder.Build();

// Используем CORS
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var adminExists = db.Users.Any(u => u.Email == "admin@studiorental.com");
    if (!adminExists)
    {
        var admin = new User
        {
            Email = "admin@studiorental.com",
            FullName = "Администратор",
            Role = "Admin",
            CreatedAt = DateTime.Now,
            Password = "admin123"
        };
        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine("=== АДМИН СОЗДАН ===");
        Console.WriteLine("Email: admin@studiorental.com");
        Console.WriteLine("Password: admin123");
        Console.WriteLine("===================");
    }
}

app.Run();