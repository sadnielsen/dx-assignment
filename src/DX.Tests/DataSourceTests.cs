using DX.Core.Abstractions;
using DX.Core.Exceptions;
using DX.Core.Types;
using DX.Infrastructure.DataSources;

namespace Dx.Tests;

public class DataSourceTests
{
    [Fact]
    public async Task ConnectAsync_SuccessfulConnection_SetsStateToConnected()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));

        // Act
        var result = await dataSource.ConnectAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DataSourceState.Connected, dataSource.State);
    }

    [Fact]
    public async Task ConnectAsync_DoubleConnect_ReturnsSuccessResultWithoutReconnecting()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();

        // Act
        var result = await dataSource.ConnectAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DataSourceState.Connected, dataSource.State);
        Assert.IsType<SuccessResult>(result);
        var successResult = result as SuccessResult;
        Assert.Contains("Already connected", successResult!.Data.ToString()!);
    }

    [Fact]
    public async Task GetDataAsync_WhenNotConnected_AutoConnects()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);

        // Act
        var result = await dataSource.GetDataAsync("SELECT * FROM Users");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DataSourceState.Connected, dataSource.State);
    }

    [Fact]
    public async Task GetDataAsync_InvalidQuery_ReturnsErrorResult()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();

        // Act
        var result = await dataSource.GetDataAsync("INVALID QUERY");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<ErrorResult>(result);
    }

    [Fact]
    public async Task GetDataAsync_NullQuery_ThrowsArgumentException()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await dataSource.GetDataAsync(null!));
    }

    [Fact]
    public async Task GetDataAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await dataSource.GetDataAsync(string.Empty));
    }

    [Fact]
    public async Task GetDataAsync_WhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await dataSource.GetDataAsync("   "));
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_SetsStateToDisconnected()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();
        Assert.Equal(DataSourceState.Connected, dataSource.State);

        // Act
        await dataSource.DisconnectAsync();

        // Assert
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNothing()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);

        // Act
        await dataSource.DisconnectAsync();

        // Assert
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);
    }

    [Fact]
    public async Task DisposeAsync_WhenConnected_CallsDisconnect()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));
        await dataSource.ConnectAsync();
        Assert.Equal(DataSourceState.Connected, dataSource.State);

        // Act
        await dataSource.DisposeAsync();

        // Assert
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var dataSource = new SqlDataSource("TestDB", new SqlConfiguration("Server=test"));

        // Act & Assert
        await dataSource.DisposeAsync();
        Assert.Equal(DataSourceState.Disconnected, dataSource.State);
    }

    [Fact]
    public async Task ConcurrentConnectAsync_ResultsInSingleConnection()
    {
        // Arrange
        var dataSource = new TestConcurrentDataSource();

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => dataSource.ConnectAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, dataSource.ConnectCallCount);
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(DataSourceState.Connected, dataSource.State);
    }

    private sealed class TestConcurrentDataSource : DataSourceBase<object>
    {
        private int _connectCallCount;
        public int ConnectCallCount => _connectCallCount;

        public TestConcurrentDataSource() : base("test", new object())
        {
        }

        protected override async Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _connectCallCount);
            await Task.Delay(100, ct); // Simulate connection time
            return new SuccessResult("Connected");
        }

        protected override Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
            => Task.FromResult<DataSourceResult>(new SuccessResult("Data"));

        protected override Task DisconnectCoreAsync(CancellationToken ct)
            => Task.CompletedTask;

        public override string GetReportInfo()
            => "Type: Test";
    }
}
