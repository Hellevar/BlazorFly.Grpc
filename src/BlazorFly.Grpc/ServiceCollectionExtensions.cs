using System;
using System.Collections.Generic;
using BlazorFly.Grpc.Internal;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorFly.Grpc
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlazorFlyGrpc(this IServiceCollection services, Type clientType, Func<Metadata> defaultMetadata = null)
        {
            services.AddBlazorFlyGrpc(new List<Type> { clientType }, defaultMetadata);
        }

        public static void AddBlazorFlyGrpc(this IServiceCollection services, ICollection<Type> clientTypes, Func<Metadata> defaultMetadata = null)
        {
            services.AddSingleton(
                typeof(IGrpcViewTypeProvider),
                new GrpcViewTypeProvider(clientTypes));

            services.AddSingleton(
                typeof(IGrpcMetadataProvider),
                new GrpcMetadataProvider(defaultMetadata != null ? defaultMetadata() : new Metadata()));
        }
    }
}