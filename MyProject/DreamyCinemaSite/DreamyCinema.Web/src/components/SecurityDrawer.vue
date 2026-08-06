<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { Clock3, KeyRound, ScrollText, ShieldCheck, TriangleAlert, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import { useSessionStore } from "@/stores/session";
import type { SecurityAuditEntry, SessionInfo } from "@/types";

const props = defineProps<{ open: boolean; session: SessionInfo | null }>();
const emit = defineEmits<{ close: [] }>();
const sessionStore = useSessionStore();
const entries = ref<SecurityAuditEntry[]>([]);
const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const loading = ref(false);
const submitting = ref(false);
const status = ref("");

const sessionExpiration = computed(() => formatDateTime(props.session?.sessionExpiresAt));
const passwordUpdatedAt = computed(() => formatDateTime(props.session?.passwordUpdatedAt));

watch(() => props.open, open => {
  if (!open) return;
  currentPassword.value = "";
  newPassword.value = "";
  confirmPassword.value = "";
  status.value = "";
  void loadAudit();
});

async function loadAudit() {
  loading.value = true;
  try {
    entries.value = await apiRequest<SecurityAuditEntry[]>("/api/security/audit?take=30");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    loading.value = false;
  }
}

async function changePassword() {
  status.value = "";
  if (newPassword.value.length < 10) {
    status.value = "新密码至少需要 10 个字符。";
    return;
  }
  if (newPassword.value.length > 128) {
    status.value = "新密码不能超过 128 个字符。";
    return;
  }
  if (newPassword.value !== confirmPassword.value) {
    status.value = "两次输入的新密码不一致。";
    return;
  }
  if (newPassword.value === currentPassword.value) {
    status.value = "新密码不能与当前密码相同。";
    return;
  }

  submitting.value = true;
  try {
    await sessionStore.changePassword(currentPassword.value, newPassword.value);
    window.location.assign("/login?reason=password-changed");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  } finally {
    submitting.value = false;
  }
}

function formatDateTime(value: string | null | undefined) {
  if (!value) return "--";
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

function actionLabel(action: string) {
  const labels: Record<string, string> = {
    "auth.setup": "创建管理员密码",
    "auth.login": "管理员登录",
    "auth.logout": "退出登录",
    "auth.password.change": "修改管理员密码",
    "backup.restore": "恢复备份",
    "backup.manage": "管理备份",
    "library.sync": "同步片库",
    "tags.manage": "管理标签",
    "jobs.manage": "管理后台任务",
    "videos.manage": "管理视频",
    "api.write": "写入数据"
  };
  return labels[action] ?? action;
}

function outcomeLabel(outcome: SecurityAuditEntry["outcome"]) {
  if (outcome === "Succeeded") return "成功";
  if (outcome === "Denied") return "无权限";
  if (outcome === "Rejected") return "已拒绝";
  return "失败";
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="security-title">
      <header class="drawer-header">
        <div><h2 id="security-title">账户与安全</h2><p>单管理员访问边界与管理记录</p></div>
        <button class="icon-button" type="button" aria-label="关闭" @click="emit('close')"><X /></button>
      </header>

      <form class="drawer-form" @submit.prevent="changePassword">
        <div class="drawer-body security-body">
          <section class="security-section">
            <header><ShieldCheck :size="19" /><div><h3>当前访问边界</h3><p>片库 API 仅登录后可用，所有写操作同时校验 CSRF。</p></div></header>
            <dl class="security-facts">
              <div><dt>本次登录到期</dt><dd>{{ sessionExpiration }}</dd></div>
              <div><dt>密码更新时间</dt><dd>{{ passwordUpdatedAt }}</dd></div>
              <div><dt>服务范围</dt><dd>可信局域网，HTTP 未加密</dd></div>
            </dl>
            <p class="security-warning"><TriangleAlert :size="15" />不要把 5210 端口直接映射到公网；异地访问应使用 Tailscale 或 HTTPS 反向代理。</p>
          </section>

          <section class="security-section">
            <header><KeyRound :size="19" /><div><h3>修改管理员密码</h3><p>成功后本机和其他设备上的旧登录都会立即失效。</p></div></header>
            <label class="field-label">当前密码
              <input v-model="currentPassword" class="field-input" type="password" maxlength="128" autocomplete="current-password" required>
            </label>
            <label class="field-label">新密码
              <input v-model="newPassword" class="field-input" type="password" minlength="10" maxlength="128" autocomplete="new-password" required>
            </label>
            <label class="field-label">确认新密码
              <input v-model="confirmPassword" class="field-input" type="password" minlength="10" maxlength="128" autocomplete="new-password" required>
            </label>
            <p v-if="status" class="form-status error">{{ status }}</p>
          </section>

          <section class="security-section audit-section">
            <header><ScrollText :size="19" /><div><h3>最近管理记录</h3><p>仅记录动作结果，不保存密码、Cookie、CSRF 或请求正文。</p></div></header>
            <p v-if="loading" class="empty-copy">正在读取记录...</p>
            <p v-else-if="!entries.length" class="empty-copy">暂无管理记录</p>
            <ol v-else class="audit-list">
              <li v-for="entry in entries" :key="entry.id">
                <div><strong>{{ actionLabel(entry.action) }}</strong><span :data-outcome="entry.outcome">{{ outcomeLabel(entry.outcome) }}</span></div>
                <p><Clock3 :size="12" />{{ formatDateTime(entry.createdAt) }} · {{ entry.method }} · {{ entry.statusCode }}</p>
                <small>{{ entry.path }}<template v-if="entry.remoteAddress"> · {{ entry.remoteAddress }}</template></small>
              </li>
            </ol>
          </section>
        </div>

        <footer class="drawer-footer">
          <button class="secondary-button" type="button" @click="emit('close')">取消</button>
          <button class="primary-button" type="submit" :disabled="submitting">
            <KeyRound :size="16" />{{ submitting ? "正在修改..." : "修改密码" }}
          </button>
        </footer>
      </form>
    </section>
  </div>
</template>
