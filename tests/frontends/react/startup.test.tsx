import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, it, vi } from 'vitest'

describe('通常起動', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  it('Googleログイン後に記録一覧を取得すると、取得したIDトークンをBearer形式で送ります', async () => {
    stubGoogleSignIn('google-id-token')
    const fetchMock = vi.fn<typeof fetch>(async (input) => Response.json(input === '/api/time-records' ? [] : { status: 'ok' }))
    vi.stubGlobal('fetch', fetchMock)
    document.body.innerHTML = '<div id="root"></div>'

    await import('../../../src/frontends/react/src/main')
    fireEvent.click(await screen.findByRole('button', { name: 'Googleアカウントでログイン' }))

    await waitFor(() => {
      const requests = fetchMock.mock.calls.map(([url, init]) => ({
        url,
        authorization: new Headers(init?.headers).get('Authorization'),
      }))
      requests.should.toContainEqual({
        url: '/api/time-records',
        authorization: 'Bearer google-id-token',
      })
    })
    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
  })
})

function stubGoogleSignIn(token: string) {
  let receiveCredential: ((response: { credential: string }) => void) | undefined
  vi.stubGlobal('google', {
    accounts: { id: {
      initialize: ({ callback }: { callback: (response: { credential: string }) => void }) => {
        receiveCredential = callback
      },
      renderButton: (container: HTMLElement) => {
        const button = document.createElement('button')
        button.textContent = 'Googleアカウントでログイン'
        button.onclick = () => receiveCredential?.({ credential: token })
        container.appendChild(button)
      },
    } },
  })
}
