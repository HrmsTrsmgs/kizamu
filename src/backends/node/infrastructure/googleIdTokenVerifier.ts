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
    const { certs } = await this.#client.getFederatedSignonCertsAsync()
    try {
      const ticket = await this.#client.verifySignedJwtWithCertsAsync(
        idToken, certs, this.#clientId, ['https://accounts.google.com', 'accounts.google.com'],
      )
      return ticket.getPayload()?.sub ?? null
    } catch {
      return null
    }
  }
}
