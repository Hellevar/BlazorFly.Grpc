namespace BlazorFly.Grpc.Utils
{
    internal enum ClientMethodType
    {
        UnaryCall,
        ClientStreaming,
        ServerStreaming,
        DuplexStreaming
    }
}