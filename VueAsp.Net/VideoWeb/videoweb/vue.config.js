// import AutoImport from "unplugin-auto-import/vite";

const { defineConfig } = require("@vue/cli-service");
module.exports = defineConfig({
  transpileDependencies: true,
  // plugins: [
  //   AutoImport({
  //     imports: ["element-plus"],
  //   }),
  // ],
  publicPath: process.env.NODE_ENV === "production" ? "./" : "/",
  devServer: {
    proxy: {
      "/api": {
        // target: "http://localhost:5142",
        // target: "https://localhost:44318",
        target: "http://localhost:5678",
        changeOrigin: true,
          // 必须添加以下配置
        onProxyReq: (proxyReq) => {
          proxyReq.setHeader('Connection', 'keep-alive');
        },
        proxyTimeout: 60000,
        pathRewrite: { "^/api": "" },
      },
    },
  },
});
