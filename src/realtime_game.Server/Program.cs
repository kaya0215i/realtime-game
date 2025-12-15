using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.StreamingHubs;
using System;

var builder = WebApplication.CreateBuilder(args);
var magiconion = builder.Services.AddMagicOnion();

if (builder.Environment.IsDevelopment()) {
    magiconion.AddJsonTranscoding();
    builder.Services.AddMagicOnionJsonTranscodingSwagger();
    builder.Services.AddSingleton<RoomContextRepository>();
}

builder.Services.AddDbContext<GameDbContext>(options => {
    var connectionString = builder.Configuration.GetConnectionString("Default");

    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => {
            mySqlOptions.EnableStringComparisonTranslations();
        });
});

builder.Services.AddSwaggerGen(options => {
    options.IncludeMagicOnionXmlComments(Path.Combine(AppContext.BaseDirectory, "realtime_game.Shared.xml"));
    options.SwaggerDoc("v1", new OpenApiInfo {
        Version = "v1",
        Title = "タイトル",
        Description = "説明",
    });
});

builder.Services.AddMvcCore().AddApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "タイトル");
    });
}

app.MapMagicOnionService();
app.MapGet("/", () => "");

app.Run();
