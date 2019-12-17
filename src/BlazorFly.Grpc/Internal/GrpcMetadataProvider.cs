using Grpc.Core;

namespace BlazorFly.Grpc.Internal
{
    internal class GrpcMetadataProvider : IGrpcMetadataProvider
    {
        private readonly Metadata _defaultMetadata;

        public GrpcMetadataProvider(Metadata defaultMetadata)
        {
            _defaultMetadata = defaultMetadata;
        }

        public Metadata GetMetadata()
        {
            return _defaultMetadata;
        }
    }
}