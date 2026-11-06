import js from '@eslint/js'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.ts'],
    rules: {
      'no-restricted-imports': ['error', { patterns: [{
        regex: '^(?!\\./|temporal-polyfill$).*',
        message: 'Domain only imports its own modules and the date/time library.',
      }] }],
      'no-restricted-syntax': ['error', { selector: 'ImportExpression', message: 'Domain dependencies must be statically inspectable.' }],
      'no-restricted-globals': ['error', 'fetch', 'XMLHttpRequest', 'window', 'document', 'navigator', 'self', 'globalThis', 'process'],
    },
  },
)
