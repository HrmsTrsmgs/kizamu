import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  resolve: {
    alias: {
      // The Node build expands the response mapper before the HTTP tests run.
      './timeRecordResponse.js': fileURLToPath(
        new URL('../../../src/backends/node/dist/api/timeRecordResponse.js', import.meta.url),
      ),
    },
  },
  test: { setupFiles: '../../should.mts' },
})
