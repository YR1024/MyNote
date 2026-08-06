import { nextTick, onBeforeUnmount, ref, toValue, watch, type MaybeRefOrGetter, type Ref } from "vue";
import type { SubtitleTrack } from "@/types";
import {
  loadSubtitleDisplayMode,
  rememberSubtitleDisplayMode,
  type SubtitleDisplayMode
} from "@/utils/subtitles";

type CueWithText = TextTrackCue & { text?: string };
type CueWithHtml = CueWithText & { getCueAsHTML?: () => DocumentFragment };

export function useSubtitleDisplay(
  player: Ref<HTMLVideoElement | null>,
  tracks: MaybeRefOrGetter<SubtitleTrack[]>,
  selectedSubtitleId: Ref<string>
) {
  const displayMode = ref<SubtitleDisplayMode>(loadSubtitleDisplayMode());
  const activeSubtitleText = ref("");
  let boundPlayer: HTMLVideoElement | null = null;
  let boundTrack: TextTrack | null = null;

  function cueText(cue: TextTrackCue) {
    const htmlCue = cue as CueWithHtml;
    if (typeof htmlCue.getCueAsHTML === "function") {
      return htmlCue.getCueAsHTML().textContent?.trim() ?? "";
    }
    return String(htmlCue.text ?? "").trim();
  }

  function updateActiveSubtitle() {
    if (displayMode.value === "native" || !boundTrack?.activeCues) {
      activeSubtitleText.value = "";
      return;
    }

    activeSubtitleText.value = Array.from(boundTrack.activeCues)
      .map(cueText)
      .filter(Boolean)
      .join("\n");
  }

  function unbindTrack() {
    boundTrack?.removeEventListener("cuechange", updateActiveSubtitle);
    boundTrack = null;
  }

  function unbindPlayer() {
    if (!boundPlayer) return;
    boundPlayer.removeEventListener("timeupdate", updateActiveSubtitle);
    boundPlayer.removeEventListener("seeking", updateActiveSubtitle);
    boundPlayer.removeEventListener("emptied", updateActiveSubtitle);
    boundPlayer = null;
  }

  function bindPlayer(element: HTMLVideoElement) {
    if (boundPlayer === element) return;
    unbindPlayer();
    boundPlayer = element;
    boundPlayer.addEventListener("timeupdate", updateActiveSubtitle);
    boundPlayer.addEventListener("seeking", updateActiveSubtitle);
    boundPlayer.addEventListener("emptied", updateActiveSubtitle);
  }

  function syncSubtitleTracks() {
    const element = player.value;
    unbindTrack();
    if (!element) {
      unbindPlayer();
      activeSubtitleText.value = "";
      return;
    }

    bindPlayer(element);
    const metadata = toValue(tracks);
    const browserTracks = Array.from(element.textTracks);
    const selectedIndex = metadata.findIndex(track => track.id === selectedSubtitleId.value);

    browserTracks.forEach((textTrack, index) => {
      const selected = index === selectedIndex;
      textTrack.mode = selected
        ? (displayMode.value === "native" ? "showing" : "hidden")
        : "disabled";
    });

    if (displayMode.value !== "native" && selectedIndex >= 0) {
      boundTrack = browserTracks[selectedIndex] ?? null;
      boundTrack?.addEventListener("cuechange", updateActiveSubtitle);
    }
    updateActiveSubtitle();
  }

  watch(
    () => [
      player.value,
      selectedSubtitleId.value,
      displayMode.value,
      toValue(tracks).map(track => track.id).join("|")
    ],
    async () => {
      await nextTick();
      syncSubtitleTracks();
    },
    { immediate: true }
  );

  watch(displayMode, rememberSubtitleDisplayMode);

  onBeforeUnmount(() => {
    unbindTrack();
    unbindPlayer();
  });

  return {
    activeSubtitleText,
    displayMode,
    syncSubtitleTracks
  };
}
