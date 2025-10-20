using INSY7315_ElevateDigitalStudios_POE.Services;

namespace INSY7315_ElevateDigitalStudios_POE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add sessions if you plan to use them
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            // Firebase Auth Service for dependency injection
            builder.Services.AddSingleton<FirebaseAuthService>();

            // Firebase Service
            builder.Services.AddSingleton<FirebaseService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Make sure session middleware is added before UseAuthorization
            app.UseSession();

            app.UseAuthorization();

            // Map your routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Quotations}/{action=Quotations}/{id?}");

            app.Run();
        }
    }
}
