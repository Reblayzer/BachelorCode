import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api/auth": {
        target: "https://localhost:7120",
        changeOrigin: true,
        secure: false,
      },
      "/api/connect": {
        target: "https://localhost:7030",
        changeOrigin: true,
        secure: false,
      },
      "/api/connections": {
        target: "https://localhost:7030",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
