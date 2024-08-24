using ComiServ;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ComiServ.Background;
using Swashbuckle.AspNetCore.SwaggerGen;
using ComiServ.Entities;

var builder = WebApplication.CreateBuilder(args);

var CONFIG_FILEPATH = "config.json";
var configService = new ConfigService(CONFIG_FILEPATH);
var config = configService.Config;
var ConnectionString = $"Data Source={config.DatabaseFile};Mode=ReadWriteCreate";
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
builder.Services.AddHttpLogging(o => { });
//builder.Services.AddRazorPages().AddRazorPagesOptions(o =>
//{
//    o.RootDirectory = "/Pages";
//});
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

app.UseAuthorization();

app.MapControllers();

app.Run();
