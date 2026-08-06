<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { RotateCcw, Settings2, X } from "@lucide/vue";
import type { HardSubtitleStyle } from "@/types";

const props = defineProps<{
  open: boolean;
  modelValue: HardSubtitleStyle;
  defaults: HardSubtitleStyle;
  previewUrl: string | null;
}>();

const emit = defineEmits<{
  close: [];
  apply: [style: HardSubtitleStyle];
}>();

const draft = ref<HardSubtitleStyle>(cloneStyle(props.modelValue));
const previewStyle = computed(() => ({
  color: toRgba(draft.value.textColor, draft.value.textOpacity),
  fontFamily: `"${draft.value.fontName}", "Microsoft YaHei", sans-serif`,
  fontSize: `${draft.value.fontSize}px`,
  WebkitTextStroke: `${draft.value.outline}px ${draft.value.outlineColor}`,
  paintOrder: "stroke fill",
  bottom: `${Math.max(8, Math.round(draft.value.marginV / 2))}px`
}));

watch(
  () => props.open,
  open => {
    if (open) draft.value = cloneStyle(props.modelValue);
  }
);

function cloneStyle(value: HardSubtitleStyle): HardSubtitleStyle {
  return { ...value };
}

function resetDefaults() {
  draft.value = cloneStyle(props.defaults);
}

function apply() {
  emit("apply", cloneStyle(draft.value));
}

function toRgba(color: string, opacity: number) {
  const red = Number.parseInt(color.slice(1, 3), 16);
  const green = Number.parseInt(color.slice(3, 5), 16);
  const blue = Number.parseInt(color.slice(5, 7), 16);
  return `rgba(${red}, ${green}, ${blue}, ${opacity / 100})`;
}
</script>

<template>
  <div
    v-if="open"
    class="drawer-backdrop hard-subtitle-settings-backdrop"
    role="presentation"
    @click.self="emit('close')"
  >
    <section
      class="drawer hard-subtitle-settings-drawer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="hard-subtitle-settings-title"
    >
      <header class="drawer-header">
        <div>
          <h2 id="hard-subtitle-settings-title">硬字幕样式</h2>
          <p>本次生成会保存以下样式</p>
        </div>
        <button class="icon-button" type="button" aria-label="关闭硬字幕样式" @click="emit('close')">
          <X :size="17" />
        </button>
      </header>

      <form class="drawer-form" @submit.prevent="apply">
        <div class="drawer-body hard-subtitle-settings-body">
          <section class="hard-subtitle-preview" aria-label="字幕样式预览">
            <img v-if="previewUrl" :src="previewUrl" alt="">
            <div class="hard-subtitle-preview-shade" />
            <span class="hard-subtitle-preview-label"><Settings2 :size="13" />实时预览</span>
            <p :style="previewStyle">这里显示硬字幕预览效果</p>
          </section>

          <label class="field-label">
            <span>字体</span>
            <input
              v-model.trim="draft.fontName"
              class="field-input"
              type="text"
              maxlength="80"
              autocomplete="off"
              required
              aria-label="硬字幕字体"
            >
          </label>

          <label class="hard-subtitle-style-field">
            <span><strong>字体大小</strong><output>{{ draft.fontSize }} px</output></span>
            <input v-model.number="draft.fontSize" type="range" min="12" max="48" step="1" aria-label="硬字幕字体大小">
          </label>

          <div class="hard-subtitle-color-grid">
            <label class="hard-subtitle-color-field">
              <span>字体颜色</span>
              <span class="hard-subtitle-color-control">
                <input v-model="draft.textColor" type="color" aria-label="硬字幕字体颜色">
                <output>{{ draft.textColor.toUpperCase() }}</output>
              </span>
            </label>
            <label class="hard-subtitle-color-field">
              <span>描边颜色</span>
              <span class="hard-subtitle-color-control">
                <input v-model="draft.outlineColor" type="color" aria-label="硬字幕描边颜色">
                <output>{{ draft.outlineColor.toUpperCase() }}</output>
              </span>
            </label>
          </div>

          <label class="hard-subtitle-style-field">
            <span><strong>字体不透明度</strong><output>{{ draft.textOpacity }}%</output></span>
            <input v-model.number="draft.textOpacity" type="range" min="20" max="100" step="1" aria-label="硬字幕字体不透明度">
          </label>

          <label class="hard-subtitle-style-field">
            <span><strong>描边宽度</strong><output>{{ draft.outline }} px</output></span>
            <input v-model.number="draft.outline" type="range" min="0" max="6" step="1" aria-label="硬字幕描边宽度">
          </label>

          <label class="hard-subtitle-style-field">
            <span><strong>底部间距</strong><output>{{ draft.marginV }} px</output></span>
            <input v-model.number="draft.marginV" type="range" min="0" max="160" step="2" aria-label="硬字幕底部间距">
          </label>

          <p class="hard-subtitle-preview-note">
            预览用于估算大小和配色；最终效果由 FFmpeg/libass 按视频分辨率渲染。
          </p>
        </div>

        <footer class="drawer-footer hard-subtitle-settings-footer">
          <button class="text-button" type="button" @click="resetDefaults">
            <RotateCcw :size="14" />恢复默认
          </button>
          <div>
            <button class="secondary-button" type="button" @click="emit('close')">取消</button>
            <button class="primary-button" type="submit">使用此样式</button>
          </div>
        </footer>
      </form>
    </section>
  </div>
