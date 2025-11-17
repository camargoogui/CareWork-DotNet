#!/bin/bash

# 🧪 Script de Teste Completo de Todos os Endpoints
# Testa todos os endpoints na ordem correta, validando lógicas

BASE_URL="http://localhost:8080/api/v1"
EMAIL="teste_$(date +%s)@example.com"
PASSWORD="senha123"
NAME="Usuário Teste"

echo "🚀 Iniciando testes completos da API CareWork"
echo "=============================================="
echo ""

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Variáveis globais
TOKEN=""
USER_ID=""
CHECKIN_IDS=()
TIP_IDS=()

# Função para fazer requisições
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    local auth=$4
    
    if [ "$auth" = "true" ] && [ -n "$TOKEN" ]; then
        if [ -n "$data" ]; then
            curl -s -X "$method" \
                -H "Content-Type: application/json" \
                -H "Authorization: Bearer $TOKEN" \
                -d "$data" \
                "$BASE_URL$endpoint"
        else
            curl -s -X "$method" \
                -H "Authorization: Bearer $TOKEN" \
                "$BASE_URL$endpoint"
        fi
    else
        if [ -n "$data" ]; then
            curl -s -X "$method" \
                -H "Content-Type: application/json" \
                -d "$data" \
                "$BASE_URL$endpoint"
        else
            curl -s -X "$method" \
                "$BASE_URL$endpoint"
        fi
    fi
}

# Função para verificar resposta
check_response() {
    local response=$1
    local expected_field=$2
    
    if echo "$response" | grep -q "$expected_field"; then
        echo -e "${GREEN}✅ Sucesso${NC}"
        return 0
    else
        echo -e "${RED}❌ Falhou${NC}"
        echo "Resposta: $response"
        return 1
    fi
}

echo "📝 PASSO 1: Autenticação"
echo "------------------------"

# 1.1 Registrar usuário
echo -n "1.1 Registrando usuário... "
REGISTER_RESPONSE=$(make_request "POST" "/auth/register" "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"name\":\"$NAME\"}" false)

