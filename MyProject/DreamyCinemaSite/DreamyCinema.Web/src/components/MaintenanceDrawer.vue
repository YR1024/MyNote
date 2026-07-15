<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { RotateCcw, Trash2, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import type { MaintenanceVideo } from "@/types";
import { formatBytes } from "@/utils/format";

const props = defineProps<{ open: boolean }>();
const emit = defineEmits<{ close: []; changed: [message: string] }>();
const videos = ref<MaintenanceVideo[]>([]);
const status = ref("");
const loading = ref(false);
const trashed = computed(() => videos.value.filter(video => video.status === "Trashed"));
const missing = computed(() => videos.value.filter(video => video.status === "Missing"));

watch(() => props.open, open => { if (open) void load(); });

async function load() {
  loading.value = true;
  status.value = "";
  try {
    videos.value = await apiRequest<MaintenanceVideo[]>("/api/videos/maintenance");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    loading.value = false;
  }
}

async function restore(video: MaintenanceVideo) {
  await run(`/api/videos/${encodeURIComponent(video.id)}/restore`, { method: "POST" }, "视频已恢复。");
}

async function remove(video: MaintenanceVideo) {
  const text = video.status === "Trashed"
    ? `永久删除“${video.number}”？文件和记录都将删除，无法恢复。`
    : `移除缺失记录“${video.number}”？`;
  if (!window.confirm(text)) return;
  await run(`/api/videos/${encodeURIComponent(video.id)}`, { method: "DELETE" }, "记录已删除。");
}

async function run(url: string, options: RequestInit, message: string) {
  status.value = "正在处理...";
  try {
    await apiRequest(url, options);
    await load();
    status.value = message;
    emit("changed", message);
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="maintenance-title">
      <header class="drawer-header"><h2 id="maintenance-title">片库维护</h2><button class="icon-button" type="button" aria-label="关闭" @click="emit('close')"><X /></button></header>
      <div class="drawer-body">
        <p v-if="loading" class="form-status">正在检查片库...</p>
        <p v-if="status" class="form-status">{{ status }}</p>

        <section class="maintenance-group">
          <h3>回收站 <span>{{ trashed.length }}</span></h3>
          <p v-if="!trashed.length" class="empty-copy">回收站为空</p>
          <article v-for="video in trashed" :key="video.id" class="maintenance-row">
            <div class="maintenance-cover"><img v-if="video.coverUrl" :src="video.coverUrl" alt=""><span v-else>无封面</span></div>
            <div class="maintenance-info"><strong>{{ video.number }}</strong><small>{{ video.fileName }} · {{ formatBytes(video.size) }}</small>
              <div class="maintenance-actions"><button class="secondary-button" type="button" @click="restore(video)"><RotateCcw :size="15" />恢复</button><button class="text-button danger" type="button" @click="remove(video)"><Trash2 :size="15" />永久删除</button></div>
            </div>
          </article>
        </section>

        <section class="maintenance-group">
          <h3>缺失文件 <span>{{ missing.length }}</span></h3>
          <p v-if="!missing.length" class="empty-copy">没有缺失记录</p>
          <article v-for="video in missing" :key="video.id" class="maintenance-row">
            <div class="maintenance-cover"><img v-if="video.coverUrl" :src="video.coverUrl" alt=""><span v-else>无封面</span></div>
            <div class="maintenance-info"><strong>{{ video.number }}</strong><small>{{ video.fileName }} · {{ formatBytes(video.size) }}</small>
              <div class="maintenance-actions"><button class="text-button danger" type="button" @click="remove(video)"><Trash2 :size="15" />移除记录</button></div>
            </div>
          </article>
        </section>
      </div>
    </section>
  </div>
</template>
