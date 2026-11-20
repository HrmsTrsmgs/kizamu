import { generateKeyPairSync, sign, type KeyObject } from 'node:crypto'
import { beforeEach, test, vi } from 'vitest'
import { GoogleIdTokenVerifier } from '../../../../src/backends/node/infrastructure/googleIdTokenVerifier.js'

const googleKey = generateKeyPairSync('rsa', { modulusLength: 2048 })
const attackerKey = generateKeyPairSync('rsa', { modulusLength: 2048 })
// 外部通信だけを差し替え、JWTの検証は実際の検証器へ渡します。
const publicKeyFetch: typeof fetch = async () => Response.json({
  'test-key': googleKey.publicKey.export({ type: 'spki', format: 'pem' }).toString(),
})

let tested: GoogleIdTokenVerifier

beforeEach(() => {
  tested = new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)
})

for (const issuer of ['https://accounts.google.com', 'accounts.google.com']) {
  test(`発行者が ${issuer} の有効なGoogle IDトークンでは、メールアドレスが変わっても同じsubで本人を識別します`, async () => {
    ({ subject: await tested.verify(createToken({ issuer, email: 'before@example.test' })) }).should.deep.equal({ subject: 'google-user' })
    ;({ subject: await tested.verify(createToken({ issuer, email: 'after@example.test' })) }).should.deep.equal({ subject: 'google-user' })
  })
}

test('公開鍵の有効期間内は再利用し、期限後は再取得してGoogle IDトークンを検証します', async () => {
  vi.useFakeTimers()
  try {
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
    const fetchPublicKey = vi.fn<typeof fetch>(async () => Response.json({
      'test-key': googleKey.publicKey.export({ type: 'spki', format: 'pem' }).toString(),
    }, { headers: { 'Cache-Control': 'public, max-age=60' } }))
    const tested = new GoogleIdTokenVerifier('kizamu-client', fetchPublicKey)
    const token = createToken()

    ;({ subject: await tested.verify(token) }).should.deep.equal({ subject: 'google-user' })
    ;({ subject: await tested.verify(token) }).should.deep.equal({ subject: 'google-user' })
    const 有効期間内の取得回数 = fetchPublicKey.mock.calls.length
    vi.setSystemTime(new Date('2026-01-01T00:01:01Z'))
    ;({ subject: await tested.verify(token) }).should.deep.equal({ subject: 'google-user' })

    ;[有効期間内の取得回数, fetchPublicKey.mock.calls.length].should.deep.equal([1, 2])
  } finally {
    vi.useRealTimers()
  }
})

test('Google IDトークンのメールアドレスが同じでも、subが異なれば別の利用者として識別します', async () => {
  ({ subject: await tested.verify(createToken({ subject: 'first-user' })) }).should.deep.equal({ subject: 'first-user' })
  ;({ subject: await tested.verify(createToken({ subject: 'second-user' })) }).should.deep.equal({ subject: 'second-user' })
})

test.each([
  ['Googleの公開鍵で署名を検証できないトークン', () => createToken({ signingKey: attackerKey.privateKey })],
  ['発行者がGoogleでないトークン', () => createToken({ issuer: 'https://attacker.example.test' })],
  ['対象のクライアントが異なるトークン', () => createToken({ audience: 'another-client' })],
  ['有効期限が過ぎたトークン', () => createToken({ expired: true })],
  ['subがないトークン', () => createToken({ subject: null })],
  ['JWT形式でない文字列', () => 'not-a-jwt'],
])('%sを検証すると、本人識別子はnullです', async (_, token) => {
  ({ subject: await tested.verify(token()) }).should.deep.equal({ subject: null })
})

function createToken({
  subject = 'google-user',
  issuer = 'https://accounts.google.com',
  audience = 'kizamu-client',
  email = 'user@example.test',
  expired = false,
  signingKey = googleKey.privateKey,
}: {
  subject?: string | null
  issuer?: string
  audience?: string
  email?: string
  expired?: boolean
  signingKey?: KeyObject
} = {}): string {
  const now = Math.floor(Date.now() / 1000)
  const header = Buffer.from(JSON.stringify({ alg: 'RS256', kid: 'test-key', typ: 'JWT' })).toString('base64url')
  const payload = Buffer.from(JSON.stringify({
    iss: issuer, aud: audience, email, iat: now - 7200, exp: now + (expired ? -3600 : 3600),
    ...(subject === null ? {} : { sub: subject }),
  })).toString('base64url')
  const content = `${header}.${payload}`
  return `${content}.${sign('RSA-SHA256', Buffer.from(content), signingKey).toString('base64url')}`
}
