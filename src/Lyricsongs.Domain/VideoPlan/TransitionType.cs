using System.Text.Json.Serialization;

namespace Lyricsongs.Domain.VideoPlan;

/// <summary>
/// Predefined background transitions mapped to FFmpeg xfade transition names.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransitionType
{
    None,
    Fade,
    Dissolve,
    WipeLeft,
    WipeRight,
    WipeUp,
    WipeDown,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    FadeBlack,
    FadeWhite,
    CircleOpen,
    CircleClose,
    Radial,
    SmoothLeft,
    SmoothRight,
    ZoomIn,
    Distance,
    Pixelize,
    DiagTopLeft,
    DiagTopRight,
    HorizontalOpen,
    VerticalOpen
}
