# Deploy com Jenkins (Docker Compose local)

Este guia descreve o fluxo definido pelo [`Jenkinsfile`](Jenkinsfile) para **deploy no mesmo servidor onde o Jenkins roda**, usando [`docker-compose.prod.yml`](docker-compose.prod.yml). O arquivo `.env` de produção **não vai para o Git**: o pipeline monta esse arquivo em tempo de execução com credenciais guardadas no Jenkins.

## Premissas

- **Jenkins executa no servidor de destino** (no mesmo host em que o Docker Compose deve subir os contêineres).
- **Agent** com:
  - **Docker** + **Docker Compose plugin** (`docker compose`).
  - **.NET SDK da família principal 10** (o pipeline verifica `dotnet --version`; deve começar com `10.`).
  - **Node.js** e **npm** compatíveis com [`src/Web/package.json`](src/Web/package.json) (`engines`, `npm ci`).
  - **curl** (health check HTTP).
  - Permissão do usuário do Jenkins para usar o Docker conforme política da equipe (por exemplo inclusão em grupo `docker` ou agent específico).
- Testes do backend (`dotnet test`) usam **Testcontainers**: durante o estágio **Backend Tests** é necessário Docker funcional para subir SQL Server efêmero.

## Visão geral do fluxo

1. Obtém o código do repositório.
2. Valida ferramentas no agent.
3. Executa testes e build backend e frontend **antes** de gerar `.env`/Compose neste mesmo job — portanto esse agent precisa tanto de SDK/Node para build quanto de Docker quando os testes de integração rodam Testcontainers (e depois uso normal do Docker no deploy).
4. Injeta valores secretos vindos do Jenkins num arquivo `.env` na raiz (permissões restritas).
5. Sobe/atualiza a stack com Compose (`build` + `up -d`).
6. Verifica `/api/health` via Nginx/host na porta configurada.
7. No **`post`**, remove o `.env` do workspace (sucesso ou falha).

Para semântica das variáveis de aplicação, veja também [`.env.example`](.env.example).

---

## Estágios do pipeline (explicação)

### 1. `Checkout`

- Executa **`checkout scm`**. O job deve ser **Pipeline from SCM** (Multibranch ou pipeline apontando para o mesmo repositório).
- Todo o trabalho ocorre na **raiz do clone** (`Jenkinsfile`, `docker-compose.prod.yml`, etc.).

### 2. `Validate Tools`

- Verifica comandos **`docker`**, **`docker compose`**, **`curl`**, **`dotnet`**, **`node`**, **`npm`**.
- Falha se a versão do SDK **não** começar com `10.` (evita erro confuso nos testes ou no publish).

### 3. `Backend Tests`

- `dotnet restore`, `dotnet build`, `dotnet test` em [`src/Tests/SafetyScale.Tests.csproj`](src/Tests/SafetyScale.Tests.csproj).
- Qualquer falha **interrompe o pipeline** antes de **Prepare Env** / **Deploy**.

### 4. `Frontend Tests`

- Em [`src/Web`](src/Web): `npm ci`, `npm run lint`, `npm run test`, `npm run build`.
- Valida código e artefatos da SPA antes de reconstruir a imagem **`web`** no Compose.

### 5. `Prepare Env`

- Usa **`withCredentials`** + bindings **`string`** (credenciais abaixo). Cada binding expõe variáveis `CRED_*` **apenas dentro** dos passos envelopados pelo `withCredentials`.
- Escreve **`./.env`** na raiz com as chaves usadas pelo Compose (ver tabela mais abaixo).
- **`umask 077`** e **`chmod 600 .env`** restringem leituras locais óbvias por outros usuários do SO (além das permissões já normais da pasta workspace).
- O conteúdo do `.env` **não** deve ser ecoado nos logs pelos comandos utilizados atualmente (`printf ... > .env`).
- **`JWT_EXPIRY_MINUTES`** é fixado em **120** no `Jenkinsfile` (alinhado ao padrão de [`.env.example`](.env.example)). **`MSSQL_PID`** usa o padrão do compose (**Developer**) a menos que você estenda manualmente esse estágio no `Jenkinsfile`.

### 6. `Deploy`

```bash
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
```

- O Compose interpola **`${...}`** a partir do `.env` na mesma pasta.
- O **`sqlserver`** publica **`SQLSERVER_PORT`** no host (**`${SQLSERVER_PORT:-1433}:1433`**). A **`api`** continua usando o hostname **`sqlserver`** e a porta **`1433`** dentro da rede Compose.
- O **`api`** publica **`API_PORT`** no host (**`${API_PORT:-8081}:8080`**). O **`web`** encaminha `/api/*` para **`api:8080`** na rede interna.
- O SQL Server mantém dados no volume **`sqlserver-data`**; este pipeline **não** inclui comandos para apagar esse volume.

