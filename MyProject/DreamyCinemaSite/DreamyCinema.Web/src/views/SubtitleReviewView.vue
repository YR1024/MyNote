<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from "vue";
import { useRouter } from "vue-router";
import { AlertTriangle, ArrowLeft, Check, Download, Languages, Play, RefreshCw, Save } from "@lucide/vue";
import { apiRequest, loadSession } from "@/api/client";
import type {
  AiSubtitleJobResult,
  AiSubtitleStatus,
  MediaJob,
  SubtitleReviewDocument,
  SubtitleReviewEditResult,
  SubtitleReviewTrack
} from "@/types";

const props = defineProps<{ id: string }>();
const router = useRouter();
const document = ref<SubtitleReviewDocument | null>(null);
const aiStatus = ref<AiSubtitleStatus | null>(null);
const player = ref<HTMLVideoElement | null>(null);
const sourceTrackId = ref("");
const targetTrackId = ref("");
const drafts = ref<Record<number, string>>({});
const sourceDrafts = ref<Record<number, string>>({});
const sourceEditingIndexes = ref(new Set<number>());
const dirtyIndexes = ref(new Set<number>());
const selectedIndexes = ref(new Set<number>());
const savingIndex = ref<number | null>(null);
const savingSourceIndex = ref<number | null>(null);
const filter = ref<"all" | "warnings" | "manual">("all");
const search = ref("");
const status = ref("正在加载字幕...");
const job = ref<MediaJob | null>(null);
const bilingualTrack = ref<SubtitleReviewTrack | null>(null);
let pollSequence = 0;

const activeJob = computed(() => job.value?.status === "Queued" || job.value?.status === "Running");
const targetTrack = computed(() => document.value?.targetTracks.find(track => track.id === targetTrackId.value) ?? null);
const visibleCues = computed(() => {
  let cues = document.value?.cues ?? [];
  if (filter.value === "warnings") cues = cues.filter(cue => cue.warnings.length > 0);
  if (filter.value === "manual") cues = cues.filter(cue => cue.isManuallyEdited || cue.isSourceManuallyEdited);
  const query = search.value.trim().toLocaleLowerCase();
  return query
    ? cues.filter(cue => cue.sourceText.toLocaleLowerCase().includes(query) || cue.targetText.toLocaleLowerCase().includes(query))
    : cues;
});

onMounted(loadInitial);
onUnmounted(() => { pollSequence++; });

async function loadInitial() {
  try {
    const session = await loadSession();
    if (!session.authenticated) {
      await router.replace("/login");
      return;
    }
    aiStatus.value = await apiRequest<AiSubtitleStatus>("/api/ai-subtitles/status");
    await loadDocument();
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
}

async function loadDocument(preferredTargetId?: string) {
  const params = new URLSearchParams();
  if (sourceTrackId.value) params.set("sourceTrackId", sourceTrackId.value);
  if (preferredTargetId || targetTrackId.value) params.set("targetTrackId", preferredTargetId || targetTrackId.value);
  const suffix = params.size ? `?${params}` : "";
  const loaded = await apiRequest<SubtitleReviewDocument>(`/api/videos/${encodeURIComponent(props.id)}/subtitle-review${suffix}`);
  document.value = loaded;
  sourceTrackId.value = loaded.sourceTrackId ?? "";
  targetTrackId.value = loaded.targetTrackId ?? "";
  drafts.value = Object.fromEntries(loaded.cues.map(cue => [cue.index, cue.targetText]));
  sourceDrafts.value = Object.fromEntries(loaded.cues.map(cue => [cue.index, cue.sourceText]));
  sourceEditingIndexes.value = new Set();
  dirtyIndexes.value = new Set();
  status.value = loaded.cues.length ? "" : "没有可校对的原文字幕。";
  await nextTick();
  applyTargetTrack();
}

async function changeTracks() {
  selectedIndexes.value = new Set();
  await loadDocument();
}

function applyTargetTrack() {
  if (!player.value || !document.value) return;
  Array.from(player.value.textTracks).forEach((textTrack, index) => {
    textTrack.mode = document.value?.targetTracks[index]?.id === targetTrackId.value ? "showing" : "disabled";
  });
}

async function seek(milliseconds: number) {
  if (!player.value) return;
  player.value.currentTime = milliseconds / 1000;
  await player.value.play().catch(() => undefined);
}

function markDirty(index: number) {
  dirtyIndexes.value = new Set(dirtyIndexes.value).add(index);
}

async function saveCue(index: number) {
  if (!targetTrackId.value) return;
  savingIndex.value = index;
  try {
    const result = await apiRequest<SubtitleReviewEditResult>(`/api/videos/${encodeURIComponent(props.id)}/subtitle-review/cue`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ targetTrackId: targetTrackId.value, index, text: drafts.value[index] })
    });
    targetTrackId.value = result.track.id;
    await loadDocument(result.track.id);
    status.value = `第 ${index + 1} 条已保存到中文校对稿。`;
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    savingIndex.value = null;
  }
}

