@echo off
setlocal

set NAMESPACE=dotnet-dev
set OVERLAY=k8s\overlays\dev
set IMAGE=xp-newrelic-dotnet:latest

echo ========================================
echo  1. Stopping port-forward
echo ========================================
for /f "tokens=5" %%p in ('netstat -ano ^| findstr ":5000.*LISTENING"') do taskkill /PID %%p /F >nul 2>&1
echo    Done.

echo.
echo ========================================
echo  2. Deleting Kubernetes resources
echo ========================================
kubectl delete -k %OVERLAY% --ignore-not-found || goto :error

echo.
echo ========================================
echo  3. Deleting namespace
echo ========================================
kubectl delete namespace %NAMESPACE% --ignore-not-found

echo.
echo ========================================
echo  4. Removing Docker image
echo ========================================
docker rmi %IMAGE% >nul 2>&1
echo    Done.

echo.
echo ========================================
echo  Teardown complete.
echo ========================================
goto :eof

:error
echo.
echo ========================================
echo  ERROR: step failed, exiting.
echo ========================================
exit /b 1
