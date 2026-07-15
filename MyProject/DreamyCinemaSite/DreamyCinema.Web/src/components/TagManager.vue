<script setup lang="ts">
import { reactive, ref } from "vue";
import { Check, Pencil, Trash2, X } from "@lucide/vue";
import { apiRequest } from "@/api/client";
import type { TagCategory, TagItem } from "@/types";

defineProps<{ open: boolean; categories: TagCategory[] }>();
const emit = defineEmits<{ close: []; changed: [message: string] }>();
const status = ref("");
const newCategory = ref("");
const newTags = reactive<Record<number, string>>({});
const editingKey = ref("");
const editingName = ref("");

async function mutate(url: string, options: RequestInit, message: string) {
  status.value = "正在保存...";
  try {
    await apiRequest(url, options);
    status.value = message;
    editingKey.value = "";
    emit("changed", message);
  } catch (error) {
    status.value = error instanceof Error ? `操作失败：${error.message}` : String(error);
  }
}

function edit(key: string, name: string) {
  editingKey.value = key;
  editingName.value = name;
}

async function renameCategory(category: TagCategory) {
  await mutate(`/api/tag-categories/${category.id}`, {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: editingName.value })
  }, "分类已重命名。");
}

async function renameTag(tag: TagItem) {
  await mutate(`/api/tags/${tag.id}`, {
    method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: editingName.value })
  }, "标签已重命名。");
}

async function addCategory() {
  await mutate("/api/tag-categories", {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: newCategory.value })
  }, "分类已添加。");
  newCategory.value = "";
}

async function addTag(category: TagCategory) {
  await mutate(`/api/tag-categories/${category.id}/tags`, {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: newTags[category.id] })
  }, "标签已添加。");
  newTags[category.id] = "";
}

async function removeCategory(category: TagCategory) {
  if (!window.confirm(`删除分类“${category.name}”？其中标签及视频关联也会移除。`)) return;
  await mutate(`/api/tag-categories/${category.id}`, { method: "DELETE" }, "分类已删除。");
}

async function removeTag(tag: TagItem) {
  if (!window.confirm(`删除标签“${tag.name}”？视频上的该标签也会移除。`)) return;
  await mutate(`/api/tags/${tag.id}`, { method: "DELETE" }, "标签已删除。");
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" @click.self="emit('close')">
    <section class="drawer" role="dialog" aria-modal="true" aria-labelledby="tags-title">
      <header class="drawer-header"><h2 id="tags-title">标签管理</h2><button class="icon-button" type="button" aria-label="关闭" @click="emit('close')"><X /></button></header>
      <div class="drawer-body">
        <form class="inline-form" @submit.prevent="addCategory">
          <input v-model="newCategory" class="field-input" maxlength="80" placeholder="新分类名称" required>
          <button class="primary-button" type="submit">添加分类</button>
        </form>
        <p v-if="status" class="form-status">{{ status }}</p>

        <section v-for="category in categories" :key="category.id" class="manage-section">
          <div class="manage-heading">
            <form v-if="editingKey === `category-${category.id}`" class="rename-form" @submit.prevent="renameCategory(category)">
              <input v-model="editingName" class="field-input" required><button class="icon-button" aria-label="保存"><Check /></button>
            </form>
            <h3 v-else>{{ category.name }}</h3>
            <div class="row-actions">
              <button class="icon-button" type="button" aria-label="重命名" @click="edit(`category-${category.id}`, category.name)"><Pencil /></button>
              <button class="icon-button danger" type="button" aria-label="删除" @click="removeCategory(category)"><Trash2 /></button>
            </div>
          </div>

          <div class="manage-tag-list">
            <div v-for="tag in category.tags" :key="tag.id" class="manage-tag-row">
              <form v-if="editingKey === `tag-${tag.id}`" class="rename-form" @submit.prevent="renameTag(tag)">
                <input v-model="editingName" class="field-input" required><button class="icon-button" aria-label="保存"><Check /></button>
              </form>
              <span v-else>#{{ tag.name }} <small>{{ tag.videoCount }}</small></span>
              <div class="row-actions">
                <button class="icon-button" type="button" aria-label="重命名" @click="edit(`tag-${tag.id}`, tag.name)"><Pencil /></button>
                <button class="icon-button danger" type="button" aria-label="删除" @click="removeTag(tag)"><Trash2 /></button>
              </div>
            </div>
          </div>

          <form class="inline-form" @submit.prevent="addTag(category)">
            <input v-model="newTags[category.id]" class="field-input" maxlength="80" :placeholder="`添加到${category.name}`" required>
            <button class="secondary-button" type="submit">添加标签</button>
          </form>
        </section>
      </div>
    </section>
  </div>
</template>
