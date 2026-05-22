# SafetyScale — SPA (`src/Web`)

Frontend **React + TypeScript + Vite** do SafetyScale.

## Fluxos principais

- **`/login`** — autenticação (`POST /api/auth/login`). O JWT inclui **`tenant_id`**; a sessão em `sessionStorage` persiste **`token`** e **`tenantId`** (entre outros dados de perfil) para todas as chamadas autenticadas à API.
- **`/signup`** — cadastro público de empresa (`POST /api/tenants/register`, anônimo). Implementação em **`src/features/tenant-registration/`**.
- **`/app/...`** — área protegida (shell, Guards, Availability, Schedules).

## Como rodar contra a API

Detalhes (proxy Vite `4863`, CORS variáveis, smoke na home): ver o **[README.md](../../README.md)** na raiz do repositório (secção **Frontend**).

Resumo rápido:

```bash
cd src/Web
cp .env.example .env   # ajustar se necessário (ex.: VITE_DEV_API_PROXY_TARGET em HTTPS local)
npm install
npm run dev
```

Scripts: `npm run dev` | `build` | `test` | `lint` | `format`.

---

_Conteúdo abaixo: template oficial do Vite (referência de ferramentas)._

---

# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
