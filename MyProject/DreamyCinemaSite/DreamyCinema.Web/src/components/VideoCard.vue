<script setup lang="ts">
import { Edit3, ExternalLink, Play } from "@lucide/vue";
import type { VideoItem } from "@/types";
import { formatBytes, formatCodec, formatDate, formatDuration, formatResolution } from "@/utils/format";

defineProps<{ video: VideoItem; inlineActive: boolean }>();
const emit = defineEmits<{
  inlinePlay: [];
  inlineEnd: [];
  openPlayer: [];
  edit: [];
  toggleTag: [id: number];
}>();
</script>

<template>
  <article class="video-card">
    <div class="media-panel" :class="{ 'has-cover': video.coverUrl }">
      <video
        v-if="inlineActive"
        :src="video.streamUrl"
        :poster="video.coverUrl ?? undefined"
        controls
        autoplay
        playsinline
        preload="metadata"
        @ended="emit('inlineEnd')"
      >
        <track
          v-for="track in video.subtitles"
          :key="track.id"
          :src="track.vttUrl"
          :label="track.label"
          :srclang="track.language"
          kind="subtitles"
          :default="track.isDefault"
        >
      </video>
      <template v-else>
        <img v-if="video.coverUrl" :src="video.coverUrl" :alt="`${video.number} 封面`" loading="lazy" decoding="async">
        <strong v-else class="media-fallback-title">{{ video.number }}</strong>
        <button class="inline-play-button" type="button" :aria-label="`在当前页面播放 ${video.number}`" @click="emit('inlinePlay')">
          <Play :size="27" fill="currentColor" />
        </button>
      </template>
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
      </div>
      <div class="video-meta">
        <span>日期：{{ formatDate(video.createdAt || video.importedAt) }}</span>
        <span>大小：{{ formatBytes(video.size) }}</span>
        <div class="card-actions">
          <button class="icon-text-button" type="button" aria-label="打开播放页" @click="emit('openPlayer')"><ExternalLink :size="15" /> 播放页</button>
          <button class="icon-text-button" type="button" @click="emit('edit')"><Edit3 :size="15" /> 编辑</button>
        </div>
      </div>
    </div>
  </article>
</template>
