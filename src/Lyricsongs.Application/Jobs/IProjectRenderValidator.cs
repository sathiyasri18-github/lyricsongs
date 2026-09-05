using Lyricsongs.Domain.Jobs;

namespace Lyricsongs.Application.Jobs;

public interface IProjectRenderValidator
{
    ProjectRenderValidationResult Validate(ProjectRecord project, IProjectFileStorage storage);
}

public sealed class ProjectRenderValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; init; } = [];
}
