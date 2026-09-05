using Lyricsongs.Application.Jobs;
using Lyricsongs.Domain.Jobs;

namespace Lyricsongs.Infrastructure.Jobs;

public sealed class ProjectRenderValidator : IProjectRenderValidator
{
    public ProjectRenderValidationResult Validate(ProjectRecord project, IProjectFileStorage storage)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(project.AudioRelativePath))
        {
            errors.Add("Project has no audio file.");
        }
        else if (!File.Exists(storage.GetAbsolutePath(project.AudioRelativePath)))
        {
            errors.Add("Audio file is missing on disk.");
        }

        if (string.IsNullOrWhiteSpace(project.SrtRelativePath))
        {
            errors.Add("Project has no SRT lyrics file.");
        }
        else if (!File.Exists(storage.GetAbsolutePath(project.SrtRelativePath)))
        {
            errors.Add("SRT file is missing on disk.");
        }

        if (project.Assets.Count == 0)
        {
            errors.Add("Project needs at least one background image or video.");
        }
        else
        {
            foreach (var asset in project.Assets)
            {
                if (string.IsNullOrWhiteSpace(asset.RelativePath) ||
                    !File.Exists(storage.GetAbsolutePath(asset.RelativePath)))
                {
                    errors.Add($"Background asset '{asset.FileName}' is missing on disk.");
                }
            }
        }

        if (project.AudioDurationMs is null or <= 0)
        {
            errors.Add("Audio duration is unknown or invalid.");
        }

        return new ProjectRenderValidationResult { Errors = errors };
    }
}
