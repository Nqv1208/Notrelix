import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// Loopback only: the console is an internal staff tool and its own origin (no tenant session shares it).
export default defineConfig({
  plugins: [tailwindcss(), react()],
  server: {
    host: "127.0.0.1",
    port: 5180,
    proxy: {
      "/operator": { target: "http://127.0.0.1:8000", changeOrigin: true },
    },
  },
});
