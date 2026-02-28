using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.Middleware;
using RecipeSugesstionApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<RecipeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

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
    });

builder.Services.AddAuthorization();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();

// ── Controllers / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Recipe Sharing Platform API",
        Version = "v1",
        Description = "Backend API for the Recipe Sharing Platform. Supports CRUD for recipes, categories, ratings, and comments with JWT auth."
    });

    // Add JWT bearer support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (allow Angular dev server) ──────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Global Error Handling ───────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Serve uploaded images as static files
app.UseStaticFiles();

app.UseCors("AngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Auto-apply migrations and seed on startup ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    db.Database.Migrate();
}

app.Run();
