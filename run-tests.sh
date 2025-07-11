#!/bin/bash

# Social Media Messaging API - Test Runner Script
# Bu script tüm testleri çalıştırır ve coverage raporu oluşturur

set -e

echo "🚀 Social Media Messaging API - Test Suite Çalıştırılıyor..."
echo "================================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test projects
UNIT_TESTS="tests/SocialMediaMessaging.UnitTests"
INTEGRATION_TESTS="tests/SocialMediaMessaging.IntegrationTests"
PERFORMANCE_TESTS="tests/SocialMediaMessaging.PerformanceTests"

# Coverage output directory
COVERAGE_DIR="TestResults/Coverage"
COVERAGE_REPORT_DIR="TestResults/CoverageReport"

# Create directories
mkdir -p $COVERAGE_DIR
mkdir -p $COVERAGE_REPORT_DIR

echo -e "${BLUE}📋 Test projelerini restore ediliyor...${NC}"
dotnet restore

echo -e "${BLUE}🔨 Projeyi build ediliyor...${NC}"
dotnet build --no-restore --configuration Release

echo ""
echo -e "${YELLOW}🧪 Unit Testler Çalıştırılıyor...${NC}"
echo "----------------------------------------"
dotnet test $UNIT_TESTS \
    --no-build \
    --configuration Release \
    --logger "trx;LogFileName=UnitTests.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory $COVERAGE_DIR \
    --verbosity normal

echo ""
echo -e "${YELLOW}🔗 Integration Testler Çalıştırılıyor...${NC}"
echo "----------------------------------------"
dotnet test $INTEGRATION_TESTS \
    --no-build \
    --configuration Release \
    --logger "trx;LogFileName=IntegrationTests.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory $COVERAGE_DIR \
    --verbosity normal

echo ""
echo -e "${YELLOW}⚡ Performance Testler Çalıştırılıyor...${NC}"
echo "----------------------------------------"
dotnet test $PERFORMANCE_TESTS \
    --no-build \
    --configuration Release \
    --logger "trx;LogFileName=PerformanceTests.trx" \
    --results-directory $COVERAGE_DIR \
    --verbosity normal

echo ""
echo -e "${BLUE}📊 Coverage Raporu Oluşturuluyor...${NC}"
echo "----------------------------------------"

# Install reportgenerator if not exists
if ! command -v reportgenerator &> /dev/null; then
    echo "ReportGenerator kuruluyor..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate coverage report
reportgenerator \
    -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
    -targetdir:"$COVERAGE_REPORT_DIR" \
    -reporttypes:"Html;Cobertura;JsonSummary" \
    -verbosity:Info

echo ""
echo -e "${GREEN}✅ Tüm testler başarıyla tamamlandı!${NC}"
echo ""

# Read coverage summary if exists
if [ -f "$COVERAGE_REPORT_DIR/Summary.json" ]; then
    echo -e "${BLUE}📈 Coverage Özeti:${NC}"
    echo "==================="
    
    # Extract coverage percentage (requires jq)
    if command -v jq &> /dev/null; then
        LINE_COVERAGE=$(jq -r '.summary.linecoverage' "$COVERAGE_REPORT_DIR/Summary.json")
        BRANCH_COVERAGE=$(jq -r '.summary.branchcoverage' "$COVERAGE_REPORT_DIR/Summary.json")
        
        echo -e "Line Coverage: ${GREEN}${LINE_COVERAGE}%${NC}"
        echo -e "Branch Coverage: ${GREEN}${BRANCH_COVERAGE}%${NC}"
        
        # Check if coverage meets threshold (90%)
        LINE_PERCENT=$(echo $LINE_COVERAGE | cut -d'.' -f1)
        if [ "$LINE_PERCENT" -ge 90 ]; then
            echo -e "${GREEN}🎯 Coverage hedefi (%90+) başarıyla karşılandı!${NC}"
        else
            echo -e "${YELLOW}⚠️  Coverage hedefi (%90) karşılanmadı. Daha fazla test gerekli.${NC}"
        fi
    fi
fi

echo ""
echo -e "${BLUE}📁 Test Sonuçları:${NC}"
echo "=================="
echo "• TRX Raporları: $COVERAGE_DIR/"
echo "• Coverage Raporu: $COVERAGE_REPORT_DIR/index.html"
echo ""

# Open coverage report in browser (optional)
read -p "Coverage raporunu tarayıcıda açmak ister misiniz? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if command -v open &> /dev/null; then
        open "$COVERAGE_REPORT_DIR/index.html"
    elif command -v xdg-open &> /dev/null; then
        xdg-open "$COVERAGE_REPORT_DIR/index.html"
    else
        echo "Tarayıcı açılamadı. Manuel olarak şu dosyayı açın: $COVERAGE_REPORT_DIR/index.html"
    fi
fi

echo -e "${GREEN}🎉 Test suite tamamlandı!${NC}" 