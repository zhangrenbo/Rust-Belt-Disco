import { defineConfig } from 'vite';

// Only public/ is shipped as static content. Story drafts, tools and references
// are outside the web build; never point publicDir at the project root.
export default defineConfig({
  base: './',
  build: { target: 'es2022' },
  server: { fs: { deny: ['.env', '.env.*', '**/*.pem', '**/*.key', '**/DS API.txt', '**/content/**', '**/tools/**', '**/Steam/**'] } },
});
