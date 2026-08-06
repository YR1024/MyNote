<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { AlertTriangle, ArrowLeft, Captions, Edit3, Film, Languages, RotateCcw, Settings2, Sparkles, Trash2, X } from "@lucide/vue";
import { apiRequest, loadSession } from "@/api/client";
import type { AiSubtitleJobResult, AiSubtitleStatus, HardSubtitleJobResult, HardSubtitleOptionsInfo, HardSubtitleStyle, MediaJob, SubtitleTrack, TagCategory, VideoItem } from "@/types";
import HardSubtitleSettings from "@/components/HardSubtitleSettings.vue";
import VideoEditor from "@/components/VideoEditor.vue";
import { useSubtitleDisplay } from "@/composables/useSubtitleDisplay";
import { formatBytes, formatCodec, formatDate, formatDuration, formatResolution } from "@/utils/format";
import { preferredSubtitleId, rememberSubtitlePreference } from "@/utils/subtitles";

const props = defineProps<{ id: string }>();
const router = useRouter();
const video = ref<VideoItem | null>(null);
const categories = ref<TagCategory[]>([]);
const editing = ref(false);
const status = ref("正在加载视频...");
const player = ref<HTMLVideoElement | null>(null);
const selectedSubtitleId = ref("");
const playbackSource = ref<"original" | "hard-subtitle">("original");
const hardSubtitleTrackId = ref("");
const hardSubtitleJob = ref<MediaJob | null>(null);
const hardSubtitleMessage = ref("");
const hardSubtitleSettingsOpen = ref(false);
const hardSubtitleDefaults = ref<HardSubtitleStyle>({
  fontName: "Microsoft YaHei",
  fontSize: 18,
  textColor: "#FFFFFF",
  textOpacity: 92,
  outlineColor: "#000000",
  outline: 2,
  marginV: 24
});
const hardSubtitleStyle = ref<HardSubtitleStyle>({ ...hardSubtitleDefaults.value });
const aiStatus = ref<AiSubtitleStatus | null>(null);
const aiJob = ref<MediaJob | null>(null);
let aiPollSequence = 0;
let hardSubtitlePollSequence = 0;

const subtitleTracks = computed(() =>
  playbackSource.value === "original" ? (video.value?.subtitles ?? []) : []);
const { activeSubtitleText, displayMode, syncSubtitleTracks } = useSubtitleDisplay(
  player,
  subtitleTracks,
  selectedSubtitleId
);
const currentStreamUrl = computed(() => {
  if (!video.value) return "";
  return playbackSource.value === "hard-subtitle" && video.value.hardSubtitle
    ? video.value.hardSubtitle.streamUrl
    : video.value.originalStreamUrl;
});
const aiJobActive = computed(() => aiJob.value?.status === "Queued" || aiJob.value?.status === "Running");
const hardSubtitleJobActive = computed(() =>
  hardSubtitleJob.value?.status === "Queued" || hardSubtitleJob.value?.status === "Running");
const hasTranslatedTrack = computed(() => video.value?.subtitles.some(track => track.kind === "Translated") ?? false);
const sourceTrack = computed<SubtitleTrack | null>(() => {
  if (!video.value) return null;
  const selected = video.value.subtitles.find(track => track.id === selectedSubtitleId.value);
  if (selected && selected.kind !== "Translated" && !selected.language.toLowerCase().startsWith("zh")) return selected;
  return video.value.subtitles.find(track => track.kind === "Original" && !track.language.toLowerCase().startsWith("zh")) ?? null;
});

watch(selectedSubtitleId, () => {
  rememberSubtitlePreference(video.value?.subtitles.find(item => item.id === selectedSubtitleId.value));
});
watch(playbackSource, async source => {
  selectedSubtitleId.value = source === "original" && video.value
    ? preferredSubtitleId(video.value.subtitles)
    : "";
  await nextTick();
  player.value?.load();
  syncSubtitleTracks();
});

onMounted(() => void load());
onUnmounted(() => {
  aiPollSequence++;
  hardSubtitlePollSequence++;
});

