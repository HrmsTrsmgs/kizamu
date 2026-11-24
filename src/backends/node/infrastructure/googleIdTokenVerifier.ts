import { OAuth2Client } from 'google-auth-library'

/** Google IDトークンを検証し、利用者の安定した識別子を取得します。 */
export class GoogleIdTokenVerifier {
  readonly #client: OAuth2Client
  readonly #clientId: string

  /** 対象アプリのクライアントIDと、Google公開鍵取得用のHTTP通信を指定します。 */
  constructor(clientId: string, publicKeyFetch: typeof fetch = fetch) {
    this.#clientId = clientId
    this.#client = new OAuth2Client({ transporterOptions: { fetchImplementation: publicKeyFetch } })
  }

  /** 署名、発行元、対象アプリ、有効期限を検証し、検証済みsubまたは不正時のnullを返します。 */
  async verify(idToken: string): Promise<string | null> {
    const { certs } = await this.#client.getFederatedSignonCertsAsync().catch((cause: unknown) => {
      throw new Error('Failed to fetch Google public keys.', { cause })
    })
    try {
      const ticket = await this.#client.verifySignedJwtWithCertsAsync(
        idToken, certs, this.#clientId, ['https://accounts.google.com', 'accounts.google.com'],
      )
      const payload = ticket.getPayload()
      if (!payload || Date.now() >= payload.exp * 1000) return null

      const subject = payload.sub
      return typeof subject === 'string' && subject.length > 0 ? subject : null
    } catch {
      return null
    }
  }
}
