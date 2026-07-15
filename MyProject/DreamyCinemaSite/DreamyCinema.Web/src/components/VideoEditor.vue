<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { ImageUp, Save, Trash2, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import type { TagCategory, VideoItem } from "@/types";

const props = defineProps<{ open: boolean; videoId: string | null; categories: TagCategory[] }>();
const emit = defineEmits<{ close: []; saved: [message: string] }>();

const detail = ref<VideoItem | null>(null);
const number = ref("");
const title = ref("");
const description = ref("");
const tagIds = ref<number[]>([]);
const coverFile = ref<File | null>(null);
const removeCover = ref(false);
const previewUrl = ref<string | null>(null);
const status = ref("");
const saving = ref(false);
let objectUrl: string | null = null;

const hasPreview = computed(() => Boolean(previewUrl.value));

watch(() => [props.open, props.videoId] as const, async ([open, videoId]) => {
  if (!open || !videoId) return;
  resetPreview();
  status.value = "正在读取视频信息...";
  try {
    const video = await apiRequest<VideoItem>(`/api/videos/${encodeURIComponent(videoId)}`);
    detail.value = video;
    number.value = video.number;
    title.value = video.title;
    description.value = video.description;
    tagIds.value = video.tags.map(tag => tag.id);
    previewUrl.value = video.coverUrl;
    status.value = "";
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
}, { immediate: true });

function resetPreview() {
  if (objectUrl) URL.revokeObjectURL(objectUrl);
  objectUrl = null;
  coverFile.value = null;
  removeCover.value = false;
  previewUrl.value = null;
}

function chooseCover(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  if (objectUrl) URL.revokeObjectURL(objectUrl);
  coverFile.value = file;
  removeCover.value = false;
  objectUrl = URL.createObjectURL(file);
  previewUrl.value = objectUrl;
}

function clearCover() {
  if (objectUrl) URL.revokeObjectURL(objectUrl);
  objectUrl = null;
  coverFile.value = null;
  removeCover.value = Boolean(detail.value?.coverUrl);
  previewUrl.value = null;
}

function toggleTag(id: number) {
  tagIds.value = tagIds.value.includes(id) ? tagIds.value.filter(item => item !== id) : [...tagIds.value, id];
}

async function save() {
  if (!props.videoId) return;
  saving.value = true;
  status.value = "正在保存...";
  try {
    await apiRequest(`/api/videos/${encodeURIComponent(props.videoId)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ number: number.value, title: title.value, description: description.value, tagIds: tagIds.value })
    });
    if (coverFile.value) {
      const form = new FormData();
      form.append("cover", coverFile.value);
      await apiRequest(`/api/videos/${encodeURIComponent(props.videoId)}/cover`, { method: "POST", body: form });
    } else if (removeCover.value) {
      await apiRequest(`/api/videos/${encodeURIComponent(props.videoId)}/cover`, { method: "DELETE" });
    }
    emit("saved", "视频信息已保存。");
    emit("close");
  } catch (error) {
    status.value = error instanceof Error ? `保存失败：${error.message}` : String(error);
  } finally {
    saving.value = false;
  }
}

async function trashVideo() {
  if (!props.videoId || !window.confirm(`将“${number.value}”移入回收站？之后可以恢复。`)) return;
  saving.value = true;
  try {
    await apiRequest(`/api/videos/${encodeURIComponent(props.videoId)}/trash`, { method: "POST" });
    emit("saved", "视频已移入回收站。");
    emit("close");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    saving.value = false;
  }
}

onBeforeUnmount(resetPreview);
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="editor-title">
      <header class="drawer-header">
        <h2 id="editor-title">编辑视频</h2>
        <button class="icon-button" type="button" aria-label="关闭" @click="emit('close')"><X /></button>
      </header>

      <form class="drawer-form" @submit.prevent="save">
        <div class="drawer-body">
          <section class="cover-section" aria-label="封面设置">
          <div class="cover-editor" :class="{ 'has-image': hasPreview }">
            <img v-if="previewUrl" :src="previewUrl" alt="封面预览">
            <span v-else>暂无封面</span>
          </div>
          <div class="cover-actions">
            <label class="secondary-button"><ImageUp :size="16" /> 选择封面<input type="file" accept="image/jpeg,image/png,image/webp" hidden @change="chooseCover"></label>
            <button class="text-button danger" type="button" :disabled="!hasPreview" @click="clearCover">移除封面</button>
          </div>
          </section>

          <label class="field-label"><span>番号</span><input v-model="number" class="field-input" maxlength="120" required></label>
          <label class="field-label"><span>标题</span><input v-model="title" class="field-input" maxlength="300"></label>
          <label class="field-label"><span>简介</span><textarea v-model="description" class="field-input textarea" maxlength="1200"></textarea></label>

          <div class="field-label">标签</div>
          <section v-for="category in categories" :key="category.id" class="editor-tag-group">
            <h3>{{ category.name }}</h3>
            <div class="editor-tags">
              <button v-for="tag in category.tags" :key="tag.id" type="button" :class="{ active: tagIds.includes(tag.id) }" @click="toggleTag(tag.id)">
                {{ tag.name }}
              </button>
            </div>
          </section>

          <p v-if="status" class="form-status">{{ status }}</p>
        </div>
        <div class="drawer-footer">
          <button class="secondary-button danger" type="button" :disabled="saving" @click="trashVideo"><Trash2 :size="16" /> 移入回收站</button>
          <button class="primary-button" type="submit" :disabled="saving"><Save :size="16" /> {{ saving ? "保存中" : "保存修改" }}</button>
        </div>
      </form>
    </section>
  </div>
</template>