function editSource(index: number) {
  sourceEditingIndexes.value = new Set(sourceEditingIndexes.value).add(index);
}

function cancelSourceEdit(index: number) {
  const cue = document.value?.cues.find(item => item.index === index);
  if (cue) sourceDrafts.value[index] = cue.sourceText;
  const next = new Set(sourceEditingIndexes.value);
  next.delete(index);
  sourceEditingIndexes.value = next;
}

async function saveSourceCue(index: number) {
  if (!sourceTrackId.value) return;
  savingSourceIndex.value = index;
  try {
    const result = await apiRequest<SubtitleReviewEditResult>(`/api/videos/${encodeURIComponent(props.id)}/subtitle-review/source-cue`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sourceTrackId: sourceTrackId.value, index, text: sourceDrafts.value[index] })
    });
    sourceTrackId.value = result.track.id;
    await loadDocument();
    status.value = `第 ${index + 1} 条原文修正已保存，原始轨道保持不变。`;
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    savingSourceIndex.value = null;
  }
}

function toggleSelection(index: number) {
  const next = new Set(selectedIndexes.value);
  next.has(index) ? next.delete(index) : next.add(index);
  selectedIndexes.value = next;
}

function selectWarnings() {
  selectedIndexes.value = new Set((document.value?.cues ?? [])
    .filter(cue => cue.warnings.length > 0 && !cue.isManuallyEdited)
    .map(cue => cue.index));
}

async function retranslateSelected() {
  if (!sourceTrackId.value || !targetTrackId.value || !selectedIndexes.value.size) return;
  job.value = await apiRequest<MediaJob>(`/api/videos/${encodeURIComponent(props.id)}/subtitle-review/retranslate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      sourceTrackId: sourceTrackId.value,
      targetTrackId: targetTrackId.value,
      cueIndexes: [...selectedIndexes.value].sort((a, b) => a - b)
    })
  });
  await waitForJob(job.value.id);
}

async function waitForJob(id: string) {
  const sequence = ++pollSequence;
  while (sequence === pollSequence) {
    const current = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(id)}`);
    if (sequence !== pollSequence) return;
    job.value = current;
    if (current.status === "Completed") {
      const result = current.result as AiSubtitleJobResult | null;
      selectedIndexes.value = new Set();
      await loadDocument(result?.trackId);
      status.value = "选中字幕已重新翻译，人工修改内容未覆盖。";
      return;
    }
    if (current.status === "Failed" || current.status === "Cancelled") return;
    await new Promise(resolve => window.setTimeout(resolve, 500));
  }
}

async function generateBilingual() {
  if (!sourceTrackId.value || !targetTrackId.value) return;
  bilingualTrack.value = await apiRequest<SubtitleReviewTrack>(`/api/videos/${encodeURIComponent(props.id)}/subtitle-review/bilingual`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sourceTrackId: sourceTrackId.value, targetTrackId: targetTrackId.value })
  });
  status.value = "双语字幕轨道已生成，可返回播放页选择。";
}

