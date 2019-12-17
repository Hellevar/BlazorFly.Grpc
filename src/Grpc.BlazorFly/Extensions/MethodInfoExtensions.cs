using System;
using System.Reflection;
using Grpc.BlazorFly.Utils;
using Grpc.Core;

namespace Grpc.BlazorFly.Extensions
{
    internal static class MethodInfoExtensions
    {
        public static ClientMethodType GetClientMethodType(this MethodInfo method)
        {
            var returnType = method.ReturnType.GetGenericTypeDefinition();
            if (returnType == typeof(AsyncUnaryCall<>))
            {
                return ClientMethodType.UnaryCall;
            }

            if (returnType == typeof(AsyncClientStreamingCall<,>))
            {
                return ClientMethodType.ClientStreaming;
            }

            if (returnType == typeof(AsyncServerStreamingCall<>))
            {
                return ClientMethodType.ServerStreaming;
            }

            return ClientMethodType.DuplexStreaming;
        }

        public static Type GetRequestTypeFromMethod(this MethodInfo method, ClientMethodType methodType)
        {
            switch (methodType)
            {
                case ClientMethodType.UnaryCall:
                    {
                        var type = method.GetParameters()[0].ParameterType;

                        return type;
                    }
                case ClientMethodType.ClientStreaming:
                    {
                        var type = method.ReturnType.GenericTypeArguments[0];

                        return type;
                    }
                case ClientMethodType.ServerStreaming:
                    {
                        var type = method.GetParameters()[0].ParameterType;

                        return type;
                    }
                default:
                    {
                        var type = method.ReturnType.GenericTypeArguments[0];

                        return type;
                    }
            }
        }

        public static Type GetResponseTypeFromMethod(this MethodInfo method, ClientMethodType methodType)
        {
            switch (methodType)
            {
                case ClientMethodType.UnaryCall:
                    {
                        var type = method.ReturnType.GenericTypeArguments[0];

                        return type;
                    }
                case ClientMethodType.ClientStreaming:
                    {
                        var type = method.ReturnType.GenericTypeArguments[1];

                        return type;
                    }
                case ClientMethodType.ServerStreaming:
                    {
                        var type = method.ReturnType.GenericTypeArguments[0];

                        return type;
                    }
                default:
                    {
                        var type = method.ReturnType.GenericTypeArguments[1];

                        return type;
                    }
            }
        }
    }
}