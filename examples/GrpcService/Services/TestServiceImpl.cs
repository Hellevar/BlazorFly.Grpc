using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoContracts;

namespace GrpcService
{
    public class TestServiceImpl : TestService.TestServiceBase
    {
        public override Task<HelloResponse> UnaryCall(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloResponse { Message = DateTime.UtcNow.ToLongTimeString() });
        }

        public override async Task<HelloResponse> ClientStreaming(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
        {
            var responseMessage = new StringBuilder("Hello ");
            while (await requestStream.MoveNext())
            {
                responseMessage.Append($", {requestStream.Current.Name}");
            }

            return new HelloResponse
            {
                Message = responseMessage.ToString()
            };
        }

        public override async Task ServerStreaming(HelloRequest request, IServerStreamWriter<HelloResponse> responseStream, ServerCallContext context)
        {
            for (int i = 0; i < 10; i++)
            {
                await responseStream.WriteAsync(new HelloResponse { Message = GetNextMessage(i) });
                await Task.Delay(1000);
            }
        }

        public override async Task DuplexStreaming(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloResponse> responseStream, ServerCallContext context)
        {
            var responseMessages = new List<string>();
            var index = 0;
            while (await requestStream.MoveNext())
            {
                responseMessages.Add($"{requestStream.Current.Name}, {GetNextMessage(index)}");
                index++;
                if (index > 9)
                {
                    index = 0;
                }
            }

            foreach (var message in responseMessages)
            {
                await responseStream.WriteAsync(new HelloResponse { Message = message });
                await Task.Delay(1000);
            }
        }

        private string GetNextMessage(int index)
        {
            var values = new[]
            {
                "One, Two...",
                "Freddy is coming for you!",
                "Three, Four...",
                "Better lock your door!",
                "Five, Six...",
                "Grab your crucifix!",
                "Seven, Eight...",
                "Gonna stay up late!",
                "Nine, Ten...",
                "Never sleep again!"
            };

            return values[index];
        }
    }
}