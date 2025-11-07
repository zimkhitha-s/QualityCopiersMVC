using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Security;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using INSY7315_ElevateDigitalStudios_POE.Helper;

namespace INSY7315_ElevateDigitalStudios_POE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1) Add authentication (cookie) and authorization
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";            // redirect here when unauthenticated
                    options.LogoutPath = "/Account/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // set to Always in prod (HTTPS)
                    // optional: options.Events.OnValidatePrincipal = ... // see server-side checks below
                });

            builder.Services.AddAuthorization(options =>
            {
                // make authentication required by default for all endpoints
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<SessionAuthorizeAttribute>();
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            Env.Load();
            // Initialize Firebase
            FirebaseInitializer.Initialize();

            // Firebase Auth and Database Services
            builder.Services.AddSingleton<FirebaseAuthService>();
            builder.Services.AddSingleton<FirebaseService>();
            builder.Services.AddSingleton<MailService>();

            

            var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            // Encryption Helper
            builder.Services.AddSingleton<EncryptionHelper>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Enforce HTTPS
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Make sure session middleware is added before UseAuthorization
            app.UseSession();

            // Enable secure cookies
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always,   // Only send cookies via HTTPS
                HttpOnly = HttpOnlyPolicy.Always      // Prevent JavaScript access to cookies
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Map your routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
