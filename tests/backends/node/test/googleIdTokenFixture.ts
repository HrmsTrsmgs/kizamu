import { generateKeyPairSync, sign, type KeyObject } from 'node:crypto'

export function createGoogleIdTokenFixture(keyId: string) {
  const key = generateKeyPairSync('rsa', { modulusLength: 2048 })

  return {
    publicKeyResponse(init?: ResponseInit) {
      return Response.json({ [keyId]: key.publicKey.export({ type: 'spki', format: 'pem' }).toString() }, init)
    },

    createToken(claims: object, {
      type,
      signingKey = key.privateKey,
    }: { type?: 'JWT'; signingKey?: KeyObject } = {}) {
      const header = Buffer.from(JSON.stringify({
        alg: 'RS256', kid: keyId, ...(type === undefined ? {} : { typ: type }),
      })).toString('base64url')
      const payload = Buffer.from(JSON.stringify(claims)).toString('base64url')
      const content = `${header}.${payload}`
      return `${content}.${sign('RSA-SHA256', Buffer.from(content), signingKey).toString('base64url')}`
    },
  }
}