function formatTime(milliseconds: number) {
  const totalSeconds = Math.max(0, Math.floor(milliseconds / 1000));
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return `${hours ? `${hours}:` : ""}${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
}
</script>

<template>
  <main class="review-page">
    <header class="player-nav review-nav">
      <button class="icon-text-button" type="button" @click="router.push(`/videos/${props.id}/play`)"><ArrowLeft :size="18" />返回播放页</button>
      <strong>{{ document?.videoNumber || "字幕校对" }}</strong>
    </header>

    <section v-if="document" class="review-player">
      <video ref="player" :src="document.streamUrl" controls playsinline preload="metadata" @loadedmetadata="applyTargetTrack">
        <track v-for="track in document.targetTracks" :key="track.id" :src="track.vttUrl" :label="track.label" :srclang="track.language" kind="subtitles">
      </video>
    </section>

    <section v-if="document" class="review-controls">
      <div class="review-track-selectors">
        <label><span>原文轨道</span><select v-model="sourceTrackId" aria-label="校对原文轨道" @change="changeTracks"><option v-for="track in document.sourceTracks" :key="track.id" :value="track.id">{{ track.label }}</option></select></label>
        <label><span>中文轨道</span><select v-model="targetTrackId" aria-label="校对中文轨道" @change="changeTracks"><option v-for="track in document.targetTracks" :key="track.id" :value="track.id">{{ track.label }}</option></select></label>
      </div>
      <div class="review-summary">
        <span>{{ document.cues.length }} 条</span>
        <span :class="{ warning: document.warningCount }">{{ document.warningCount }} 条警告</span>
        <span>{{ document.sourceManualEditCount }} 条原文修正</span>
        <span>{{ document.manualEditCount }} 条人工修改</span>
      </div>
      <div class="review-filter" role="group" aria-label="字幕筛选">
        <button type="button" :class="{ active: filter === 'all' }" @click="filter = 'all'">全部</button>
        <button type="button" :class="{ active: filter === 'warnings' }" @click="filter = 'warnings'">警告</button>
        <button type="button" :class="{ active: filter === 'manual' }" @click="filter = 'manual'">人工修改</button>
      </div>
      <input v-model="search" class="review-search" type="search" aria-label="搜索字幕文本" placeholder="搜索原文或中文">
      <div class="review-actions">
        <button class="secondary-button" type="button" @click="selectWarnings"><AlertTriangle :size="15" />选择警告项</button>
        <button class="primary-button" type="button" :disabled="!selectedIndexes.size || !aiStatus?.enabled || !aiStatus.translation.available || activeJob" @click="retranslateSelected"><RefreshCw :size="15" />重译 {{ selectedIndexes.size || '' }}</button>
        <button class="secondary-button" type="button" :disabled="!targetTrackId" @click="generateBilingual"><Languages :size="15" />生成双语</button>
      </div>
      <div v-if="targetTrack" class="review-export">
        <a :href="`/api/subtitles/${encodeURIComponent(targetTrack.id)}/export?format=srt`" download><Download :size="14" />SRT</a>
        <a :href="`/api/subtitles/${encodeURIComponent(targetTrack.id)}/export?format=vtt`" download><Download :size="14" />WebVTT</a>
        <a v-if="bilingualTrack" :href="`/api/subtitles/${encodeURIComponent(bilingualTrack.id)}/export?format=srt`" download><Download :size="14" />双语 SRT</a>
      </div>
      <div v-if="job" class="ai-job review-job" :data-status="job.status">
        <div class="ai-job-line"><strong>{{ job.stage }}</strong><span>{{ job.progress }}%</span></div>
        <progress :value="job.progress" max="100" />
        <p v-if="job.error" class="job-error">{{ job.error }}</p>
      </div>
      <p v-if="status" class="review-status">{{ status }}</p>
    </section>

    <section v-if="document" class="review-cue-list">
      <article v-for="cue in visibleCues" :key="`${targetTrackId}-${cue.index}`" class="review-cue" :class="{ selected: selectedIndexes.has(cue.index), manual: cue.isManuallyEdited }">
        <header>
          <label class="review-check"><input type="checkbox" :checked="selectedIndexes.has(cue.index)" :disabled="cue.isManuallyEdited" :aria-label="`选择第 ${cue.index + 1} 条字幕`" @change="toggleSelection(cue.index)"><span>#{{ cue.index + 1 }}</span></label>
          <button class="cue-time" type="button" :aria-label="`播放第 ${cue.index + 1} 条字幕`" @click="seek(cue.startMilliseconds)"><Play :size="13" />{{ formatTime(cue.startMilliseconds) }}</button>
          <span v-if="cue.isManuallyEdited" class="manual-badge"><Check :size="12" />人工修改</span>
        </header>
        <div class="cue-source-block">
          <template v-if="sourceEditingIndexes.has(cue.index)">
            <textarea v-model="sourceDrafts[cue.index]" class="source-cue-editor" :aria-label="`第 ${cue.index + 1} 条原文字幕`" rows="2" />
            <div class="source-edit-actions">
              <button class="text-button" type="button" @click="cancelSourceEdit(cue.index)">取消</button>
              <button class="secondary-button" type="button" :disabled="savingSourceIndex !== null" @click="saveSourceCue(cue.index)"><Save :size="13" />{{ savingSourceIndex === cue.index ? "保存中" : "保存原文" }}</button>
            </div>
          </template>
          <template v-else>
            <p class="cue-source">{{ cue.sourceText }}</p>
            <button class="source-edit-button" type="button" @click="editSource(cue.index)">{{ cue.isSourceManuallyEdited ? "再次修正原文" : "修正原文" }}</button>
          </template>
        </div>
        <textarea v-model="drafts[cue.index]" :aria-label="`第 ${cue.index + 1} 条中文字幕`" rows="2" @input="markDirty(cue.index)" />
        <div v-if="cue.warnings.length" class="cue-warnings"><span v-for="warning in cue.warnings" :key="warning.code"><AlertTriangle :size="12" />{{ warning.message }}</span></div>
        <footer v-if="dirtyIndexes.has(cue.index)"><button class="primary-button" type="button" :disabled="savingIndex !== null" @click="saveCue(cue.index)"><Save :size="14" />{{ savingIndex === cue.index ? "保存中" : "保存此条" }}</button></footer>
      </article>
      <p v-if="!visibleCues.length" class="empty-copy">当前筛选下没有字幕。</p>
    </section>
  </main>
</template>