### 7. `Verify`

- `docker compose ps` para inspeção rápida.
- Obtém **`WEB_PORT`** carregando o `.env` e chama **`http://127.0.0.1:${WEB_PORT}/api/health`**.
  - **`401`** (não autorizado sem token) ou **`200`** contam como êxito, conforme a API atual.
- Repetições com intervalo até a stack responder (várias tentativas; ordem grande de alguns minutos no total).

### 8. `post { always }` — cleanup

- **`rm -f .env`** no workspace quando o build termina, para não reter aquele arquivo com segredos no diretório de trabalho do job.

---

## Credenciais que devem existir no Jenkins

O `Jenkinsfile` espera **`credentialsId`** iguais aos listados (altere apenas se também alterar os IDs no próprio Jenkinsfile).

Tipo usual: credencial compatível com o step **`withCredentials`** + **`string(...)`**, em Jenkins clássico normalmente como **Secret text** (valor armazenado cifrado pelo Jenkins).

| ID da credencial no Jenkins | Variável injetada no job | Chave gravada no `.env` | Uso |
|-------------------------------|---------------------------|-------------------------|-----|
| `safetyscale-mssql-sa-password` | `CRED_MSSQL_SA_PASSWORD` | `MSSQL_SA_PASSWORD` | Senha **`sa`** do SQL Server em contêiner; mesma senha interpolada na string de conexão da **`api`** no [`docker-compose.prod.yml`](docker-compose.prod.yml). |
| `safetyscale-sqlserver-port` | `CRED_SQLSERVER_PORT` | `SQLSERVER_PORT` | Porta host mapeada para o SQL Server (**ex.: `1433`** → contêiner **`1433`**). |
| `safetyscale-jwt-key` | `CRED_JWT_KEY` | `JWT_KEY` | Chave secreta JWT da API (**produção: longa e aleatória**). |
| `safetyscale-jwt-issuer` | `CRED_JWT_ISSUER` | `JWT_ISSUER` | **`Jwt__Issuer`** (ambiente Compose → API). |
| `safetyscale-jwt-audience` | `CRED_JWT_AUDIENCE` | `JWT_AUDIENCE` | **`Jwt__Audience`**. |
| `safetyscale-db-name` | `CRED_DB_NAME` | `SAFETYSCALE_DB_NAME` | Nome do banco na connection string (`Database=`). |
| `safetyscale-api-port` | `CRED_API_PORT` | `API_PORT` | Porta host mapeada para a API (**ex.: `8081`** → contêiner `8080`; ver [`docker-compose.prod.yml`](docker-compose.prod.yml)). |
| `safetyscale-web-port` | `CRED_WEB_PORT` | `WEB_PORT` | Porta host do Nginx (**ex.: `80`**); usada também no verificação de saúde. |

**Variáveis no `.env` sem credencial própria no Jenkins (atualmente)**

- **`JWT_EXPIRY_MINUTES`** — sempre `120` no script estágio Prepare Env.

**Opcional ao customizar `Jenkinsfile`**

- **`MSSQL_PID`** — SKU do SQL Server dentro do Compose; padrão **Developer** se omitido (`${MSSQL_PID:-Developer}` no compose).

### Como registrar as credenciais (resumo Jenkins)

No Jenkins típico: **Manage Jenkins** → **Credentials** → dominio onde o pipeline roda (ex.: sistema ou pasta) → **Add Credentials**:

- Escolha o tipo compatível com “Secret”.
- Informe cada **Secret** usando exatamente o **ID** da primeira coluna da tabela (ex.: marque manualmente ou use esse ID ao criar a credencial, conforme disponível na sua instalação).
- O job só precisa de permissão **`Credentials → Use credential`** para essas entradas (geralmente o mesmo usuário/agent que já roda SCM).

### Boas práticas rápidas

- **Roção de segredos:** rotacione **`safetyscale-mssql-sa-password`** apenas com planejamento — o mesmo valor está no SQL em volume persistente até recriação do volume/administração dentro do servidor.
- **Auditoria:** restrinja jobs e agents que conseguem ver essas credenciais ao mínimo necessário.

---

## Referências rápidas

| Peça | Ficheiro |
|------|-----------|
| Pipeline | [`Jenkinsfile`](Jenkinsfile) |
| Stack produção | [`docker-compose.prod.yml`](docker-compose.prod.yml) |
| Exemplo de variáveis | [`.env.example`](.env.example) |
