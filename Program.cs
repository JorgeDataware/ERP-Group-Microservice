using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
//           .UseSnakeCaseNamingConvention());

//builder.Services.AddAutoMapper(_ => { }, typeof(UserMappers));

//builder.Services.AddScoped<IJWTService, JWTService>();
//builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

//builder.Services.AddScoped<IDbConnection>(_ =>
//    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

//Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
//        };

//        options.Events = new JwtBearerEvents
//        {
//            OnMessageReceived = context =>
//            {
//                var token = context.Request.Headers["Authorization"]
//                    .FirstOrDefault()?.Split(" ").Last();

//                if (string.IsNullOrEmpty(token))
//                    token = context.Request.Cookies["jwt"];

//                if (!string.IsNullOrEmpty(token))
//                    context.Token = token;

//                return Task.CompletedTask;
//            }
//        };
//    });

builder.Services.AddControllers();
//builder.Services.AddScoped<IUserRepositorie, UserRepositorie>();
builder.Services.AddOpenApi();

var app = builder.Build();

//// Aplicar migraciones
//await using (var scope = app.Services.CreateAsyncScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    await dbContext.Database.MigrateAsync();
//}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Groups Microservice API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();