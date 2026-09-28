using CinemaApp.Servies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CinemaApp;

public class Program
{
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    builder.Services.AddScoped<IFileUpload, FileUpload>();

    builder.Services.AddScoped<IRepository<Category>, Repository<Category>>();
    builder.Services.AddScoped<IRepository<Cinema>, Repository<Cinema>>();
    builder.Services.AddScoped<IRepository<Actor>, Repository<Actor>>();
    builder.Services.AddScoped<IRepository<Movie>, Repository<Movie>>();
    builder.Services.AddScoped<IBulkRepository<MovieSubImg>, BulkRepository<MovieSubImg>>();
    builder.Services.AddScoped<IBulkRepository<MovieActor>, BulkRepository<MovieActor>>();
    builder.Services.AddScoped<IRepository<ApplicationUserOTP>, Repository<ApplicationUserOTP>>();
    builder.Services.AddScoped<IBulkRepository<Seat>, BulkRepository<Seat>>();
    builder.Services.AddScoped<IRepository<Seat>, Repository<Seat>>();
    builder.Services.AddScoped<IRepository<Favorite>, Repository<Favorite>>();
    builder.Services.AddScoped<IRepository<Cart>, Repository<Cart>>();
    builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
    builder.Services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();

    builder.Services.AddScoped<IDbInitializer, DbInitializer>();
    builder.Services.AddTransient<IEmailSender, EmailSender>();

    var connectionString =
                    builder.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string"
                        + "'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(optionsBuilder =>
    {
      optionsBuilder.UseSqlServer(connectionString);
    });

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
      options.User.RequireUniqueEmail = true;
      options.Password.RequiredLength = 8;
      options.Password.RequiredUniqueChars = 0;
      options.Lockout.MaxFailedAccessAttempts = 6;
      options.SignIn.RequireConfirmedEmail = true;
      options.SignIn.RequireConfirmedPhoneNumber = false;
    })
      .AddEntityFrameworkStores<ApplicationDbContext>()
      .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(option =>
    {
      option.LoginPath = "/identity/account/login";
      option.AccessDeniedPath = "/identity/account/AccessDenied";
    });


    builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(options =>
    {
      options.ValidationInterval = TimeSpan.FromSeconds(30);
    });

    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

    var app = builder.Build();

    StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

    using (var scope = app.Services.CreateScope())
    {
      var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
      dbInitializer.Initialize();
    }


    var defaultCulture = new System.Globalization.CultureInfo("en-US");
    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
    app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
    {
      DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
      SupportedCultures = new[] { defaultCulture },
      SupportedUICultures = new[] { defaultCulture },
    });

    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler("/Home/Error");
      app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

    app.Run();
  }
}
