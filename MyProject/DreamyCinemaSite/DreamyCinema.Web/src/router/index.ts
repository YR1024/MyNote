import { createRouter, createWebHistory } from "vue-router";
import LibraryView from "@/views/LibraryView.vue";
import LoginView from "@/views/LoginView.vue";
import PlayerView from "@/views/PlayerView.vue";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/login", name: "login", component: LoginView },
    { path: "/", name: "library", component: LibraryView },
    { path: "/videos/:id/play", name: "player", component: PlayerView, props: true },
    { path: "/:pathMatch(.*)*", redirect: "/" }
  ],
  scrollBehavior: () => ({ top: 0 })
});
