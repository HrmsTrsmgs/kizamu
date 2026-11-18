import { createPublicKey, type webcrypto } from 'node:crypto'
import { OAuth2Client } from 'google-auth-library'

/** Google IDトークンを検証し、利用者の安定した識別子を取得します。 */
export class GoogleIdTokenVerifier {
  readonly #client = new OAuth2Client()
  readonly #clientId: string
  readonly #publicKeyFetch: typeof fetch

  /** 対象アプリのクライアントIDと、Google公開鍵取得用のHTTP通信を指定します。 */
  constructor(clientId: string, publicKeyFetch: typeof fetch = fetch) {
    this.#clientId = clientId
    this.#publicKeyFetch = publicKeyFetch
  }

  /** 署名、発行元、対象アプリ、有効期限を検証し、検証済みsubまたは不正時のnullを返します。 */
  async verify(idToken: string): Promise<string | null> {
    const response = await this.#publicKeyFetch('https://www.googleapis.com/oauth2/v3/certs')
    const { keys }: { keys: (webcrypto.JsonWebKey & { kid: string })[] } = await response.json()
    // Node版の公式検証APIへ渡せるPEM形式へ、標準APIで公開鍵の表現だけを変換します。
    const certificates = Object.fromEntries(keys.map(key => [
      key.kid,
      createPublicKey({ key, format: 'jwk' }).export({ type: 'spki', format: 'pem' }).toString(),
    ]))
    try {
      const ticket = await this.#client.verifySignedJwtWithCertsAsync(
        idToken, certificates, this.#clientId, ['https://accounts.google.com', 'accounts.google.com'],
      )
      return ticket.getPayload()?.sub ?? null
    } catch {
      return null
    }
  }
}
