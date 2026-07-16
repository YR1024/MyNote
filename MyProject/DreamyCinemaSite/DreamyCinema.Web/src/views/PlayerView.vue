<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { ArrowLeft, Edit3, Languages, RotateCcw, Sparkles, X } from "@lucide/vue";
import { apiRequest, loadSession } from "@/api/client";
import type { AiSubtitleJobResult, AiSubtitleStatus, MediaJob, SubtitleTrack, TagCategory, VideoItem } from "@/types";
import VideoEditor from "@/components/VideoEditor.vue";
import { formatBytes, formatCodec, formatDate, formatDuration, formatResolution } from "@/utils/format";

const props = defineProps<{ id: string }>();
const router = useRouter();
const video = ref<VideoItem | null>(null);
const categories = ref<TagCategory[]>([]);
const editing = ref(false);
const status = ref("正在加载视频...");
const player = ref<HTMLVideoElement | null>(null);
const selectedSubtitleId = ref("");
const aiStatus = ref<AiSubtitleStatus | null>(null);
const aiJob = ref<MediaJob | null>(null);
let aiPollSequence = 0;

const aiJobActive = computed(() => aiJob.value?.status === "Queued" || aiJob.value?.status === "Running");
const hasTranslatedTrack = computed(() => video.value?.subtitles.some(track => track.kind === "Translated") ?? false);
const sourceTrack = computed<SubtitleTrack | null>(() => {
  if (!video.value) return null;
  const selected = video.value.subtitles.find(track => track.id === selectedSubtitleId.value);
  if (selected && selected.kind !== "Translated" && !selected.language.toLowerCase().startsWith("zh")) return selected;
  return video.value.subtitles.find(track => track.kind === "Original" && !track.language.toLowerCase().startsWith("zh")) ?? null;
});

watch(selectedSubtitleId, applySubtitleSelection);

onMounted(load);
onUnmounted(() => { aiPollSequence++; });

async function load() {
  try {
    const session = await loadSession();
    if (!session.authenticated) {
      await router.replace("/login");
      return;
    }
    const [loadedVideo, loadedCategories, loadedAiStatus, jobs] = await Promise.all([
      apiRequest<VideoItem>(`/api/videos/${encodeURIComponent(props.id)}`),
      apiRequest<TagCategory[]>("/api/tag-categories"),
      apiRequest<AiSubtitleStatus>("/api/ai-subtitles/status"),
      apiRequest<MediaJob[]>("/api/jobs")
    ]);
    video.value = loadedVideo;
    categories.value = loadedCategories;
    aiStatus.value = loadedAiStatus;
    aiJob.value = jobs.find(job => job.videoId === props.id
      && (job.type === "SpeechRecognition" || job.type === "SubtitleTranslation")) ?? null;
    selectedSubtitleId.value = video.value.subtitles.find(track => track.isDefault)?.id ?? "";
    await nextTick();
    applySubtitleSelection();
    status.value = "";
    if (aiJobActive.value && aiJob.value) void waitForAiJob(aiJob.value.id);
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
}

async function refreshVideo(preferredTrackId?: string) {
  video.value = await apiRequest<VideoItem>(`/api/videos/${encodeURIComponent(props.id)}`);
  if (preferredTrackId && video.value.subtitles.some(track => track.id === preferredTrackId)) {
    selectedSubtitleId.value = preferredTrackId;
  }
  await nextTick();
  applySubtitleSelection();
}

async function startSpeechRecognition() {
  aiJob.value = await apiRequest<MediaJob>(`/api/videos/${encodeURIComponent(props.id)}/subtitles/transcribe`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ language: null })
  });
  await waitForAiJob(aiJob.value.id);
}

async function startTranslation() {
  if (!sourceTrack.value) return;
  const force = hasTranslatedTrack.value ? "?force=true" : "";
  aiJob.value = await apiRequest<MediaJob>(
    `/api/videos/${encodeURIComponent(props.id)}/subtitles/${encodeURIComponent(sourceTrack.value.id)}/translate${force}`,
    { method: "POST" }
  );
  await waitForAiJob(aiJob.value.id);
}

async function waitForAiJob(id: string) {
  const sequence = ++aiPollSequence;
  while (sequence === aiPollSequence) {
    const job = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(id)}`);
    if (sequence !== aiPollSequence) return;
    aiJob.value = job;
    if (job.status === "Completed") {
      const result = job.result as AiSubtitleJobResult | null;
      await refreshVideo(result?.trackId);
      return;
    }
    if (job.status === "Failed" || job.status === "Cancelled") return;
    await new Promise(resolve => window.setTimeout(resolve, 500));
  }
}

async function cancelAiJob() {
  if (!aiJob.value || !aiJobActive.value) return;
  aiJob.value = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(aiJob.value.id)}/cancel`, { method: "POST" });
}

