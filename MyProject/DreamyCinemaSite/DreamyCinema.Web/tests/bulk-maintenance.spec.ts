import fs from "node:fs";
import path from "node:path";
import { expect, test } from "@playwright/test";

const videoRoot = process.env.DREAMY_TEST_VIDEO_ROOT;

test("admin can bulk organize videos and retry media maintenance", async ({ page }) => {
  test.setTimeout(120_000);
  expect(videoRoot, "DREAMY_TEST_VIDEO_ROOT must point to the isolated fixture library").toBeTruthy();

  await page.goto("/login");
  await page.getByLabel("管理员密码").fill("Dreamy-Bulk-2026");
  const confirmPassword = page.getByLabel("确认密码");
  if (await confirmPassword.isVisible()) {
    await confirmPassword.fill("Dreamy-Bulk-2026");
    await page.getByRole("button", { name: "创建密码并进入" }).click();
  } else {
    await page.getByRole("button", { name: "登录" }).click();
  }

  await page.getByTitle("同步视频").click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 40_000 });
  await expect(page.locator(".video-card")).toHaveCount(3);

  const initialPage = await (await page.context().request.get("/api/videos?page=1&pageSize=20")).json();
  expect(initialPage.total).toBe(3);
  const fullHd = initialPage.items.find((video: { width: number; height: number }) =>
    Math.min(video.width, video.height) === 1080);
  const hd = initialPage.items.find((video: { width: number; height: number }) =>
    Math.min(video.width, video.height) === 720);
  const incompatible = initialPage.items.find((video: { videoCodec: string }) => video.videoCodec === "mpeg4");
  expect(fullHd).toBeTruthy();
  expect(hd).toBeTruthy();
  expect(incompatible).toBeTruthy();
  expect(incompatible.playbackWarning).toContain("可能不受手机浏览器支持");
  await expect(page.locator(".video-card").filter({ hasText: incompatible.number }).locator(".codec-warning"))
    .toContainText("建议使用 H.264");

  await selectVideos(page, [fullHd.number, hd.number]);
  await page.getByRole("button", { name: "打开批量整理" }).click();
  await expect(page.getByRole("heading", { name: "批量整理" })).toBeVisible();
  await expect(page.locator(".drawer-header")).toContainText("已选择 2 个视频");
  await page.getByRole("button", { name: "#动作", exact: true }).click();
  await page.getByRole("button", { name: "确认添加" }).click();
  await expect(page.getByRole("heading", { name: "批量整理" })).toBeHidden();

  const taggedPage = await (await page.context().request.get("/api/videos?page=1&pageSize=20")).json();
  for (const id of [fullHd.id, hd.id]) {
    const item = taggedPage.items.find((video: { id: string }) => video.id === id);
    expect(item.tags.map((tag: { name: string }) => tag.name)).toContain("动作");
  }

  await selectVideos(page, [fullHd.number, hd.number]);
  await page.getByRole("button", { name: "打开批量整理" }).click();
  await page.getByRole("button", { name: "分析标签建议" }).click();
  await expect(page.locator(".resolution-suggestions")).toContainText("#1080P");
  await expect(page.locator(".resolution-suggestions")).toContainText("#720P");
  await page.screenshot({ path: "test-results/mobile-bulk-organizer.png" });
  const drawerBox = await page.locator(".drawer").boundingBox();
  expect(drawerBox).not.toBeNull();
  expect(drawerBox!.y + drawerBox!.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
  await page.getByRole("button", { name: "确认应用建议" }).click();
  await expect(page.getByRole("heading", { name: "批量整理" })).toBeHidden();

  const resolvedPage = await (await page.context().request.get("/api/videos?page=1&pageSize=20")).json();
  const fullHdAfterResolution = resolvedPage.items.find((video: { id: string }) => video.id === fullHd.id);
  const hdAfterResolution = resolvedPage.items.find((video: { id: string }) => video.id === hd.id);
  expect(fullHdAfterResolution.tags.map((tag: { name: string }) => tag.name)).toContain("1080P");
  expect(hdAfterResolution.tags.map((tag: { name: string }) => tag.name)).toContain("720P");

  await selectVideos(page, [fullHd.number]);
  await page.getByRole("button", { name: "打开批量整理" }).click();
  await page.getByRole("button", { name: "重新分析信息" }).click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Failed", { timeout: 20_000 });
  await expect(page.locator(".job-panel")).toContainText("模拟媒体重新分析失败");
  await page.getByRole("button", { name: "重试" }).click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });

  const jobsAfterRetry = await (await page.context().request.get("/api/jobs")).json();
  const retriedJob = jobsAfterRetry.find((job: { type: string }) => job.type === "MediaMaintenance");
  expect(retriedJob).toMatchObject({ status: "Completed", attemptCount: 2 });
  expect(retriedJob.result).toMatchObject({
    action: "Reanalyze",
    requestedCount: 1,
    processedCount: 1,
    updatedCount: 1,
    issues: []
  });

  await selectVideos(page, [fullHd.number, hd.number]);
  await page.getByRole("button", { name: "打开批量整理" }).click();
  await page.getByRole("button", { name: "重新生成封面" }).click();
  await expect(page.getByRole("heading", { name: "批量整理" })).toBeHidden();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });
  const coverJobs = await (await page.context().request.get("/api/jobs")).json();
  const coverJob = coverJobs.find((job: { type: string; result?: { action?: string } }) =>
    job.type === "MediaMaintenance" && job.result?.action === "RegenerateCover");
  expect(coverJob).toBeTruthy();
  expect(coverJob.result).toMatchObject({ requestedCount: 2, processedCount: 2, updatedCount: 2, issues: [] });

  await selectVideos(page, [fullHd.number, hd.number]);
  await page.getByRole("button", { name: "打开批量整理" }).click();
  const trashButton = page.getByRole("button", { name: "批量移入回收站" });
  await trashButton.scrollIntoViewIfNeeded();
  page.once("dialog", dialog => dialog.accept());
  await trashButton.click();
  await expect(page.locator(".video-card")).toHaveCount(1);
  await expect(page.locator(".library-count")).toContainText("共 1 个视频");

  const finalPage = await (await page.context().request.get("/api/videos?page=1&pageSize=20")).json();
  expect(finalPage.total).toBe(1);
  const originalFiles = listFiles(path.join(videoRoot!, "originals"));
  const trashFiles = listFiles(path.join(videoRoot!, "trash"));
  const coverFiles = listFiles(path.join(videoRoot!, "covers"));
  expect(originalFiles).toHaveLength(1);
  expect(trashFiles).toHaveLength(2);
  expect(coverFiles).toHaveLength(3);
  expect(coverFiles.some(file => path.basename(file).includes("-auto-"))).toBeTruthy();
});

async function selectVideos(page: import("@playwright/test").Page, numbers: string[]) {
  await page.getByRole("button", { name: "批量整理" }).click();
  for (const number of numbers) {
    await page.getByRole("button", { name: `选择 ${number}` }).click();
  }
  await expect(page.locator(".bulk-bottom-bar")).toContainText(`${numbers.length} 个视频`);
}

function listFiles(root: string): string[] {
  if (!fs.existsSync(root)) return [];
  return fs.readdirSync(root, { withFileTypes: true }).flatMap(entry => {
    const fullPath = path.join(root, entry.name);
    return entry.isDirectory() ? listFiles(fullPath) : [fullPath];
  });
}
