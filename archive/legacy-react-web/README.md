# SafetyScale — SPA React (arquivado — B11)

> **Arquivado em 2026-06-23.** Este era o frontend em `src/Web`. Não participa de build, CI ou deploy. Frontend oficial: [`src/Web.Blazor`](../../src/Web.Blazor/README.md). Referência histórica de paridade visual/funcional.

Frontend **React + TypeScript + Vite** do SafetyScale.

## Fluxos principais

- **`/login`** — autenticação (`POST /api/auth/login`). O JWT inclui **`tenant_id`**; a sessão em `sessionStorage` persiste **`token`** e **`tenantId`** (entre outros dados de perfil) para todas as chamadas autenticadas à API.
- **`/signup`** — cadastro público de empresa (`POST /api/tenants/register`, anônimo). Implementação em **`src/features/tenant-registration/`**.
- **`/app/...`** — área protegida (shell com bottom nav **Dashboard**, **Sectors**, **Guards**, **Availability**, **Schedules**).

### Área `/app`

- **`/app/sectors`** — setores (**`Sector`**) do tenant: **`Admin`** pode criar/editar/inativar/reativar (**`PATCH`** ativo/inativo) e configurar **`requiredGuardsPerDay`** (≥ 1 onde a UI valida); **`Supervisor`** normalmente apenas consulta a lista (**`GET /api/sectors`**).
- **`/app/security-guards`** — CRUD/listagem conforme perfil (**`PATCH`** inativo/**ativo** só **`Admin`**); formulário permite associar seguranças a setores via **`PUT /api/security-guards/{id}/sectors`** (lista de GUIDs substitutiva — só **`Admin`**).
- **`/app/unavailable-days`** — indisponibilidades por segurança (ver raiz **`README.md`**).
- **`/app/schedules`** — consulta **`GET /api/schedules/month/{month}/year/{year}`**; **`Admin`** gera com **`POST /api/schedules/generate`**; quando a API responde **400** com JSON **`code: "ScheduleCoverageFailed"`**, a UI prioriza **`message`** e usa **`failedDate`** quando vier no corpo (**tipos camelCase**, alinhados ao `AddJsonOptions` da API). Cada item da lista deve mostrar **`sectorName`** (ou equivalente exposto pela API quando o mês já foi gerado).
- **`/app`** dashboard — pode resumir o dia usando os mesmos dados (setores nas atribuições).

Contratos camelCase devem estar alinhados aos DTOs em `src/Api/Contracts/` e aos tipos em `src/Web/shared/` (onde existirem).

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
      // Alternatively, use this for more strict rules
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
