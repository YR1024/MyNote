import { fileURLToPath, URL } from "node:url";
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vite";
export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url))
        }
    },
    server: {
        host: "0.0.0.0",
        port: 5173,
        proxy: {
            "/api": {
                target: "http://127.0.0.1:5210",
                changeOrigin: false
            }
        }
    },
    build: {
        outDir: fileURLToPath(new URL("../wwwroot", import.meta.url)),
        emptyOutDir: true
    }
});
