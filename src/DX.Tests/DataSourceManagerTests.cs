using DX.Application.Services;
using DX.Core.Abstractions;
using DX.Core.Configuration;
using DX.Core.Types;
using DX.Infrastructure.DataSources;

namespace Dx.Tests;

public class DataSourceManagerTests
{
    [Fact]
    public async Task ExecuteOnAllAsync_ExecutesQueryOnAllDataSources()
    {
        // Arrange
        var manager = new DataSourceManager();
        var sql = new SqlDataSource("SQL", new SqlConfiguration("Server=test"));
        var api = new ApiDataSource("API", new ApiConfiguration("http://test.com"));
        var file = new FileDataSource("File", new FileConfiguration("test.txt"));

        manager.Register(sql);
        manager.Register(api);
        manager.Register(file);

        // Act
        var results = await manager.ExecuteOnAllAsync("header", CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains("SQL", results.Keys);
        Assert.Contains("API", results.Keys);
        Assert.Contains("File", results.Keys);
    }

    [Fact]
    public async Task ExecuteOnAllAsync_OneFailingSource_DoesNotBlockOthers()
    {
        // Arrange
        var manager = new DataSourceManager();
        var successSource = new TestDataSource("Success", shouldGetDataFail: false);
        var failSource = new TestDataSource("Fail", shouldGetDataFail: true);
        var anotherSuccessSource = new TestDataSource("Success2", shouldGetDataFail: false);

        manager.Register(successSource);
        manager.Register(failSource);
        manager.Register(anotherSuccessSource);

        // Act
        var results = await manager.ExecuteOnAllAsync("test", CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results["Success"].IsSuccess);
        Assert.False(results["Fail"].IsSuccess);
        Assert.True(results["Success2"].IsSuccess);
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new DataSourceManager();
        var source1 = new SqlDataSource("Duplicate", new SqlConfiguration("Server=test1"));
        var source2 = new SqlDataSource("Duplicate", new SqlConfiguration("Server=test2"));

        manager.Register(source1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => manager.Register(source2));
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void Register_NullDataSource_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new DataSourceManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => manager.Register(null!));
    }

    [Fact]
    public void GetByType_ReturnsOnlyMatchingTypes()
    {
        // Arrange
        var manager = new DataSourceManager();
        var sql1 = new SqlDataSource("SQL1", new SqlConfiguration("Server=test1"));
        var sql2 = new SqlDataSource("SQL2", new SqlConfiguration("Server=test2"));
        var api = new ApiDataSource("API", new ApiConfiguration("http://test.com"));
        var file = new FileDataSource("File", new FileConfiguration("test.txt"));

        manager.Register(sql1);
        manager.Register(sql2);
        manager.Register(api);
        manager.Register(file);

        // Act
        var sqlSources = manager.GetByType<SqlDataSource>();
        var apiSources = manager.GetByType<ApiDataSource>();
        var fileSources = manager.GetByType<FileDataSource>();

        // Assert
        Assert.Equal(2, sqlSources.Count);
        Assert.Single(apiSources);
        Assert.Single(fileSources);
    }

    [Fact]
    public void GenerateReportAsync_ReturnsReportWithAllDataSources()
    {
        // Arrange
        var manager = new DataSourceManager();
        var sql = new SqlDataSource("SQL", new SqlConfiguration("Server=test"));
        var api = new ApiDataSource("API", new ApiConfiguration("http://test.com"));
        var file = new FileDataSource("File", new FileConfiguration("test.txt"));

        manager.Register(sql);
        manager.Register(api);
        manager.Register(file);

        // Act
        var report = manager.GenerateReportAsync();

        // Assert
        Assert.Contains("SQL", report);
        Assert.Contains("API", report);
        Assert.Contains("File", report);
        Assert.Contains("Type: SQL", report);
        Assert.Contains("Type: API", report);
        Assert.Contains("Type: FILE", report);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllDataSources()
    {
        // Arrange
        var manager = new DataSourceManager();
        var source1 = new TestDataSource("Source1");
        var source2 = new TestDataSource("Source2");

        manager.Register(source1);
        manager.Register(source2);

        // Act
        await manager.DisposeAsync();

        // Assert
        Assert.True(source1.IsDisposed);
        Assert.True(source2.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_OneSourceThrowsException_DisposesOtherSources()
    {
        // Arrange
        var manager = new DataSourceManager();
        var source1 = new TestDataSource("Source1");
        var failingSource = new TestDataSource("Failing", shouldDisposeFail: true);
        var source2 = new TestDataSource("Source2");

        manager.Register(source1);
        manager.Register(failingSource);
        manager.Register(source2);

        // Act
        await manager.DisposeAsync();

        // Assert
        Assert.True(source1.IsDisposed);
        Assert.True(source2.IsDisposed);
    }

    private sealed class TestDataSource : DataSourceBase<object>
    {
        private readonly bool _shouldConnectFail;
        private readonly bool _shouldGetDataFail;
        private readonly bool _shouldDisposeFail;

        public bool IsDisposed { get; private set; }

        public TestDataSource(
            string name, 
            bool shouldConnectFail = false, 
            bool shouldGetDataFail = false,
            bool shouldDisposeFail = false) 
            : base(name, new object())
        {
            _shouldConnectFail = shouldConnectFail;
            _shouldGetDataFail = shouldGetDataFail;
            _shouldDisposeFail = shouldDisposeFail;
        }

        protected override Task<DataSourceResult> ConnectCoreAsync(CancellationToken ct)
        {
            if (_shouldConnectFail)
                throw new Exception("Simulated connect failure");

            return Task.FromResult<DataSourceResult>(new SuccessResult("Connected"));
        }

        protected override Task<DataSourceResult> GetDataCoreAsync(string query, CancellationToken ct)
        {
            if (_shouldGetDataFail)
                throw new Exception("Simulated failure");

            return Task.FromResult<DataSourceResult>(new SuccessResult("Data"));
        }

        protected override Task DisconnectCoreAsync(CancellationToken ct)
            => Task.CompletedTask;

        public override string GetReportInfo()
            => "Type: Test";

        public override async ValueTask DisposeAsync()
        {
            if (_shouldDisposeFail)
            {
                throw new Exception("Simulated dispose failure");
            }

            await base.DisposeAsync();
            IsDisposed = true;
        }
    }
}
