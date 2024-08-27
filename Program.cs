using ComiServ;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ComiServ.Background;
using Swashbuckle.AspNetCore.SwaggerGen;
using ComiServ.Entities;
using ComiServ.Services;
using ComiServ.Middleware;
using ComiServ.Controllers;
using System.Text;

var CONFIG_FILEPATH = "config.json";
var configService = new JsonConfigService(CONFIG_FILEPATH);
var config = configService.Config;
var ConnectionString = $"Data Source={config.DatabaseFile};Mode=ReadWriteCreate";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--addadmin")
    {
        string username;
        if (args.ElementAtOrDefault(i + 1) is string _username)
        {
            username = _username;
        }
        else
        {
            Console.Write("Username: ");
            username = Console.ReadLine()
                ?? throw new Exception("must provide a username");
        }
        username = username.Trim();
        Console.Write("Password: ");
        string password = Console.ReadLine()?.Trim()
            ?? throw new Exception("must provide a username");
        var salt = User.MakeSalt();
        var hashed = User.Hash(Encoding.UTF8.GetBytes(password), salt);
        using var context = new ComicsContext(
            new DbContextOptionsBuilder<ComicsContext>()
                .UseSqlite(ConnectionString).Options);
        context.Users.Add(new User()
        {
            Username = username,
            Salt = salt,
            HashedPassword = hashed,
            UserTypeId = UserTypeEnum.Administrator
        });
        context.SaveChanges();
        return;
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<EntitySwaggerFilter>();
});
builder.Services.AddSingleton<IConfigService>(configService);
builder.Services.AddDbContext<ComicsContext>(options =>
    options.UseSqlite(ConnectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<ITaskManager>(sp =>
    new TaskManager(sp.GetService<ILogger<ITaskManager>>()));
builder.Services.AddSingleton<IComicAnalyzer>(sp =>
    new SynchronousComicAnalyzer(
        logger: sp.GetRequiredService<ILogger<IComicAnalyzer>>()));
builder.Services.AddSingleton<IComicScanner>(sp =>
    new ComicScanner(provider: sp));
builder.Services.AddSingleton<IPictureConverter>(
    new ResharperPictureConverter(true));
builder.Services.AddHttpLogging(o => { });
builder.Services.AddScoped<IAuthenticationService>(
    sp => new AuthenticationService());
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});
var app = builder.Build();
app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    using var context = services.GetRequiredService<ComicsContext>();
    context.Database.EnsureCreated();
}

var scanner = app.Services.GetRequiredService<IComicScanner>();
scanner.TriggerLibraryScan();
scanner.ScheduleRepeatedLibraryScans(TimeSpan.FromDays(1));

app.UseHttpsRedirection();

//ensures that the user is authenticated (if auth is provided) but does not restrict access to any routes
app.UseBasicAuthentication([]);
//require user or admin account to access any comic resource (uses the authentication 
app.UseWhen(context => context.Request.Path.StartsWithSegments(ComicController.ROUTE), appBuilder =>
{
    appBuilder.UseBasicAuthentication([UserTypeEnum.User, UserTypeEnum.Administrator]);
});
//require user or admin account to access any user resource
app.UseWhen(context => context.Request.Path.StartsWithSegments(UserController.ROUTE), appBuilder =>
{
    appBuilder.UseBasicAuthentication([UserTypeEnum.User, UserTypeEnum.Administrator]);
});
//require admin account to access any task resource
app.UseWhen(context => context.Request.Path.StartsWithSegments(TaskController.ROUTE), appBuilder =>
{
    appBuilder.UseBasicAuthentication([UserTypeEnum.Administrator]);
});

app.MapControllers();

app.Run();
