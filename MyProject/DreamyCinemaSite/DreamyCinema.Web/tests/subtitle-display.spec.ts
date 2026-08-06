import { expect, test } from "@playwright/test";

test("mobile playback renders WebVTT captions outside the native video surface", async ({ page }) => {
  test.setTimeout(45_000);
  await page.goto("/login");
  await page.getByLabel("管理员密码").fill("Dreamy-Subtitle-QA-2026");
  const confirmPassword = page.getByLabel("确认密码");
  if (await confirmPassword.isVisible()) {
    await confirmPassword.fill("Dreamy-Subtitle-QA-2026");
    await page.getByRole("button", { name: "创建密码并进入" }).click();
  } else {
    await page.getByRole("button", { name: "登录" }).click();
  }

  await expect(page).toHaveURL(/\/$/);
  await page.getByTitle("同步视频").click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });
  await expect(page.locator(".video-card")).toHaveCount(1);

  const videoPage = await (await page.context().request.get("/api/videos?page=1&pageSize=1")).json();
  const selectedTrackId = videoPage.items[0].subtitles[0].id as string;

  await page.getByRole("button", { name: /在当前页面播放/ }).click();
  await expect(page.locator(".card-media-shell .subtitle-compat-bar")).toBeVisible();
  await page.locator(".media-panel video").evaluate(element => {
    (element as HTMLVideoElement).currentTime = 1;
  });
  await expect(page.locator(".card-media-shell .subtitle-compat-bar")).toContainText("Hello from Dreamy Cinema.");

  await page.getByRole("button", { name: "打开播放页" }).click();
  await expect(page.getByLabel("字幕轨道")).toHaveValue(selectedTrackId);
  await expect(page.getByLabel("字幕显示方式")).toHaveValue("below");
  await page.locator(".player-stage video").evaluate(element => {
    (element as HTMLVideoElement).currentTime = 1;
  });
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toContainText("Hello from Dreamy Cinema.");

  await page.getByLabel("字幕显示方式").selectOption("overlay");
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toHaveCount(0);
  await expect(page.locator(".player-stage .subtitle-overlay")).toContainText("Hello from Dreamy Cinema.");

  await page.getByLabel("字幕显示方式").selectOption("native");
  await expect(page.locator(".player-stage .subtitle-overlay")).toHaveCount(0);
  await expect.poll(() => page.locator(".player-stage video").evaluate((element, trackId) => {
    const video = element as HTMLVideoElement;
    const index = Array.from(video.querySelectorAll("track")).findIndex(track => track.dataset.trackId === trackId);
    return index >= 0 ? video.textTracks[index]?.mode : null;
  }, selectedTrackId)).toBe("showing");

  await page.getByLabel("字幕显示方式").selectOption("below");
  await expect(page.getByLabel("字幕显示方式")).toHaveValue("below");
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toContainText("Hello from Dreamy Cinema.");
  await page.screenshot({ path: "test-results/mobile-subtitle-display.png", fullPage: true });
});