if echo "$REGISTER_RESPONSE" | grep -q "token"; then
    echo -e "${GREEN}✅ Sucesso${NC}"
    TOKEN=$(echo "$REGISTER_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
    USER_ID=$(echo "$REGISTER_RESPONSE" | grep -o '"userId":"[^"]*' | cut -d'"' -f4)
    echo "   Token obtido: ${TOKEN:0:20}..."
    echo "   User ID: $USER_ID"
else
    echo -e "${YELLOW}⚠️ Usuário pode já existir, tentando login...${NC}"
    # Tentar login
    LOGIN_RESPONSE=$(make_request "POST" "/auth/login" "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" false)
    if echo "$LOGIN_RESPONSE" | grep -q "token"; then
        TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
        USER_ID=$(echo "$LOGIN_RESPONSE" | grep -o '"userId":"[^"]*' | cut -d'"' -f4)
        echo -e "${GREEN}✅ Login bem-sucedido${NC}"
    else
        echo -e "${RED}❌ Falha no registro e login${NC}"
        exit 1
    fi
fi

# 1.2 Fazer login (verificar)
echo -n "1.2 Fazendo login... "
LOGIN_RESPONSE=$(make_request "POST" "/auth/login" "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" false)
if check_response "$LOGIN_RESPONSE" "token"; then
    TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
fi

echo ""
echo "📊 PASSO 2: Criar Check-ins"
echo "---------------------------"

# Criar 7 check-ins variados para testes
echo "Criando 7 check-ins com dados variados..."

for i in {1..7}; do
    MOOD=$((RANDOM % 5 + 1))
    STRESS=$((RANDOM % 5 + 1))
    SLEEP=$((RANDOM % 5 + 1))
    
    CHECKIN_DATA="{\"mood\":$MOOD,\"stress\":$STRESS,\"sleep\":$SLEEP,\"notes\":\"Check-in teste $i\",\"tags\":[\"teste\",\"dia$i\"]}"
    
    echo -n "   Check-in $i (mood:$MOOD, stress:$STRESS, sleep:$SLEEP)... "
    RESPONSE=$(make_request "POST" "/checkins" "$CHECKIN_DATA" true)
    
    if echo "$RESPONSE" | grep -q "\"id\""; then
        echo -e "${GREEN}✅${NC}"
        CHECKIN_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*' | cut -d'"' -f4 | head -1)
        CHECKIN_IDS+=("$CHECKIN_ID")
    else
        echo -e "${RED}❌${NC}"
    fi
    
    sleep 0.5
done

echo ""
echo "📋 PASSO 3: Testar Check-ins"
echo "----------------------------"

# 3.1 Listar check-ins
echo -n "3.1 Listando check-ins (página 1)... "
RESPONSE=$(make_request "GET" "/checkins?page=1&pageSize=10" "" true)
if check_response "$RESPONSE" "data"; then
    COUNT=$(echo "$RESPONSE" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
    echo "   Total: $COUNT check-ins"
fi

# 3.2 Buscar check-in específico
if [ ${#CHECKIN_IDS[@]} -gt 0 ]; then
    echo -n "3.2 Buscando check-in ${CHECKIN_IDS[0]}... "
    RESPONSE=$(make_request "GET" "/checkins/${CHECKIN_IDS[0]}" "" true)
    check_response "$RESPONSE" "id"
fi

# 3.3 Atualizar check-in
if [ ${#CHECKIN_IDS[@]} -gt 0 ]; then
    echo -n "3.3 Atualizando check-in ${CHECKIN_IDS[0]}... "
    UPDATE_DATA="{\"mood\":5,\"stress\":1,\"sleep\":5,\"notes\":\"Atualizado\"}"
    RESPONSE=$(make_request "PUT" "/checkins/${CHECKIN_IDS[0]}" "$UPDATE_DATA" true)
    check_response "$RESPONSE" "updatedAt"
fi

echo ""
echo "💡 PASSO 4: Testar Tips"
echo "----------------------"

# 4.1 Listar tips (requer autenticação)
echo -n "4.1 Listando tips... "
RESPONSE=$(make_request "GET" "/tips?page=1&pageSize=10" "" true)

# Verificar se a resposta não está vazia e contém dados
if [ -n "$RESPONSE" ] && (echo "$RESPONSE" | grep -q "success\|data\|totalCount" || echo "$RESPONSE" | grep -q "\"id\""); then
    echo -e "${GREEN}✅ Sucesso${NC}"
    COUNT=$(echo "$RESPONSE" | grep -o '"totalCount":[0-9]*' | cut -d':' -f2)
    if [ -n "$COUNT" ]; then
        echo "   Total: $COUNT tips"
    else
        # Tentar contar IDs diretamente
        TIP_COUNT=$(echo "$RESPONSE" | grep -o '"id":"[^"]*' | wc -l | tr -d ' ')
        if [ -n "$TIP_COUNT" ] && [ "$TIP_COUNT" -gt 0 ]; then
            echo "   $TIP_COUNT tips encontradas"
        else
            echo "   Tips disponíveis"
        fi
    fi
    
    # Pegar primeiro ID de tip
    TIP_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*' | cut -d'"' -f4 | head -1)
    if [ -n "$TIP_ID" ]; then
        TIP_IDS+=("$TIP_ID")
    fi
else
    echo -e "${RED}❌ Falhou${NC}"
    if [ -z "$RESPONSE" ]; then
        echo "   Resposta vazia (endpoint pode não estar respondendo)"
    else
        echo "   Resposta: ${RESPONSE:0:200}"
    fi
fi

# 4.2 Buscar tip específica (requer autenticação)
if [ ${#TIP_IDS[@]} -gt 0 ]; then
    echo -n "4.2 Buscando tip ${TIP_IDS[0]}... "
    RESPONSE=$(make_request "GET" "/tips/${TIP_IDS[0]}" "" true)
    check_response "$RESPONSE" "id"
fi

echo ""
echo "🔍 PASSO 5: Testar Insights"
echo "---------------------------"

# 5.1 Trends week
echo -n "5.1 Trends semana (week)... "
RESPONSE=$(make_request "GET" "/insights/trends?period=week" "" true)
if check_response "$RESPONSE" "mood"; then
    MOOD_TREND=$(echo "$RESPONSE" | grep -o '"trend":"[^"]*' | head -1 | cut -d'"' -f4)
    SLEEP_TREND=$(echo "$RESPONSE" | grep -o '"trend":"[^"]*' | tail -1 | cut -d'"' -f4)
    echo "   Mood trend: $MOOD_TREND, Sleep trend: $SLEEP_TREND"
fi

# 5.2 Streak
echo -n "5.2 Streak... "
RESPONSE=$(make_request "GET" "/insights/streak" "" true)
if check_response "$RESPONSE" "currentStreak"; then
    STREAK=$(echo "$RESPONSE" | grep -o '"currentStreak":[0-9]*' | cut -d':' -f2)
    echo "   Current streak: $STREAK dias"
fi

# 5.3 Recommended tips
echo -n "5.3 Recommended tips... "
RESPONSE=$(make_request "GET" "/insights/recommended-tips" "" true)
if check_response "$RESPONSE" "id"; then
    COUNT=$(echo "$RESPONSE" | grep -o '"id":"[^"]*' | wc -l)
    echo "   $COUNT tips recomendadas"
    
    # Verificar consistência com trends
    CATEGORY=$(echo "$RESPONSE" | grep -o '"category":"[^"]*' | head -1 | cut -d'"' -f4)
    echo "   Primeira categoria: $CATEGORY"
fi

echo ""
echo "📊 PASSO 6: Testar Relatórios"
echo "-----------------------------"

# 6.1 Weekly report
WEEK_START=$(date -u -v-7d +"%Y-%m-%d" 2>/dev/null || date -u -d "7 days ago" +"%Y-%m-%d" 2>/dev/null || echo "2024-11-07")
echo -n "6.1 Weekly report (weekStart=$WEEK_START)... "
RESPONSE=$(make_request "GET" "/reports/weekly?weekStart=$WEEK_START" "" true)
check_response "$RESPONSE" "averages"

# 6.2 Monthly report
YEAR=$(date +%Y)
MONTH=$(date +%m)
echo -n "6.2 Monthly report (year=$YEAR, month=$MONTH)... "
RESPONSE=$(make_request "GET" "/reports/monthly?year=$YEAR&month=$MONTH" "" true)
check_response "$RESPONSE" "averages"

echo ""
echo "👤 PASSO 7: Testar Perfil"
echo "-------------------------"

# 7.1 Atualizar perfil
echo -n "7.1 Atualizando perfil... "
UPDATE_PROFILE="{\"name\":\"$NAME Atualizado\",\"email\":\"$EMAIL\"}"
RESPONSE=$(make_request "PUT" "/auth/profile" "$UPDATE_PROFILE" true)
check_response "$RESPONSE" "name"

# 7.2 Atualizar senha
echo -n "7.2 Atualizando senha... "
NEW_PASSWORD="senha123nova"
UPDATE_PASSWORD="{\"currentPassword\":\"$PASSWORD\",\"newPassword\":\"$NEW_PASSWORD\"}"
RESPONSE=$(make_request "PUT" "/auth/password" "$UPDATE_PASSWORD" true)
if check_response "$RESPONSE" "success"; then
    # Fazer login com nova senha
    echo -n "   7.2.1 Login com nova senha... "
    LOGIN_RESPONSE=$(make_request "POST" "/auth/login" "{\"email\":\"$EMAIL\",\"password\":\"$NEW_PASSWORD\"}" false)
    if check_response "$LOGIN_RESPONSE" "token"; then
        TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
        echo -e "${GREEN}✅ Senha atualizada com sucesso${NC}"
    fi
fi

echo ""
echo "🏥 PASSO 8: Health Check"
echo "------------------------"
echo -n "8.1 Health check... "
RESPONSE=$(curl -s "http://localhost:8080/health")
if echo "$RESPONSE" | grep -q "Healthy"; then
    echo -e "${GREEN}✅ API está saudável${NC}"
else
    echo -e "${RED}❌ API não está respondendo${NC}"
fi

echo ""
echo "=============================================="
echo -e "${GREEN}✅ Testes completos finalizados!${NC}"
echo ""
echo "📊 Resumo:"
echo "   - Usuário: $EMAIL"
echo "   - Check-ins criados: ${#CHECKIN_IDS[@]}"
echo "   - Tips disponíveis: ${#TIP_IDS[@]}"
echo ""
echo "💡 Para testar manualmente, use o Swagger:"
echo "   http://localhost:8080/swagger"
echo ""

