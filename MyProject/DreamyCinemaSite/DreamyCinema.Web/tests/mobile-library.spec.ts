import { expect, test } from "@playwright/test";

test("admin can set up, sync, browse, and open the dedicated player", async ({ page }) => {
  await page.goto("/login");
  await expect(page.getByRole("heading", { name: "Dreamy Cinema" })).toBeVisible();

  await page.getByLabel("管理员密码").fill("Dreamy-Test-2026");
  const confirmPassword = page.getByLabel("确认密码");
  if (await confirmPassword.isVisible()) {
    await confirmPassword.fill("Dreamy-Test-2026");
    await page.getByRole("button", { name: "创建密码并进入" }).click();
  } else {
    await page.getByRole("button", { name: "登录" }).click();
  }

  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("heading", { name: "Dreamy Cinema" })).toBeVisible();

  const syncButton = page.getByTitle("同步视频");
  await syncButton.click();
  await expect(page.locator(".job-panel")).toBeVisible();
  await expect(syncButton).toBeDisabled();
  await page.getByRole("button", { name: "取消任务" }).click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Cancelled", { timeout: 20_000 });
  await page.getByRole("button", { name: "重试" }).click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });
  await expect(page.locator(".job-panel progress")).toHaveAttribute("value", "100");
  await expect(page.locator(".video-card")).toHaveCount(1);
  const jobsResponse = await page.context().request.get("/api/jobs");
  expect(jobsResponse.ok()).toBeTruthy();
  const jobs = await jobsResponse.json();
  expect(jobs).toHaveLength(1);
  expect(jobs[0]).toMatchObject({ type: "Sync", status: "Completed", progress: 100 });
  expect(jobs[0].attemptCount).toBeGreaterThanOrEqual(1);
  await page.reload();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed");
  await expect(page.locator(".job-panel")).toContainText("已完成");
  const pageResponse = await page.context().request.get("/api/videos?page=1&pageSize=1");
  expect(pageResponse.ok()).toBeTruthy();
  const videoPage = await pageResponse.json();
  expect(videoPage).toMatchObject({ total: 1, page: 1, pageSize: 1, hasMore: false });
  expect(videoPage.items[0].subtitles).toHaveLength(1);
  expect(videoPage.items[0].subtitles[0]).toMatchObject({ language: "en", cueCount: 2, isDefault: true });
  const subtitleResponse = await page.context().request.get(videoPage.items[0].subtitles[0].vttUrl);
  expect(subtitleResponse.ok()).toBeTruthy();
  expect(subtitleResponse.headers()["content-type"]).toContain("text/vtt");
  expect(await subtitleResponse.text()).toContain("WEBVTT\n\n1\n00:00:00.500 --> 00:00:02.500\nHello from Dreamy Cinema.");
  await expect(page.locator(".library-count")).toContainText("已加载 1 / 共 1");
  await expect(page.locator(".video-card")).toContainText("番号：");
  await expect(page.locator(".media-panel")).toHaveCount(1);
  await expect(page.locator(".cover-panel")).toHaveCount(0);
  await expect(page.locator(".media-panel img")).toBeVisible();
  await expect(page.locator(".media-panel img")).toHaveAttribute("loading", "lazy");
  await expect(page.locator(".video-tech-meta")).not.toContainText("--");
  await expect(page.locator(".video-tech-meta")).toContainText("字幕 1 轨");

  await page.getByRole("button", { name: "编辑" }).click();
  await expect(page.getByRole("heading", { name: "编辑视频" })).toBeVisible();
  const saveButton = page.getByRole("button", { name: "保存修改" });
  await expect(saveButton).toBeVisible();
  const scrollMetrics = await page.locator(".drawer-body").evaluate(element => ({
    clientHeight: element.clientHeight,
    scrollHeight: element.scrollHeight
  }));
  expect(scrollMetrics.scrollHeight).toBeGreaterThan(scrollMetrics.clientHeight);
  await page.locator(".drawer-body").evaluate(element => { element.scrollTop = element.scrollHeight; });
  await expect.poll(() => page.locator(".drawer-body").evaluate(element => element.scrollTop)).toBeGreaterThan(0);
  const footerBox = await page.locator(".drawer-footer").boundingBox();
  expect(footerBox).not.toBeNull();
  expect(footerBox!.y + footerBox!.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
  const coverBox = await page.locator(".cover-editor").boundingBox();
  const coverActionsBox = await page.locator(".cover-actions").boundingBox();
  const numberFieldBox = await page.locator(".drawer-body > .field-label").first().boundingBox();
  expect(coverBox).not.toBeNull();
  expect(coverActionsBox).not.toBeNull();
  expect(numberFieldBox).not.toBeNull();
  expect(coverActionsBox!.y).toBeGreaterThanOrEqual(coverBox!.y + coverBox!.height - 1);
  expect(numberFieldBox!.y).toBeGreaterThanOrEqual(coverActionsBox!.y + coverActionsBox!.height - 1);
  await page.screenshot({ path: "test-results/mobile-editor.png", fullPage: true });
  await page.getByRole("button", { name: "移除封面" }).click();
  await saveButton.click();
  await expect(page.getByRole("heading", { name: "编辑视频" })).toBeHidden();
  await syncButton.click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });
  await expect(page.locator(".media-panel img")).toBeVisible();

  await page.getByRole("button", { name: /在当前页面播放/ }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator(".media-panel video")).toBeVisible();
  await expect(page.locator(".media-panel video track")).toHaveCount(1);
  await page.screenshot({ path: "test-results/mobile-library-inline.png", fullPage: true });

  await page.getByRole("button", { name: "打开播放页" }).click();
  await expect(page).toHaveURL(/\/videos\/[^/]+\/play$/);
  await expect(page.locator(".player-stage video")).toBeVisible();
  await expect(page.locator(".player-stage video track")).toHaveCount(1);
  await expect(page.getByLabel("字幕轨道")).toHaveValue(videoPage.items[0].subtitles[0].id);
  await expect(page.getByLabel("字幕轨道").locator("option")).toHaveCount(2);
  await expect(page.getByRole("heading", { name: "AI 字幕" })).toBeVisible();
  await expect(page.getByText("Mock · mock-translation（模拟）")).toBeVisible();
  await page.getByRole("button", { name: "翻译为中文" }).click();
  await expect(page.locator(".ai-job")).toHaveAttribute("data-status", "Completed", { timeout: 20_000 });
  await expect(page.locator(".player-stage video track")).toHaveCount(2);
  await expect(page.getByLabel("字幕轨道").locator("option")).toHaveCount(3);
  const translatedTracksResponse = await page.context().request.get(`/api/videos/${videoPage.items[0].id}/subtitles`);
  expect(translatedTracksResponse.ok()).toBeTruthy();
  const translatedTracks = await translatedTracksResponse.json();
  const translatedTrack = translatedTracks.find((track: { kind: string }) => track.kind === "Translated");
  expect(translatedTrack).toMatchObject({ language: "zh-CN", source: "AiTranslation", cueCount: 2 });
  await expect(page.getByLabel("字幕轨道")).toHaveValue(translatedTrack.id);
  const translatedVtt = await page.context().request.get(translatedTrack.vttUrl);
  expect(await translatedVtt.text()).toContain("你好，这里是 Dreamy Cinema。");
  expect(await (await page.context().request.get(translatedTrack.vttUrl)).text()).toContain("00:00:00.500 --> 00:00:02.500");

  const recognitionJob = await page.evaluate(async videoId => {
    const session = await fetch("/api/auth/session", { credentials: "same-origin" }).then(response => response.json());
    const response = await fetch(`/api/videos/${videoId}/subtitles/transcribe`, {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": session.requestToken },
      body: JSON.stringify({ language: null })
    });
    return response.json();
  }, videoPage.items[0].id);
  await expect.poll(async () => {
    const response = await page.context().request.get(`/api/jobs/${recognitionJob.id}`);
    return (await response.json()).status;
  }, { timeout: 20_000 }).toBe("Completed");
  const recognizedTracksResponse = await page.context().request.get(`/api/videos/${videoPage.items[0].id}/subtitles`);
  const recognizedTracks = await recognizedTracksResponse.json();
  expect(recognizedTracks.find((track: { source: string }) => track.source === "SpeechRecognition"))
    .toMatchObject({ language: "en", cueCount: 3 });
  await expect(page.getByRole("button", { name: "返回片库" })).toBeVisible();
  await expect(page.locator(".detail-grid")).toContainText("DEMO-001.mp4");
  await page.screenshot({ path: "test-results/mobile-player.png", fullPage: true });
});
