using System;
using BlazorFly.Grpc.Internal;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorFly.Grpc
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGrpcBlazorFly(this IServiceCollection services, Type clientType, Func<Metadata> defaultMetadata = null)
        {
            services.AddSingleton(typeof(IGrpcViewTypeProvider), new GrpcViewTypeProvider(clientType));
            if (defaultMetadata != null)
            {
                services.AddSingleton(typeof(IGrpcMetadataProvider), new GrpcMetadataProvider(defaultMetadata()));
            }
        }
    }
}