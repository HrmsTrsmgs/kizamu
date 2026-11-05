import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { expect, test } from 'vitest'

test('Nodeの起動入口を空きポートで実行すると出力されたURLで業務APIが応答します', async () => {
  const serverEntry = fileURLToPath(new URL('../../../src/backends/node/dist/server.js', import.meta.url))
  const server = spawn(process.execPath, [serverEntry], {
    env: { ...process.env, PORT: '0' },
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  })
  const closed = new Promise<void>((resolve) => server.once('close', () => resolve()))
  let standardOutput = ''
  let standardError = ''
  server.stderr.setEncoding('utf8')
  server.stderr.on('data', (chunk: string) => { standardError += chunk })
  server.stdout.setEncoding('utf8')

  let startupTimeout: ReturnType<typeof setTimeout> | undefined
  try {
    const listeningUrl = await new Promise<string>((resolve, reject) => {
      startupTimeout = setTimeout(() => {
        reject(new Error(`起動URLが出力されませんでした。stdout: ${standardOutput}\nstderr: ${standardError}`))
      }, 5000)
      server.once('error', reject)
      server.once('close', (code, signal) => {
        reject(new Error(`起動プロセスが終了しました。code: ${code}, signal: ${signal}\nstderr: ${standardError}`))
      })
      server.stdout.on('data', (chunk: string) => {
        standardOutput += chunk
        const url = standardOutput.match(/http:\/\/(?:localhost|127\.0\.0\.1|\[::1\]):[1-9]\d*/)?.[0]
        if (url) resolve(url)
      })
    })
    clearTimeout(startupTimeout)

    const response = await fetch(`${listeningUrl}/api/health`, { signal: AbortSignal.timeout(3000) })

    expect(response.status).toBe(200)
    expect(await response.json()).toEqual({ status: 'ok' })
  } finally {
    clearTimeout(startupTimeout)
    if (server.exitCode === null && server.signalCode === null) server.kill()
    await closed
  }
}, 10000)
