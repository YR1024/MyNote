import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  timeout: 30_000,
  use: {
    baseURL: process.env.DREAMY_BASE_URL ?? "http://127.0.0.1:5250",
    ...devices["Pixel 7"],
    channel: "msedge",
    screenshot: "only-on-failure",
    trace: "retain-on-failure"
  }
});
