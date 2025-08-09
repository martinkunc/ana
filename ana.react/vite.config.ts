import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    console.log("VITE_PORT" + env.VITE_PORT);
    return {
        plugins: [react()],
        server: {
            port: parseInt(env.VITE_PORT),
        },
        build: {
            outDir: 'dist',
            rollupOptions: {
                input: './index.html'
            }
        }
    }
})