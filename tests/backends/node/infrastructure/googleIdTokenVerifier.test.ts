import { generateKeyPairSync, type KeyObject } from 'node:crypto'
import { afterEach, beforeEach, describe, test, vi } from 'vitest'
import { GoogleIdTokenVerifier } from '../../../../src/backends/node/infrastructure/googleIdTokenVerifier.js'
import { createGoogleIdTokenFixture } from '../test/googleIdTokenFixture.js'

const googleTokens = createGoogleIdTokenFixture('test-key')
const attackerKey = generateKeyPairSync('rsa', { modulusLength: 2048 })
// 外部通信だけを差し替え、JWTの検証は実際の検証器へ渡します。
const publicKeyFetch: typeof fetch = async () => googleTokens.publicKeyResponse()

let tested: GoogleIdTokenVerifier

beforeEach(() => {
  tested = new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)
})

test.each(['https://accounts.google.com', 'accounts.google.com'])(
  '発行者が %s の有効なGoogle IDトークンでは、メールアドレスが変わっても同じsubで本人を識別します',
  async (issuer) => {
    await tested.verify(createToken({ issuer, email: 'before@example.test' })).should.resolves.toBe('google-user')
    await tested.verify(createToken({ issuer, email: 'after@example.test' })).should.resolves.toBe('google-user')
  },
)

describe('検証時刻を指定するとき', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))
  })

  afterEach(() => vi.useRealTimers())

  test.each([
    ['有効期限の1ミリ秒前に検証すると、本人識別子を返します', -1, 'google-user'],
    ['有効期限に達した時点で検証すると、本人識別子はnullです', 0, null],
    ['有効期限の1ミリ秒後に検証すると、本人識別子はnullです', 1, null],
  ])('Google IDトークンを%s', async (_, 期限との差, 本人識別子) => {
    const 有効期限 = new Date('2026-01-01T01:00:00Z')
    const token = createToken({ expiresAt: 有効期限 })

    vi.setSystemTime(有効期限.getTime() + 期限との差)

    await tested.verify(token).should.resolves.toBe(本人識別子)
  })

  test('公開鍵の有効期間内は再利用し、期限後は再取得してGoogle IDトークンを検証します', async () => {
    const fetchPublicKey = vi.fn<typeof fetch>(async () =>
      googleTokens.publicKeyResponse({ headers: { 'Cache-Control': 'public, max-age=60' } }),
    )
    const verifier = new GoogleIdTokenVerifier('kizamu-client', fetchPublicKey)
    const token = createToken()

    await verifier.verify(token).should.resolves.toBe('google-user')
    await verifier.verify(token).should.resolves.toBe('google-user')
    fetchPublicKey.should.have.callCount(1)

    vi.setSystemTime(new Date('2026-01-01T00:01:01Z'))
    await verifier.verify(token).should.resolves.toBe('google-user')
    fetchPublicKey.should.have.callCount(2)
  })
})

test('Google IDトークンのメールアドレスが同じでも、subが異なれば別の利用者として識別します', async () => {
  const email = 'user@example.test'

  await tested.verify(createToken({ email, subject: 'first-user' })).should.resolves.toBe('first-user')
  await tested.verify(createToken({ email, subject: 'second-user' })).should.resolves.toBe('second-user')
})

test.each([
  ['Googleの公開鍵で署名を検証できないトークン', () => createToken({ signingKey: attackerKey.privateKey })],
  ['発行者がGoogleでないトークン', () => createToken({ issuer: 'https://attacker.example.test' })],
  ['対象のクライアントが異なるトークン', () => createToken({ audience: 'another-client' })],
  ['有効期限が過ぎたトークン', () => createToken({ expiresAt: new Date(Date.now() - 3_600_000) })],
  ['subがないトークン', () => createToken({ subject: null })],
  ['subが空文字列のトークン', () => createToken({ subject: '' })],
  ['subが数値のトークン', () => createToken({ subject: 123 })],
  ['JWT形式でない文字列', () => 'not-a-jwt'],
])('%sを検証すると、本人識別子はnullです', async (_, token) => {
  await tested.verify(token()).should.resolves.toBe(null)
})

function createToken({
  subject = 'google-user',
  issuer = 'https://accounts.google.com',
  audience = 'kizamu-client',
  email = 'user@example.test',
  expiresAt = new Date(Date.now() + 3_600_000),
  signingKey,
}: {
  subject?: string | number | null
  issuer?: string
  audience?: string
  email?: string
  expiresAt?: Date
  signingKey?: KeyObject
} = {}): string {
  const now = Math.floor(Date.now() / 1000)
  return googleTokens.createToken({
    iss: issuer, aud: audience, email, iat: now - 7200, exp: Math.floor(expiresAt.getTime() / 1000),
    ...(subject === null ? {} : { sub: subject }),
  }, { type: 'JWT', signingKey })
}
