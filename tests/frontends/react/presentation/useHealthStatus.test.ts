import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, it } from 'vitest'
import { useHealthStatus } from '../../../../src/frontends/react/src/presentation/useHealthStatus'
import { deferred } from '../test/deferred'

describe('useHealthStatus', () => {
  it('確認前は未確定の状態を公開します', () => {
    const health = { checkHealth: () => new Promise<boolean>(() => {}) }

    const { result } = renderHook(() => useHealthStatus(health))

    result.should.have.property('current', null)
  })

  it.each([true, false])('確認した接続状態を公開します: %s', async isHealthy => {
    const health = { checkHealth: async () => isHealthy }

    const { result } = renderHook(() => useHealthStatus(health))

    await waitFor(() => result.should.have.property('current', isHealthy))
  })

  it('APIに接続できない場合は失敗の状態を公開します', async () => {
    const health = { checkHealth: async () => { throw new Error('offline') } }

    const { result } = renderHook(() => useHealthStatus(health))

    await waitFor(() => result.should.have.property('current', false))
  })

  it('接続先の切替後に届いた旧接続先の結果を無視します', async () => {
    const oldHealth = deferred<boolean>()
    const { result, rerender } = renderHook(({ health }) => useHealthStatus(health), {
      initialProps: { health: { checkHealth: () => oldHealth.promise } },
    })
    rerender({ health: { checkHealth: async () => false } })
    await waitFor(() => result.should.have.property('current', false))

    await act(async () => oldHealth.resolve(true))

    result.should.have.property('current', false)
  })
})
