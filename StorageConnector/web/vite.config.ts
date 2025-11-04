import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import fs from "fs";
import path from "path";

// Look for locally-generated mkcert certs in ./certs. If present, enable HTTPS for Vite.
const certDir = path.resolve(__dirname, "certs");
const keyPath = path.join(certDir, "localhost+2-key.pem");
const certPath = path.join(certDir, "localhost+2.pem");
let httpsConfig: boolean | { key: Buffer; cert: Buffer } = false;
try {
  if (fs.existsSync(keyPath) && fs.existsSync(certPath)) {
    httpsConfig = { key: fs.readFileSync(keyPath), cert: fs.readFileSync(certPath) };
    console.log("Vite: using HTTPS with certs from", certDir);
  }
} catch (e) {
  // ignore and fall back to HTTP
  httpsConfig = false;
}

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    https: httpsConfig as any,
    proxy: {
      "/api/auth": {
        target: "https://localhost:7166",
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
      "/api/files": {
        target: "https://localhost:7030",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
