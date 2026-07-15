import { defineStore } from "pinia";
import { apiRequest } from "@/api/client";
import type { MediaJob, SyncResult, TagCategory, VideoItem, VideoPage } from "@/types";

const pageSize = 20;
let videoRequestSequence = 0;
let jobPollSequence = 0;

export const useLibraryStore = defineStore("library", {
  state: () => ({
    categories: [] as TagCategory[],
    videos: [] as VideoItem[],
    selectedTagIds: [] as number[],
    activeCategoryId: null as number | null,
    search: "",
    sort: "imported-desc",
    total: 0,
    page: 0,
    hasMore: false,
    loading: false,
    loadingMore: false,
    activeJob: null as MediaJob | null,
    status: ""
  }),
  getters: {
    activeCategory(state): TagCategory | undefined {
      return state.categories.find(category => category.id === state.activeCategoryId);
    },
    selectedTags(state) {
      const ids = new Set(state.selectedTagIds);
      return state.categories.flatMap(category => category.tags).filter(tag => ids.has(tag.id));
    },
    jobActive(state) {
      return state.activeJob?.status === "Queued" || state.activeJob?.status === "Running";
    }
  },
  actions: {
    async loadCategories() {
      this.categories = await apiRequest<TagCategory[]>("/api/tag-categories");
      const validIds = new Set(this.categories.flatMap(category => category.tags.map(tag => tag.id)));
      this.selectedTagIds = this.selectedTagIds.filter(id => validIds.has(id));
      if (!this.categories.some(category => category.id === this.activeCategoryId)) {
        this.activeCategoryId = this.categories[0]?.id ?? null;
      }
    },
    async loadVideos(append = false) {
      if (append && (this.loading || this.loadingMore || !this.hasMore)) return;
      const requestId = ++videoRequestSequence;
      const requestedPage = append ? this.page + 1 : 1;
      if (append) this.loadingMore = true;
      else this.loading = true;
      try {
        const parameters = new URLSearchParams({
          sort: this.sort,
          page: String(requestedPage),
          pageSize: String(pageSize)
        });
        if (this.search.trim()) parameters.set("q", this.search.trim());
        if (this.selectedTagIds.length) parameters.set("tagIds", this.selectedTagIds.join(","));
        const result = await apiRequest<VideoPage>(`/api/videos?${parameters}`);
        if (requestId !== videoRequestSequence) return;
        if (append) {
          const existingIds = new Set(this.videos.map(video => video.id));
          this.videos.push(...result.items.filter(video => !existingIds.has(video.id)));
        } else {
          this.videos = result.items;
        }
        this.total = result.total;
        this.page = result.page;
        this.hasMore = result.hasMore;
      } finally {
        if (requestId === videoRequestSequence) {
          this.loading = false;
          this.loadingMore = false;
        }
      }
    },
    async loadMore() {
      await this.loadVideos(true);
    },
    async refresh() {
      await Promise.all([this.loadCategories(), this.loadVideos()]);
    },
    toggleTag(id: number) {
      this.selectedTagIds = this.selectedTagIds.includes(id)
        ? this.selectedTagIds.filter(tagId => tagId !== id)
        : [...this.selectedTagIds, id];
    },
    clearTags() {
      this.selectedTagIds = [];
    },
    async syncVideos() {
      const job = await apiRequest<MediaJob>("/api/videos/sync", { method: "POST" });
      this.activeJob = job;
      this.updateJobStatus(job);
      await this.waitForJob(job.id);
    },
    async resumeJobs() {
      const jobs = await apiRequest<MediaJob[]>("/api/jobs");
      const syncJobs = jobs.filter(job => job.type === "Sync");
      const active = syncJobs.find(job => job.status === "Queued" || job.status === "Running");
      this.activeJob = active ?? syncJobs[0] ?? null;
      if (!this.activeJob) return;
      this.updateJobStatus(this.activeJob);
      if (active) await this.waitForJob(active.id);
    },
    async waitForJob(id: string) {
      const pollId = ++jobPollSequence;
      while (pollId === jobPollSequence) {
        const job = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(id)}`);
        if (pollId !== jobPollSequence) return;
        this.activeJob = job;
        this.updateJobStatus(job);
        if (job.status === "Completed") {
          await this.refresh();
          return;
        }
        if (job.status === "Failed" || job.status === "Cancelled") return;
        await new Promise(resolve => window.setTimeout(resolve, 400));
      }
    },
    async cancelJob() {
      if (!this.activeJob || !this.jobActive) return;
      const job = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(this.activeJob.id)}/cancel`, { method: "POST" });
      this.activeJob = job;
      this.updateJobStatus(job);
    },
    async retryJob() {
      if (!this.activeJob || !["Failed", "Cancelled"].includes(this.activeJob.status)) return;
      const job = await apiRequest<MediaJob>(`/api/jobs/${encodeURIComponent(this.activeJob.id)}/retry`, { method: "POST" });
      this.activeJob = job;
      this.updateJobStatus(job);
      await this.waitForJob(job.id);
    },
    updateJobStatus(job: MediaJob) {
      if (job.status === "Queued" || job.status === "Running") {
        this.status = `${job.stage} ${job.progress}%${job.currentItem ? `：${job.currentItem}` : ""}`;
        return;
      }
      if (job.status === "Failed") {
        this.status = `任务失败：${job.error || "没有错误详情"}`;
        return;
      }
      if (job.status === "Cancelled") {
        this.status = "同步任务已取消，可重新执行。";
        return;
      }

      const rawResult = job.result as (SyncResult & Record<string, unknown>) | null;
      const result = rawResult && !rawResult.failed && rawResult.Failed
        ? {
            importedCount: rawResult.ImportedCount,
            availableCount: rawResult.AvailableCount,
            missingCount: rawResult.MissingCount,
            analyzedCount: rawResult.AnalyzedCount,
            generatedCoverCount: rawResult.GeneratedCoverCount,
            importedSubtitleCount: rawResult.ImportedSubtitleCount,
            failed: rawResult.Failed,
            warnings: rawResult.Warnings
          } as SyncResult
        : rawResult;
      if (!result) {
        this.status = "任务已完成。";
        return;
      }
      const failed = result.failed.length ? `，失败 ${result.failed.length} 个` : "";
      const warnings = result.warnings.length ? `，媒体处理警告 ${result.warnings.length} 个` : "";
      this.status = `导入 ${result.importedCount} 个，字幕 ${result.importedSubtitleCount} 轨，分析 ${result.analyzedCount} 个，生成封面 ${result.generatedCoverCount} 张，可播放 ${result.availableCount} 个，缺失 ${result.missingCount} 个${failed}${warnings}。`;
    }
  }
});
