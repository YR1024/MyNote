import { expect, test } from "@playwright/test";

const initialPassword = "Dreamy-Security-2026";
const changedPassword = "Dreamy-Security-Changed-2026";
const baseURL = process.env.DREAMY_BASE_URL ?? "http://127.0.0.1:5250";

test("admin can rotate the password and invalidate every old session", async ({ browser, page }) => {
  test.setTimeout(60_000);

  const anonymous = await browser.newContext({ baseURL });
  const anonymousVideos = await anonymous.request.get("/api/videos");
  expect(anonymousVideos.status()).toBe(401);
  await anonymous.close();

  await page.goto("/login");
  await page.getByLabel("管理员密码").fill(initialPassword);
  await page.getByLabel("确认密码").fill(initialPassword);
  await page.getByRole("button", { name: "创建密码并进入" }).click();
  await expect(page).toHaveURL(/\/$/);

  const secondContext = await browser.newContext({ baseURL });
  const secondPage = await secondContext.newPage();
  await secondPage.goto("/login");
  await secondPage.getByLabel("管理员密码").fill(initialPassword);
  await secondPage.getByRole("button", { name: "登录" }).click();
  await expect(secondPage).toHaveURL(/\/$/);
  expect((await secondContext.request.get("/api/videos")).status()).toBe(200);

  const noCsrfResponse = await page.context().request.post("/api/auth/change-password", {
    data: {
      currentPassword: initialPassword,
      newPassword: changedPassword
    }
  });
  expect(noCsrfResponse.status()).toBe(400);

  await page.getByRole("button", { name: "账户" }).click();
  await expect(page.getByRole("heading", { name: "账户与安全" })).toBeVisible();
  await expect(page.getByText("可信局域网，HTTP 未加密")).toBeVisible();
  await expect(page.getByText("片库 API 仅登录后可用，所有写操作同时校验 CSRF。")).toBeVisible();
  await expect(page.locator(".security-facts")).not.toContainText("--");

  await page.getByLabel("当前密码").fill("Wrong-Security-Password");
  await page.getByLabel("新密码", { exact: true }).fill(changedPassword);
  await page.getByLabel("确认新密码").fill(changedPassword);
  await page.getByRole("button", { name: "修改密码" }).click();
  await expect(page.getByText("当前密码不正确。")).toBeVisible();
  expect((await page.context().request.get("/api/videos")).status()).toBe(200);

  await page.getByLabel("当前密码").fill(initialPassword);
  await page.getByRole("button", { name: "修改密码" }).click();
  await expect(page).toHaveURL(/\/login\?reason=password-changed$/);
  await expect(page.getByText("密码已修改，所有旧登录均已失效。请使用新密码重新登录。")).toBeVisible();

  expect((await secondContext.request.get("/api/videos")).status()).toBe(401);
  await secondPage.getByTitle("同步视频").click();
  await expect(secondPage).toHaveURL(/\/login\?reason=session-expired$/);
  await expect(secondPage.getByText("登录已过期或已在其他位置失效，请重新登录。")).toBeVisible();

  await page.getByLabel("管理员密码").fill(initialPassword);
  await page.getByRole("button", { name: "登录" }).click();
  await expect(page).toHaveURL(/\/login/);

  await page.getByLabel("管理员密码").fill(changedPassword);
  await page.getByRole("button", { name: "登录" }).click();
  await expect(page).toHaveURL(/\/$/);

  await page.getByRole("button", { name: "账户" }).click();
  await expect(page.getByRole("heading", { name: "账户与安全" })).toBeVisible();
  await expect(page.locator(".audit-list")).toContainText("修改管理员密码");
  await expect(page.locator(".audit-list")).toContainText("已拒绝");
  await expect(page.locator(".audit-list")).toContainText("成功");
  const auditResponse = await page.context().request.get("/api/security/audit?take=100");
  expect(auditResponse.status()).toBe(200);
  const auditText = await auditResponse.text();
  expect(auditText).not.toContain(initialPassword);
  expect(auditText).not.toContain(changedPassword);
  expect(auditText).not.toContain("X-CSRF-TOKEN");
  expect(auditText).not.toContain("DreamyCinema.Auth");

  const drawerBox = await page.locator(".drawer").boundingBox();
  const footerBox = await page.locator(".drawer-footer").boundingBox();
  expect(drawerBox).not.toBeNull();
  expect(footerBox).not.toBeNull();
  expect(drawerBox!.y + drawerBox!.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
  expect(footerBox!.y + footerBox!.height).toBeLessThanOrEqual(page.viewportSize()!.height + 1);
  await page.screenshot({ path: "test-results/mobile-security.png", fullPage: true });

  await secondContext.close();
});
