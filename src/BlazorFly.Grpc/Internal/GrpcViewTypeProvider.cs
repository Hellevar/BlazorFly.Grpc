using System;
using System.Collections.Generic;
using BlazorFly.Grpc.Builders;

namespace BlazorFly.Grpc.Internal
{
    internal class GrpcViewTypeProvider : IGrpcViewTypeProvider
    {
        private readonly Type _componentType;

        public GrpcViewTypeProvider(ICollection<Type> clientTypes)
        {
            _componentType = new ComponentBuilder().Build(clientTypes);
        }

        public Type GetGrpcViewType()
        {
            return _componentType;
        }
    }
}