async function load() {
  try {
    const session = await loadSession();
    if (!session.authenticated) {
      await router.replace("/login");
      return;
    }
    const [loadedVideo, loadedCategories, loadedAiStatus, loadedHardSubtitleOptions, jobs] = await Promise.all([
      apiRequest<VideoItem>(`/api/videos/${encodeURIComponent(props.id)}`),
      apiRequest<TagCategory[]>("/api/tag-categories"),
      apiRequest<AiSubtitleStatus>("/api/ai-subtitles/status"),
      apiRequest<HardSubtitleOptionsInfo>("/api/hard-subtitle/options"),
      apiRequest<MediaJob[]>("/api/jobs")
    ]);
    video.value = loadedVideo;
    categories.value = loadedCategories;
    aiStatus.value = loadedAiStatus;
    hardSubtitleDefaults.value = { ...loadedHardSubtitleOptions.defaultStyle };
    hardSubtitleStyle.value = {
      ...(loadedVideo.hardSubtitle?.style ?? loadedHardSubtitleOptions.defaultStyle)
    };
    aiJob.value = jobs.find(job => job.videoId === props.id
      && (job.type === "SpeechRecognition" || job.type === "SubtitleTranslation")) ?? null;
    hardSubtitleJob.value = jobs.find(job => job.videoId === props.id && job.type === "HardSubtitle") ?? null;
    normalizePlaybackSource();
    hardSubtitleTrackId.value = preferredHardSubtitleTrackId();
    selectedSubtitleId.value = playbackSource.value === "original"
      ? preferredSubtitleId(video.value.subtitles)
      : "";
    await nextTick();
    syncSubtitleTracks();
    status.value = "";
    if (aiJobActive.value && aiJob.value) void waitForAiJob(aiJob.value.id);
    if (hardSubtitleJobActive.value && hardSubtitleJob.value) void waitForHardSubtitleJob(hardSubtitleJob.value.id);
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
}

async function refreshVideo(preferredTrackId?: string) {
  video.value = await apiRequest<VideoItem>(`/api/videos/${encodeURIComponent(props.id)}`);
  normalizePlaybackSource();
  if (preferredTrackId && video.value.subtitles.some(track => track.id === preferredTrackId)) {
    selectedSubtitleId.value = preferredTrackId;
  }
  if (!video.value.subtitles.some(track => track.id === hardSubtitleTrackId.value)) {
    hardSubtitleTrackId.value = preferredHardSubtitleTrackId();
  }
  await nextTick();
  syncSubtitleTracks();
}

function normalizePlaybackSource() {
  if (!video.value) return;
  if (!video.value.originalAvailable && video.value.hardSubtitle) {
    playbackSource.value = "hard-subtitle";
  } else if (playbackSource.value === "hard-subtitle" && !video.value.hardSubtitle) {
    playbackSource.value = "original";
  }
}

function preferredHardSubtitleTrackId() {
  if (!video.value) return "";
  const tracks = video.value.subtitles;
  const existing = video.value.hardSubtitle?.subtitleTrackId;
  if (existing && tracks.some(track => track.id === existing)) return existing;
  return tracks.find(track =>
    track.revisionStage === "FinalPolished"
    && (track.language.toLowerCase().startsWith("zh") || track.kind === "Bilingual"))?.id
    ?? tracks.find(track => track.language.toLowerCase().startsWith("zh"))?.id
    ?? tracks.find(track => track.kind === "Bilingual")?.id
    ?? preferredSubtitleId(tracks);
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

async function startHardSubtitle() {
  if (!video.value || !hardSubtitleTrackId.value || hardSubtitleJobActive.value) return;
  hardSubtitleMessage.value = "";
  hardSubtitleJob.value = await apiRequest<MediaJob>(
    `/api/videos/${encodeURIComponent(video.value.id)}/hard-subtitle`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        subtitleTrackId: hardSubtitleTrackId.value,
        style: hardSubtitleStyle.value
      })
    }
  );
  await waitForHardSubtitleJob(hardSubtitleJob.value.id);
}

