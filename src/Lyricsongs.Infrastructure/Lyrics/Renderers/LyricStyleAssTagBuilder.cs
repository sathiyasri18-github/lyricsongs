using Lyricsongs.Domain.Lyrics;
using Lyricsongs.Domain.Subtitles;
using Lyricsongs.Infrastructure.Lyrics.Ass;

namespace Lyricsongs.Infrastructure.Lyrics.Renderers;

internal static class LyricStyleAssTagBuilder
{
    public static string BuildOverrideTags(LyricAnimationStyle style, LyricCue cue, LyricStyleConfiguration config)
    {
        var fadeIn = ClampFade(config.FadeInMs, cue.DurationMs);
        var fadeOut = ClampFade(config.FadeOutMs, cue.DurationMs);
        var (x, y) = AssLayoutHelper.GetAnchorPoint(config);
        var half = Math.Max(1, cue.DurationMs / 2);
        var quarter = Math.Max(1, cue.DurationMs / 4);
        var anim = Math.Min(cue.DurationMs, 500);
        var shortAnim = Math.Min(cue.DurationMs, 350);

        return style switch
        {
            LyricAnimationStyle.Classic => $"\\fad({fadeIn},{fadeOut})",
            LyricAnimationStyle.Minimal => $"\\fad({fadeIn},{fadeOut})",
            LyricAnimationStyle.Bold => $"\\fad({fadeIn},{fadeOut})\\bord{Math.Max(config.OutlineWidth, 4)}",
            LyricAnimationStyle.Karaoke => "\\q2",
            LyricAnimationStyle.Pop => BuildPopTags(cue, config),
            LyricAnimationStyle.Cinematic => $"\\fad({fadeIn},{fadeOut})\\move({x},{y + config.CinematicRisePixels},{x},{y})",
            LyricAnimationStyle.Neon => $"\\fad({fadeIn},{fadeOut})\\blur2\\bord{Math.Max(config.OutlineWidth, 2)}",
            LyricAnimationStyle.Glow => $"\\fad({fadeIn},{fadeOut})\\blur3\\shad{Math.Max(config.ShadowDepth, 4)}",
            LyricAnimationStyle.Bounce => BuildScaleTags(cue, config, 70, 110, 75),
            LyricAnimationStyle.SlideUp => $"\\fad({fadeIn},{fadeOut})\\move({x},{y + 80},{x},{y})",
            LyricAnimationStyle.SlideDown => $"\\fad({fadeIn},{fadeOut})\\move({x},{y - 80},{x},{y})",
            LyricAnimationStyle.SlideLeft => $"\\fad({fadeIn},{fadeOut})\\move({x + 120},{y},{x},{y})",
            LyricAnimationStyle.SlideRight => $"\\fad({fadeIn},{fadeOut})\\move({x - 120},{y},{x},{y})",
            LyricAnimationStyle.Typewriter => $"\\fad(0,{fadeOut})\\alpha&HFF&\\t(0,{Math.Min(cue.DurationMs, 1200)},\\alpha&H00&)",
            LyricAnimationStyle.Wave => $"\\fad({fadeIn},{fadeOut})\\move({x},{y + 12},{x},{y})\\t(0,{cue.DurationMs},\\move({x},{y},{x},{y - 8}))",
            LyricAnimationStyle.Pulse => $"\\fad({fadeIn},{fadeOut})\\fscx100\\fscy100\\t(0,{half},\\fscx108\\fscy108)\\t({half},{cue.DurationMs},\\fscx100\\fscy100)",
            LyricAnimationStyle.Shake => $"\\fad({fadeIn},{fadeOut})\\move({x - 8},{y},{x + 8},{y})\\t(0,{half},\\move({x + 8},{y},{x - 8},{y}))",
            LyricAnimationStyle.Rotate => $"\\fad({fadeIn},{fadeOut})\\frz-8\\t(0,{Math.Min(cue.DurationMs, 400)},\\frz0)",
            LyricAnimationStyle.BlurIn => $"\\blur8\\fad({fadeIn},{fadeOut})\\t(0,{Math.Min(cue.DurationMs, 500)},\\blur0)",
            LyricAnimationStyle.Shadow => $"\\fad({fadeIn},{fadeOut})\\shad{Math.Max(config.ShadowDepth, 6)}",
            LyricAnimationStyle.Outline => $"\\fad({fadeIn},{fadeOut})\\bord{Math.Max(config.OutlineWidth, 6)}",
            LyricAnimationStyle.Gradient => "\\q2",
            LyricAnimationStyle.Retro => BuildScaleTags(cue, config, 95, 100, 200),
            LyricAnimationStyle.Elegant => $"\\fad({Math.Max(fadeIn, 400)},{Math.Max(fadeOut, 400)})",

            LyricAnimationStyle.ZoomIn => $"\\fad({fadeIn},{fadeOut})\\fscx150\\fscy150\\t(0,{anim},\\fscx100\\fscy100)",
            LyricAnimationStyle.ZoomOut => $"\\fad({fadeIn},{fadeOut})\\fscx40\\fscy40\\t(0,{anim},\\fscx100\\fscy100)",
            LyricAnimationStyle.DropIn => $"\\fad({fadeIn},{fadeOut})\\move({x},{y - 140},{x},{y})\\fscx120\\fscy120\\t(0,{shortAnim},\\fscx100\\fscy100)",
            LyricAnimationStyle.FlipIn => $"\\fad({fadeIn},{fadeOut})\\frx-70\\t(0,{anim},\\frx0)",
            LyricAnimationStyle.Spin => $"\\fad({fadeIn},{fadeOut})\\frz-180\\t(0,{Math.Min(cue.DurationMs, 600)},\\frz0)",
            LyricAnimationStyle.Drift => $"\\fad({fadeIn},{fadeOut})\\move({x - 40},{y},{x + 40},{y})",
            LyricAnimationStyle.Float => $"\\fad({fadeIn},{fadeOut})\\move({x},{y + 24},{x},{y - 16})",
            LyricAnimationStyle.Elastic => BuildScaleTags(cue, config, 50, 125, 120),
            LyricAnimationStyle.Slam => $"\\fad({fadeIn},{fadeOut})\\fscx180\\fscy180\\t(0,{Math.Min(cue.DurationMs, 180)},\\fscx100\\fscy100)",
            LyricAnimationStyle.Whisper => $"\\fad({Math.Max(fadeIn, 350)},{Math.Max(fadeOut, 350)})\\alpha&H40&\\t(0,{half},\\alpha&H00&)\\t({half},{cue.DurationMs},\\alpha&H40&)",
            LyricAnimationStyle.SoftGlow => $"\\fad({fadeIn},{fadeOut})\\blur5\\shad{Math.Max(config.ShadowDepth, 5)}\\t(0,{anim},\\blur1)",
            LyricAnimationStyle.HardCut => "\\fad(0,0)",
            LyricAnimationStyle.Glitch => $"\\fad({fadeIn},{fadeOut})\\move({x - 14},{y},{x + 14},{y})\\t(0,{quarter},\\alpha&H60&)\\t({quarter},{half},\\alpha&H00&)\\t({half},{cue.DurationMs},\\move({x + 14},{y},{x - 10},{y}))",
            LyricAnimationStyle.RiseFade => $"\\fad({Math.Max(fadeIn, 450)},{Math.Max(fadeOut, 450)})\\move({x},{y + 60},{x},{y})",
            LyricAnimationStyle.Cascade => $"\\fad({fadeIn},{fadeOut})\\move({x + 90},{y - 70},{x},{y})",
            LyricAnimationStyle.SoftSlide => $"\\fad({fadeIn},{fadeOut})\\move({x},{y + 36},{x},{y})",
            LyricAnimationStyle.Instant => "\\fad(0,0)\\bord2",
            LyricAnimationStyle.SlowReveal => $"\\fad(0,{fadeOut})\\alpha&HFF&\\t(0,{Math.Min(cue.DurationMs, 1600)},\\alpha&H00&)",
            LyricAnimationStyle.Swing => $"\\fad({fadeIn},{fadeOut})\\frz12\\t(0,{half},\\frz-10)\\t({half},{cue.DurationMs},\\frz0)",
            LyricAnimationStyle.ZoomPulse => $"\\fad({fadeIn},{fadeOut})\\fscx130\\fscy130\\t(0,{quarter},\\fscx100\\fscy100)\\t({quarter},{half},\\fscx108\\fscy108)\\t({half},{cue.DurationMs},\\fscx100\\fscy100)",
            LyricAnimationStyle.Echo => $"\\fad({fadeIn},{fadeOut})\\shad{Math.Max(config.ShadowDepth, 8)}\\blur1",
            LyricAnimationStyle.Spotlight => $"\\fad({fadeIn},{fadeOut})\\blur6\\t(0,{anim},\\blur0)\\bord{Math.Max(config.OutlineWidth, 3)}",

            _ => $"\\fad({fadeIn},{fadeOut})"
        };
    }

