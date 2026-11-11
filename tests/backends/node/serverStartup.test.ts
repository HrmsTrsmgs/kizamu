import { spawn, type ChildProcessByStdio } from 'node:child_process'
import type { Readable } from 'node:stream'
import { fileURLToPath } from 'node:url'
import { test } from 'vitest'

test('Nodeの起動入口を空きポートで実行すると出力されたURLで業務APIが応答します', async () => {
  await withListeningServer(async (listeningUrl) => {
    const response = await fetch(`${listeningUrl}/api/health`, { signal: AbortSignal.timeout(3000) })

    response.status.should.equal(200)
    const content = { body: await response.json() }
    content.should.have.property('body').that.deep.equal({ status: 'ok' })
  })
}, 10000)

async function withListeningServer(verify: (listeningUrl: string) => Promise<void>) {
  const serverEntry = fileURLToPath(new URL('../../../src/backends/node/dist/server.js', import.meta.url))
  const server = spawn(process.execPath, [serverEntry], {
    env: { ...process.env, PORT: '0' },
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  })
  const closed = new Promise<void>((resolve) => server.once('close', () => resolve()))
  try {
    const listeningUrl = await waitForListeningUrl(server)
    await verify(listeningUrl)
  } finally {
    if (server.exitCode === null && server.signalCode === null) server.kill()
    await closed
  }
}

async function waitForListeningUrl(server: ChildProcessByStdio<null, Readable, Readable>): Promise<string> {
  let standardOutput = ''
  let standardError = ''
  server.stderr.setEncoding('utf8')
  server.stderr.on('data', (chunk: string) => { standardError += chunk })
  server.stdout.setEncoding('utf8')

  let startupTimeout: ReturnType<typeof setTimeout> | undefined
  try {
    return await new Promise<string>((resolve, reject) => {
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
  } finally {
    clearTimeout(startupTimeout)
  }
}
