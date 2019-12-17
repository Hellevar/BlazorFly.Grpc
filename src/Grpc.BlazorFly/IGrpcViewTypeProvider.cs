using System;

namespace Grpc.BlazorFly
{
    public interface IGrpcViewTypeProvider
    {
        Type GetGrpcViewType();
    }
}