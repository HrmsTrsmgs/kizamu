import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, it, vi } from 'vitest'

describe('通常起動', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  it('Googleログインで取得したIDトークンを記録一覧の要求へ送ります', async () => {
    let receiveCredential: ((response: { credential: string }) => void) | undefined
    vi.stubGlobal('google', {
      accounts: { id: {
        initialize: ({ callback }: { callback: (response: { credential: string }) => void }) => { receiveCredential = callback },
        renderButton: (container: HTMLElement) => {
          const button = document.createElement('button')
          button.textContent = 'Googleアカウントでログイン'
          button.onclick = () => receiveCredential?.({ credential: 'google-id-token' })
          container.appendChild(button)
        },
      } },
    })
    const fetchMock = vi.fn<typeof fetch>(async (input) => Response.json(input === '/api/time-records' ? [] : { status: 'healthy' }))
    vi.stubGlobal('fetch', fetchMock)
    document.body.innerHTML = '<div id="root"></div>'

    await import('../../../src/frontends/react/src/main')
    fireEvent.click(await screen.findByRole('button', { name: 'Googleアカウントでログイン' }))

    await waitFor(() => {
      fetchMock.mock.calls.some(([url, init]) =>
        url === '/api/time-records' && new Headers(init?.headers).get('Authorization') === 'Bearer google-id-token',
      ).should.equal(true)
    })
    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
  })
})
