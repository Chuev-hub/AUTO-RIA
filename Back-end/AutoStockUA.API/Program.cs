﻿using AutoStockUA.API;
using AutoStockUA.API.Controllers.Api;
using AutoStockUA.BLL.Services;
using AutoStockUA.DAL.Context;
using AutoStockUA.DAL.Context.Models.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

AuthOptions.KEY = builder.Configuration["KEY"];
AuthOptions.ISSUER = builder.Configuration["ISSUER"];
AuthOptions.AUDIENCE = builder.Configuration["AUDIENCE"];
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AutoStockContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

});
builder.Services.AddIdentity<User, IdentityRole<int>>()
 .AddEntityFrameworkStores<AutoStockContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/Login/AccessDenied";

});
//CookieAuthenticationDefaults.AuthenticationScheme
builder.Services.AddAuthentication()
.AddCookie()
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = AuthOptions.ISSUER,
        ValidateAudience = true,
        ValidAudience = AuthOptions.AUDIENCE,
        ValidateLifetime = true,
        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
        ValidateIssuerSigningKey = true,
    };
    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["GClientId"];
    options.ClientSecret = builder.Configuration["GSecret"];

    options.Scope.Add("profile");
    options.SignInScheme = IdentityConstants.ExternalScheme;
});
builder.Services.AddSession();
builder.Services.AddScoped(typeof(IService<,>), typeof(GenericService<,>));
builder.Services.AddScoped(typeof(GenericService<,>));
builder.Services.AddScoped(typeof(OptionsService));
builder.Services.AddScoped(typeof(UserService));
builder.Services.AddCors(options =>
{

    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AutoStockContext>();
    await context.Database.EnsureCreatedAsync();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseCors();
app.Run();