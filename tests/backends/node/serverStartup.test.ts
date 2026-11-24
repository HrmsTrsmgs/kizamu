import { ChildProcess, spawn, type ChildProcessByStdio } from 'node:child_process'
import { PassThrough, type Readable } from 'node:stream'
import { fileURLToPath } from 'node:url'
import { test } from 'vitest'

test('Node版の業務APIを空きポートで起動すると、出力されたURLで正常性を確認できます', async () => {
  await withListeningServer(async (listeningUrl) => {
    const response = await fetch(`${listeningUrl}/api/health`, { signal: AbortSignal.timeout(3000) })

    response.status.should.equal(200)
  })
}, 10000)

test('起動URLが分割して出力されると、改行を受信してから完全なURLを返します', async () => {
  const server = createServerWithWritableOutput()
  let completed = false
  const listeningUrl = waitForListeningUrl(server).then((url) => {
    completed = true
    return url
  })

  server.stdout.write('Starting server\nhttp://localhost:52')
  await new Promise<void>((resolve) => setImmediate(resolve))
  const completedBeforeNewline = completed
  server.stdout.write('13\n')

  const url = await listeningUrl
  completedBeforeNewline.should.equal(false)
  url.should.equal('http://localhost:5213')
})

function createServerWithWritableOutput() {
  const stdout = new PassThrough()
  const stderr = new PassThrough()
  const stdio: [null, PassThrough, PassThrough, undefined, undefined] = [null, stdout, stderr, undefined, undefined]

  return Object.assign(new ChildProcess(), { stdin: null, stdout, stderr, stdio })
}

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
        const completeOutput = standardOutput.slice(0, standardOutput.lastIndexOf('\n'))
        const url = completeOutput.match(/http:\/\/(?:localhost|127\.0\.0\.1|\[::1\]):[1-9]\d*/)?.[0]
        if (url) resolve(url)
      })
    })
  } finally {
    clearTimeout(startupTimeout)
  }
}
