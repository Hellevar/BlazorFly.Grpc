using Grpc.Core;

namespace BlazorFly.Grpc.Internal
{
    internal interface IGrpcMetadataProvider
    {
        Metadata GetMetadata();
    }
}