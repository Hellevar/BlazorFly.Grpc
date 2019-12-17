using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace BlazorFly.Grpc.Tests.Core
{
    public class AsyncStreamWriterStub : IClientStreamWriter<HelloRequest>
    {
        private List<HelloRequest> _storage = new List<HelloRequest>();

        public WriteOptions WriteOptions { get; set; }

        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }

        public Task WriteAsync(HelloRequest message)
        {
            _storage.Add(message);
            return Task.CompletedTask;
        }
    }
}