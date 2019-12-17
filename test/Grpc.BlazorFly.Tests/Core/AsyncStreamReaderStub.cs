using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Grpc.BlazorFly.Tests.Core
{
    public class AsyncStreamReaderStub : IAsyncStreamReader<HelloResponse>
    {
        private readonly string _message;
        private bool _valueTaken = false;

        public AsyncStreamReaderStub(string message)
        {
            _message = message;
        }

        public HelloResponse Current => new HelloResponse
        {
            Message = _message
        };

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (!_valueTaken)
            {
                _valueTaken = !_valueTaken;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}