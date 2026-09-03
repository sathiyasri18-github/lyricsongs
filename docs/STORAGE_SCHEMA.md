# Storage schema (file-based persistence)

Lyricsongs uses JSON files on disk instead of a SQL database. Default root: `{ApiContentRoot}/storage/`.

## Directory layout

```
storage/
├── projects/
│   └── {projectId}/
│       ├── project.json          # Project metadata + asset index
│       ├── audio/                # Uploaded MP3
│       ├── srt/                  # Uploaded SRT
│       ├── images/               # Background JPG/PNG
│       ├── videos/               # Background MP4 clips
│       └── renders/
│           └── {jobId}.mp4       # Final output (e.g. song-lyric-video.mp4)
├── jobs/
│   └── {jobId}.json              # Render job state (restart-safe)
└── temp/
    └── jobs/
        └── {jobId}/              # FFmpeg workspace (cleaned after render)
```

## `project.json`

```json
{
  "id": "a1b2c3…",
  "name": "Ennai Vittu",
  "createdAt": "2026-08-23T08:00:00Z",
  "updatedAt": "2026-08-23T08:05:00Z",
  "audioRelativePath": "projects/{id}/audio/{guid}_song.mp3",
  "audioFileName": "song.mp3",
  "audioDurationMs": 252000,
  "srtRelativePath": "projects/{id}/srt/{guid}_lyrics.srt",
  "srtFileName": "lyrics.srt",
  "lyricCount": 128,
  "settingsJson": "{ … VideoPlanSettings … }",
  "assets": [
    {
      "id": "asset-guid",
      "fileName": "bg1.jpg",
      "relativePath": "projects/{id}/images/{guid}_bg1.jpg",
      "type": "image",
      "sourceDurationMs": null,
      "sortOrder": 0
    }
  ]
}
```

## `jobs/{jobId}.json`

```json
{
  "id": "job-guid",
  "projectId": "project-guid",
  "projectName": "Ennai Vittu",
  "status": "Rendering",
  "progressPercent": 67,
  "message": "Rendering lyric animation…",
  "cancelRequested": false,
  "outputFilePath": "…/renders/{jobId}.mp4",
  "outputFileName": "song-lyric-video.mp4",
  "createdAt": "…",
  "startedAt": "…",
  "updatedAt": "…"
}
```

## Job status enum

`Queued` → `Preparing` → `Rendering` → `Completed` | `Failed` | `Cancelled`

On API restart, incomplete jobs are reset to `Queued` and re-enqueued.
