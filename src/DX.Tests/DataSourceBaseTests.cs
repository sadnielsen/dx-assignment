using DX.Core.Abstractions;
using DX.Core.Types;

namespace Dx.Tests;

public class DataSourceBaseTests
{
    [Fact]
    public async Task GetDataAsync_Serializes_Concurrent_Calls()
    {
        var sut = new TestDataSource();

        var tasks = Enumerable.Range(0, 10)
            .Select(i => sut.GetDataAsync($"query-{i}"))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, sut.MaxConcurrentCalls);
    }

    private sealed class TestDataSource : DataSourceBase<object>
    {
        private int _currentCalls;
        public int MaxConcurrentCalls { get; private set; }

        public TestDataSource() : base("test", new object())
        {
        }

        protected override Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
            => Task.FromResult<DataSourceResult>(new SuccessResult("Connected"));

        protected override async Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
        {
            var current = Interlocked.Increment(ref _currentCalls);
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, current);

            try
            {
                await Task.Delay(50, ct);
                return new SuccessResult(query);
            }
            finally
            {
                Interlocked.Decrement(ref _currentCalls);
            }
        }

        protected override Task DisconnectCoreAsync(CancellationToken ct)
            => Task.CompletedTask;

        public override string GetReportInfo()
            => "Type: Test";

        public override ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}