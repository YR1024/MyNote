import { expect, test } from "@playwright/test";

test("admin can set up, sync, browse, and open the dedicated player", async ({ page }) => {
  test.setTimeout(90_000);
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

  await page.getByTitle("数据备份").click();
  await expect(page.getByRole("heading", { name: "数据备份" })).toBeVisible();
  await expect(page.getByText("不包含原视频、回收站视频、管理员密码、Cookie 或 AI 密钥")).toBeVisible();
  await page.getByRole("button", { name: "创建新备份" }).click();
  await expect(page.locator(".backup-card")).toHaveCount(1);
  await expect(page.locator(".backup-card")).toContainText("清单有效");
  await expect(page.locator(".backup-card")).toContainText("1 个视频记录");
  await expect(page.locator(".backup-card")).toContainText("1 张封面");
  await expect(page.locator(".backup-card")).toContainText("1 个字幕文件");
  const backupsResponse = await page.context().request.get("/api/backups");
  expect(backupsResponse.ok()).toBeTruthy();
  const backups = await backupsResponse.json();
  expect(backups).toHaveLength(1);
  expect(backups[0]).toMatchObject({
    videoCount: 1,
    coverCount: 1,
    subtitleFileCount: 1,
    includesOriginalVideos: false,
    purpose: "Manual",
    isValid: true,
    warnings: []
  });
  const originalBackupId = backups[0].id as string;
  const originalTitle = videoPage.items[0].title as string;
  expect(backups[0].sha256).toMatch(/^[0-9a-f]{64}$/);
  const backupDownload = await page.context().request.get(`/api/backups/${originalBackupId}/download`);
  expect(backupDownload.ok()).toBeTruthy();
  expect(backupDownload.headers()["content-type"]).toContain("application/zip");
  const backupBytes = await backupDownload.body();
  expect(backupBytes.subarray(0, 2).toString("ascii")).toBe("PK");
  await page.screenshot({ path: "test-results/mobile-backups.png", fullPage: true });
  await page.locator(".drawer").getByRole("button", { name: "关闭" }).click();

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
  await expect(page.locator(".card-media-shell .subtitle-compat-bar")).toBeVisible();
  await page.locator(".media-panel video").evaluate(element => {
    (element as HTMLVideoElement).currentTime = 1;
  });
  await expect(page.locator(".card-media-shell .subtitle-compat-bar")).toContainText("Hello from Dreamy Cinema.");
  await page.screenshot({ path: "test-results/mobile-library-inline.png", fullPage: true });

  await page.getByRole("button", { name: "打开播放页" }).click();
  await expect(page).toHaveURL(/\/videos\/[^/]+\/play$/);
  await expect(page.locator(".player-stage video")).toBeVisible();
  await expect(page.locator(".player-stage video track")).toHaveCount(1);
  await expect(page.getByLabel("字幕轨道")).toHaveValue(videoPage.items[0].subtitles[0].id);
  await expect(page.getByLabel("字幕轨道").locator("option")).toHaveCount(2);
  await expect(page.getByLabel("字幕显示方式")).toHaveValue("below");
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toBeVisible();
  await expect(page.getByRole("heading", { name: "AI 字幕" })).toBeVisible();
  await expect(page.getByText(/翻译：Mock · .+（模拟）/)).toBeVisible();
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
  await page.locator(".player-stage video").evaluate(element => {
    (element as HTMLVideoElement).currentTime = 1;
  });
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toContainText("你好，这里是 Dreamy Cinema。");
  await page.getByLabel("字幕显示方式").selectOption("overlay");
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toHaveCount(0);
  await expect(page.locator(".player-stage .subtitle-overlay")).toContainText("你好，这里是 Dreamy Cinema。");
  await page.getByLabel("字幕显示方式").selectOption("native");
  await expect(page.locator(".player-stage .subtitle-overlay")).toHaveCount(0);
  await expect.poll(() => page.locator(".player-stage video").evaluate((element, selectedId) => {
    const video = element as HTMLVideoElement;
    const tracks = Array.from(video.textTracks);
    const index = Array.from(video.querySelectorAll("track")).findIndex(track => track.dataset.trackId === selectedId);
    return index >= 0 ? tracks[index]?.mode : null;
  }, translatedTrack.id)).toBe("showing");
  await page.getByLabel("字幕显示方式").selectOption("below");
  await expect(page.locator(".player-stage .subtitle-compat-bar")).toContainText("你好，这里是 Dreamy Cinema。");
  await expect(page.getByRole("button", { name: "重新翻译为中文" })).toBeVisible();
  await page.getByRole("button", { name: "重新翻译为中文" }).click();
  await expect(page.getByLabel("字幕轨道").locator("option")).toHaveCount(4, { timeout: 20_000 });
  const retranslatedTracks = await (await page.context().request.get(`/api/videos/${videoPage.items[0].id}/subtitles`)).json();
  expect(retranslatedTracks.some((track: { label: string }) => track.label.includes("重译"))).toBeTruthy();

  await page.getByRole("button", { name: "校对" }).click();
  await expect(page).toHaveURL(/\/videos\/[^/]+\/subtitles\/review$/);
  await expect(page.locator(".review-player video")).toBeVisible();
  await expect(page.locator(".review-cue")).toHaveCount(2);
  await expect(page.getByLabel("校对原文轨道")).not.toHaveValue("");
  await expect(page.getByLabel("校对中文轨道")).not.toHaveValue("");
  await page.getByLabel("搜索字幕文本").fill("import");
  await expect(page.locator(".review-cue")).toHaveCount(1);
  await page.getByLabel("搜索字幕文本").fill("");
  await expect(page.locator(".review-cue")).toHaveCount(2);
  const manualText = "这是人工校对并保留的较长中文字幕，后续局部重译不能覆盖这条已经确认的内容。";
  await page.getByLabel("第 1 条中文字幕").fill(manualText);
  await page.getByRole("button", { name: "保存此条" }).click();
  await expect(page.getByText("人工修改", { exact: true })).toBeVisible();
  await expect(page.getByLabel("校对中文轨道")).toHaveValue(/.+/);
  await expect(page.getByLabel("选择第 1 条字幕")).toBeDisabled();
  const correctedSourceText = "Subtitle import is working correctly.";
  await page.getByRole("button", { name: "修正原文" }).nth(1).click();
  await page.getByLabel("第 2 条原文字幕").fill(correctedSourceText);
  await page.getByRole("button", { name: "保存原文" }).click();
  await expect(page.getByLabel("校对原文轨道")).toHaveValue(/.+/);
  await expect(page.getByText("1 条原文修正")).toBeVisible();
  await page.getByLabel("选择第 2 条字幕").check();
  await page.getByRole("button", { name: /重译 1/ }).click();
  await expect(page.getByText("选中字幕已重新翻译，人工修改内容未覆盖。")).toBeVisible({ timeout: 20_000 });
  await expect(page.getByLabel("第 1 条中文字幕")).toHaveValue(manualText);

  const reviewResponse = await page.context().request.get(`/api/videos/${videoPage.items[0].id}/subtitle-review`);
  expect(reviewResponse.ok()).toBeTruthy();
  const reviewDocument = await reviewResponse.json();
  expect(reviewDocument.manualEditCount).toBe(1);
  expect(reviewDocument.sourceManualEditCount).toBe(1);
  expect(reviewDocument.cues[1]).toMatchObject({ sourceText: correctedSourceText, isSourceManuallyEdited: true });
  expect(reviewDocument.cues[0]).toMatchObject({ targetText: manualText, isManuallyEdited: true });
  expect(reviewDocument.targetTracks[0].revisionStage).toBe("FinalPolished");

  await page.getByRole("button", { name: "生成双语" }).click();
  await expect(page.getByText("双语字幕轨道已生成，可返回播放页选择。")).toBeVisible();
  await expect(page.getByRole("link", { name: "双语 SRT" })).toBeVisible();
  const tracksAfterReview = await (await page.context().request.get(`/api/videos/${videoPage.items[0].id}/subtitles`)).json();
  const bilingualTrack = tracksAfterReview.find((track: { kind: string }) => track.kind === "Bilingual");
  expect(bilingualTrack).toMatchObject({ language: "mul", cueCount: 2, revisionStage: "FinalPolished" });
  const bilingualExport = await page.context().request.get(`/api/subtitles/${bilingualTrack.id}/export?format=srt`);
  expect(bilingualExport.ok()).toBeTruthy();
  const bilingualText = await bilingualExport.text();
  expect(bilingualText).toContain("Hello from Dreamy Cinema.\n" + manualText);
  expect(bilingualText).toContain(correctedSourceText);
  expect(bilingualText).toContain("00:00:00,500 --> 00:00:02,500");
  const untouchedSource = await page.context().request.get(videoPage.items[0].subtitles[0].vttUrl);
  expect(await untouchedSource.text()).toContain("Subtitle import is working.");
  const finalTrackId = reviewDocument.targetTracks[0].id;
  const finalVttExport = await page.context().request.get(`/api/subtitles/${finalTrackId}/export?format=vtt`);
  expect(finalVttExport.ok()).toBeTruthy();
  expect(await finalVttExport.text()).toContain("WEBVTT");
  await page.screenshot({ path: "test-results/mobile-subtitle-review.png", fullPage: true });
  await page.getByRole("button", { name: "返回播放页" }).click();
  await expect(page).toHaveURL(/\/videos\/[^/]+\/play$/);
  await expect(page.locator(".player-stage video track")).toHaveCount(tracksAfterReview.length);
  await expect(page.getByLabel("字幕轨道")).toHaveValue(finalTrackId);
  await page.reload();
  await expect(page.getByLabel("字幕轨道")).toHaveValue(finalTrackId);

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

  await page.getByRole("button", { name: "返回片库" }).click();
  await expect(page).toHaveURL(/\/$/);
  await page.getByRole("button", { name: "编辑" }).click();
  await page.getByLabel("标题").fill("仅用于恢复测试的临时标题");
  await page.getByRole("button", { name: "移除封面" }).click();
  await page.getByRole("button", { name: "保存修改" }).click();
  await expect(page.locator(".video-card")).toContainText("仅用于恢复测试的临时标题");
  await expect(page.locator(".media-panel img")).toHaveCount(0);

  await page.getByTitle("数据备份").click();
  const originalBackupCard = page.locator(".backup-card").filter({ hasText: "手动备份" });
  await expect(originalBackupCard).toHaveCount(1);

  page.once("dialog", dialog => dialog.accept());
  await originalBackupCard.getByRole("button", { name: "恢复", exact: true }).click();
  await expect(page.locator(".backup-status")).toContainText("恢复失败，已自动回滚", { timeout: 20_000 });
  const rolledBackPage = await (await page.context().request.get("/api/videos?page=1&pageSize=10")).json();
  expect(rolledBackPage.items[0]).toMatchObject({
    title: "仅用于恢复测试的临时标题",
    coverUrl: null
  });
  expect(rolledBackPage.items[0].subtitles.length).toBeGreaterThan(1);

  page.once("dialog", dialog => dialog.accept());
  await originalBackupCard.getByRole("button", { name: "恢复", exact: true }).click();
  await expect(page.getByRole("heading", { name: "数据备份" })).toBeHidden({ timeout: 20_000 });
  await expect(page.locator(".video-card")).toContainText(originalTitle);
  await expect(page.locator(".video-card")).not.toContainText("仅用于恢复测试的临时标题");
  await expect(page.locator(".media-panel img")).toBeVisible();
  const restoredPage = await (await page.context().request.get("/api/videos?page=1&pageSize=10")).json();
  expect(restoredPage.items[0].title).toBe(originalTitle);
  expect(restoredPage.items[0].subtitles).toHaveLength(1);
  const restoredStream = await page.context().request.get(
    restoredPage.items[0].streamUrl,
    { headers: { Range: "bytes=0-15" } }
  );
  expect(restoredStream.status()).toBe(206);

  await page.getByTitle("数据备份").click();
  await expect(page.locator(".backup-purpose.automatic")).toHaveCount(2);
  const manualBackupCard = page.locator(".backup-card").filter({ hasText: "手动备份" });
  page.once("dialog", dialog => dialog.accept());
  await manualBackupCard.getByRole("button", { name: "删除", exact: true }).click();
  await expect(page.locator(".backup-card")).toHaveCount(2);
  await page.locator(".backup-file-input").setInputFiles({
    name: "dreamy-cinema-roundtrip.zip",
    mimeType: "application/zip",
    buffer: backupBytes
  });
  await expect(page.getByText("已导入", { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(page.locator(".backup-card")).toHaveCount(3);
  const importedBackups = await (await page.context().request.get("/api/backups")).json();
  expect(importedBackups.some((backup: { id: string; purpose: string }) =>
    backup.id === originalBackupId && backup.purpose === "Imported")).toBeTruthy();

  await page.locator(".backup-file-input").setInputFiles({
    name: "invalid.zip",
    mimeType: "application/zip",
    buffer: Buffer.from("not a zip package")
  });
  await expect(page.locator(".backup-status")).toContainText("备份包校验失败");
  await expect(page.locator(".backup-card")).toHaveCount(3);
  await page.screenshot({ path: "test-results/mobile-restore-complete.png", fullPage: true });
});
