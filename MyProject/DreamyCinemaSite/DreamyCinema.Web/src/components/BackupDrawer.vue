<script setup lang="ts">
import { ref, watch } from "vue";
import { DatabaseBackup, Download, Plus, RotateCcw, ShieldCheck, Trash2, TriangleAlert, Upload, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import type { BackupPackage, RestoreResult } from "@/types";
import { formatBytes } from "@/utils/format";

const props = defineProps<{ open: boolean }>();
const emit = defineEmits<{ close: []; restored: [message: string] }>();
const backups = ref<BackupPackage[]>([]);
const fileInput = ref<HTMLInputElement | null>(null);
const loading = ref(false);
const creating = ref(false);
const importing = ref(false);
const deletingId = ref("");
const restoringId = ref("");
const status = ref("");

watch(() => props.open, open => {
  if (open) void load();
});

async function load() {
  loading.value = true;
  status.value = "";
  try {
    backups.value = await apiRequest<BackupPackage[]>("/api/backups");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    loading.value = false;
  }
}

async function createBackup() {
  creating.value = true;
  status.value = "正在创建一致性快照并校验备份包...";
  try {
    const created = await apiRequest<BackupPackage>("/api/backups", { method: "POST" });
    await load();
    status.value = created.warnings.length
      ? `备份已创建，但有 ${created.warnings.length} 项文件警告。`
      : "备份已创建并校验完成。";
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    creating.value = false;
  }
}

async function importBackup(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  importing.value = true;
  status.value = "正在上传并校验备份包...";
  try {
    const form = new FormData();
    form.append("file", file);
    const imported = await apiRequest<BackupPackage>("/api/backups/import", {
      method: "POST",
      body: form
    });
    await load();
    status.value = `已导入 ${formatCreatedAt(imported.createdAt)} 的备份，恢复前仍会再次完整校验。`;
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    importing.value = false;
    input.value = "";
  }
}

async function restoreBackup(backup: BackupPackage) {
  const confirmed = window.confirm(
    `恢复 ${formatCreatedAt(backup.createdAt)} 的备份？\n\n`
    + "数据库、标签、封面和字幕将恢复到该时间点；原视频和回收站视频不会被移动或删除。"
    + "\n系统会先自动创建一份“恢复前快照”，失败时自动回滚。"
  );
  if (!confirmed) return;

  restoringId.value = backup.id;
  status.value = "正在校验备份、创建恢复前快照并应用数据，请勿关闭服务...";
  try {
    const result = await apiRequest<RestoreResult>(
      `/api/backups/${encodeURIComponent(backup.id)}/restore`,
      { method: "POST" }
    );
    await load();
    const missing = result.missingVideoCount
      ? `，其中 ${result.missingVideoCount} 个原视频当前缺失`
      : "";
    const restart = result.configurationRestartRequired
      ? "；AI 字幕偏好将在下次重启服务后生效"
      : "";
    const message = `已恢复 ${result.database.videoCount} 个视频记录${missing}${restart}。`;
    status.value = message;
    emit("restored", message);
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    restoringId.value = "";
  }
}

async function removeBackup(backup: BackupPackage) {
  if (!window.confirm(`删除 ${formatCreatedAt(backup.createdAt)} 创建的备份？此操作不会影响当前片库。`)) {
    return;
  }

  deletingId.value = backup.id;
  status.value = "正在删除备份...";
  try {
    await apiRequest(`/api/backups/${encodeURIComponent(backup.id)}`, { method: "DELETE" });
    await load();
    status.value = "备份已删除。";
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    deletingId.value = "";
  }
}

function purposeLabel(purpose: BackupPackage["purpose"]) {
  if (purpose === "PreRestore") return "恢复前快照";
  if (purpose === "Imported") return "已导入";
  if (purpose === "Unknown") return "未知来源";
  return "手动备份";
}

function formatCreatedAt(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "--";
  return new Intl.DateTimeFormat("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  }).format(date);
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="backup-title">
      <header class="drawer-header">
        <h2 id="backup-title">数据备份</h2>
        <button class="icon-button" type="button" aria-label="关闭" @click="emit('close')"><X /></button>
      </header>

      <div class="drawer-body backup-body">
        <section class="backup-scope">
          <div class="backup-scope-heading">
            <DatabaseBackup :size="20" />
            <div><strong>片库整理数据</strong><span>可独立于原视频保存和迁移</span></div>
          </div>
          <ul>
            <li><ShieldCheck :size="15" />包含数据库、标签、封面、字幕、校对结果和翻译词汇表</li>
            <li><ShieldCheck :size="15" />创建时执行 SQLite 完整性检查和 SHA-256 文件校验</li>
            <li><TriangleAlert :size="15" />不包含原视频、回收站视频、管理员密码、Cookie 或 AI 密钥</li>
          </ul>
          <button
            class="primary-button wide"
            type="button"
            :disabled="creating || importing || Boolean(restoringId)"
            @click="createBackup"
          >
            <Plus :size="17" />{{ creating ? "正在创建..." : "创建新备份" }}
          </button>
          <input
            ref="fileInput"
            class="backup-file-input"
            type="file"
            accept=".zip,application/zip"
            @change="importBackup"
          >
          <button
            class="secondary-button wide backup-import"
            type="button"
            :disabled="importing || creating || Boolean(restoringId)"
            @click="fileInput?.click()"
          >
            <Upload :size="17" />{{ importing ? "正在导入..." : "导入 ZIP 备份" }}
          </button>
        </section>

        <p v-if="status" class="form-status backup-status">{{ status }}</p>
        <p v-if="loading" class="empty-copy">正在读取备份...</p>
        <p v-else-if="!backups.length" class="empty-copy">尚未创建备份</p>

        <section v-else class="backup-list" aria-label="备份列表">
          <article v-for="backup in backups" :key="backup.id || backup.fileName" class="backup-card" :class="{ invalid: !backup.isValid }">
            <header>
              <div>
                <div class="backup-title-line">
                  <strong>{{ formatCreatedAt(backup.createdAt) }}</strong>
                  <span class="backup-purpose" :class="{ automatic: backup.purpose === 'PreRestore' }">
                    {{ purposeLabel(backup.purpose) }}
                  </span>
                </div>
                <span>{{ formatBytes(backup.size) }}</span>
              </div>
              <span class="backup-validity" :class="{ invalid: !backup.isValid }">
                <ShieldCheck v-if="backup.isValid" :size="14" />{{ backup.isValid ? "清单有效" : "包已损坏" }}
              </span>
            </header>

            <div v-if="backup.isValid" class="backup-stats">
              <span>{{ backup.videoCount }} 个视频记录</span>
              <span>{{ backup.coverCount }} 张封面</span>
              <span>{{ backup.subtitleFileCount }} 个字幕文件</span>
            </div>
            <p v-else class="backup-error">{{ backup.validationError }}</p>
            <p v-if="backup.sha256" class="backup-checksum" :title="backup.sha256">SHA-256 {{ backup.sha256 }}</p>
            <ul v-if="backup.warnings.length" class="backup-warnings">
              <li v-for="warning in backup.warnings" :key="warning"><TriangleAlert :size="13" />{{ warning }}</li>
            </ul>

            <footer>
              <div class="backup-safe-actions">
                <a
                  v-if="backup.isValid"
                  class="secondary-button backup-download"
                  :href="`/api/backups/${encodeURIComponent(backup.id)}/download`"
                  download
                ><Download :size="15" />下载</a>
                <button
                  v-if="backup.isValid"
                  class="secondary-button backup-restore"
                  type="button"
                  :disabled="Boolean(restoringId) || importing || creating"
                  @click="restoreBackup(backup)"
                ><RotateCcw :size="15" />{{ restoringId === backup.id ? "恢复中..." : "恢复" }}</button>
              </div>
              <button
                class="text-button danger"
                type="button"
                :disabled="deletingId === backup.id || Boolean(restoringId)"
                @click="removeBackup(backup)"
              ><Trash2 :size="15" />{{ deletingId === backup.id ? "删除中..." : "删除" }}</button>
            </footer>
          </article>
        </section>
      </div>
    </section>
  </div>
</template>
