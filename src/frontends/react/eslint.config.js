import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

const viewImports = ['**/App', '**/App.tsx', '**/main', '**/main.tsx', '**/timeRecords/**', '**/health/**']
const browserGlobals = ['fetch', 'XMLHttpRequest', 'window', 'document', 'navigator', 'self', 'globalThis']

export default tseslint.config(
  { ignores: ['dist'] },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    },
  },
  {
    files: ['src/presentation/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-globals': ['error', ...browserGlobals],
      'no-restricted-syntax': ['error', {
        selector: 'ImportExpression',
        message: 'Inner layers use static imports so dependency checks can inspect them.',
      }],
      'no-restricted-imports': ['error', { patterns: [...viewImports, '**/infrastructure/**'] }],
    },
  },
  {
    files: ['src/infrastructure/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', { patterns: [...viewImports, 'react', 'react-dom', 'react-dom/**'] }],
    },
  },
  {
    files: ['src/App.tsx', 'src/{timeRecords,health}/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', { patterns: ['**/infrastructure/**'] }],
      'no-restricted-globals': ['error', ...browserGlobals],
    },
  },
)
