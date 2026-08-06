<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { CheckSquare, DatabaseBackup, Film, LogOut, Search, ShieldCheck, Tags, Trash2, RefreshCw, RotateCcw, X } from "@lucide/vue";
import BackupDrawer from "@/components/BackupDrawer.vue";
import BulkOrganizer from "@/components/BulkOrganizer.vue";
import MaintenanceDrawer from "@/components/MaintenanceDrawer.vue";
import SecurityDrawer from "@/components/SecurityDrawer.vue";
import TagFilters from "@/components/TagFilters.vue";
import TagManager from "@/components/TagManager.vue";
import VideoCard from "@/components/VideoCard.vue";
import VideoEditor from "@/components/VideoEditor.vue";
import { useLibraryStore } from "@/stores/library";
import { useSessionStore } from "@/stores/session";
import type { MediaJob } from "@/types";

const router = useRouter();
const library = useLibraryStore();
const session = useSessionStore();
const editorVideoId = ref<string | null>(null);
const tagManagerOpen = ref(false);
const maintenanceOpen = ref(false);
const backupOpen = ref(false);
const bulkOrganizerOpen = ref(false);
const securityOpen = ref(false);
const inlineVideoId = ref<string | null>(null);
const selectionMode = ref(false);
const selectedVideoIds = ref<string[]>([]);
const busy = ref(false);
const pageError = ref("");
const loadMoreTrigger = ref<HTMLElement | null>(null);
let searchTimer = 0;
let loadObserver: IntersectionObserver | null = null;
const selectedVideoIdSet = computed(() => new Set(selectedVideoIds.value));

onMounted(async () => {
  loadObserver = new IntersectionObserver(entries => {
    if (entries.some(entry => entry.isIntersecting)) {
      library.loadMore().catch(showError);
    }
  }, { rootMargin: "320px 0px" });
  if (loadMoreTrigger.value) loadObserver.observe(loadMoreTrigger.value);

  try {
    const info = await session.refresh();
    if (!info.authenticated) {
      await router.replace("/login");
      return;
    }
    await library.refresh();
    library.resumeJobs().catch(showError);
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : String(error);
  }
});

watch(loadMoreTrigger, (current, previous) => {
  if (previous) loadObserver?.unobserve(previous);
  if (current) loadObserver?.observe(current);
});

onBeforeUnmount(() => {
  window.clearTimeout(searchTimer);
  loadObserver?.disconnect();
});

watch(() => library.search, () => {
  inlineVideoId.value = null;
  exitSelection();
  window.clearTimeout(searchTimer);
  searchTimer = window.setTimeout(() => library.loadVideos().catch(showError), 250);
});

watch(() => library.sort, () => {
  inlineVideoId.value = null;
  exitSelection();
  library.loadVideos().catch(showError);
});

watch(() => library.videos.map(video => video.id).join(","), () => {
  const loadedIds = new Set(library.videos.map(video => video.id));
  selectedVideoIds.value = selectedVideoIds.value.filter(id => loadedIds.has(id));
});

function showError(error: unknown) {
  pageError.value = error instanceof Error ? error.message : String(error);
}

async function changeFilters(action: () => void) {
  inlineVideoId.value = null;
  exitSelection();
  action();
  await library.loadVideos();
}

async function sync() {
  busy.value = true;
  try { await library.syncVideos(); } catch (error) { showError(error); } finally { busy.value = false; }
}

async function logout() {
  await session.logout();
  await router.replace("/login");
}

async function refreshAfterMutation(message: string) {
  inlineVideoId.value = null;
  library.status = message;
  await library.refresh();
}

async function refreshAfterRestore(message: string) {
  backupOpen.value = false;
  await refreshAfterMutation(message);
}

function enterSelectionMode() {
  inlineVideoId.value = null;
  selectionMode.value = true;
}

function exitSelection() {
  bulkOrganizerOpen.value = false;
  selectionMode.value = false;
  selectedVideoIds.value = [];
}

function toggleVideoSelection(videoId: string) {
  selectedVideoIds.value = selectedVideoIdSet.value.has(videoId)
    ? selectedVideoIds.value.filter(id => id !== videoId)
    : [...selectedVideoIds.value, videoId];
}

function selectLoadedVideos() {
  selectedVideoIds.value = library.videos.map(video => video.id);
}

async function handleBulkChanged(message: string) {
  exitSelection();
  await refreshAfterMutation(message);
}

function handleBulkJob(job: MediaJob) {
  exitSelection();
  library.trackJob(job).catch(showError);
}

function jobTypeLabel(job: MediaJob) {
  return job.type === "Sync" ? "视频同步" : "媒体重新处理";
}
</script>

