# Convenções do frontend Blazor — SafetyScale

Guia de convenções da equipe para a migração React → Blazor WebAssembly. Fase **B0.3** — [`roadmap-blazor-migration.md`](../roadmap-blazor-migration.md).

Decisões de arquitetura: [ADR 001 — Frontend Blazor WebAssembly Standalone](adr/001-blazor-wasm-frontend.md).

---

## 1. Escopo e princípios

| Princípio | Regra |
|---|---|
| Paridade funcional | Comportamento e rotas espelham o React F0–F4 antes de polish |
| API inalterada | Contratos REST e JWT permanecem; DTOs no Blazor **espelham** JSON da API |
| Identidade visual | Copiar CSS do React (Stitch, Inter, Material Symbols) — **sem redesign** nesta migração |
| Sem UI library nova | Não introduzir MudBlazor, Radzen, Bootstrap Components etc. durante B1–B10 |
| Uma fonte de verdade por fase | Após **B4**, novas features só no Blazor (ver seção 6) |

---

## 2. Estrutura de pastas (`src/Web.Blazor`)

Estrutura-alvo (criada formalmente na **B1**; spike B0.2 pode ter arquivos fora dela até reorganização):

```text
src/Web.Blazor/
 ├── Components/              # Componentes reutilizáveis (AppHeader, MonthCalendar, …)
 │   └── Calendar/
 ├── Layout/                  # AppLayout (shell autenticado), layouts públicos
 ├── Pages/
 │   ├── Home.razor           # rota /
 │   ├── Auth/                # login, signup (rotas públicas)
 │   └── App/                 # área autenticada /app/*
 ├── Services/
 │   ├── Api/                 # *ApiClient, handlers HTTP
 │   └── Auth/                # sessão JWT, AuthenticationStateProvider
 ├── Models/                  # DTOs e requests/responses (JSON camelCase)
 ├── wwwroot/
 │   ├── css/app.css          # estilos globais (Inter, Material Symbols)
 │   ├── js/                  # interop mínimo (ex.: sessionStorage)
 │   ├── appsettings.json
 │   └── icons.svg            # copiar de src/Web/public (B1)
 ├── Program.cs
 └── README.md
```

### O que vai onde

| Tipo | Pasta | Exemplo |
|---|---|---|
| Página pública | `Pages/` ou `Pages/Auth/` | `Pages/Auth/Login.razor` |
| Página autenticada | `Pages/App/` | `Pages/App/Sectors.razor` |
| Componente compartilhado | `Components/` | `Components/AppHeader.razor` |
| Cliente HTTP por domínio | `Services/Api/` | `Services/Api/SectorsApiClient.cs` |
| Auth / sessão | `Services/Auth/` | `Services/Auth/JwtSessionService.cs` |
| DTO espelhando API | `Models/` | `Models/SectorDto.cs` |
| Spike / POC temporária | `Pages/` (até B1) | `Pages/Spike.razor` — remover ou mover após bootstrap |

### Mapeamento React → Blazor

| React | Blazor |
|---|---|
| `src/Web/src/features/auth/LoginPage.tsx` | `Pages/Auth/Login.razor` + `Login.razor.css` |
| `src/Web/src/features/sectors/SectorsPage.tsx` | `Pages/App/Sectors.razor` + `Sectors.razor.css` |
| `src/Web/src/shared/components/AppHeader/` | `Components/AppHeader.razor` |
| `src/Web/src/features/sectors/sectorsApi.ts` | `Services/Api/SectorsApiClient.cs` |
| `src/Web/src/features/sectors/types.ts` | `Models/SectorDto.cs` |
| `src/Web/src/shared/auth/` | `Services/Auth/` |
| `src/Web/src/app/AppLayout.tsx` | `Layout/AppLayout.razor` |

Inventário completo de rotas: seção **Inventário de referência** em [`roadmap-blazor-migration.md`](../roadmap-blazor-migration.md).

---

## 3. Nomenclatura

