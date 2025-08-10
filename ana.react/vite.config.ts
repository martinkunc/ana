import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    console.log("VITE_PORT: " + env.VITE_PORT);
    
    // Get API target from environment or use fallback
    const apiTarget = env.services__apiservice__https__0 || 
                     env.VITE_API_URL || 
                     'https://localhost:7001';
    
    console.log("API Proxy Target: " + apiTarget);
    
    return {
        plugins: [react()],
        server: {
            port: parseInt(env.VITE_PORT) || 3000,
            proxy: {
                '/api': {
                    target: apiTarget,
                    changeOrigin: true,
                    rewrite: (path) => path.replace(/^\/api/, ''),
                    secure: false, // Set to true if your target uses valid SSL
                    configure: (proxy, _options) => {
                        proxy.on('proxyReq', (_proxyReq, req, _res) => {
                            console.log('Proxying request:', req.method, req.url, 'to', apiTarget);
                        });
                        proxy.on('error', (err, _req, _res) => {
                            console.error('Proxy error:', err);
                        });
                    }
                }
            }
        },
        build: {
            outDir: 'dist',
            rollupOptions: {
                input: './index.html'
            }
        }
    }
})