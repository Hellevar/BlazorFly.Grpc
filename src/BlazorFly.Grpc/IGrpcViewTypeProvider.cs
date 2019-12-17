using System;

namespace BlazorFly.Grpc
{
    public interface IGrpcViewTypeProvider
    {
        Type GetGrpcViewType();
    }
}