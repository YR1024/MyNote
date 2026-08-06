import { expect, test } from "@playwright/test";

const password = "Dreamy-Hard-Subtitle-QA-2026";

test("creates a hard-subtitle derivative and safely removes the original", async ({ page }) => {
  test.setTimeout(120_000);
  await page.goto("/login");
  await page.getByLabel("管理员密码").fill(password);
  const confirmPassword = page.getByLabel("确认密码");
  if (await confirmPassword.isVisible()) {
    await confirmPassword.fill(password);
    await page.getByRole("button", { name: "创建密码并进入" }).click();
  } else {
    await page.getByRole("button", { name: "登录" }).click();
  }

  await expect(page).toHaveURL(/\/$/);
  await page.getByTitle("同步视频").click();
  await expect(page.locator(".job-panel")).toHaveAttribute("data-status", "Completed", { timeout: 30_000 });

  const videoPage = await (await page.context().request.get("/api/videos?page=1&pageSize=1")).json();
  const initialVideo = videoPage.items[0];
  expect(initialVideo.subtitles).toHaveLength(1);
  await page.goto(`/videos/${initialVideo.id}/play`);

  await expect(page.getByRole("heading", { name: "硬字幕手机版" })).toBeVisible();
  const options = await (await page.context().request.get("/api/hard-subtitle/options")).json();
  expect(options.defaultStyle).toMatchObject({
    fontName: "Microsoft YaHei",
    fontSize: 18,
    textColor: "#FFFFFF",
    textOpacity: 92,
    outlineColor: "#000000",
    outline: 2,
    marginV: 24
  });
  await page.getByRole("button", { name: "设置", exact: true }).click();
  await expect(page.getByRole("heading", { name: "硬字幕样式" })).toBeVisible();
  await expect(page.getByLabel("硬字幕字体大小")).toHaveValue("18");
  await expect(page.getByLabel("硬字幕字体不透明度")).toHaveValue("92");
  await page.getByLabel("硬字幕字体大小").fill("16");
  await page.getByLabel("硬字幕字体颜色").fill("#f2e7c4");
  await page.getByLabel("硬字幕字体不透明度").fill("80");
  await page.getByLabel("硬字幕描边颜色").fill("#101820");
  await page.getByLabel("硬字幕描边宽度").fill("1");
  await page.getByLabel("硬字幕底部间距").fill("18");
  await page.screenshot({ path: "test-results/hard-subtitle-style-settings.png", fullPage: true });
  await page.getByRole("button", { name: "使用此样式" }).click();
  await expect(page.getByText("Microsoft YaHei · 16 px · 80%")).toBeVisible();

  const expectedStyle = {
    fontName: "Microsoft YaHei",
    fontSize: 16,
    textColor: "#F2E7C4",
    textOpacity: 80,
    outlineColor: "#101820",
    outline: 1,
    marginV: 18
  };
  await page.getByLabel("硬字幕轨道").selectOption(initialVideo.subtitles[0].id);
  await page.getByRole("button", { name: "生成硬字幕版" }).click();
  await expect(page.locator(".hard-subtitle-job")).toHaveAttribute("data-status", "Completed", { timeout: 60_000 });
  await expect(page.getByRole("button", { name: "硬字幕版", exact: true })).toHaveAttribute("aria-pressed", "true");

  let video = await (await page.context().request.get(`/api/videos/${initialVideo.id}`)).json();
  expect(video.originalAvailable).toBe(true);
  expect(video.hardSubtitle).not.toBeNull();
  expect(video.hardSubtitle.isStale).toBe(false);
  expect(video.hardSubtitle.style).toEqual(expectedStyle);
  const jobs = await (await page.context().request.get("/api/jobs")).json();
  const completedHardSubtitleJob = jobs.find((job: { type: string; videoId: string }) =>
    job.type === "HardSubtitle" && job.videoId === initialVideo.id);
  expect(completedHardSubtitleJob?.status).toBe("Completed");
  expect(completedHardSubtitleJob?.result?.encoder).toBe("libx264");
  expect(completedHardSubtitleJob?.result?.style).toEqual(expectedStyle);
  let response = await page.context().request.get(video.hardSubtitle.streamUrl, {
    headers: { Range: "bytes=0-1023" }
  });
  expect(response.status()).toBe(206);
  await page.getByRole("button", { name: "原始视频", exact: true }).click();
  await expect(page.getByRole("button", { name: "原始视频", exact: true })).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("button", { name: "硬字幕版", exact: true }).click();
  await expect(page.getByRole("button", { name: "硬字幕版", exact: true })).toHaveAttribute("aria-pressed", "true");

  page.once("dialog", dialog => dialog.accept());
  await page.getByRole("button", { name: "删除硬字幕版" }).click();
  await expect(page.getByRole("button", { name: "硬字幕版", exact: true })).toBeDisabled();

  await page.getByRole("button", { name: "生成硬字幕版" }).click();
  await expect.poll(async () => {
    const current = await (await page.context().request.get(`/api/videos/${initialVideo.id}`)).json();
    return current.hardSubtitle?.subtitleTrackId ?? null;
  }, { timeout: 60_000 }).toBe(initialVideo.subtitles[0].id);
  video = await (await page.context().request.get(`/api/videos/${initialVideo.id}`)).json();
  expect(video.hardSubtitle.style).toEqual(expectedStyle);

  const session = await (await page.context().request.get("/api/auth/session")).json();
  response = await page.context().request.post(`/api/videos/${initialVideo.id}/original/delete`, {
    headers: { "X-CSRF-TOKEN": session.requestToken },
    data: { confirmNumber: "wrong-number" }
  });
  expect(response.status()).toBe(400);
  await expect(response.json()).resolves.toMatchObject({ message: "确认番号不匹配，未删除原视频。" });

  page.once("dialog", dialog => dialog.accept(initialVideo.number));
  await page.getByRole("button", { name: "永久删除原视频" }).click();
  await expect(page.getByRole("button", { name: "原始视频" })).toBeDisabled();
  await expect(page.getByRole("button", { name: "硬字幕版", exact: true })).toHaveAttribute("aria-pressed", "true");

  video = await (await page.context().request.get(`/api/videos/${initialVideo.id}`)).json();
  expect(video.originalAvailable).toBe(false);
  expect(video.hardSubtitle).not.toBeNull();
  response = await page.context().request.get(video.streamUrl, {
    headers: { Range: "bytes=0-1023" }
  });
  expect(response.status()).toBe(206);
  response = await page.context().request.get(video.originalStreamUrl, {
    headers: { Range: "bytes=0-1023" }
  });
  expect(response.status()).toBe(404);

  await expect(page.getByText("原视频已删除，现有硬字幕版是唯一可播放文件")).toBeVisible();
  await page.screenshot({ path: "test-results/hard-subtitle-derived-only.png", fullPage: true });
});
