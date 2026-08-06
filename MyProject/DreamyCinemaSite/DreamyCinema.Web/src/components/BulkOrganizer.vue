<script setup lang="ts">
import { ref, watch } from "vue";
import { AlertTriangle, Image, RefreshCw, Tags, Trash2, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import type {
  BatchTagResult,
  BatchTrashResult,
  MediaJob,
  ResolutionApplyResult,
  ResolutionSuggestionResult,
  TagCategory
} from "@/types";

const props = defineProps<{
  open: boolean;
  videoIds: string[];
  categories: TagCategory[];
}>();
const emit = defineEmits<{
  close: [];
  changed: [message: string];
  jobCreated: [job: MediaJob];
}>();

const tagMode = ref<"add" | "remove">("add");
const selectedTagIds = ref<number[]>([]);
const suggestions = ref<ResolutionSuggestionResult | null>(null);
const status = ref("");
const busy = ref(false);

watch(() => props.open, open => {
  if (!open) return;
  tagMode.value = "add";
  selectedTagIds.value = [];
  suggestions.value = null;
  status.value = "";
});

function toggleTag(tagId: number) {
  selectedTagIds.value = selectedTagIds.value.includes(tagId)
    ? selectedTagIds.value.filter(id => id !== tagId)
    : [...selectedTagIds.value, tagId];
}

async function applyTags() {
  if (!selectedTagIds.value.length) return;
  await run(async () => {
    const result = await apiRequest<BatchTagResult>("/api/videos/batch/tags", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        videoIds: props.videoIds,
        addTagIds: tagMode.value === "add" ? selectedTagIds.value : [],
        removeTagIds: tagMode.value === "remove" ? selectedTagIds.value : []
      })
    });
    const relationCount = tagMode.value === "add"
      ? result.addedRelationshipCount
      : result.removedRelationshipCount;
    emit("changed", `已为 ${result.videoCount} 个视频${tagMode.value === "add" ? "添加" : "移除"}标签，共更新 ${relationCount} 条关系。`);
  });
}

async function loadResolutionSuggestions() {
  await run(async () => {
    suggestions.value = await apiRequest<ResolutionSuggestionResult>("/api/videos/batch/resolution-suggestions", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ videoIds: props.videoIds })
    });
    status.value = suggestions.value.groups.length
      ? "建议已生成，确认后才会修改清晰度标签。"
      : "所选视频没有可应用的清晰度建议，请先重新分析媒体信息。";
  }, false);
}

async function applyResolutionSuggestions() {
  if (!suggestions.value) return;
  await run(async () => {
    const result = await apiRequest<ResolutionApplyResult>("/api/videos/batch/resolution-tags", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ videoIds: props.videoIds })
    });
    emit("changed", `已确认清晰度标签：更新 ${result.updatedVideoCount} 个视频，${result.noSuggestionCount} 个无建议。`);
  });
}

async function startMediaJob(action: "Reanalyze" | "RegenerateCover") {
  await run(async () => {
    const job = await apiRequest<MediaJob>("/api/videos/batch/media-jobs", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ videoIds: props.videoIds, action })
    });
    emit("jobCreated", job);
  });
}

async function trashVideos() {
  if (!window.confirm(`将选中的 ${props.videoIds.length} 个视频移入回收站？原文件会移动到 Videos/trash，可从回收站恢复。`)) {
    return;
  }
  await run(async () => {
    const result = await apiRequest<BatchTrashResult>("/api/videos/batch/trash", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ videoIds: props.videoIds })
    });
    emit("changed", `已将 ${result.videoCount} 个视频移入回收站，移动 ${result.movedFileCount} 个文件。`);
  });
}

async function run(action: () => Promise<void>, closeOnSuccess = true) {
  busy.value = true;
  status.value = "正在处理...";
  try {
    await action();
    if (closeOnSuccess) emit("close");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="bulk-organizer-title">
      <header class="drawer-header">
        <div><h2 id="bulk-organizer-title">批量整理</h2><p>已选择 {{ videoIds.length }} 个视频</p></div>
        <button class="icon-button" type="button" aria-label="关闭" :disabled="busy" @click="emit('close')"><X /></button>
      </header>

      <div class="drawer-body bulk-organizer-body">
        <p v-if="status" class="form-status" :class="{ error: status.includes('失败') || status.includes('不存在') }">{{ status }}</p>

        <section class="bulk-section">
          <header><Tags :size="17" /><div><h3>批量标签</h3><p>只修改这里选择的标签，其他标签保持不变。</p></div></header>
          <div class="bulk-segmented" role="group" aria-label="标签操作">
            <button type="button" :class="{ active: tagMode === 'add' }" @click="tagMode = 'add'">添加标签</button>
            <button type="button" :class="{ active: tagMode === 'remove' }" @click="tagMode = 'remove'">移除标签</button>
          </div>
          <div v-for="category in categories" :key="category.id" class="bulk-tag-group">
            <h4>{{ category.name }}</h4>
            <div class="editor-tags">
              <button
                v-for="tag in category.tags"
                :key="tag.id"
                type="button"
                :class="{ active: selectedTagIds.includes(tag.id) }"
                @click="toggleTag(tag.id)"
              >#{{ tag.name }}</button>
            </div>
          </div>
          <button class="primary-button" type="button" :disabled="busy || !selectedTagIds.length" @click="applyTags">
            确认{{ tagMode === "add" ? "添加" : "移除" }}
          </button>
        </section>

        <section class="bulk-section">
          <header><RefreshCw :size="17" /><div><h3>清晰度标签</h3><p>根据视频短边建议 4K、1080P 或 720P，确认前不会修改。</p></div></header>
          <button class="secondary-button" type="button" :disabled="busy" @click="loadResolutionSuggestions">分析标签建议</button>
          <div v-if="suggestions" class="resolution-suggestions">
            <p v-for="group in suggestions.groups" :key="group.tagId"><strong>#{{ group.tagName }}</strong><span>{{ group.count }} 个</span></p>
            <p v-if="suggestions.noSuggestionCount"><strong>无建议</strong><span>{{ suggestions.noSuggestionCount }} 个</span></p>
            <button v-if="suggestions.groups.length" class="primary-button" type="button" :disabled="busy" @click="applyResolutionSuggestions">确认应用建议</button>
          </div>
        </section>

        <section class="bulk-section">
          <header><Image :size="17" /><div><h3>媒体重新处理</h3><p>任务在后台执行，可在片库顶部取消或失败后重试。</p></div></header>
          <div class="bulk-action-grid">
            <button class="secondary-button" type="button" :disabled="busy" @click="startMediaJob('Reanalyze')"><RefreshCw :size="15" />重新分析信息</button>
            <button class="secondary-button" type="button" :disabled="busy" @click="startMediaJob('RegenerateCover')"><Image :size="15" />重新生成封面</button>
          </div>
          <p class="bulk-caution"><AlertTriangle :size="13" />重新生成封面会替换手动上传的封面。</p>
        </section>

        <section class="bulk-section danger-zone">
          <header><Trash2 :size="17" /><div><h3>移入回收站</h3><p>数据库记录保留，原视频移动到 Videos/trash。</p></div></header>
          <button class="secondary-button danger" type="button" :disabled="busy" @click="trashVideos"><Trash2 :size="15" />批量移入回收站</button>
        </section>
      </div>
    </section>
  </div>
</template>
