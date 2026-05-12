@echo off
setlocal

set IMAGE=xp-newrelic-springboot:latest
set NAMESPACE=springboot-dev
set OVERLAY=k8s\overlays\dev

echo ========================================
echo  1. Building Docker image
echo ========================================
docker build -t %IMAGE% . || goto :error

echo.
echo ========================================
echo  2. Applying Instrumentation CR (operator namespace)
echo ========================================
kubectl apply -f k8s\base\operator-instrumentation.yml || goto :error

echo.
echo ========================================
echo  3. Applying Kustomize overlays (%OVERLAY%)
echo ========================================
kubectl apply -k %OVERLAY% || goto :error

echo.
echo ========================================
echo  4. Waiting for deployment rollout
echo ========================================
kubectl rollout status deployment/springboot -n %NAMESPACE% --timeout=120s || goto :error

echo.
echo ========================================
echo  5. Port-forwarding to localhost:8080
echo ========================================
start /B kubectl port-forward svc/springboot 8080:80 -n %NAMESPACE%
timeout /t 3 /nobreak >nul

echo.
echo ========================================
echo  6. Testing API
echo ========================================

echo --- Health check ---
curl -s http://localhost:8080/actuator/health
echo.

echo.
echo --- Create item ---
curl -s -X POST http://localhost:8080/api/items ^
  -H "Content-Type: application/json" ^
  -d "{\"name\":\"test-item\",\"description\":\"created by deploy.bat\"}"
echo.

echo.
echo --- List items ---
curl -s http://localhost:8080/api/items
echo.

echo.
echo ========================================
echo  Done! Port-forward is running in the
echo  background. Press Ctrl+C to stop.
echo ========================================
goto :eof

:error
echo.
echo ========================================
echo  ERROR: step failed, exiting.
echo ========================================
exit /b 1
