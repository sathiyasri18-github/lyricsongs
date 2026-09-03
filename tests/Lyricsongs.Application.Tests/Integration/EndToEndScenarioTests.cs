using Lyricsongs.Application.Jobs;
using Lyricsongs.Application.Projects;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Domain.Jobs;
using Lyricsongs.Domain.VideoPlan;
using Lyricsongs.Infrastructure.Jobs;
using Lyricsongs.Infrastructure.Projects;
using Lyricsongs.Infrastructure.Storage;
using Lyricsongs.Infrastructure.Subtitles;
using Lyricsongs.Infrastructure.VideoPlan;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyricsongs.Application.Tests.Integration;

/// <summary>
/// Validates the full planning pipeline for the documented test scenario
/// (song.mp3 + lyrics.srt + 4 images + 2 videos → 1920×1080 plan).
/// </summary>
public sealed class EndToEndScenarioTests
{
    [Fact]
    public async Task VideoPlan_Builds1080pTimeline_ForMixedAssets()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyricsongs-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var storage = new ProjectFileStorage(new TestConfiguration(root), new TestHostEnvironment(root));
            var projectStore = new FileProjectStore(storage);
            var srtParser = new SrtParser();
            var srtValidation = new SrtValidationService();
            var srtProcessing = new SrtProcessingService(srtParser, srtValidation);
            var planner = new BackgroundPlanner();
            var styleManager = new LyricStyleManager();
            var validator = new VideoPlanValidator();
            var planEngine = new VideoPlanEngine(planner, styleManager, validator);
            var planService = new ProjectPlanService(
                projectStore,
                storage,
                new ProjectRenderValidator(),
                srtProcessing,
                planEngine);

            var project = await projectStore.CreateAsync("Test Scenario");

            var assetDefs = new[]
            {
                ("bg1", "bg1.jpg", "image", 0, (long?)null),
                ("bg2", "bg2.jpg", "image", 1, null),
                ("bg3", "bg3.jpg", "image", 2, null),
                ("bg4", "bg4.jpg", "image", 3, null),
                ("clip1", "clip1.mp4", "video", 4, 15_000L),
                ("clip2", "clip2.mp4", "video", 5, 12_000L)
            };

            project.Assets = [];
            foreach (var (id, fileName, type, sort, duration) in assetDefs)
            {
                var relative = $"projects/{project.Id}/{type}s/{fileName}";
                Directory.CreateDirectory(Path.GetDirectoryName(storage.GetAbsolutePath(relative))!);
                await File.WriteAllBytesAsync(storage.GetAbsolutePath(relative), [0x01]);
                project.Assets.Add(new ProjectAssetRecord
                {
                    Id = id,
                    FileName = fileName,
                    RelativePath = relative,
                    Type = type,
                    SortOrder = sort,
                    SourceDurationMs = duration
                });
            }

            project.SrtRelativePath = $"projects/{project.Id}/srt/lyrics.srt";
            project.AudioRelativePath = $"projects/{project.Id}/audio/song.mp3";
            var srtContent = """
                1
                00:00:05,000 --> 00:00:09,000
                என்னை விட்டு போகாதே

                2
                00:00:09,000 --> 00:00:13,500
                என் உயிரே நீயே
                """;
            Directory.CreateDirectory(Path.GetDirectoryName(storage.GetAbsolutePath(project.SrtRelativePath))!);
            await File.WriteAllTextAsync(storage.GetAbsolutePath(project.SrtRelativePath), srtContent);
            Directory.CreateDirectory(Path.GetDirectoryName(storage.GetAbsolutePath(project.AudioRelativePath))!);
            await File.WriteAllBytesAsync(storage.GetAbsolutePath(project.AudioRelativePath), [0x01]);
            project.SrtFileName = "lyrics.srt";
            project.AudioFileName = "song.mp3";
            project.AudioDurationMs = 245_000;
            project.SettingsJson = System.Text.Json.JsonSerializer.Serialize(new VideoPlanSettings
            {
                ResolutionPreset = VideoResolutionPreset.FullHd1080p,
                Fps = 30,
                LyricStyle = Domain.Lyrics.LyricAnimationStyle.Cinematic,
                FontOverride = "Noto Sans Tamil"
            });
            await projectStore.SaveAsync(project);

            var plan = await planService.BuildAsync(project.Id);

            Assert.True(plan.IsValid);
            Assert.Equal(1920, plan.Output.Width);
            Assert.Equal(1080, plan.Output.Height);
            Assert.Equal(30, plan.Output.Fps);
            Assert.Equal("libx264", plan.Output.VideoCodec);
            Assert.Equal("aac", plan.Output.AudioCodec);
            Assert.Equal(6, plan.Timeline.Count);
            Assert.Contains(plan.Lyrics, l => l.Text.Contains("என்னை"));
            Assert.Equal("Noto Sans Tamil", plan.LyricsStyle.Font);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class TestConfiguration : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly string _root;
        public TestConfiguration(string root) => _root = root;
        public string? this[string key] { get => key == "Storage:Root" ? _root : null; set { } }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
            new Microsoft.Extensions.Primitives.CancellationChangeToken(CancellationToken.None);
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) =>
            new TestSection(this, key);
    }

    private sealed class TestSection : Microsoft.Extensions.Configuration.IConfigurationSection
    {
        private readonly TestConfiguration _parent;
        public TestSection(TestConfiguration parent, string key) { _parent = parent; Key = key; }
        public string Key { get; }
        public string Path => Key;
        public string? Value { get; set; }
        public string? this[string key] { get => _parent[key]; set => _parent[key] = value; }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
            new Microsoft.Extensions.Primitives.CancellationChangeToken(CancellationToken.None);
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) =>
            new TestSection(_parent, key);
    }

    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public TestHostEnvironment(string root) => ContentRootPath = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
