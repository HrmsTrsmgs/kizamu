export function deferred<T>() {
  let resolve: (value: T) => void = () => { throw new Error('Promise is not initialized') }
  const promise = new Promise<T>(resolvePromise => { resolve = resolvePromise })
  return { promise, resolve }
}
