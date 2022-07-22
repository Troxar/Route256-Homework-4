using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Ozon.Route256.Postgres.Api;

public static class Program
{
    public static void Main(string[] args) =>
        CreateHostBuilder(args)
            .Build()
            .Run();

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHost(
                builder =>
                {
                    builder.UseKestrel(o => o.Listen(IPAddress.Any, 5002, options => options.Protocols = HttpProtocols.Http2));
                    builder.UseStartup<Startup>();
                });
}
