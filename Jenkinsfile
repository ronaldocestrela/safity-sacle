/*
 * SafetyScale — deploy local com Docker Compose (servidor onde o Jenkins roda).
 *
 * Crie estas credenciais no Jenkins (tipo "Secret text" ou equivalente) com estes IDs:
 *
 *   safetyscale-mssql-sa-password  → senha forte do SQL Server (usuário sa)
 *   safetyscale-sqlserver-port     → porta TCP do SQL Server exposta no host (ex.: 1433 → 1433 no contêiner)
 *   safetyscale-jwt-key            → chave JWT (≥ 32 caracteres recomendado)
 *   safetyscale-jwt-issuer         → Jwt:Issuer (ex.: SafetyScale)
 *   safetyscale-jwt-audience       → Jwt:Audience (ex.: SafetyScale.Api)
 *   safetyscale-db-name            → nome lógico do banco (ex.: SafetyScale)
 *   safetyscale-api-port           → porta da API exposta no host (ex.: 8081 → 8080 no contêiner)
 *   safetyscale-web-port           → porta HTTP publicada pelo Nginx do front (ex.: 80)
 *   safetyscale-cors-origins       → origens CORS da API separadas por vírgula (pode ser vazio = não usa CORS)
 *   safetyscale-vite-api-base-url → URL absoluta da API na build da SPA Vite (vazio = /api pelo Nginx)
 *
 * Opcionalmente ajuste JWT_EXPIRY_MINUTES e MSSQL_PID no estágio Prepare Env se precisar
 * diferente dos padrões (120 e Developer).
 */

pipeline {
  agent any

  options {
    timestamps()
    // Evita dois deploys concorrentes no mesmo workspace/servidor
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
          string(credentialsId: 'safetyscale-vite-api-base-url', variable: 'CRED_VITE_API_BASE_URL'),
        ]) {
          // writeFile via Groovy evita erro de shell "Unterminated quoted string" quando
          // algum secret contém aspas, `$`, etc. (`printf "...${CRED}..."` no sh quebrava.)
          script {
            def normalizeOptionalCred = { raw ->
              if (raw == null) return ''
              String s = raw.toString().trim()
              if (s.isEmpty() || s == '-') return ''
              return s
            }
            def cors = normalizeOptionalCred(env.CRED_CORS_ORIGINS)
            def vite = normalizeOptionalCred(env.CRED_VITE_API_BASE_URL)
            def content =
              ('MSSQL_SA_PASSWORD=' + (env.CRED_MSSQL_SA_PASSWORD ?: '') + '\n'
                + 'SQLSERVER_PORT=' + (env.CRED_SQLSERVER_PORT ?: '') + '\n'
                + 'SAFETYSCALE_DB_NAME=' + (env.CRED_DB_NAME ?: '') + '\n'
                + 'JWT_ISSUER=' + (env.CRED_JWT_ISSUER ?: '') + '\n'
                + 'JWT_AUDIENCE=' + (env.CRED_JWT_AUDIENCE ?: '') + '\n'
                + 'JWT_KEY=' + (env.CRED_JWT_KEY ?: '') + '\n'
                + 'JWT_EXPIRY_MINUTES=120\n'
                + 'API_PORT=' + (env.CRED_API_PORT ?: '') + '\n'
                + 'WEB_PORT=' + (env.CRED_WEB_PORT ?: '') + '\n'
                + 'CORS_ORIGINS=' + cors + '\n'
                + 'VITE_API_BASE_URL=' + vite + '\n')
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
          set -a
          # shellcheck disable=SC1091
          . ./.env
          set +a
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
            echo "ERROR: Health check falhou após tentativas."
            exit 1
          fi
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
