@echo off
rem Génère le wiki puis ouvre la version joueur (publique) dans le navigateur par défaut.
python "%~dp0build.py" || (echo Python 3 est nécessaire. & pause & exit /b 1)
start "" "%~dp0public\index.html"
