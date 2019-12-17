using System;
using Grpc.BlazorFly.Builders;

namespace Grpc.BlazorFly.Internal
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