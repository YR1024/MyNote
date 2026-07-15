<script setup lang="ts">
import { computed } from "vue";
import { X } from "@lucide/vue";
import type { TagCategory } from "@/types";

const props = defineProps<{
  categories: TagCategory[];
  activeCategoryId: number | null;
  selectedTagIds: number[];
}>();

const emit = defineEmits<{
  selectCategory: [id: number];
  toggleTag: [id: number];
  clear: [];
}>();

const activeCategory = computed(() => props.categories.find(item => item.id === props.activeCategoryId));
const selected = computed(() => {
  const ids = new Set(props.selectedTagIds);
  return props.categories.flatMap(category => category.tags).filter(tag => ids.has(tag.id));
});
</script>

<template>
  <section class="filter-block" aria-label="标签筛选">
    <div v-if="selected.length" class="selected-tags">
      <button v-for="tag in selected" :key="tag.id" class="selected-chip" type="button" @click="emit('toggleTag', tag.id)">
        #{{ tag.name }} <X :size="13" />
      </button>
      <button class="clear-button" type="button" @click="emit('clear')">清空</button>
    </div>
    <p v-else class="filter-empty">未选择标签</p>

    <div class="category-tabs">
      <button
        v-for="category in categories"
        :key="category.id"
        class="category-tab"
        :class="{ active: category.id === activeCategoryId }"
        type="button"
        @click="emit('selectCategory', category.id)"
      >
        {{ category.name }}
      </button>
    </div>

    <div class="tag-grid">
      <button
        v-for="tag in activeCategory?.tags ?? []"
        :key="tag.id"
        class="tag-option"
        :class="{ active: selectedTagIds.includes(tag.id) }"
        type="button"
        @click="emit('toggleTag', tag.id)"
      >
        #{{ tag.name }} <span>{{ tag.videoCount }}</span>
      </button>
    </div>
    <p class="filter-note">已选标签按交集筛选。</p>
  </section>
</template>