async function waitForHardSubtitleJob(id: string) {
  const sequence = ++hardSubtitlePollSequence;
  while (sequence === hardSubtitlePollSequence) {
    const job = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(id)}`);
    if (sequence !== hardSubtitlePollSequence) return;
    hardSubtitleJob.value = job;
    if (job.status === "Completed") {
      const result = job.result as HardSubtitleJobResult | null;
      if (result?.style) hardSubtitleStyle.value = { ...result.style };
      await refreshVideo();
      playbackSource.value = "hard-subtitle";
      hardSubtitleMessage.value = result
        ? `硬字幕版已生成：${result.subtitleLabel} · ${formatBytes(result.size)} · ${result.encoder === "h264_nvenc" ? "NVIDIA 编码" : "CPU 编码"}`
        : "硬字幕版已生成。";
      return;
    }
    if (job.status === "Failed" || job.status === "Cancelled") return;
    await new Promise(resolve => window.setTimeout(resolve, 800));
  }
}

async function cancelHardSubtitleJob() {
  if (!hardSubtitleJob.value || !hardSubtitleJobActive.value) return;
  hardSubtitleJob.value = await apiRequest<MediaJob>(
    `/api/jobs/${encodeURIComponent(hardSubtitleJob.value.id)}/cancel`,
    { method: "POST" }
  );
}

async function retryHardSubtitleJob() {
  if (!hardSubtitleJob.value || !["Failed", "Cancelled"].includes(hardSubtitleJob.value.status)) return;
  hardSubtitleJob.value = await apiRequest<MediaJob>(
    `/api/jobs/${encodeURIComponent(hardSubtitleJob.value.id)}/retry`,
    { method: "POST" }
  );
  await waitForHardSubtitleJob(hardSubtitleJob.value.id);
}

function applyHardSubtitleStyle(style: HardSubtitleStyle) {
  hardSubtitleStyle.value = { ...style };
  hardSubtitleSettingsOpen.value = false;
  hardSubtitleMessage.value = "字幕样式已设置，将在下次生成时使用。";
}

async function deleteHardSubtitle() {
  if (!video.value?.hardSubtitle || !video.value.originalAvailable) return;
  if (!window.confirm("删除硬字幕版并保留原视频？该版本需要重新执行 FFmpeg 才能恢复。")) return;
  await apiRequest<void>(`/api/videos/${encodeURIComponent(video.value.id)}/hard-subtitle`, { method: "DELETE" });
  playbackSource.value = "original";
  hardSubtitleMessage.value = "硬字幕版已删除。";
  await refreshVideo();
}

async function deleteOriginal() {
  if (!video.value?.hardSubtitle || !video.value.originalAvailable || video.value.hardSubtitle.isStale) return;
  const confirmation = window.prompt(
    `此操作会永久删除原视频并只保留硬字幕版，无法撤销。\n请输入番号 ${video.value.number} 确认：`,
    ""
  );
  if (confirmation === null) return;
  const result = await apiRequest<{ storageReleased: boolean; message: string }>(
    `/api/videos/${encodeURIComponent(video.value.id)}/original/delete`,
    {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ confirmNumber: confirmation })
    }
  );
  playbackSource.value = "hard-subtitle";
  hardSubtitleMessage.value = result.storageReleased
    ? "原视频已永久删除，当前仅保留硬字幕版。"
    : result.message;
  await refreshVideo();
}

function aiProviderLabel(provider: AiSubtitleStatus["speech"]) {
  const model = provider.model ? ` · ${provider.model}` : "";
  return `${provider.provider}${model}${provider.mock ? "（模拟）" : ""}`;
}

</script>

<template>
  <main class="player-page">
    <header class="player-nav">
      <button class="icon-text-button" type="button" @click="router.push('/')"><ArrowLeft :size="18" />返回片库</button>
      <div v-if="video" class="player-nav-actions">
        <button v-if="sourceTrack && hasTranslatedTrack" class="icon-text-button" type="button" @click="router.push(`/videos/${video.id}/subtitles/review`)"><Captions :size="16" />校对</button>
        <button class="icon-text-button" type="button" @click="editing = true"><Edit3 :size="16" />编辑</button>
      </div>
    </header>

    <p v-if="status" class="empty-copy">{{ status }}</p>
    <template v-if="video">
      <section class="player-stage">
        <div class="player-video-frame">
          <video
            ref="player"
            :src="currentStreamUrl"
            controls
            autoplay
            playsinline
            preload="metadata"
            @loadedmetadata="syncSubtitleTracks"
          >
            <track
              v-for="track in video.subtitles"
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
            v-if="playbackSource === 'original' && displayMode === 'overlay' && activeSubtitleText"
            class="subtitle-overlay"
            data-subtitle-mode="overlay"
          >{{ activeSubtitleText }}</p>
        </div>
        <div
          v-if="playbackSource === 'original' && displayMode === 'below' && selectedSubtitleId"
          class="subtitle-compat-bar"
          data-subtitle-mode="below"
          aria-label="兼容字幕"
        >
          <span>{{ activeSubtitleText || "\u00a0" }}</span>
        </div>
      </section>
      <article class="player-details">
        <section class="playback-version-panel">
          <div class="playback-version-heading">
            <div><p class="eyebrow">PLAYBACK SOURCE</p><h2>播放版本</h2></div>
            <span v-if="!video.originalAvailable" class="source-status warning">仅硬字幕版</span>
          </div>
          <div class="playback-version-options" role="group" aria-label="播放版本">
            <button
              type="button"
              :class="{ active: playbackSource === 'original' }"
              :disabled="!video.originalAvailable"
              :aria-pressed="playbackSource === 'original'"
              @click="playbackSource = 'original'"
            ><Film :size="16" />原始视频</button>
            <button
              type="button"
              :class="{ active: playbackSource === 'hard-subtitle' }"
              :disabled="!video.hardSubtitle"
              :aria-pressed="playbackSource === 'hard-subtitle'"
              @click="playbackSource = 'hard-subtitle'"
            ><Captions :size="16" />硬字幕版</button>
          </div>
          <p v-if="playbackSource === 'hard-subtitle' && video.hardSubtitle" class="playback-version-meta">
            {{ video.hardSubtitle.subtitleLabel }} · {{ formatBytes(video.hardSubtitle.size) }}
            <span v-if="video.hardSubtitle.isStale"> · 字幕已修改，需要重新生成</span>
          </p>
        </section>

        <section v-if="playbackSource === 'original' && video.subtitles.length" class="player-subtitle-controls">
          <label class="player-subtitle-control">
            <span>字幕</span>
            <select v-model="selectedSubtitleId" aria-label="字幕轨道">
              <option value="">关闭字幕</option>
              <option v-for="track in video.subtitles" :key="track.id" :value="track.id">{{ track.label }} · {{ track.cueCount }} 条</option>
            </select>
          </label>
          <label class="player-subtitle-control">
            <span>显示</span>
            <select v-model="displayMode" aria-label="字幕显示方式">
              <option value="native">浏览器原生</option>
              <option value="overlay">画面内（自绘）</option>
              <option value="below">画面下方（兼容）</option>
            </select>
          </label>
        </section>

        <section class="hard-subtitle-panel">
          <div class="hard-subtitle-heading">
            <div><p class="eyebrow">MOBILE COMPATIBLE</p><h2>硬字幕手机版</h2></div>
            <span v-if="video.hardSubtitle" :class="{ warning: video.hardSubtitle.isStale }">
              {{ video.hardSubtitle.isStale ? "需要更新" : "已生成" }}
            </span>
            <span v-else>未生成</span>
          </div>
          <p class="hard-subtitle-copy">字幕会写入视频画面，适用于手机浏览器原生全屏。任务在后台执行，不影响原视频。</p>
          <label v-if="video.subtitles.length" class="hard-subtitle-track">
            <span>烧录字幕</span>
            <select v-model="hardSubtitleTrackId" aria-label="硬字幕轨道" :disabled="hardSubtitleJobActive || !video.originalAvailable">
              <option v-for="track in video.subtitles" :key="track.id" :value="track.id">
                {{ track.label }} · {{ track.cueCount }} 条
              </option>
            </select>
          </label>
          <div class="hard-subtitle-style-summary">
            <div>
              <span>烧录样式</span>
              <strong>{{ hardSubtitleStyle.fontName }} · {{ hardSubtitleStyle.fontSize }} px · {{ hardSubtitleStyle.textOpacity }}%</strong>
            </div>
            <button
              class="secondary-button"
              type="button"
              :disabled="hardSubtitleJobActive"
              @click="hardSubtitleSettingsOpen = true"
            ><Settings2 :size="15" />设置</button>
          </div>
          <p v-if="!video.originalAvailable" class="hard-subtitle-warning">原视频已删除，现有硬字幕版是唯一可播放文件，不能重新生成或删除。</p>
          <p v-else-if="!video.subtitles.length" class="hard-subtitle-warning">当前没有可烧录的字幕轨道。</p>
          <div class="hard-subtitle-actions">
            <button
              class="primary-button"
              type="button"
              :disabled="!video.originalAvailable || !hardSubtitleTrackId || hardSubtitleJobActive"
              @click="startHardSubtitle"
            ><Sparkles :size="16" />{{ video.hardSubtitle ? "重新生成硬字幕版" : "生成硬字幕版" }}</button>
            <button
              v-if="video.hardSubtitle && video.originalAvailable"
              class="secondary-button danger"
              type="button"
              :disabled="hardSubtitleJobActive"
              @click="deleteHardSubtitle"
            ><Trash2 :size="15" />删除硬字幕版</button>
          </div>
          <div v-if="hardSubtitleJob" class="ai-job hard-subtitle-job" :data-status="hardSubtitleJob.status">
            <div class="ai-job-line"><strong>{{ hardSubtitleJob.stage }}</strong><span>{{ hardSubtitleJob.progress }}%</span></div>
            <progress :value="hardSubtitleJob.progress" max="100" />
            <p v-if="hardSubtitleJob.currentItem">{{ hardSubtitleJob.currentItem }}</p>
            <p v-if="hardSubtitleJob.error" class="job-error">{{ hardSubtitleJob.error }}</p>
            <button v-if="hardSubtitleJobActive" class="job-action" type="button" @click="cancelHardSubtitleJob"><X :size="14" />取消任务</button>
            <button v-else-if="hardSubtitleJob.status === 'Failed' || hardSubtitleJob.status === 'Cancelled'" class="job-action" type="button" @click="retryHardSubtitleJob"><RotateCcw :size="14" />重试</button>
          </div>
          <p v-if="hardSubtitleMessage" class="hard-subtitle-message">{{ hardSubtitleMessage }}</p>
          <div v-if="video.hardSubtitle && video.originalAvailable" class="original-delete-zone">
            <div>
              <strong>释放原视频空间</strong>
              <p>只保留当前硬字幕版。删除后不能更换字幕或重新生成。</p>
            </div>
            <button
              class="danger-button"
              type="button"
              :disabled="hardSubtitleJobActive || video.hardSubtitle.isStale"
              @click="deleteOriginal"
            ><Trash2 :size="15" />永久删除原视频</button>
          </div>
        </section>

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
        <p v-if="video.playbackWarning" class="codec-warning player-codec-warning"><AlertTriangle :size="15" />{{ video.playbackWarning }}</p>
        <div class="card-tags"><span v-for="tag in video.tags" :key="tag.id">#{{ tag.name }}</span></div>
        <dl class="detail-grid"><div><dt>创建日期</dt><dd>{{ formatDate(video.createdAt || video.importedAt) }}</dd></div><div><dt>原文件大小</dt><dd>{{ video.originalAvailable ? formatBytes(video.size) : `已删除（原 ${formatBytes(video.size)}）` }}</dd></div><div v-if="video.hardSubtitle"><dt>硬字幕大小</dt><dd>{{ formatBytes(video.hardSubtitle.size) }}</dd></div><div><dt>视频时长</dt><dd>{{ formatDuration(video.durationSeconds) }}</dd></div><div><dt>分辨率</dt><dd>{{ formatResolution(video.width, video.height) }}</dd></div><div><dt>视频编码</dt><dd>{{ formatCodec(video.videoCodec) }}</dd></div><div><dt>字幕轨道</dt><dd>{{ video.subtitles.length ? video.subtitles.map(track => track.label).join('、') : '无' }}</dd></div><div><dt>原文件名</dt><dd>{{ video.fileName }}{{ video.originalAvailable ? '' : '（已删除）' }}</dd></div></dl>
      </article>
    </template>

    <VideoEditor :open="editing" :video-id="video?.id ?? null" :categories="categories" @close="editing = false" @saved="load" />
    <HardSubtitleSettings
      :open="hardSubtitleSettingsOpen"
      :model-value="hardSubtitleStyle"
      :defaults="hardSubtitleDefaults"
      :preview-url="video?.coverUrl ?? null"
      @close="hardSubtitleSettingsOpen = false"
      @apply="applyHardSubtitleStyle"
    />
  </main>
</template>
