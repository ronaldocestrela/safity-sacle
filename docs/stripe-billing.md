# Stripe Billing — SafetyScale

Guia operacional para assinaturas recorrentes via **Stripe Checkout** + **Customer Portal**.

## Visão geral

- **Modelo:** assinatura mensal por plano fixo (`PlatformPlan`).
- **Checkout:** Stripe Checkout hospedado (`mode: subscription`).
- **Self-service:** Stripe Customer Portal (upgrade, cancelamento, cartão).
- **Sincronização:** webhooks Stripe atualizam `Tenant.BillingStatus`, plano e `LeadStatus`.

## Configuração

### 1. Stripe Dashboard

1. Crie **Products** e **Prices** recorrentes (mensal) no [Stripe Dashboard](https://dashboard.stripe.com/products).
2. Ative métodos de pagamento em **Settings → Payment methods** (não hardcode `payment_method_types` no código).
3. Configure o **Customer Portal** em **Settings → Billing → Customer portal**.
4. Crie um **Webhook endpoint** apontando para:
   - Local (CLI): `stripe listen --latest --forward-to http://localhost:5003/api/stripe/webhook`
   - Produção: `https://<seu-dominio>/api/stripe/webhook`

   Use API version **`2026-06-24.dahlia`** (deve coincidir com `Stripe:ApiVersion` da aplicação).

Eventos mínimos:

- `checkout.session.completed`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_failed`
- `invoice.paid`

### 2. Variáveis de ambiente

Copie de [`.env.example`](../.env.example):

| Variável | Descrição |
|----------|-----------|
| `STRIPE_SECRET_KEY` | Chave secreta ou RAK (`sk_` / `rk_`) — **somente backend** |
| `STRIPE_WEBHOOK_SECRET` | Segredo do endpoint webhook (`whsec_`) |
| `STRIPE_API_VERSION` | Versão da API Stripe (`2026-06-24.dahlia`) |
| `PUBLIC_WEB_BASE_URL` | URL pública do Blazor (success/cancel/portal return) |

No Compose, mapeadas para `Stripe__SecretKey`, `Stripe__WebhookSecret` e `Stripe__ApiVersion`.

> **Segurança:** prefira [Restricted API Keys (RAK)](https://docs.stripe.com/keys/restricted-api-keys) com permissões mínimas. Nunca commite chaves. Rotacione se expostas.

### 3. Vincular planos internos ao Stripe

No portal da plataforma (`/platform/plans`), use **Stripe** em cada plano e informe:

- `Stripe Price ID` (`price_...`) — obrigatório
- `Stripe Product ID` (`prod_...`) — opcional

Sem `price_...`, o botão **Assinar plano** fica indisponível para tenants.

## Fluxo do tenant (Admin)

1. Login como **Admin** do tenant.
2. Acesse **Assinatura** (`/app/billing`).
3. Escolha um plano → redireciona ao Stripe Checkout.
4. Após pagamento, webhook atualiza status (pode levar alguns segundos).
5. **Gerenciar assinatura** abre o Customer Portal.

## Endpoints da API

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/api/billing/plans` | Admin | Planos ativos |
| GET | `/api/billing/status` | Admin | Status da assinatura do tenant |
| POST | `/api/billing/checkout-session` | Admin | Inicia Checkout |
| POST | `/api/billing/portal-session` | Admin | Abre Customer Portal |
| POST | `/api/stripe/webhook` | Anônimo (assinatura Stripe) | Webhooks |
| PATCH | `/api/platform/plans/{id}/stripe` | PlatformOwner/Admin | Vincula price/product |

## Desenvolvimento local

```bash
# Terminal 1 — API
dotnet run --project src/Api/SafetyScale.Api.csproj

# Terminal 2 — Stripe CLI (encaminha webhooks na versão mais recente / dahlia)
stripe listen --latest \
  --forward-to http://localhost:5003/api/stripe/webhook \
  --events checkout.session.completed,customer.subscription.updated,customer.subscription.deleted,invoice.payment_failed,invoice.paid

# Terminal 3 — Blazor
dotnet run --project src/Web.Blazor/SafetyScale.Web.Blazor.csproj
```

Defina em `src/Api/appsettings.Development.json` ou user-secrets:

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "WebhookSecret": "whsec_...",
  "ApiVersion": "2026-06-24.dahlia"
}
```

> Copie o `whsec_...` exibido pelo `stripe listen` ativo. Se reiniciar o listener, atualize o secret e reinicie a API.

Use cartões de teste Stripe (`4242 4242 4242 4242`).

## Checklist go-live

- [ ] Chaves **live** separadas das de teste
- [ ] RAK ou secret key com IP allowlist na infra de produção
- [ ] Webhook de produção registrado e `whsec_` configurado
- [ ] `PUBLIC_WEB_BASE_URL` com HTTPS correto
- [ ] Customer Portal configurado (planos permitidos, cancelamento)
- [ ] Planos internos vinculados a `price_...` live
- [ ] Teste E2E: signup → assinatura → webhook → acesso ativo
- [ ] Monitorar falhas de pagamento (`invoice.payment_failed`)
- [ ] 2FA no Stripe Dashboard (passkey/authenticator, não SMS)

## Modelo de dados

**PlatformPlan:** `StripeProductId`, `StripePriceId`

**Tenant:** `StripeCustomerId`, `StripeSubscriptionId`, `BillingStatus`, `CurrentPeriodEnd`

**StripeWebhookEvents:** idempotência por `StripeEventId`

## Troubleshooting

| Sintoma | Causa provável |
|---------|----------------|
| 503 em checkout | `Stripe:SecretKey` vazio |
| 400 webhook | `WebhookSecret` incorreto, body alterado por proxy, ou **API version mismatch** (`ReceivedApiVersion` nos logs) |
| publishable key error | `SecretKey` configurada como `pk_...`; use `sk_...` ou `rk_...` |
| Plano indisponível | Plano sem `StripePriceId` |
| Status não atualiza | Webhook não entregue ou evento não tratado — ver logs da API |
