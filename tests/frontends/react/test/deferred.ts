export function deferred<T>() {
  let resolve: (value: T) => void = () => { throw new Error('Promise is not initialized') }
  let reject: (reason: Error) => void = () => { throw new Error('Promise is not initialized') }
  const promise = new Promise<T>((resolvePromise, rejectPromise) => { resolve = resolvePromise; reject = rejectPromise })
  return { promise, resolve, reject }
}
