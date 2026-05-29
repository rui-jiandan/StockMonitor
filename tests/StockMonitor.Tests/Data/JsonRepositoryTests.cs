using FluentAssertions;
using StockMonitor.Data;
using System.IO;
using System.Text.Json;
using Xunit;

namespace StockMonitor.Tests.Data;

public class JsonRepositoryTests : IDisposable
{
    private readonly string _tempDir;

    public JsonRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JsonRepoTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string GetFilePath() => Path.Combine(_tempDir, "test.json");

    [Fact]
    public void Load_NonExistentFile_ReturnsDefault()
    {
        var repo = new JsonFileRepository<TestData>(GetFilePath());

        var result = repo.Load();

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        var filePath = GetFilePath();
        var repo = new JsonFileRepository<TestData>(filePath);
        var data = new TestData
        {
            Items =
            [
                new TestItem { Name = "测试", Value = 42 }
            ]
        };

        repo.Save(data);

        var repo2 = new JsonFileRepository<TestData>(filePath);
        var loaded = repo2.Load();

        loaded.Should().NotBeNull();
        loaded.Items.Should().HaveCount(1);
        loaded.Items[0].Name.Should().Be("测试");
        loaded.Items[0].Value.Should().Be(42);
    }

    [Fact]
    public void Exists_ReturnsCorrectly()
    {
        var filePath = GetFilePath();
        var repo = new JsonFileRepository<TestData>(filePath);

        repo.Exists().Should().BeFalse();

        repo.Save(new TestData());

        repo.Exists().Should().BeTrue();
    }

    [Fact]
    public void Load_CorruptedFile_FallsBackToBackup()
    {
        var filePath = GetFilePath();
        var repo = new JsonFileRepository<TestData>(filePath);
        var data = new TestData
        {
            Items =
            [
                new TestItem { Name = "备份测试", Value = 100 }
            ]
        };

        repo.Save(data);
        repo.Save(data);

        File.WriteAllText(filePath, "{ corrupted json !!!");

        var repo2 = new JsonFileRepository<TestData>(filePath);
        var loaded = repo2.Load();

        loaded.Should().NotBeNull();
        loaded.Items.Should().HaveCount(1);
        loaded.Items[0].Name.Should().Be("备份测试");
        loaded.Items[0].Value.Should().Be(100);
    }

    private class TestData
    {
        public List<TestItem> Items { get; set; } = [];
    }

    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
