# Lyricsongs FFmpeg render pipeline — example generated commands
# Multi-stage pipeline (not a single monolithic command)

# ── Stage 1: Render image segment with Ken Burns zoom ──
ffmpeg -nostdin -progress pipe:1 -nostats -hide_banner -y \
  -loop 1 -i bg1.jpg \
  -t 42.500 \
  -vf "scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,zoompan=z='if(lte(zoom,1.0),1.0+on*0.0006,zoom)':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1275:s=1920x1080:fps=30" \
  -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -r 30 -an \
  seg_001.mp4

# ── Stage 2: Render video segment (trim/loop + scale/pad) ──
ffmpeg -nostdin -progress pipe:1 -nostats -hide_banner -y \
  -i clip1.mp4 \
  -t 35.000 \
  -vf "scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1,fps=30" \
  -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -an \
  seg_002.mp4

# ── Stage 3a: Compose background (concat, no transitions) ──
# concat_list.txt:
#   file 'seg_001.mp4'
#   file 'seg_002.mp4'
ffmpeg -nostdin -progress pipe:1 -nostats -hide_banner -y \
  -f concat -safe 0 -i concat_list.txt \
  -c copy background.mp4

# ── Stage 3b: Compose background (xfade transitions) ──
# xfade_filter.txt:
#   [0:v][1:v]xfade=transition=fade:duration=0.500:offset=42.000[v1];[v1][2:v]xfade=transition=fade:duration=0.500:offset=77.000[vout]
ffmpeg -nostdin -progress pipe:1 -nostats -hide_banner -y \
  -i seg_001.mp4 -i seg_002.mp4 -i seg_003.mp4 \
  -filter_complex_script xfade_filter.txt \
  -map [vout] -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -r 30 -t 245.000 -an \
  background.mp4

# ── Stage 4: Overlay ASS lyrics (Tamil Unicode) + mux MP3 audio ──
# lyrics.ass written separately (UTF-8 BOM) with Noto Sans Tamil, fade/karaoke tags
ffmpeg -nostdin -progress pipe:1 -nostats -hide_banner -y \
  -i background.mp4 -i song.mp3 \
  -vf "ass='lyrics.ass'" \
  -map 0:v:0 -map 1:a:0 \
  -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p \
  -c:a aac -b:a 192k \
  -shortest -movflags +faststart -f mp4 \
  output.mp4

# Output: 1920x1080, 30 FPS, H.264 (libx264), AAC, MP4
