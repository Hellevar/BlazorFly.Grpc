using System;
using BlazorFly.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProtoContracts;

namespace BlazorApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddBlazorFlyGrpc(
                typeof(TestService.TestServiceClient),
                () =>
                {
                    var metadata = new Metadata();
                    metadata.Add(new Metadata.Entry("testkey", "testvalue"));

                    return metadata;
                });
            services.AddGrpcClient<TestService.TestServiceClient>(options =>
            {
                options.Address = new Uri("https://localhost:5001");
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
