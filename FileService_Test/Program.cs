using Microsoft.EntityFrameworkCore;
using FileService_Test.Context;
using FileService_Test.Users;
using FileService_Test.Files;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using FileService_Test.Options;

var builder = WebApplication.CreateBuilder(args);

// Add database connection

builder.Services
    .AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]));

// Add services to the container.

builder.Services.AddTransient<IGetUser, GetUser>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IFileHandlerService, FileHandlerService>();
builder.Services.Configure<AddressOptions>(builder.Configuration.GetSection("AddressOptions"));

// Remove kestrel restriction of max reuest body size

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

// Add loop json handler

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