<template>
  <main class="app-page">
    <header class="library-header">
      <div class="title-row">
        <div><p class="eyebrow"><Film :size="14" /> PRIVATE LIBRARY</p><h1>Dreamy Cinema</h1><p class="library-count">已加载 {{ library.videos.length }} / 共 {{ library.total }} 个视频</p></div>
        <div class="header-actions">
          <button class="icon-command" type="button" title="数据备份" @click="backupOpen = true"><DatabaseBackup /></button>
          <button class="icon-command danger-soft" type="button" title="回收站" @click="maintenanceOpen = true"><Trash2 /></button>
          <button class="icon-command" type="button" title="标签管理" @click="tagManagerOpen = true"><Tags /></button>
          <button class="icon-command primary" type="button" title="同步视频" :disabled="busy || library.jobActive" @click="sync"><RefreshCw :class="{ spin: busy || library.jobActive }" /></button>
        </div>
      </div>

      <div class="search-row">
        <label class="search-box"><Search :size="18" /><input v-model="library.search" maxlength="120" placeholder="搜索番号、标题、简介"></label>
        <select v-model="library.sort" class="sort-select" aria-label="排序">
          <option value="imported-desc">最近同步</option><option value="created-desc">日期最新</option><option value="created-asc">日期最早</option><option value="number-asc">番号排序</option><option value="size-desc">文件最大</option>
        </select>
      </div>

      <TagFilters
        :categories="library.categories"
        :active-category-id="library.activeCategoryId"
        :selected-tag-ids="library.selectedTagIds"
        @select-category="library.activeCategoryId = $event"
        @toggle-tag="changeFilters(() => library.toggleTag($event))"
        @clear="changeFilters(() => library.clearTags())"
      />
    </header>

    <section class="library-content" :class="{ 'has-bulk-bar': selectionMode && selectedVideoIds.length }">
      <section v-if="library.activeJob" class="job-panel" :data-status="library.activeJob.status">
        <div class="job-summary">
          <strong>{{ library.activeJob.stage }}</strong>
          <span>{{ library.activeJob.progress }}%</span>
        </div>
        <progress :value="library.activeJob.progress" max="100"></progress>
        <div class="job-detail">
          <span>{{ library.activeJob.currentItem || `${jobTypeLabel(library.activeJob)} · 第 ${library.activeJob.attemptCount || 1} 次执行` }}</span>
          <button v-if="library.jobActive" class="job-action" type="button" @click="library.cancelJob().catch(showError)"><X :size="14" />取消任务</button>
          <button v-else-if="library.activeJob.status === 'Failed' || library.activeJob.status === 'Cancelled'" class="job-action" type="button" @click="library.retryJob().catch(showError)"><RotateCcw :size="14" />重试</button>
        </div>
        <p v-if="library.activeJob.error" class="job-error">{{ library.activeJob.error }}</p>
      </section>
      <div class="bulk-selection-row" :class="{ active: selectionMode }">
        <template v-if="selectionMode">
          <strong>已选择 {{ selectedVideoIds.length }} 个</strong>
          <div>
            <button type="button" @click="selectLoadedVideos">全选已加载</button>
            <button type="button" :disabled="!selectedVideoIds.length" @click="selectedVideoIds = []">清空</button>
            <button type="button" @click="exitSelection">完成</button>
          </div>
        </template>
        <button v-else class="bulk-entry-button" type="button" :disabled="!library.videos.length" @click="enterSelectionMode">
          <CheckSquare :size="15" />批量整理
        </button>
      </div>
      <div class="status-row">
        <p>{{ pageError || library.status }}</p>
        <div class="status-actions">
          <button class="logout-link" type="button" @click="securityOpen = true"><ShieldCheck :size="14" />账户</button>
          <button class="logout-link" type="button" @click="logout"><LogOut :size="14" />退出</button>
        </div>
      </div>
      <p v-if="library.loading" class="empty-copy">正在读取片库...</p>
      <p v-else-if="!library.videos.length" class="empty-copy">没有符合条件的视频</p>
      <div v-else class="video-list">
        <VideoCard
          v-for="video in library.videos"
          :key="video.id"
          :video="video"
          :inline-active="inlineVideoId === video.id"
          :selectable="selectionMode"
          :selected="selectedVideoIdSet.has(video.id)"
          @inline-play="inlineVideoId = video.id"
          @inline-end="inlineVideoId = null"
          @open-player="router.push(`/videos/${encodeURIComponent(video.id)}/play`)"
          @edit="inlineVideoId = null; editorVideoId = video.id"
          @toggle-tag="changeFilters(() => library.toggleTag($event))"
          @toggle-selection="toggleVideoSelection(video.id)"
        />
        <div ref="loadMoreTrigger" class="load-more-sentinel">
          <span v-if="library.loadingMore">正在加载更多...</span>
          <button v-else-if="library.hasMore" type="button" @click="library.loadMore().catch(showError)">加载更多</button>
          <span v-else>已加载全部</span>
        </div>
      </div>
    </section>

    <div v-if="selectionMode && selectedVideoIds.length" class="bulk-bottom-bar">
      <span>{{ selectedVideoIds.length }} 个视频</span>
      <button class="primary-button" type="button" @click="bulkOrganizerOpen = true">打开批量整理</button>
    </div>

    <VideoEditor :open="Boolean(editorVideoId)" :video-id="editorVideoId" :categories="library.categories" @close="editorVideoId = null" @saved="refreshAfterMutation" />
    <BackupDrawer :open="backupOpen" @close="backupOpen = false" @restored="refreshAfterRestore" />
    <BulkOrganizer
      :open="bulkOrganizerOpen"
      :video-ids="selectedVideoIds"
      :categories="library.categories"
      @close="bulkOrganizerOpen = false"
      @changed="handleBulkChanged"
      @job-created="handleBulkJob"
    />
    <TagManager :open="tagManagerOpen" :categories="library.categories" @close="tagManagerOpen = false" @changed="refreshAfterMutation" />
    <MaintenanceDrawer :open="maintenanceOpen" @close="maintenanceOpen = false" @changed="refreshAfterMutation" />
    <SecurityDrawer :open="securityOpen" :session="session.info" @close="securityOpen = false" />
  </main>
</template>
