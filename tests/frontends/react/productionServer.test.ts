// @vitest-environment node
import { execFile, spawn, type ChildProcessByStdio } from 'node:child_process'
import { mkdtemp, rm, writeFile } from 'node:fs/promises'
import { createServer } from 'node:http'
import { basename, join } from 'node:path'
import type { Readable } from 'node:stream'
import { fileURLToPath } from 'node:url'
import { promisify } from 'node:util'
import { beforeAll, describe, test } from 'vitest'

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
}, 30000)

describe('公開用サーバーの静的成果物配信', () => {
  let listeningUrl: string

  beforeAll(async () => {
    const npmEntry = process.env.npm_execpath
    if (!npmEntry) throw new Error('npm経由でテストを実行してください')
    await promisify(execFile)(process.execPath, [npmEntry, 'run', 'build', '--workspace=kizamu-frontend-react'])

    const server = await startListeningServer('http://127.0.0.1:1')
    listeningUrl = server.url
    return server.close
  }, 90000)

  test('ルートへアクセスするとReactの画面を配信します', async () => {
    const page = await fetch(listeningUrl, { signal: AbortSignal.timeout(3000) })
    page.status.should.equal(200)
    ;(page.headers.get('content-type') ?? '').should.match(/text\/html/)
    ;(await page.text()).should.include('<title>Kizamu</title>')
  })

  test('ルートへのクエリ付きURLでも同じ画面を配信します', async () => {
    const page = await fetch(listeningUrl, { signal: AbortSignal.timeout(3000) })
    const pageWithQuery = await fetch(`${listeningUrl}/?x=1`, { signal: AbortSignal.timeout(3000) })

    pageWithQuery.status.should.equal(200)
    ;(await pageWithQuery.text()).should.equal(await page.text())
  })

  test('画面から参照されるReactのJavaScriptを配信します', async () => {
    const page = await fetch(listeningUrl, { signal: AbortSignal.timeout(3000) })
    const scriptPath = (await page.text()).match(/<script[^>]+src="(\/assets\/[^"]+)"/)?.[1]
    if (!scriptPath) throw new Error('画面にReactのJavaScriptの参照がありません')

    const script = await fetch(new URL(scriptPath, listeningUrl), { signal: AbortSignal.timeout(3000) })
    script.status.should.equal(200)
    ;(script.headers.get('content-type') ?? '').should.match(/javascript/)
    ;(await script.text()).length.should.be.greaterThan(0)
  })

  test('画面から参照されるCSSを配信します', async () => {
    const page = await fetch(listeningUrl, { signal: AbortSignal.timeout(3000) })
    const stylesheetPath = (await page.text()).match(/<link\b(?=[^>]*\brel="stylesheet")[^>]*\bhref="([^"]+)"/)?.[1]
    if (!stylesheetPath) throw new Error('画面にCSSの参照がありません')

    const stylesheet = await fetch(new URL(stylesheetPath, listeningUrl), { signal: AbortSignal.timeout(3000) })
    stylesheet.status.should.equal(200)
    ;(stylesheet.headers.get('content-type') ?? '').should.match(/text\/css/)
    ;(await stylesheet.text()).length.should.be.greaterThan(0)
  })

  test('PNG画像を画像として配信します', async () => {
    const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/a2QAAAAASUVORK5CYII=', 'base64')
    await withPngFixture(png, async (imagePath) => {
      const image = await fetch(new URL(imagePath, listeningUrl), { signal: AbortSignal.timeout(3000) })
      image.status.should.equal(200)
      ;(image.headers.get('content-type') ?? '').should.match(/image\/png/)
      Buffer.from(await image.arrayBuffer()).should.deep.equal(png)
    })
  })
})

async function withPngFixture(png: Buffer, verify: (imagePath: string) => Promise<void>) {
  const distPath = fileURLToPath(new URL('../../../src/frontends/react/dist/', import.meta.url))
  const fixturePath = await mkdtemp(join(distPath, 'image-test-'))
  try {
    await writeFile(join(fixturePath, 'sample.png'), png)
    await verify(`/${basename(fixturePath)}/sample.png`)
  } finally {
    await rm(fixturePath, { recursive: true })
  }
}

async function withListeningServer(apiBaseUrl: string, verify: (listeningUrl: string) => Promise<void>) {
  const server = await startListeningServer(apiBaseUrl)
  try {
    await verify(server.url)
  } finally {
    await server.close()
  }
}

async function startListeningServer(apiBaseUrl: string) {
  const npmEntry = process.env.npm_execpath
  if (!npmEntry) throw new Error('npm経由でテストを実行してください')
  const server = spawn(process.execPath, [npmEntry, 'run', 'start', '--workspace=kizamu-frontend-react'], {
    env: { ...process.env, PORT: '0', API_BASE_URL: apiBaseUrl },
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
    detached: process.platform !== 'win32',
  })
  const closed = new Promise<void>((resolve) => server.once('close', () => resolve()))
  const close = async () => {
    if (server.pid && server.exitCode === null && server.signalCode === null) {
      if (process.platform === 'win32') {
        await promisify(execFile)('taskkill', ['/pid', String(server.pid), '/T', '/F'])
      } else {
        process.kill(-server.pid, 'SIGTERM')
      }
    }
    await closed
  }
  try {
    return { url: await waitForListeningUrl(server), close }
  } catch (error) {
    await close()
    throw error
  }
}

async function waitForListeningUrl(server: ChildProcessByStdio<null, Readable, Readable>): Promise<string> {
  let output = ''
  let startupTimeout: ReturnType<typeof setTimeout> | undefined
  try {
    return await new Promise<string>((resolve, reject) => {
      startupTimeout = setTimeout(() => reject(new Error(`起動URLが出力されませんでした: ${output}`)), 15000)
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
