<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { Film, LogOut, Search, Tags, Trash2, RefreshCw, RotateCcw, X } from "@lucide/vue";
import MaintenanceDrawer from "@/components/MaintenanceDrawer.vue";
import TagFilters from "@/components/TagFilters.vue";
import TagManager from "@/components/TagManager.vue";
import VideoCard from "@/components/VideoCard.vue";
import VideoEditor from "@/components/VideoEditor.vue";
import { useLibraryStore } from "@/stores/library";
import { useSessionStore } from "@/stores/session";

const router = useRouter();
const library = useLibraryStore();
const session = useSessionStore();
const editorVideoId = ref<string | null>(null);
const tagManagerOpen = ref(false);
const maintenanceOpen = ref(false);
const inlineVideoId = ref<string | null>(null);
const busy = ref(false);
const pageError = ref("");
const loadMoreTrigger = ref<HTMLElement | null>(null);
let searchTimer = 0;
let loadObserver: IntersectionObserver | null = null;

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
  window.clearTimeout(searchTimer);
  searchTimer = window.setTimeout(() => library.loadVideos().catch(showError), 250);
});

watch(() => library.sort, () => {
  inlineVideoId.value = null;
  library.loadVideos().catch(showError);
});

function showError(error: unknown) {
  pageError.value = error instanceof Error ? error.message : String(error);
}

async function changeFilters(action: () => void) {
  inlineVideoId.value = null;
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
</script>

<template>
  <main class="app-page">
    <header class="library-header">
      <div class="title-row">
        <div><p class="eyebrow"><Film :size="14" /> PRIVATE LIBRARY</p><h1>Dreamy Cinema</h1><p class="library-count">已加载 {{ library.videos.length }} / 共 {{ library.total }} 个视频</p></div>
        <div class="header-actions">
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

    <section class="library-content">
      <section v-if="library.activeJob" class="job-panel" :data-status="library.activeJob.status">
        <div class="job-summary">
          <strong>{{ library.activeJob.stage }}</strong>
          <span>{{ library.activeJob.progress }}%</span>
        </div>
        <progress :value="library.activeJob.progress" max="100"></progress>
        <div class="job-detail">
          <span>{{ library.activeJob.currentItem || `同步任务 · 第 ${library.activeJob.attemptCount || 1} 次执行` }}</span>
          <button v-if="library.jobActive" class="job-action" type="button" @click="library.cancelJob().catch(showError)"><X :size="14" />取消任务</button>
          <button v-else-if="library.activeJob.status === 'Failed' || library.activeJob.status === 'Cancelled'" class="job-action" type="button" @click="library.retryJob().catch(showError)"><RotateCcw :size="14" />重试</button>
        </div>
        <p v-if="library.activeJob.error" class="job-error">{{ library.activeJob.error }}</p>
      </section>
      <div class="status-row"><p>{{ pageError || library.status }}</p><button class="logout-link" type="button" @click="logout"><LogOut :size="14" />退出</button></div>
      <p v-if="library.loading" class="empty-copy">正在读取片库...</p>
      <p v-else-if="!library.videos.length" class="empty-copy">没有符合条件的视频</p>
      <div v-else class="video-list">
        <VideoCard
          v-for="video in library.videos"
          :key="video.id"
          :video="video"
          :inline-active="inlineVideoId === video.id"
          @inline-play="inlineVideoId = video.id"
          @inline-end="inlineVideoId = null"
          @open-player="router.push(`/videos/${encodeURIComponent(video.id)}/play`)"
          @edit="inlineVideoId = null; editorVideoId = video.id"
          @toggle-tag="changeFilters(() => library.toggleTag($event))"
        />
        <div ref="loadMoreTrigger" class="load-more-sentinel">
          <span v-if="library.loadingMore">正在加载更多...</span>
          <button v-else-if="library.hasMore" type="button" @click="library.loadMore().catch(showError)">加载更多</button>
          <span v-else>已加载全部</span>
        </div>
      </div>
    </section>

    <VideoEditor :open="Boolean(editorVideoId)" :video-id="editorVideoId" :categories="library.categories" @close="editorVideoId = null" @saved="refreshAfterMutation" />
    <TagManager :open="tagManagerOpen" :categories="library.categories" @close="tagManagerOpen = false" @changed="refreshAfterMutation" />
    <MaintenanceDrawer :open="maintenanceOpen" @close="maintenanceOpen = false" @changed="refreshAfterMutation" />
  </main>
</template>
