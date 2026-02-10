import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
    plugins: [vue()],
    server: {
        proxy: {
            "/api": {
                target: "http://localhost:5310", //5130 likely won't be used, it's filler (likely 5175)
                changeOrigin: true,
                secure: false,
            },
        },
    },
});