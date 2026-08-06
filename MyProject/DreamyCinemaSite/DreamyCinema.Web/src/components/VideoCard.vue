<script setup lang="ts">
import { AlertTriangle, CheckSquare, Edit3, ExternalLink, Play, Square } from "@lucide/vue";
import { computed, ref, watch } from "vue";
import { useSubtitleDisplay } from "@/composables/useSubtitleDisplay";
import type { VideoItem } from "@/types";
import { formatBytes, formatCodec, formatDate, formatDuration, formatResolution } from "@/utils/format";
import { preferredSubtitleId } from "@/utils/subtitles";

const props = defineProps<{
  video: VideoItem;
  inlineActive: boolean;
  selectable?: boolean;
  selected?: boolean;
}>();
const emit = defineEmits<{
  inlinePlay: [];
  inlineEnd: [];
  openPlayer: [];
  edit: [];
  toggleTag: [id: number];
  toggleSelection: [];
}>();

const player = ref<HTMLVideoElement | null>(null);
const selectedSubtitleId = ref("");
const softSubtitlesEnabled = computed(() => props.video.originalAvailable);
const subtitleTracks = computed(() => softSubtitlesEnabled.value ? props.video.subtitles : []);
const { activeSubtitleText, displayMode, syncSubtitleTracks } = useSubtitleDisplay(
  player,
  subtitleTracks,
  selectedSubtitleId
);

watch(
  () => props.video.subtitles.map(track => track.id).join("|"),
  () => {
    if (props.video.subtitles.some(track => track.id === selectedSubtitleId.value)) return;
    selectedSubtitleId.value = preferredSubtitleId(props.video.subtitles);
  },
  { immediate: true }
);
</script>

<template>
  <article class="video-card" :class="{ selected }">
    <div
      class="card-media-shell"
      :class="{ 'has-compat-caption': inlineActive && displayMode === 'below' && selectedSubtitleId }"
    >
      <div class="media-panel" :class="{ 'has-cover': video.coverUrl }">
        <button
          v-if="selectable"
          class="selection-toggle"
          type="button"
          :aria-label="selected ? `取消选择 ${video.number}` : `选择 ${video.number}`"
          :aria-pressed="selected"
          @click="emit('toggleSelection')"
        >
          <CheckSquare v-if="selected" :size="21" />
          <Square v-else :size="21" />
        </button>
        <video
          v-if="inlineActive && !selectable"
          ref="player"
          :src="video.streamUrl"
          :poster="video.coverUrl ?? undefined"
          controls
          autoplay
          playsinline
          preload="metadata"
          @loadedmetadata="syncSubtitleTracks"
          @ended="emit('inlineEnd')"
        >
          <track
            v-for="track in subtitleTracks"
            :key="track.id"
            :src="track.vttUrl"
            :label="track.label"
            :srclang="track.language"
            :data-track-id="track.id"
            kind="subtitles"
            :default="displayMode === 'native' && track.id === selectedSubtitleId"
            @load="syncSubtitleTracks"
          >
        </video>
        <p
          v-if="inlineActive && softSubtitlesEnabled && displayMode === 'overlay' && activeSubtitleText"
          class="subtitle-overlay"
          data-subtitle-mode="overlay"
        >{{ activeSubtitleText }}</p>
        <template v-if="!inlineActive || selectable">
          <img v-if="video.coverUrl" :src="video.coverUrl" :alt="`${video.number} 封面`" loading="lazy" decoding="async">
          <strong v-else class="media-fallback-title">{{ video.number }}</strong>
          <button v-if="!selectable" class="inline-play-button" type="button" :aria-label="`在当前页面播放 ${video.number}`" @click="emit('inlinePlay')">
            <Play :size="27" fill="currentColor" />
          </button>
        </template>
      </div>
      <div
        v-if="inlineActive && softSubtitlesEnabled && displayMode === 'below' && selectedSubtitleId"
        class="subtitle-compat-bar"
        data-subtitle-mode="below"
        aria-label="兼容字幕"
      ><span>{{ activeSubtitleText || "\u00a0" }}</span></div>
    </div>

    <div class="video-info">
      <h2>番号：{{ video.number }}</h2>
      <p class="video-description">
        <template v-if="video.title && video.title !== video.number">标题：{{ video.title }}<br></template>
        简介：{{ video.description || "暂无简介" }}
      </p>
      <div class="card-tags">
        <button v-for="tag in video.tags" :key="tag.id" type="button" @click="emit('toggleTag', tag.id)">#{{ tag.name }}</button>
        <span v-if="!video.tags.length">#未分类</span>
      </div>
      <div class="video-tech-meta">
        <span>时长 {{ formatDuration(video.durationSeconds) }}</span>
        <span>{{ formatResolution(video.width, video.height) }}</span>
        <span>{{ formatCodec(video.videoCodec) }}</span>
        <span v-if="video.subtitles.length">字幕 {{ video.subtitles.length }} 轨</span>
        <span v-if="video.hardSubtitle">硬字幕版</span>
      </div>
      <p v-if="video.playbackWarning" class="codec-warning"><AlertTriangle :size="14" />{{ video.playbackWarning }}</p>
      <div class="video-meta">
        <span>日期：{{ formatDate(video.createdAt || video.importedAt) }}</span>
        <span>大小：{{ formatBytes(video.originalAvailable ? video.size : (video.hardSubtitle?.size ?? video.size)) }}</span>
        <div v-if="!selectable" class="card-actions">
          <button class="icon-text-button" type="button" aria-label="打开播放页" @click="emit('openPlayer')"><ExternalLink :size="15" /> 播放页</button>
          <button class="icon-text-button" type="button" @click="emit('edit')"><Edit3 :size="15" /> 编辑</button>
        </div>
      </div>
    </div>
  </article>
</template>
