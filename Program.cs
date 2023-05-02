using System.Configuration;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShoppingCart.Context;
using ShoppingCart.ServiceLayer;
using ShoppingCart.ShoppingCart.Repository;
using ShoppingCart.ShoppingCart.Services;
using ShoppingCart.UtilityService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(C =>
{
    C.SwaggerDoc("v1", new OpenApiInfo { Title = "dotnetClaimAuthorization", Version = "v1" });
    C.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{
        In = ParameterLocation.Header,
        Description = "Please Insert Token",
        Name ="Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    C.AddSecurityRequirement(new OpenApiSecurityRequirement{
    {
        new OpenApiSecurityScheme{
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[]{}
    }
    });
});


 
builder.Services.AddCors(option => 
//TO Solve angular connection error
{
    option.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

//connection string
builder.Services.AddDbContext<AppDbContext>(option=>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnStr"));
});

//only after configure this dependency injection will work
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartItemService, CartService>();
builder.Services.AddScoped<IEmailService, EmailService>();



// builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
// builder.Services.AddScoped<ICategoryService, CategoryService>();

// builder.Services.AddAuthentication().AddGoogle(googleOptions => {
//     googleOptions.ClientId = "1061078543208-tt13ktdaldc0g2kb07jejfn196ffg7b5.apps.googleusercontent.com";
//     googleOptions.ClientSecret = "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";

//     });




//Configure the Token
builder.Services.AddAuthentication(x =>
{
     x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
     x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("veryverysecret.....")),
        ValidateAudience = false,
        ValidateIssuer = false
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//FOR ENABLE TOKEN BEARER FUNCTION IN SWAGGER
app.UseSwagger();
app.UseSwaggerUI(c =>{
    c.SwaggerEndpoint("/swagger/v1/swagger.json","MY API"); 

});

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();



app.UseHttpsRedirection();
//pipeline added after // angular conn error
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
