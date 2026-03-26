using DX.Core.Abstractions;
using DX.Core.Types;
using DX.Infrastructure.DataSources;

namespace Dx.Tests;

public class RetryDataSourceDecoratorTests
{
    [Fact]
    public async Task GetDataAsync_SuccessResult_ReturnsImmediately()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", DataSourceResult: new SuccessResult("Success"));
        var decorator = new RetryDataSourceDecorator(innerSource, maxRetries: 3);

        // Act
        var result = await decorator.GetDataAsync("test");

        // Assert
        Assert.IsType<SuccessResult>(result);
        Assert.Equal(1, innerSource.CallCount);
    }

    [Fact]
    public async Task GetDataAsync_ErrorResult_ReturnsImmediatelyWithoutRetry()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", DataSourceResult: new ErrorResult("Error"));
        var decorator = new RetryDataSourceDecorator(innerSource, maxRetries: 3);

        // Act
        var result = await decorator.GetDataAsync("test");

        // Assert
        Assert.IsType<ErrorResult>(result);
        Assert.Equal(1, innerSource.CallCount);
    }

    [Fact]
    public async Task GetDataAsync_RetryableError_RetriesUntilSuccess()
    {
        // Arrange
        var innerSource = new TestRetryableDataSourceWithRecovery("Test", 
            new DataSourceResult[] 
            { 
                new RetryableError("Retry 1", TimeSpan.FromMilliseconds(10)),
                new RetryableError("Retry 2", TimeSpan.FromMilliseconds(10)),
                new SuccessResult("Success")
            });
        var decorator = new RetryDataSourceDecorator(innerSource, maxRetries: 3);

        // Act
        var result = await decorator.GetDataAsync("test");

        // Assert
        Assert.IsType<SuccessResult>(result);
        Assert.Equal(3, innerSource.CallCount);
    }

    [Fact]
    public async Task GetDataAsync_RetryableError_StopsAfterMaxRetries()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", 
            DataSourceResult: new RetryableError("Retry", TimeSpan.FromMilliseconds(10)));
        var decorator = new RetryDataSourceDecorator(innerSource, maxRetries: 3);

        // Act
        var result = await decorator.GetDataAsync("test");

        // Assert
        Assert.IsType<RetryableError>(result);
        Assert.Equal(3, innerSource.CallCount);
    }

    [Fact]
    public async Task GetDataAsync_RetryableError_WaitsRetryAfterDuration()
    {
        // Arrange
        var retryAfter = TimeSpan.FromMilliseconds(100);
        var innerSource = new TestRetryableDataSource("Test", 
            DataSourceResult: new RetryableError("Retry", retryAfter));
        var decorator = new RetryDataSourceDecorator(innerSource, maxRetries: 2);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await decorator.GetDataAsync("test");
        stopwatch.Stop();

        // Assert
        Assert.IsType<RetryableError>(result);
        Assert.Equal(2, innerSource.CallCount);
        // Should have waited at least once (first retry)
        Assert.True(stopwatch.ElapsedMilliseconds >= retryAfter.TotalMilliseconds);
    }

    [Fact]
    public async Task ConnectAsync_DelegatesToInner()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", DataSourceResult: new SuccessResult("Connected"));
        var decorator = new RetryDataSourceDecorator(innerSource);

        // Act
        var result = await decorator.ConnectAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DataSourceState.Connected, decorator.State);
    }

    [Fact]
    public async Task DisconnectAsync_DelegatesToInner()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", DataSourceResult: new SuccessResult("Data"));
        await innerSource.ConnectAsync();
        var decorator = new RetryDataSourceDecorator(innerSource);

        // Act
        await decorator.DisconnectAsync();

        // Assert
        Assert.Equal(DataSourceState.Disconnected, decorator.State);
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner()
    {
        // Arrange
        var innerSource = new TestRetryableDataSource("Test", DataSourceResult: new SuccessResult("Data"));
        var decorator = new RetryDataSourceDecorator(innerSource);

        // Act
        await decorator.DisposeAsync();

        // Assert
        Assert.True(innerSource.IsDisposed);
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RetryDataSourceDecorator(null!));
    }

    private sealed class TestRetryableDataSource : DataSourceBase<object>
    {
        private readonly DataSourceResult _result;
        public int CallCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public TestRetryableDataSource(string name, DataSourceResult DataSourceResult) 
            : base(name, new object())
        {
            _result = DataSourceResult;
        }

        protected override Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
            => Task.FromResult<DataSourceResult>(new SuccessResult("Connected"));

        protected override Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(_result);
        }

        protected override Task DisconnectCoreAsync(CancellationToken ct)
            => Task.CompletedTask;

        public override string GetReportInfo()
            => "Type: Test";

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            IsDisposed = true;
        }
    }

    private sealed class TestRetryableDataSourceWithRecovery : DataSourceBase<object>
    {
        private readonly DataSourceResult[] _results;
        public int CallCount { get; private set; }

        public TestRetryableDataSourceWithRecovery(string name, DataSourceResult[] results) 
            : base(name, new object())
        {
            _results = results;
        }

        protected override Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
            => Task.FromResult<DataSourceResult>(new SuccessResult("Connected"));

        protected override Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
        {
            var result = _results[Math.Min(CallCount, _results.Length - 1)];
            CallCount++;
            return Task.FromResult(result);
        }

        protected override Task DisconnectCoreAsync(CancellationToken ct)
            => Task.CompletedTask;

        public override string GetReportInfo()
            => "Type: Test";
    }
}
