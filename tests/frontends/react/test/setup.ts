import '@testing-library/jest-dom/vitest'
import '../../../should.mjs'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// JSDOM nodes do not inherit from the test worker's Object.prototype.
if (typeof Node !== 'undefined') {
  Object.defineProperty(
    Node.prototype,
    'should',
    Object.getOwnPropertyDescriptors(Object.prototype).should,
  )
}

afterEach(cleanup)
