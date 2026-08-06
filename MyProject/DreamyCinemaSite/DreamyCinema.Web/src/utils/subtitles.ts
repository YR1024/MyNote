import type { SubtitleTrack } from "@/types";

export type SubtitleDisplayMode = "native" | "overlay" | "below";

const subtitlePreferenceKey = "dreamy.subtitle.preference";
const subtitleDisplayModeKey = "dreamy.subtitle.display-mode";

export function preferredSubtitleId(tracks: SubtitleTrack[]) {
  try {
    const stored = JSON.parse(window.localStorage.getItem(subtitlePreferenceKey) ?? "null") as {
      language?: string;
      kind?: string;
    } | null;
    if (stored?.language) {
      const matches = tracks.filter(track => track.language === stored.language && (!stored.kind || track.kind === stored.kind));
      const reviewed = matches.find(track => track.revisionStage === "FinalPolished");
      if (reviewed) return reviewed.id;
      const latestMatch = matches.at(-1);
      if (latestMatch) return latestMatch.id;
    }
  } catch {
    // Invalid device-local preferences should not block playback.
  }
  return tracks.find(track => track.isDefault)?.id ?? "";
}

export function rememberSubtitlePreference(track: SubtitleTrack | undefined) {
  if (!track) return;
  window.localStorage.setItem(subtitlePreferenceKey, JSON.stringify({
    language: track.language,
    kind: track.kind
  }));
}

export function loadSubtitleDisplayMode(): SubtitleDisplayMode {
  const stored = window.localStorage.getItem(subtitleDisplayModeKey);
  if (stored === "native" || stored === "overlay" || stored === "below") return stored;

  const touchDevice = navigator.maxTouchPoints > 0 || window.matchMedia("(pointer: coarse)").matches;
  return touchDevice ? "below" : "overlay";
}

export function rememberSubtitleDisplayMode(mode: SubtitleDisplayMode) {
  window.localStorage.setItem(subtitleDisplayModeKey, mode);
}
