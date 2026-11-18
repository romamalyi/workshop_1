// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const eslintConfigPrettier = require('eslint-config-prettier');

module.exports = defineConfig([
  {
    ignores: ['dist/**', 'node_modules/**', '.angular/**', 'coverage/**'],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
      eslintConfigPrettier,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
      '@angular-eslint/prefer-standalone': 'off',
      '@angular-eslint/prefer-inject': 'warn',
      '@angular-eslint/no-output-native': 'warn',
      '@angular-eslint/no-output-on-prefix': 'warn',
      '@typescript-eslint/consistent-type-definitions': 'warn',
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [
      angular.configs.templateRecommended,
      angular.configs.templateAccessibility,
      eslintConfigPrettier,
    ],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/click-events-have-key-events': 'warn',
      '@angular-eslint/template/interactive-supports-focus': 'warn',
      '@angular-eslint/template/label-has-associated-control': 'warn',
      '@angular-eslint/template/eqeqeq': ['error', { allowNullOrUndefined: true }],
    },
  },
  {
    files: [
      'src/app/shared/components/paginator/paginator.ts',
      'src/app/shared/directives/tooltip.directive.ts',
    ],
    rules: {
      '@angular-eslint/component-selector': 'off',
      '@angular-eslint/directive-selector': 'off',
    },
  },
]);
