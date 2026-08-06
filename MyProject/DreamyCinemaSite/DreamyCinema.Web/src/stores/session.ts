import { defineStore } from "pinia";
import { apiRequest, loadSession } from "@/api/client";
import type { SessionInfo } from "@/types";

export const useSessionStore = defineStore("session", {
  state: () => ({
    info: null as SessionInfo | null,
    loading: false
  }),
  actions: {
    async refresh() {
      this.loading = true;
      try {
        this.info = await loadSession();
        return this.info;
      } finally {
        this.loading = false;
      }
    },
    async login(password: string) {
      await apiRequest("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ password }),
        skipAuthRedirect: true
      });
      return this.refresh();
    },
    async setup(password: string) {
      await apiRequest("/api/auth/setup", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ password }),
        skipAuthRedirect: true
      });
      return this.refresh();
    },
    async changePassword(currentPassword: string, newPassword: string) {
      await apiRequest("/api/auth/change-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ currentPassword, newPassword })
      });
      this.info = null;
    },
    async logout() {
      await apiRequest("/api/auth/logout", { method: "POST" });
      this.info = await loadSession();
    }
  }
});
