using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Security;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace INSY7315_ElevateDigitalStudios_POE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();


            // Add global anti-forgery token validation
            builder.Services.AddControllersWithViews(options =>
            {
                // Automatically validate anti-forgery tokens on POST actions
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            // Add sessions if you plan to use them
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            // Firebase Auth and Database Services
            builder.Services.AddSingleton<FirebaseAuthService>();
            builder.Services.AddSingleton<FirebaseService>();

            var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("/Users/thando/VSCode Projects/INSY7315_ElevateDigitalStudios_POE/Database/firebase-key.json"),
                ProjectId = builder.Configuration["Firebase:ProjectId"]
            });


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

            app.UseAuthorization();

            // Map your routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
