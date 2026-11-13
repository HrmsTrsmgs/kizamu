// @vitest-environment node
import { execFile, spawn, type ChildProcessByStdio } from 'node:child_process'
import { createServer } from 'node:http'
import type { Readable } from 'node:stream'
import { promisify } from 'node:util'
import { test } from 'vitest'

test('公開用サーバーは設定した業務APIへ同一オリジンの要求を中継します', async () => {
  const backend = createServer((request, response) => {
    response.writeHead(201, { 'content-type': 'application/json' })
    response.end(JSON.stringify({ path: request.url }))
  })
  await new Promise<void>((resolve) => backend.listen(0, '127.0.0.1', resolve))
  try {
    const backendAddress = backend.address()
    if (!backendAddress || typeof backendAddress === 'string') throw new Error('業務APIのポートを取得できませんでした')

    await withListeningServer(`http://127.0.0.1:${backendAddress.port}`, async (listeningUrl) => {
      const response = await fetch(`${listeningUrl}/api/time-records?project=sample`, { signal: AbortSignal.timeout(3000) })

      response.status.should.equal(201)
      const content = { body: await response.json() }
      content.should.have.property('body').that.deep.equal({ path: '/api/time-records?project=sample' })
    })
  } finally {
    await new Promise<void>((resolve, reject) => backend.close((error) => error ? reject(error) : resolve()))
  }
}, 10000)

async function withListeningServer(apiBaseUrl: string, verify: (listeningUrl: string) => Promise<void>) {
  const npmEntry = process.env.npm_execpath
  if (!npmEntry) throw new Error('npm経由でテストを実行してください')
  const server = spawn(process.execPath, [npmEntry, 'run', 'start', '--workspace=kizamu-frontend-react'], {
    env: { ...process.env, PORT: '0', API_BASE_URL: apiBaseUrl },
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
    detached: process.platform !== 'win32',
  })
  const closed = new Promise<void>((resolve) => server.once('close', () => resolve()))
  try {
    await verify(await waitForListeningUrl(server))
  } finally {
    if (server.pid && server.exitCode === null && server.signalCode === null) {
      if (process.platform === 'win32') {
        await promisify(execFile)('taskkill', ['/pid', String(server.pid), '/T', '/F'])
      } else {
        process.kill(-server.pid, 'SIGTERM')
      }
    }
    await closed
  }
}

async function waitForListeningUrl(server: ChildProcessByStdio<null, Readable, Readable>): Promise<string> {
  let output = ''
  let startupTimeout: ReturnType<typeof setTimeout> | undefined
  try {
    return await new Promise<string>((resolve, reject) => {
      startupTimeout = setTimeout(() => reject(new Error(`起動URLが出力されませんでした: ${output}`)), 5000)
      server.once('error', reject)
      server.once('close', (code) => reject(new Error(`公開用サーバーが終了しました (${code}): ${output}`)))
      const onData = (chunk: Buffer) => {
        output += chunk.toString()
        const url = output.match(/http:\/\/(?:localhost|127\.0\.0\.1|\[::1\]):[1-9]\d*/)?.[0]
        if (url) resolve(url)
      }
      server.stdout.on('data', onData)
      server.stderr.on('data', onData)
    })
  } finally {
    clearTimeout(startupTimeout)
  }
}