### Arquivos e classes

| Artefato | Convenção | Exemplo |
|---|---|---|
| Página | PascalCase, sem sufixo `Page` | `Sectors.razor`, não `SectorsPage.razor` |
| Scoped CSS | mesmo nome + `.razor.css` | `Sectors.razor.css` |
| Componente | PascalCase descritivo | `AppHeader.razor`, `MonthCalendar.razor` |
| ApiClient | `{Domínio}ApiClient` | `SecurityGuardsApiClient` |
| DTO | `{Nome}Dto` | `SectorDto`, `MonthlyScheduleDto` |
| Request body | `{Ação}{Entidade}Request` | `CreateSectorRequest`, `LoginRequest` |
| Serviço auth | substantivo + `Service` ou `Provider` | `AuthSessionService`, `JwtAuthStateProvider` |

### Namespaces

Root: `SafetyScale.Web.Blazor`

- `SafetyScale.Web.Blazor.Pages.App`
- `SafetyScale.Web.Blazor.Pages.Auth`
- `SafetyScale.Web.Blazor.Components`
- `SafetyScale.Web.Blazor.Services.Api`
- `SafetyScale.Web.Blazor.Services.Auth`
- `SafetyScale.Web.Blazor.Models`

### Rotas (`@page`)

Espelhar o React em [`src/Web/src/app/routes.tsx`](../src/Web/src/app/routes.tsx):

| Rota | Arquivo |
|---|---|
| `/` | `Pages/Home.razor` |
| `/login` | `Pages/Auth/Login.razor` |
| `/signup` | `Pages/Auth/RegisterTenant.razor` |
| `/app` | `Pages/App/Welcome.razor` |
| `/app/access-denied` | `Pages/App/AccessDenied.razor` |
| `/app/sectors` | `Pages/App/Sectors.razor` |
| `/app/security-guards` | `Pages/App/SecurityGuards.razor` |
| `/app/unavailable-days` | `Pages/App/UnavailableDays.razor` |
| `/app/schedules` | `Pages/App/Schedules.razor` |

### ApiClient — responsabilidades

- Um client por agregado/domínio de API (não um client monolítico).
- Métodos espelham endpoints: `ListAsync`, `CreateAsync`, `UpdateAsync`, etc.
- Usam `HttpClient` + `ApiUrlBuilder` (B2); não duplicam lógica de base URL.
- Não contêm regra de negócio — só serialização HTTP e tipos.

Exemplo de nomes alinhados aos módulos React:

- `AuthApiClient` — login (equivalente a `loginApi.ts`)
- `TenantsApiClient` — registro (`registerTenantApi.ts`)
- `SectorsApiClient`, `SecurityGuardsApiClient`, `UnavailableDaysApiClient`, `SchedulesApiClient`

### DTOs

- Propriedades em **PascalCase** no C#; serialização **camelCase** (`JsonSerializerOptions` global na B2).
- Nomes alinhados ao JSON da API (ex.: `requiredGuardsPerDay`, `sectorName`, `tenantId`).
- Manter DTOs no projeto Blazor — **não** referenciar `Application`/`Domain` do backend no WASM.

---

## 4. Estilo e identidade visual

### Regra geral: scoped CSS 1:1 com React

1. Cada `.razor` de página ou componente tem par `.razor.css` (isolated CSS do Blazor).
2. Ao portar uma tela React, copiar valores de `*.module.css` para o `.razor.css` correspondente.
3. Estilos globais (reset, `body`, `.material-symbols-outlined`) ficam em `wwwroot/css/app.css` — paridade com [`src/Web/src/index.css`](../src/Web/src/index.css).
4. Fontes e ícones: **Inter** + **Material Symbols Outlined** via `wwwroot/index.html` (já validado na spike B0.2).

### O que não fazer

- Não adicionar biblioteca de componentes UI nesta migração.
- Não reintroduzir Bootstrap do template default como sistema de design (remover/usar mínimo na B1).
- Não alterar tokens Stitch/cores/layout “por melhoria” — polish fica após cutover (B10+).

