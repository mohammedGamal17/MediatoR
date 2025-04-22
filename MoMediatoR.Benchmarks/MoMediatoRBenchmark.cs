using System.Reflection.Metadata;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MoMediatoR.Benchmarks
{
    public class Ping : IRequest<string>
    {
        public string Message { get; set; } = "Ping!";
    }
    public class Ping2 : MediatR.IRequest<string>
    {
        public string Message { get; set; } = "Ping!2";
    }

    public class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Pong: {request.Message}");
        }
    }
    public class PingHandler2 : MediatR.IRequestHandler<Ping2, string>
    {
        public Task<string> Handle(Ping2 request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Pong2: {request.Message}");
        }
    }
    [MemoryDiagnoser]
    public class MoMediatoRBenchmark
    {
        private IMoMediatoR _moMediator;
        private IMediator _mediator;

        [GlobalSetup]
        public void Setup()
        {
            // Setup MediatR in one container
            var mediatRServices = new ServiceCollection();
            mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping2).Assembly));
            var mediatRProvider = mediatRServices.BuildServiceProvider();
            _mediator = mediatRProvider.GetRequiredService<IMediator>();

            // Setup MoMediatoR in a separate container
            var moServices = new ServiceCollection();
            moServices.AddMoMediatoR(typeof(PingHandler).Assembly);
            var moProvider = moServices.BuildServiceProvider();
            _moMediator = moProvider.GetRequiredService<IMoMediatoR>();
        }

        [Benchmark]
        public async Task MoMediatoR_Request()
        {
            await _moMediator.Send(new Ping());
        }
        [Benchmark]
        public async Task MediatR_Request()
        {
            await _mediator.Send(new Ping2());
        }
    }
}
