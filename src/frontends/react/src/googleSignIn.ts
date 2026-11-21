declare const google: {
  accounts: { id: {
    initialize: (options: { client_id: string, callback: (response: { credential: string }) => void }) => void
    renderButton: (container: HTMLElement, options: { type: 'standard' | 'icon' }) => void
  } }
}

export function renderGoogleButton(container: HTMLElement, onCredential: (token: string) => void) {
  google.accounts.id.initialize({ client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '', callback: response => onCredential(response.credential) })
  container.replaceChildren()
  google.accounts.id.renderButton(container, { type: 'standard' })
}
