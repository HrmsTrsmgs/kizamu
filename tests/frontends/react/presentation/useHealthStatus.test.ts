import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, it, vi, type Mock } from 'vitest'
import { useHealthStatus } from '../../../../src/frontends/react/src/presentation/useHealthStatus'
import type { HealthService } from '../../../../src/frontends/react/src/presentation/healthService'
import { deferred } from '../test/deferred'

describe('useHealthStatus', () => {
  let checkHealth: Mock<HealthService['checkHealth']>
  let health: HealthService

  beforeEach(() => {
    checkHealth = vi.fn<HealthService['checkHealth']>()
    health = { checkHealth }
  })

  it('サーバーの応答を待つ間、接続状態は未確定を表すnullです', () => {
    checkHealth.mockReturnValue(new Promise<boolean>(() => {}))

    const { result } = renderHook(() => useHealthStatus(health))

    result.should.have.property('current', null)
  })

  it.each([true, false])('サーバーの接続確認が完了すると、その結果（%s）を接続状態として返します', async isHealthy => {
    checkHealth.mockResolvedValue(isHealthy)

    const { result } = renderHook(() => useHealthStatus(health))

    await waitFor(() => result.should.have.property('current', isHealthy))
  })

  it('接続確認で通信エラーが起きると、接続状態は失敗を表すfalseです', async () => {
    checkHealth.mockImplementation(async () => { throw new Error('offline') })

    const { result } = renderHook(() => useHealthStatus(health))

    await waitFor(() => result.should.have.property('current', false))
  })

  it('接続先の切替後に届いた旧接続先の結果を無視します', async () => {
    const oldHealth = deferred<boolean>()
    checkHealth.mockReturnValue(oldHealth.promise)
    const { result, rerender } = renderHook(({ health }) => useHealthStatus(health), {
      initialProps: { health },
    })
    rerender({ health: { checkHealth: async () => false } })
    await waitFor(() => result.should.have.property('current', false))

    await act(async () => oldHealth.resolve(true))

    result.should.have.property('current', false)
  })
})