</template>

<style scoped>
.hard-subtitle-settings-backdrop { z-index: 70; }
.hard-subtitle-settings-drawer { width: min(100%, 560px); }
.hard-subtitle-settings-body { gap: 16px; }
.hard-subtitle-preview {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  overflow: hidden;
  border: 1px solid #334555;
  border-radius: 8px;
  background: #05080c;
}
.hard-subtitle-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.hard-subtitle-preview-shade {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, .08);
}
.hard-subtitle-preview-label {
  position: absolute;
  top: 9px;
  left: 9px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  border-radius: 5px;
  padding: 4px 6px;
  background: rgba(4, 9, 14, .78);
  color: #a9bac8;
  font-size: 10px;
}
.hard-subtitle-preview p {
  position: absolute;
  right: 12px;
  left: 12px;
  z-index: 1;
  margin: 0;
  font-weight: 700;
  line-height: 1.35;
  text-align: center;
  overflow-wrap: anywhere;
}
.hard-subtitle-style-field {
  display: grid;
  gap: 9px;
  color: #c5cfdb;
  font-size: 12px;
}
.hard-subtitle-style-field > span {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}
.hard-subtitle-style-field strong { font-weight: 650; }
.hard-subtitle-style-field output { color: #82d7e5; font-variant-numeric: tabular-nums; }
.hard-subtitle-style-field input[type="range"] {
  width: 100%;
  margin: 0;
  accent-color: #3db3ca;
}
.hard-subtitle-color-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 9px;
}
.hard-subtitle-color-field {
  display: grid;
  gap: 7px;
  color: #c5cfdb;
  font-size: 12px;
}
.hard-subtitle-color-control {
  display: flex;
  min-width: 0;
  height: 42px;
  align-items: center;
  gap: 9px;
  border: 1px solid #2b394a;
  border-radius: 7px;
  padding: 5px 8px;
  background: #0c121a;
}
.hard-subtitle-color-control input {
  width: 35px;
  height: 28px;
  flex: 0 0 auto;
  border: 0;
  padding: 0;
  background: transparent;
}
.hard-subtitle-color-control output {
  min-width: 0;
  color: #aebac6;
  font-size: 11px;
  overflow-wrap: anywhere;
}
.hard-subtitle-preview-note {
  margin: 0;
  color: #758493;
  font-size: 10px;
  line-height: 1.55;
}
.hard-subtitle-settings-footer > div { display: flex; gap: 8px; }

@media (max-width: 380px) {
  .hard-subtitle-color-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
