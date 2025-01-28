using Microsoft.AspNetCore.Server.Kestrel.Core;
using Rps.Api;
using Rps.Api.Grpc;
using Rps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7299, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps();
    });
});

builder.Services.AddGrpc();
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGrpcService<GrpcGameService>();
app.MapControllers();

app.Run();
