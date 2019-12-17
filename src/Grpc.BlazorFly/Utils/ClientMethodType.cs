namespace Grpc.BlazorFly.Utils
{
    internal enum ClientMethodType
    {
        UnaryCall,
        ClientStreaming,
        ServerStreaming,
        DuplexStreaming
    }
}