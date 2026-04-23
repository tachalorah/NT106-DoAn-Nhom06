using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureChat.Models;
using SecureChat.Repositories;
using SecureChat.Services;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Default")
	?? throw new InvalidOperationException("Connection string 'Default' not found.");

if (connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
	|| connStr.Contains(".db", StringComparison.OrdinalIgnoreCase)
	|| connStr.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
{
	throw new InvalidOperationException("SQLite connection string detected. SecureChat.Server only supports MariaDB/MySQL.");
}

builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(
    connStr,
	ServerVersion.AutoDetect(connStr),
	my => {
		my.MigrationsAssembly("SecureChat.Server");
		my.EnableRetryOnFailure(maxRetryCount: 3);
	})
);

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FriendRepository>();
builder.Services.AddScoped<ConversationRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<CallRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<ForgotPasswordService>();

var jwtKey = builder.Configuration["Jwt:Key"]
	?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
	o.TokenValidationParameters = new TokenValidationParameters {
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		ClockSkew = TimeSpan.FromMinutes(5)
	};
});

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(o => {
		o.JsonSerializerOptions.Converters.Add(
			new System.Text.Json.Serialization.JsonStringEnumConverter());
	}
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "SecureChat API", Version = "v1" } );

var scheme = new OpenApiSecurityScheme {
	Name         = "Authorization",
	Type         = SecuritySchemeType.Http,
	Scheme       = "bearer",
	BearerFormat = "JWT",
	In           = ParameterLocation.Header,
	Description  = "Nhập JWT access token (không cần tiền tố 'Bearer ')"
};

c.AddSecurityDefinition("Bearer", scheme);
c.AddSecurityRequirement(new OpenApiSecurityRequirement {
		{
			new OpenApiSecurityScheme {
				Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id   = "Bearer" }
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI(c => {
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecureChat API v1");
		c.RoutePrefix = string.Empty;
	});
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
