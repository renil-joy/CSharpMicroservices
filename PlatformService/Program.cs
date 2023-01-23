using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.SyncDataServices.Grpc;
using PlatformService.SyncDataServices.Http;

namespace PlatformService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsProduction())
        {
            Console.WriteLine($"-->Use SQL Db");
            builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(builder.Configuration.GetConnectionString("PlatformConn")));
        }
        else
        {
            Console.WriteLine($"-->Use InMemory Db");
            builder.Services.AddDbContext<AppDbContext>(opt =>
              opt.UseInMemoryDatabase("InMem"));
        }

        builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
        builder.Services.AddHttpClient<ICommandDataClient, CommandDataClient>();
        builder.Services.AddScoped<IMessageBusClient, MessageBusClient>();
        builder.Services.AddGrpc();
        builder.Services.AddControllers();
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        Console.WriteLine($"-->Command Service Endpoint {builder.Configuration["CommandService"]}");

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();
        app.MapGrpcService<GrpcPlatformService>();
        app.MapGet("protos/platforms.proto", async context =>
         {
             await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
         });

        PrepDb.PrepPopulation(app);

        app.Run();
    }
}
