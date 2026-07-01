/*
 * SafetyScale — deploy local com Docker Compose (servidor onde o Jenkins roda).
 *
 * Credenciais Jenkins (tipo "Secret text"):
 *
 *   safetyscale-mssql-sa-password  → senha forte do SQL Server (usuário sa)
 *   safetyscale-sqlserver-port     → porta TCP do SQL Server exposta no host
 *   safetyscale-jwt-key            → chave JWT (≥ 32 caracteres recomendado)
 *   safetyscale-jwt-issuer         → Jwt:Issuer (ex.: SafetyScale)
 *   safetyscale-jwt-audience       → Jwt:Audience (ex.: SafetyScale.Api)
 *   safetyscale-db-name            → nome lógico do banco (ex.: SafetyScale)
 *   safetyscale-api-port           → porta da API exposta no host
 *   safetyscale-web-port           → porta HTTP publicada pelo Nginx do front
 *   safetyscale-cors-origins       → origens CORS da API (CSV; vazio = same-origin)
 *   safetyscale-api-base-url       → URL absoluta da API no build Blazor (vazio = /api via Nginx)
 *   safetyscale-public-web-base-url → URL pública do front (links em e-mail)
 *   safetyscale-smtp-host            → host SMTP (vazio ou `-` = desabilita envio real)
 *   safetyscale-smtp-port            → porta SMTP (padrão 587)
 *   safetyscale-smtp-username        → usuário SMTP (vazio ou `-` se não aplicável)
 *   safetyscale-smtp-password        → senha ou app password SMTP
 *   safetyscale-smtp-from-address    → endereço remetente (From)
 *   safetyscale-smtp-from-display-name → nome exibido do remetente (padrão SafetyScale)
 *   safetyscale-smtp-enable-ssl      → true/false (padrão true)
 *   safetyscale-bootstrap-user-email → e-mail bootstrap do Platform Admin
 *   safetyscale-bootstrap-user-password → senha bootstrap do Platform Admin
 *
 * Opcional: JWT_EXPIRY_MINUTES e MSSQL_PID no estágio Prepare Env.
 */