async function retryAiJob() {
  if (!aiJob.value || !["Failed", "Cancelled"].includes(aiJob.value.status)) return;
  aiJob.value = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(aiJob.value.id)}/retry`, { method: "POST" });
  await waitForAiJob(aiJob.value.id);
}

function aiProviderLabel(provider: AiSubtitleStatus["speech"]) {
  const model = provider.model ? ` · ${provider.model}` : "";
  return `${provider.provider}${model}${provider.mock ? "（模拟）" : ""}`;
}

function applySubtitleSelection() {
  if (!player.value || !video.value) return;
  Array.from(player.value.textTracks).forEach((textTrack, index) => {
    textTrack.mode = video.value?.subtitles[index]?.id === selectedSubtitleId.value ? "showing" : "disabled";
  });
}
</script>

<template>
  <main class="player-page">
    <header class="player-nav">
      <button class="icon-text-button" type="button" @click="router.push('/')"><ArrowLeft :size="18" />返回片库</button>
      <button v-if="video" class="icon-text-button" type="button" @click="editing = true"><Edit3 :size="16" />编辑</button>
    </header>

    <p v-if="status" class="empty-copy">{{ status }}</p>
    <template v-if="video">
      <section class="player-stage">
        <video ref="player" :src="video.streamUrl" controls autoplay playsinline preload="metadata" @loadedmetadata="applySubtitleSelection">
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
      </section>
      <article class="player-details">
        <label v-if="video.subtitles.length" class="player-subtitle-control">
          <span>字幕</span>
          <select v-model="selectedSubtitleId" aria-label="字幕轨道">
            <option value="">关闭字幕</option>
            <option v-for="track in video.subtitles" :key="track.id" :value="track.id">{{ track.label }} · {{ track.cueCount }} 条</option>
          </select>
        </label>
        <section v-if="aiStatus" class="ai-subtitle-panel">
          <div class="ai-subtitle-heading">
            <div><p class="eyebrow">AI SUBTITLES</p><h2>AI 字幕</h2></div>
            <span :class="{ ready: aiStatus.enabled }">{{ aiStatus.enabled ? "已配置" : "未启用" }}</span>
          </div>
          <p v-if="!aiStatus.enabled" class="ai-subtitle-copy">当前电脑默认关闭 AI。回家部署模型后，通过配置启用本地 Provider。</p>
          <p v-else class="ai-subtitle-copy">
            识别：{{ aiProviderLabel(aiStatus.speech) }}<br>
            翻译：{{ aiProviderLabel(aiStatus.translation) }}
          </p>
          <div class="ai-subtitle-actions">
            <button
              v-if="!sourceTrack"
              class="secondary-button"
              type="button"
              :disabled="!aiStatus.enabled || !aiStatus.speech.available || aiJobActive"
              @click="startSpeechRecognition"
            ><Sparkles :size="16" />识别原文字幕</button>
            <button
              v-else
              class="primary-button"
              type="button"
              :disabled="!aiStatus.enabled || !aiStatus.translation.available || aiJobActive"
              @click="startTranslation"
            ><Languages :size="16" />{{ hasTranslatedTrack ? "重新翻译为中文" : "翻译为中文" }}</button>
          </div>
          <div v-if="aiJob" class="ai-job" :data-status="aiJob.status">
            <div class="ai-job-line"><strong>{{ aiJob.stage }}</strong><span>{{ aiJob.progress }}%</span></div>
            <progress :value="aiJob.progress" max="100" />
            <p v-if="aiJob.currentItem">{{ aiJob.currentItem }}</p>
            <p v-if="aiJob.error" class="job-error">{{ aiJob.error }}</p>
            <button v-if="aiJobActive" class="job-action" type="button" @click="cancelAiJob"><X :size="14" />取消任务</button>
            <button v-else-if="aiJob.status === 'Failed' || aiJob.status === 'Cancelled'" class="job-action" type="button" @click="retryAiJob"><RotateCcw :size="14" />重试</button>
          </div>
        </section>
        <p class="eyebrow">NOW PLAYING</p>
        <h1>{{ video.number }}</h1>
        <h2 v-if="video.title && video.title !== video.number">{{ video.title }}</h2>
        <p class="player-description">{{ video.description || "暂无简介" }}</p>
        <div class="card-tags"><span v-for="tag in video.tags" :key="tag.id">#{{ tag.name }}</span></div>
        <dl class="detail-grid"><div><dt>创建日期</dt><dd>{{ formatDate(video.createdAt || video.importedAt) }}</dd></div><div><dt>文件大小</dt><dd>{{ formatBytes(video.size) }}</dd></div><div><dt>视频时长</dt><dd>{{ formatDuration(video.durationSeconds) }}</dd></div><div><dt>分辨率</dt><dd>{{ formatResolution(video.width, video.height) }}</dd></div><div><dt>视频编码</dt><dd>{{ formatCodec(video.videoCodec) }}</dd></div><div><dt>字幕轨道</dt><dd>{{ video.subtitles.length ? video.subtitles.map(track => track.label).join('、') : '无' }}</dd></div><div><dt>原文件名</dt><dd>{{ video.fileName }}</dd></div></dl>
      </article>
    </template>

    <VideoEditor :open="editing" :video-id="video?.id ?? null" :categories="categories" @close="editing = false" @saved="load" />
  </main>
</template>
