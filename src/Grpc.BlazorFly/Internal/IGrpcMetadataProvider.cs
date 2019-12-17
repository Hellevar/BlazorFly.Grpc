using Grpc.Core;

namespace Grpc.BlazorFly.Internal
{
    internal interface IGrpcMetadataProvider
    {
        Metadata GetMetadata();
    }
}