pipeline {
  agent any

  options {
    timestamps()
    disableConcurrentBuilds(abortPrevious: false)
  }

  environment {
    COMPOSE_FILE = 'docker-compose.prod.yml'
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
      }
    }

    stage('Validate Docker') {
      steps {
        sh '''
          set -eu
          command -v docker
          docker version
          docker compose version
        '''
      }
    }

    stage('Backend Tests') {
      steps {
        sh '''
          set -eu
          dotnet --version
          dotnet restore SafetyScale.sln
          dotnet build SafetyScale.sln --configuration Release --no-restore
          dotnet test src/Tests/SafetyScale.Tests.csproj --configuration Release --no-build
        '''
      }
    }

    stage('Prepare Env') {
      steps {
        withCredentials([
          string(credentialsId: 'safetyscale-mssql-sa-password', variable: 'CRED_MSSQL_SA_PASSWORD'),
          string(credentialsId: 'safetyscale-sqlserver-port', variable: 'CRED_SQLSERVER_PORT'),
          string(credentialsId: 'safetyscale-jwt-key', variable: 'CRED_JWT_KEY'),
          string(credentialsId: 'safetyscale-jwt-issuer', variable: 'CRED_JWT_ISSUER'),
          string(credentialsId: 'safetyscale-jwt-audience', variable: 'CRED_JWT_AUDIENCE'),
          string(credentialsId: 'safetyscale-db-name', variable: 'CRED_DB_NAME'),
          string(credentialsId: 'safetyscale-api-port', variable: 'CRED_API_PORT'),
          string(credentialsId: 'safetyscale-web-port', variable: 'CRED_WEB_PORT'),
          string(credentialsId: 'safetyscale-cors-origins', variable: 'CRED_CORS_ORIGINS'),
          string(credentialsId: 'safetyscale-api-base-url', variable: 'CRED_API_BASE_URL'),
          string(credentialsId: 'safetyscale-public-web-base-url', variable: 'CRED_PUBLIC_WEB_BASE_URL'),
          string(credentialsId: 'safetyscale-smtp-host', variable: 'CRED_SMTP_HOST'),
          string(credentialsId: 'safetyscale-smtp-port', variable: 'CRED_SMTP_PORT'),
          string(credentialsId: 'safetyscale-smtp-username', variable: 'CRED_SMTP_USERNAME'),
          string(credentialsId: 'safetyscale-smtp-password', variable: 'CRED_SMTP_PASSWORD'),
          string(credentialsId: 'safetyscale-smtp-from-address', variable: 'CRED_SMTP_FROM_ADDRESS'),
          string(credentialsId: 'safetyscale-smtp-from-display-name', variable: 'CRED_SMTP_FROM_DISPLAY_NAME'),
          string(credentialsId: 'safetyscale-smtp-enable-ssl', variable: 'CRED_SMTP_ENABLE_SSL'),
          string(credentialsId: 'safetyscale-bootstrap-user-email', variable: 'CRED_BOOTSTRAP_USER_EMAIL'),
          string(credentialsId: 'safetyscale-bootstrap-user-password', variable: 'CRED_BOOTSTRAP_USER_PASSWORD'),
        ]) {
          script {
            def normalizeOptionalCred = { raw ->
              if (raw == null) return ''
              String s = raw.toString().trim()
              if (s.isEmpty() || s == '-') return ''
              return s
            }
            def cors = normalizeOptionalCred(env.CRED_CORS_ORIGINS)
            def apiBase = normalizeOptionalCred(env.CRED_API_BASE_URL)
            def smtpHost = normalizeOptionalCred(env.CRED_SMTP_HOST)
            def smtpPortRaw = (env.CRED_SMTP_PORT ?: '587').toString().trim()
            def smtpPort = (smtpPortRaw.isEmpty() || smtpPortRaw == '-') ? '587' : smtpPortRaw
            def smtpUsername = normalizeOptionalCred(env.CRED_SMTP_USERNAME)
            def smtpFromAddress = normalizeOptionalCred(env.CRED_SMTP_FROM_ADDRESS)
            def smtpFromDisplayName = normalizeOptionalCred(env.CRED_SMTP_FROM_DISPLAY_NAME)
            if (smtpFromDisplayName.isEmpty()) {
              smtpFromDisplayName = 'SafetyScale'
            }
            def smtpEnableSslRaw = normalizeOptionalCred(env.CRED_SMTP_ENABLE_SSL)
            def smtpEnableSsl = smtpEnableSslRaw.isEmpty() ? 'true' : smtpEnableSslRaw
            def content =
              'MSSQL_SA_PASSWORD=' + (env.CRED_MSSQL_SA_PASSWORD ?: '') + '\n' +
              'SQLSERVER_PORT=' + (env.CRED_SQLSERVER_PORT ?: '') + '\n' +
              'SAFETYSCALE_DB_NAME=' + (env.CRED_DB_NAME ?: '') + '\n' +
              'JWT_ISSUER=' + (env.CRED_JWT_ISSUER ?: '') + '\n' +
              'JWT_AUDIENCE=' + (env.CRED_JWT_AUDIENCE ?: '') + '\n' +
              'JWT_KEY=' + (env.CRED_JWT_KEY ?: '') + '\n' +
              'JWT_EXPIRY_MINUTES=120\n' +
              'API_PORT=' + (env.CRED_API_PORT ?: '') + '\n' +
              'WEB_PORT=' + (env.CRED_WEB_PORT ?: '') + '\n' +
              'CORS_ORIGINS=' + cors + '\n' +
              'API_BASE_URL=' + apiBase + '\n' +
              'MSSQL_PID=Developer\n' +
              'PUBLIC_WEB_BASE_URL=' + (env.CRED_PUBLIC_WEB_BASE_URL ?: '') + '\n' +
              'SMTP_HOST=' + smtpHost + '\n' +
              'SMTP_PORT=' + smtpPort + '\n' +
              'SMTP_USERNAME=' + smtpUsername + '\n' +
              'SMTP_PASSWORD=' + (env.CRED_SMTP_PASSWORD ?: '') + '\n' +
              'SMTP_FROM_ADDRESS=' + smtpFromAddress + '\n' +
              'SMTP_FROM_DISPLAY_NAME=' + smtpFromDisplayName + '\n' +
              'SMTP_ENABLE_SSL=' + smtpEnableSsl + '\n' +
              'EMAIL_QUEUE_ENABLED=true\n' +
              'EMAIL_QUEUE_POLL_INTERVAL_SECONDS=5\n' +
              'EMAIL_QUEUE_BATCH_SIZE=10\n' +
              'EMAIL_QUEUE_MAX_ATTEMPTS=5\n' +
              'EMAIL_QUEUE_INITIAL_RETRY_DELAY_SECONDS=30\n' +
              'EMAIL_QUEUE_MAX_RETRY_DELAY_SECONDS=3600\n' +
              'EMAIL_QUEUE_STALE_PROCESSING_MINUTES=10\n' +
              'BOOTSTRAP_USER_EMAIL=' + (env.CRED_BOOTSTRAP_USER_EMAIL ?: '') + '\n' +
              'BOOTSTRAP_USER_PASSWORD=' + (env.CRED_BOOTSTRAP_USER_PASSWORD ?: '') + '\n' +
              'BOOTSTRAP_USER_DISPLAY_NAME=Platform Admin\n' +
              'BOOTSTRAP_USER_ROLE=PlatformOwner\n'
            writeFile file: '.env', text: content, encoding: 'UTF-8'
            sh 'chmod 600 .env'
          }
        }
      }
    }

    stage('Deploy') {
      steps {
        sh '''
          set -eu
          docker compose -f "${COMPOSE_FILE}" up -d --build --remove-orphans
        '''
      }
    }

    stage('Verify') {
      steps {
        sh '''
          set -eu
          docker compose -f "${COMPOSE_FILE}" ps
          WEB_PORT="$(awk -F= '/^WEB_PORT=/{print $2; exit}' ./.env)"
          if [ -z "${WEB_PORT:-}" ]; then
            echo "ERROR: WEB_PORT nao encontrado no arquivo .env"
            exit 1
          fi
          ok=0
          i=0
          while [ "$i" -lt 30 ]; do
            code="$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:${WEB_PORT}/api/health" || true)"
            if [ "$code" = "401" ] || [ "$code" = "200" ]; then
              echo "Health check OK (HTTP ${code})"
              ok=1
              break
            fi
            echo "Aguardando API (HTTP ${code:-000})..."
            sleep 5
            i=$((i + 1))
          done
          if [ "$ok" -ne 1 ]; then
            echo "ERROR: Health check falhou apos tentativas."
            exit 1
          fi
          chmod +x scripts/verify-blazor-deploy.sh
          BLAZOR_VERIFY_BASE_URL="http://127.0.0.1:${WEB_PORT}" ./scripts/verify-blazor-deploy.sh
        '''
      }
    }
  }

  post {
    always {
      sh 'rm -f .env 2>/dev/null || true'
    }
  }
}
