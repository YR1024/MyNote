<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { Film, LockKeyhole } from "@lucide/vue";
import { useSessionStore } from "@/stores/session";

const router = useRouter();
const session = useSessionStore();
const password = ref("");
const confirmPassword = ref("");
const status = ref("");
const submitting = ref(false);

const setupRequired = computed(() => session.info?.setupRequired === true);
const canSubmitSetup = computed(() => !setupRequired.value || session.info?.canSetup === true);

onMounted(async () => {
  try {
    const info = await session.refresh();
    if (info.authenticated) await router.replace("/");
  } catch (error) {
    status.value = error instanceof Error ? error.message : String(error);
  }
});

async function submit() {
  status.value = "";
  if (password.value.length < 10) {
    status.value = "密码至少需要 10 个字符。";
    return;
  }
  if (setupRequired.value && password.value !== confirmPassword.value) {
    status.value = "两次输入的密码不一致。";
    return;
  }

  submitting.value = true;
  try {
    const info = setupRequired.value
      ? await session.setup(password.value)
      : await session.login(password.value);
    if (info.authenticated) await router.replace("/");
  } catch (error) {
    status.value = error instanceof Error ? error.message : "登录失败，请检查密码。";
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-panel">
      <div class="brand-mark"><Film :size="28" /></div>
      <p class="eyebrow">PRIVATE VIDEO LIBRARY</p>
      <h1>Dreamy Cinema</h1>
      <p class="login-copy">
        {{ setupRequired ? "首次使用，请创建管理员密码。" : "输入管理员密码进入片库。" }}
      </p>

      <form class="login-form" @submit.prevent="submit">
        <label class="field-label" for="password">管理员密码</label>
        <div class="input-with-icon">
          <LockKeyhole :size="18" />
          <input id="password" v-model="password" type="password" minlength="10" autocomplete="current-password" required>
        </div>

        <template v-if="setupRequired">
          <label class="field-label" for="confirmPassword">确认密码</label>
          <div class="input-with-icon">
            <LockKeyhole :size="18" />
            <input id="confirmPassword" v-model="confirmPassword" type="password" minlength="10" autocomplete="new-password" required>
          </div>
        </template>

        <p v-if="setupRequired && !canSubmitSetup" class="form-status error">
          首次设置只能在服务器电脑上完成。
        </p>
        <p v-if="status" class="form-status error">{{ status }}</p>

        <button class="primary-button wide" type="submit" :disabled="submitting || !canSubmitSetup">
          {{ submitting ? "请稍候..." : setupRequired ? "创建密码并进入" : "登录" }}
        </button>
      </form>
    </section>
  </main>
</template>
