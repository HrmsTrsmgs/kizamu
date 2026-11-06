import { should as enableShould, type Assertion } from 'vitest'

declare global {
  interface Object {
    should: Assertion<unknown>
  }
}

export const should = enableShould()
