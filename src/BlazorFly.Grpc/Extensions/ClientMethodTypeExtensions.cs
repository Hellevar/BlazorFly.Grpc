using BlazorFly.Grpc.Utils;

namespace BlazorFly.Grpc.Extensions
{
    internal static class ClientMethodTypeExtensions
    {
        public static string GetNormalizedName(this ClientMethodType methodType)
        {
            switch (methodType)
            {
                case ClientMethodType.UnaryCall:
                    {
                        return "UNARY CALL";
                    }
                case ClientMethodType.ClientStreaming:
                    {
                        return "CLIENT STREAMING";
                    }
                case ClientMethodType.ServerStreaming:
                    {
                        return "SERVER STREAMING";
                    }
                default:
                    {
                        return "DUPLEX STREAMING";
                    }
            }
        }
    }
}