    public static bool IsPreformatted(LyricAnimationStyle style) =>
        style is LyricAnimationStyle.Karaoke or LyricAnimationStyle.Gradient;

    public static string BuildDialogueText(LyricAnimationStyle style, LyricCue cue, LyricStyleConfiguration config) =>
        style switch
        {
            LyricAnimationStyle.Karaoke => KaraokeTextBuilder.Build(cue.Text, cue.DurationMs, config.KaraokeSplit),
            LyricAnimationStyle.Gradient => KaraokeTextBuilder.Build(cue.Text, cue.DurationMs, KaraokeSplitMode.Word),
            _ => cue.Text
        };

    private static string BuildPopTags(LyricCue cue, LyricStyleConfiguration config)
    {
        var animMs = Math.Min(config.PopAnimationMs, cue.DurationMs / 2);
        return $"\\fad({config.FadeInMs},0)\\fscx{config.PopStartScalePercent}\\fscy{config.PopStartScalePercent}\\t(0,{animMs},\\fscx{config.PopEndScalePercent}\\fscy{config.PopEndScalePercent})";
    }

    private static string BuildScaleTags(
        LyricCue cue,
        LyricStyleConfiguration config,
        int start,
        int peak,
        int animMs)
    {
        var duration = Math.Min(animMs, cue.DurationMs / 2);
        return $"\\fad({config.FadeInMs},{config.FadeOutMs})\\fscx{start}\\fscy{start}\\t(0,{duration},\\fscx{peak}\\fscy{peak})\\t({duration},{cue.DurationMs},\\fscx100\\fscy100)";
    }

    private static int ClampFade(int fadeMs, long cueDurationMs)
    {
        var max = (int)Math.Max(0, cueDurationMs / 3);
        return Math.Min(fadeMs, max);
    }
}
