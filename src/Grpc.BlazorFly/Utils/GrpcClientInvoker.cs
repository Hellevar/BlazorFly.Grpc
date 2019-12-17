using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

[assembly: InternalsVisibleTo("Grpc.BlazorFly.GeneratedAssembly")]
[assembly: InternalsVisibleTo("Grpc.BlazorFly.Tests")]

namespace Grpc.BlazorFly.Utils
{    
    internal static class GrpcClientInvoker
    {
        public static string InitializeStringValue<TModel>(JsonSerializerOptions options, bool isList)
        {
            if (isList)
            {
                var requests = Activator.CreateInstance<List<TModel>>();
                var request = Activator.CreateInstance<TModel>();
                requests.Add(request);
                return JsonSerializer.Serialize(requests, options);
            }
            else
            {
                var request = Activator.CreateInstance<TModel>();
                return JsonSerializer.Serialize(request, options);
            }
        }

        public static void DoUnaryCall<TRequest, TResponse>(
            Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> requestAction,
            string requestValue,
            CancellationTokenSource cancellationTokenSource,
            JsonSerializerOptions options,
            Func<string, Task> responseAction)
        {
            ExecuteAndLogError(async () =>
            {
                var response = await requestAction(JsonSerializer.Deserialize<TRequest>(requestValue), new CallOptions(cancellationToken: cancellationTokenSource.Token));
                await responseAction(JsonSerializer.Serialize(response, options));
            },
            responseAction);
        }

        public static void DoClientStreaming<TRequest, TResponse>(
            Func<CallOptions, AsyncClientStreamingCall<TRequest, TResponse>> requestAction,
            string requestValue,
            CancellationTokenSource cancellationTokenSource,
            JsonSerializerOptions options,
            Func<string, Task> responseAction)
        {
            ExecuteAndLogError(async () =>
            {
                var clientStreamingCall = requestAction(new CallOptions(cancellationToken: cancellationTokenSource.Token));
                var requests = JsonSerializer.Deserialize<List<TRequest>>(requestValue);
                var requestStream = clientStreamingCall.RequestStream;

                foreach (var request in requests)
                {
                    await requestStream.WriteAsync(request);
                    await Task.Delay(200);
                }

                await requestStream.CompleteAsync();

                var response = await clientStreamingCall;
                await responseAction(JsonSerializer.Serialize(response, options));
            },
            responseAction);
        }

        public static void DoServerStreaming<TRequest, TResponse>(
            Func<TRequest, CallOptions, AsyncServerStreamingCall<TResponse>> requestAction,
            string requestValue,
            CancellationTokenSource cancellationTokenSource,
            JsonSerializerOptions options,
            Func<string> responseValueGetter,
            Func<string, Task> responseAction)
        {
            ExecuteAndLogError(async () =>
            {
                var request = JsonSerializer.Deserialize<TRequest>(requestValue);
                var serverStream = requestAction(request, new CallOptions(cancellationToken: cancellationTokenSource.Token)).ResponseStream;

                while (await serverStream.MoveNext(cancellationTokenSource.Token))
                {
                    await responseAction($"{responseValueGetter()}{Environment.NewLine}{JsonSerializer.Serialize(serverStream.Current, options)}");
                }
            },
            responseAction);

        }

        public static void DoDuplexStreaming<TRequest, TResponse>(
            Func<CallOptions, AsyncDuplexStreamingCall<TRequest, TResponse>> requestAction,
            string requestValue,
            CancellationTokenSource cancellationTokenSource,
            JsonSerializerOptions options,
            Func<string> responseValueGetter,
            Func<string, Task> responseAction)
        {
            ExecuteAndLogError(async () =>
            {
                var requests = JsonSerializer.Deserialize<List<TRequest>>(requestValue);
                var duplexCall = requestAction(new CallOptions(cancellationToken: cancellationTokenSource.Token));
                var clientStream = duplexCall.RequestStream;
                var serverStream = duplexCall.ResponseStream;

                foreach (var request in requests)
                {
                    await clientStream.WriteAsync(request);
                    await Task.Delay(200);
                }

                await clientStream.CompleteAsync();

                while (await serverStream.MoveNext(cancellationTokenSource.Token))
                {
                    await responseAction($"{responseValueGetter()}{Environment.NewLine}{JsonSerializer.Serialize(serverStream.Current, options)}");
                }
            },
            responseAction);
        }

        private static void ExecuteAndLogError(Func<Task> requestProcessing, Func<string, Task> responseAction)
        {
            Task.Run(async () =>
            {
                try
                {
                    await requestProcessing();
                }
                catch (Exception e)
                {
                    await responseAction(e.ToString());
                }
            });
        }
    }
}