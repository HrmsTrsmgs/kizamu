import { generateKeyPairSync, sign, type KeyObject } from 'node:crypto'
import { test } from 'vitest'
import { GoogleIdTokenVerifier } from '../../../src/backends/node/infrastructure/googleIdTokenVerifier.js'

const googleKey = generateKeyPairSync('rsa', { modulusLength: 2048 })
const attackerKey = generateKeyPairSync('rsa', { modulusLength: 2048 })
// 外部通信だけを差し替え、JWTの検証は実際の検証器へ渡します。
const publicKeyFetch: typeof fetch = async () => Response.json({
  keys: [{ ...googleKey.publicKey.export({ format: 'jwk' }), kid: 'test-key', alg: 'RS256', use: 'sig' }],
})

for (const issuer of ['https://accounts.google.com', 'accounts.google.com']) {
  test(`有効なGoogleトークンでは更新やメール変更後も同じsubで本人を識別する (${issuer})`, async () => {
    const tested = new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)

    ;({ subject: await tested.verify(createToken({ issuer, email: 'before@example.test' })) }).should.deep.equal({ subject: 'google-user' })
    ;({ subject: await tested.verify(createToken({ issuer, email: 'after@example.test' })) }).should.deep.equal({ subject: 'google-user' })
  })
}

test('メールが同じでもsubが異なる利用者を区別する', async () => {
  const tested = new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)

  ;({ subject: await tested.verify(createToken({ subject: 'first-user' })) }).should.deep.equal({ subject: 'first-user' })
  ;({ subject: await tested.verify(createToken({ subject: 'second-user' })) }).should.deep.equal({ subject: 'second-user' })
})

test.each([
  ['signature', () => createToken({ signingKey: attackerKey.privateKey })],
  ['issuer', () => createToken({ issuer: 'https://attacker.example.test' })],
  ['audience', () => createToken({ audience: 'another-client' })],
  ['expired', () => createToken({ expired: true })],
  ['missing-sub', () => createToken({ subject: null })],
  ['malformed', () => 'not-a-jwt'],
])('不正なGoogleトークンから本人識別子を返さない (%s)', async (_, token) => {
  const tested = new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)

  ;({ subject: await tested.verify(token()) }).should.deep.equal({ subject: null })
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