### Referência Stitch

Antes de implementar telas administrativas novas, manter fluxo Stitch documentado em [`agents.md`](../agents.md) — referência visual; implementação em Blazor segue estas convenções de CSS.

### Spike B0.2 (temporário)

`Pages/Spike.razor` usa classes globais `.spike-*` em `app.css` — **exceção** só para POC. Telas reais (B4+) usam scoped CSS.

---

## 5. Configuração e HTTP

| Item | Convenção |
|---|---|
| Base URL API | `ApiBaseUrl` em `wwwroot/appsettings.json` (substitui `VITE_API_BASE_URL`) |
| Dev | `appsettings.Development.json` → `http://localhost:5003` |
| Prod | `ApiBaseUrl` vazio → URLs relativas `/api/...` |
| SessionStorage | Chave `safetyscale.auth.session`, valor `{ "token": "<jwt>" }` — paridade React |
| Porta dev Blazor | **4864** (React permanece **4863**) |

Detalhes: [ADR 001](adr/001-blazor-wasm-frontend.md).

---

## 6. Convivência React + Blazor e freeze do React

### Período atual (B0–B3)

- **React** (`src/Web`) continua frontend de produção e referência de paridade.
- **Blazor** (`src/Web.Blazor`) é trilha de migração; spike e infra evoluem em paralelo.
- Ambos podem rodar em dev (4863 + 4864) contra a mesma API.

### Antes da fase B4

- React: correções de bugs, ajustes mínimos para manter operação, F5 **não** duplicado no React se já planejado só no Blazor.
- Blazor: bootstrap (B1), infra (B2), layout (B3).

### A partir da fase B4 (freeze)

| Permitido no React | Proibido no React (salvo exceção) |
|---|---|
| Bugfix **P0/P1** em produção | Novas features de produto |
| Correção de regressão bloqueante | Novas telas ou fluxos |
| Manutenção de CI/build até B11 | Refactor ou F5 UX |
| — | Duplicar trabalho já iniciado no Blazor |

**Evolução funcional e novas telas:** somente em `src/Web.Blazor` a partir de B4.

### Exceções ao freeze

Exigem no PR:

1. Label ou seção **“Exceção freeze React”**
2. Justificativa (P0/P1, produção, prazo)
3. Confirmação de que a mesma correção **não** será reimplementada no Blazor (ou link para issue Blazor equivalente)

### Fim da convivência

- **B10:** cutover — Blazor vira frontend servido em produção.
- **B11:** remoção de `src/Web` (React).

---

## 7. Checklist de PR (migração Blazor)

Usar ao abrir PR que toca frontend:

- [ ] Rota e perfis (`Admin` / `Supervisor`) espelham React?
- [ ] DTOs e endpoints alinhados à API (sem regra de negócio no client)?
- [ ] CSS scoped portado do `.module.css` equivalente?
- [ ] Loading, empty state e erros HTTP tratados?
- [ ] Testes bUnit nas partes críticas (quando aplicável — B2+)?
- [ ] PR impacta React? Se sim, está dentro da política de freeze (seção 6)?
- [ ] Referência Stitch citada quando for tela administrativa nova?

---

## 8. Referências

- [roadmap-blazor-migration.md](../roadmap-blazor-migration.md) — trilha B0–B11
- [ADR 001](adr/001-blazor-wasm-frontend.md) — WASM Standalone, portas, ApiBaseUrl
- [src/Web.Blazor/README.md](../src/Web.Blazor/README.md) — spike e execução local
- [agents.md](../agents.md) — governança geral e transição React/Blazor
- React atual: `src/Web/src/features/`, `src/Web/src/shared/`, `src/Web/src/app/routes.tsx`

---

## Histórico

| Data | Versão | Notas |
|---|---|---|
| 2026-06-22 | 1.0 | Convenções B0.3 — estrutura, nomenclatura, CSS, freeze React |
