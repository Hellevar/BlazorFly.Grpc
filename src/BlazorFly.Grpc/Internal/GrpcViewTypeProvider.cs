using System;
using BlazorFly.Grpc;
using BlazorFly.Grpc.Builders;

namespace BlazorFly.Grpc.Internal
{
    internal class GrpcViewTypeProvider : IGrpcViewTypeProvider
    {
        private readonly Type _componentType;

        public GrpcViewTypeProvider(Type clientType)
        {
            _componentType = new ComponentBuilder().Build(clientType);
        }

        public Type GetGrpcViewType()
        {
            return _componentType;
        }
    }
}