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

Espelhar o React arquivado em [`archive/legacy-react-web/src/app/routes.tsx`](../archive/legacy-react-web/src/app/routes.tsx):

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
- Manter DTOs no projeto Blazor — **não** referenciar `Application`/`Domain`/`Api` do backend no WASM.
- **B1.1:** não criar projeto `SafetyScale.Contracts` compartilhado; espelhar [`src/Api/Contracts/`](../src/Api/Contracts/) em `Models/` conforme cada módulo for portado (B2+).

---

## 4. Estilo e identidade visual

### Regra geral: scoped CSS 1:1 com React

1. Cada `.razor` de página ou componente tem par `.razor.css` (isolated CSS do Blazor).
2. Ao portar uma tela React, copiar valores de `*.module.css` para o `.razor.css` correspondente.
3. Estilos globais (reset, `body`, `.material-symbols-outlined`) ficam em `wwwroot/css/app.css` — paridade com [`archive/legacy-react-web/src/index.css`](../archive/legacy-react-web/src/index.css).
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
| Porta dev Blazor | **4864** |

Detalhes: [ADR 001](adr/001-blazor-wasm-frontend.md).

---

## 6. React arquivado (pós-B11)

- Frontend **oficial e único:** `src/Web.Blazor` (Blazor WASM).
- React legado: [`archive/legacy-react-web/`](../archive/legacy-react-web/) — **somente referência histórica** de paridade; não buildar nem deployar.
- **Não** renomear `Web.Blazor` → `Web` nesta fase (paths estáveis em compose, CI e scripts).

### Checklist de PR (frontend Blazor)

- [ ] Rota e perfis (`Admin` / `Supervisor`) corretos?
- [ ] DTOs e endpoints alinhados à API?
- [ ] CSS scoped portado do legado React quando aplicável?
- [ ] Loading, empty state e erros HTTP tratados?
- [ ] Testes bUnit nas partes críticas?
- [ ] Referência Stitch citada quando for tela administrativa nova?

---

## 7. Referências

- [roadmap-blazor-migration.md](../roadmap-blazor-migration.md) — trilha B0–B11
- [ADR 001](adr/001-blazor-wasm-frontend.md) — WASM Standalone, portas, ApiBaseUrl
- [src/Web.Blazor/README.md](../src/Web.Blazor/README.md) — spike e execução local
- [agents.md](../agents.md) — governança geral e transição React/Blazor
- React legado arquivado: [`archive/legacy-react-web/`](../archive/legacy-react-web/)

---

## Histórico

| Data | Versão | Notas |
|---|---|---|
| 2026-06-22 | 1.0 | Convenções B0.3 — estrutura, nomenclatura, CSS, freeze React |
